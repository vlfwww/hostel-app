using System.Text;
using HotelApi.Auth;
using HotelApi.Common;
using HotelApi.Data;
using HotelApi.Endpoints;
using HotelApi.Models;
using HotelApi.Options;
using HotelApi.Repositories;
using HotelApi.Services;
using HotelApi.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 1024 * 1024;
});

var signingKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(signingKey) || signingKey.Length < 32)
{
    throw new InvalidOperationException(
        "Задайте секрет Jwt:Key длиной от 32 символов командой " +
        "dotnet user-secrets set \"Jwt:Key\" \"<секрет>\" --project HotelApi " +
        "или переменной окружения Jwt__Key. Секрет нельзя хранить в appsettings.json и коммитить в репозиторий.");
}

var connectionString = BuildSqliteConnectionString(
    builder.Configuration.GetConnectionString("HotelDb")
        ?? throw new InvalidOperationException("Не задана строка подключения ConnectionStrings:HotelDb."),
    builder.Environment.ContentRootPath);

builder.Services.AddDbContext<HotelDbContext>(options => options.UseSqlite(connectionString));

builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .Validate(options =>
            !string.IsNullOrWhiteSpace(options.Issuer) &&
            !string.IsNullOrWhiteSpace(options.Audience) &&
            options.ExpireMinutes > 0,
        "Секция Jwt заполнена некорректно.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.Key) && options.Key.Length >= 32,
        "Jwt:Key должен быть задан через user-secrets или переменную Jwt__Key и содержать не меньше 32 символов.")
    .ValidateOnStart();

builder.Services.AddOptions<PaginationOptions>()
    .Bind(builder.Configuration.GetSection(PaginationOptions.SectionName))
    .Validate(options => options.DefaultPageSize > 0 && options.MaxPageSize >= options.DefaultPageSize,
        "Секция Pagination заполнена некорректно.")
    .ValidateOnStart();

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

// Transient — дешёвый объект без состояния, новый экземпляр на каждую инъекцию.
builder.Services.AddTransient<IClock, SystemClock>();
// Singleton — общий на всё приложение: нет данных конкретного запроса.
builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddSingleton<ITokenService, JwtTokenService>();
// Scoped — один экземпляр на HTTP-запрос, вместе с DbContext.
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();
builder.Services.AddScoped<IRoomRepository, RoomRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IGuestRepository, GuestRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IGuestService, GuestService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddHttpLogging(logging =>
{
    logging.LoggingFields = HttpLoggingFields.RequestMethod
        | HttpLoggingFields.RequestPath
        | HttpLoggingFields.ResponseStatusCode
        | HttpLoggingFields.Duration;
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("HotelCors", policy =>
        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ClockSkew = TimeSpan.FromMinutes(1),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            NameClaimType = System.Security.Claims.ClaimTypes.Name
        };
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("HotelApi.Jwt");
                logger.LogWarning("JWT отклонён: {Message}", context.Exception.Message);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole(Roles.Admin));
    options.AddPolicy("GuestOrAdmin", policy => policy.RequireRole(Roles.Admin, Roles.Guest));
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Hotel API",
        Version = "v1",
        Description = "REST API гостиницы: номера и бронирования. Роли Admin и Guest. Авторизация — JWT Bearer."
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Вставьте JWT из POST /api/auth/login. Префикс Bearer добавляется автоматически."
    });
    options.OperationFilter<AuthorizeOperationFilter>();
});

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
    var passwords = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    await DbInitializer.SeedAsync(db, passwords, app.Logger);
}

app.Logger.LogInformation(
    "Среда {Environment}. Сервер Kestrel. Документация: /swagger",
    app.Environment.EnvironmentName);

// Порядок middleware: каждый следующий видит уже обработанный запрос,
// а ответ проходит обратно в обратном порядке.
app.UseHttpLogging();
app.UseExceptionHandler();
app.UseStatusCodePages(async statusContext =>
{
    var response = statusContext.HttpContext.Response;
    if (response.StatusCode < StatusCodes.Status400BadRequest ||
        response.HasStarted ||
        !string.IsNullOrEmpty(response.ContentType))
    {
        return;
    }

    var problem = new ProblemDetails
    {
        Status = response.StatusCode,
        Title = response.StatusCode switch
        {
            StatusCodes.Status401Unauthorized => "Требуется аутентификация",
            StatusCodes.Status403Forbidden => "Доступ запрещён",
            StatusCodes.Status404NotFound => "Не найдено",
            _ => "Ошибка запроса"
        },
        Detail = response.StatusCode switch
        {
            StatusCodes.Status401Unauthorized => "Передайте заголовок Authorization: Bearer {token}.",
            StatusCodes.Status403Forbidden => "Недостаточно прав для этой операции.",
            StatusCodes.Status404NotFound => "Ресурс не найден.",
            _ => "Запрос отклонён."
        },
        Instance = statusContext.HttpContext.Request.Path
    };
    problem.Extensions["traceId"] = statusContext.HttpContext.TraceIdentifier;
    response.ContentType = "application/problem+json";
    await response.WriteAsJsonAsync(problem);
});

var urls = builder.Configuration["ASPNETCORE_URLS"] ?? string.Empty;
if (urls.Contains("https", StringComparison.OrdinalIgnoreCase))
{
    app.UseHttpsRedirection();
}

app.UseCors("HotelCors");
app.UseAuthentication();
app.UseAuthorization();
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Hotel API v1");
    options.DocumentTitle = "Hotel API";
    options.DisplayRequestDuration();
});

app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapAuthEndpoints();
app.MapRoomEndpoints();
app.MapBookingEndpoints();
app.MapControllers();

app.Run();

static string BuildSqliteConnectionString(string rawConnection, string contentRoot)
{
    var sqlite = new SqliteConnectionStringBuilder(rawConnection);
    if (!Path.IsPathRooted(sqlite.DataSource))
    {
        sqlite.DataSource = Path.Combine(contentRoot, sqlite.DataSource);
    }

    return sqlite.ToString();
}

public partial class Program;

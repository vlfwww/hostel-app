using HotelApi.Auth;
using HotelApi.Dtos;
using HotelApi.Services;

namespace HotelApi.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Авторизация")
            .AddEndpointFilter<Common.ValidationFilter>();

        group.MapPost("/register", Register)
            .WithName("Register")
            .WithSummary("Регистрация гостя")
            .AllowAnonymous()
            .Produces<LoginResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/login", Login)
            .WithName("Login")
            .WithSummary("Вход и выдача JWT")
            .AllowAnonymous()
            .Produces<LoginResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/me", Me)
            .WithName("Me")
            .WithSummary("Текущий пользователь по JWT")
            .RequireAuthorization()
            .Produces<UserInfoResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return group;
    }

    private static async Task<IResult> Register(RegisterRequest request, IAuthService auth, CancellationToken cancellationToken)
    {
        var response = await auth.RegisterAsync(request, cancellationToken);
        return TypedResults.Created("/api/auth/me", response);
    }

    private static async Task<IResult> Login(LoginRequest request, IAuthService auth, CancellationToken cancellationToken)
    {
        var response = await auth.LoginAsync(request, cancellationToken);
        return TypedResults.Ok(response);
    }

    private static IResult Me(ICurrentUser currentUser)
    {
        return TypedResults.Ok(new UserInfoResponse(
            currentUser.UserId,
            currentUser.UserName,
            currentUser.Role,
            currentUser.GuestId));
    }
}

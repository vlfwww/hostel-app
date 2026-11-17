using System.Globalization;
using HotelBlazor.Components;
using HotelBlazor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;

var culture = CultureInfo.GetCultureInfo("ru-RU");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.Configure<CircuitOptions>(options =>
{
    options.DetailedErrors = builder.Environment.IsDevelopment();
});

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, AllowBlazorToDecide>();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthSession>();
builder.Services.AddScoped<HotelAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<HotelAuthStateProvider>());
builder.Services.AddScoped<UiMessenger>();

builder.Services.AddHttpClient("hotel", (sp, client) =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var raw = configuration["Api:BaseUrl"] ?? "http://localhost:5080";
    if (!raw.EndsWith('/'))
    {
        raw += "/";
    }

    client.BaseAddress = new Uri(raw);
    client.Timeout = TimeSpan.FromSeconds(20);
});
builder.Services.AddScoped<HotelApiClient>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace HotelBlazor.Services;

// [Authorize] на странице попадает и в endpoint.
// Токен лежит в браузере, а не в cookie, поэтому HTTP-запрос всегда анонимный.
// Решение о доступе принимает AuthorizeRouteView уже внутри circuit.
public sealed class AllowBlazorToDecide : IAuthorizationMiddlewareResultHandler
{
    public Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult) =>
        next(context);
}

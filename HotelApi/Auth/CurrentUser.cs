using System.Security.Claims;

namespace HotelApi.Auth;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    int UserId { get; }

    string UserName { get; }

    string Role { get; }

    bool IsAdmin { get; }

    int? GuestId { get; }
}

public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal Principal =>
        _httpContextAccessor.HttpContext?.User
        ?? throw new InvalidOperationException("HttpContext недоступен.");

    public bool IsAuthenticated => Principal.Identity?.IsAuthenticated == true;

    public int UserId =>
        int.TryParse(Principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : throw new InvalidOperationException("В токене нет идентификатора пользователя.");

    public string UserName => Principal.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

    public string Role => Principal.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

    public bool IsAdmin => Role == Models.Roles.Admin;

    public int? GuestId =>
        int.TryParse(Principal.FindFirstValue("guest_id"), out var id) ? id : null;
}

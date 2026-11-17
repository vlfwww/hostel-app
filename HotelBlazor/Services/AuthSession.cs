using System.Security.Claims;
using HotelBlazor.Models;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace HotelBlazor.Services;

public sealed class StoredAuth
{
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public int UserId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public int? GuestId { get; set; }
}

public sealed class AuthSession
{
    public const string StorageKey = "aurelia-auth";

    private readonly ProtectedSessionStorage _storage;

    public AuthSession(ProtectedSessionStorage storage)
    {
        _storage = storage;
    }

    public string? Token { get; private set; }

    public DateTime? ExpiresAt { get; private set; }

    public int UserId { get; private set; }

    public string? UserName { get; private set; }

    public string? Role { get; private set; }

    public int? GuestId { get; private set; }

    public bool Restored { get; private set; }

    public bool IsAuthenticated =>
        !string.IsNullOrWhiteSpace(Token) &&
        ExpiresAt is DateTime expires &&
        expires > DateTime.UtcNow;

    public bool IsAdmin => string.Equals(Role, "Admin", StringComparison.Ordinal);

    public event Action? Changed;

    public ClaimsPrincipal Principal
    {
        get
        {
            if (!IsAuthenticated || UserName is null || Role is null)
            {
                return new ClaimsPrincipal(new ClaimsIdentity());
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, UserId.ToString()),
                new(ClaimTypes.Name, UserName),
                new(ClaimTypes.Role, Role)
            };
            if (GuestId is int guestId)
            {
                claims.Add(new Claim("guest_id", guestId.ToString()));
            }

            return new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));
        }
    }

    public void Hold(LoginResult login)
    {
        Token = login.Token;
        ExpiresAt = AsUtc(login.ExpiresAt);
        UserName = login.UserName;
        Role = login.Role;
        Changed?.Invoke();
    }

    public async Task CommitAsync(UserProfile profile)
    {
        UserId = profile.UserId;
        UserName = profile.UserName;
        Role = profile.Role;
        GuestId = profile.GuestId;
        Restored = true;
        await SaveAsync();
        Changed?.Invoke();
    }

    public async Task<bool> RestoreAsync()
    {
        if (Restored)
        {
            return IsAuthenticated;
        }

        try
        {
            var stored = await _storage.GetAsync<StoredAuth>(StorageKey);
            Restored = true;
            if (stored.Success && stored.Value is { } value && AsUtc(value.ExpiresAt) > DateTime.UtcNow)
            {
                Token = value.Token;
                ExpiresAt = AsUtc(value.ExpiresAt);
                UserId = value.UserId;
                UserName = value.UserName;
                Role = value.Role;
                GuestId = value.GuestId;
            }
        }
        catch (InvalidOperationException)
        {
            return false;
        }

        Changed?.Invoke();
        return IsAuthenticated;
    }

    public async Task ClearAsync()
    {
        Token = null;
        ExpiresAt = null;
        UserId = 0;
        UserName = null;
        Role = null;
        GuestId = null;
        Restored = true;
        try
        {
            await _storage.DeleteAsync(StorageKey);
        }
        catch (InvalidOperationException)
        {
            // Сессия в памяти уже сброшена. Хранилище браузера очистится при следующем заходе.
        }

        Changed?.Invoke();
    }

    private async Task SaveAsync()
    {
        if (Token is null || ExpiresAt is null || UserName is null || Role is null)
        {
            return;
        }

        await _storage.SetAsync(StorageKey, new StoredAuth
        {
            Token = Token,
            ExpiresAt = ExpiresAt.Value,
            UserId = UserId,
            UserName = UserName,
            Role = Role,
            GuestId = GuestId
        });
    }

    private static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}

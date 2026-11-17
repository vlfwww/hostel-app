using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HotelApi.Models;
using HotelApi.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HotelApi.Auth;

public readonly record struct AccessToken(string Token, DateTime ExpiresAt);

public interface ITokenService
{
    AccessToken Create(AppUser user);
}

public sealed class JwtTokenService : ITokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public AccessToken Create(AppUser user)
    {
        var expires = DateTime.UtcNow.AddMinutes(_options.ExpireMinutes);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Role, user.Role)
        };

        if (user.GuestId is int guestId)
        {
            claims.Add(new Claim("guest_id", guestId.ToString()));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return new AccessToken(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}

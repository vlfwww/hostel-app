using HotelApi.Auth;
using HotelApi.Common;
using HotelApi.Data;
using HotelApi.Dtos;
using HotelApi.Models;
using HotelApi.Repositories;

namespace HotelApi.Services;

public interface IAuthService
{
    Task<LoginResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);

    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
}

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IGuestRepository _guests;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwords;
    private readonly ITokenService _tokens;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository users,
        IGuestRepository guests,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwords,
        ITokenService tokens,
        ILogger<AuthService> logger)
    {
        _users = users;
        _guests = guests;
        _unitOfWork = unitOfWork;
        _passwords = passwords;
        _tokens = tokens;
        _logger = logger;
    }

    public Task<LoginResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        return _unitOfWork.InTransactionAsync(async () =>
        {
            var userName = request.UserName.Trim();
            if (await _users.UserNameExistsAsync(userName, cancellationToken))
            {
                throw ApiException.Conflict("Пользователь с таким именем уже зарегистрирован.");
            }

            var passport = request.PassportNumber.Trim();
            if (await _guests.PassportExistsAsync(passport, cancellationToken))
            {
                throw ApiException.Conflict("Гость с таким номером паспорта уже есть.");
            }

            var guest = new Guest
            {
                FullName = request.FullName.Trim(),
                Phone = Clean(request.Phone),
                Email = Clean(request.Email),
                PassportNumber = passport
            };
            await _guests.AddAsync(guest, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var user = new AppUser
            {
                UserName = userName,
                PasswordHash = _passwords.Hash(request.Password),
                Role = Roles.Guest,
                GuestId = guest.Id
            };
            await _users.AddAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Зарегистрирован гость {UserName} (гость {GuestId})", user.UserName, guest.Id);
            return ToResponse(_tokens.Create(user), user);
        }, cancellationToken);
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await _users.GetByUserNameAsync(request.UserName, cancellationToken);
        if (user is null || !_passwords.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Неудачный вход: {UserName}", request.UserName.Trim());
            throw ApiException.BadRequest("Неверное имя пользователя или пароль.");
        }

        _logger.LogInformation("Пользователь {UserName} вошёл с ролью {Role}", user.UserName, user.Role);
        return ToResponse(_tokens.Create(user), user);
    }

    private static LoginResponse ToResponse(AccessToken accessToken, AppUser user) =>
        new(accessToken.Token, accessToken.ExpiresAt, user.UserName, user.Role);

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

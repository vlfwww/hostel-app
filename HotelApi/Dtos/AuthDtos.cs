using System.ComponentModel.DataAnnotations;

namespace HotelApi.Dtos;

public class RegisterRequest
{
    [Required(ErrorMessage = "Укажите имя пользователя.")]
    [MinLength(3, ErrorMessage = "Имя пользователя — не короче 3 символов.")]
    [MaxLength(50)]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите пароль.")]
    [MinLength(6, ErrorMessage = "Пароль — не короче 6 символов.")]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите ФИО.")]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "Некорректный адрес электронной почты.")]
    [MaxLength(100)]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Укажите номер паспорта.")]
    [MaxLength(20)]
    public string PassportNumber { get; set; } = string.Empty;
}

public class LoginRequest
{
    [Required(ErrorMessage = "Укажите имя пользователя.")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите пароль.")]
    public string Password { get; set; } = string.Empty;
}

public record LoginResponse(string Token, DateTime ExpiresAt, string UserName, string Role);

public record UserInfoResponse(int UserId, string UserName, string Role, int? GuestId);

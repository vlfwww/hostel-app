using System.ComponentModel.DataAnnotations;

namespace HotelApi.Dtos;

public class RegisterRequest
{
    [Required(ErrorMessage = "Укажите имя пользователя.")]
    [MinLength(3, ErrorMessage = "Имя пользователя — не короче 3 символов.")]
    [MaxLength(50, ErrorMessage = "Имя пользователя — не длиннее 50 символов.")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите пароль.")]
    [MinLength(6, ErrorMessage = "Пароль — не короче 6 символов.")]
    [MaxLength(100, ErrorMessage = "Пароль — не длиннее 100 символов.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите ФИО.")]
    [MaxLength(100, ErrorMessage = "ФИО — не длиннее 100 символов.")]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(30, ErrorMessage = "Телефон — не длиннее 30 символов.")]
    [RegularExpression(@"^$|^\+?[0-9][0-9\s\-()]{5,28}$", ErrorMessage = "Телефон: цифры, пробелы, скобки и дефис, можно начать с +.")]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "Некорректный адрес электронной почты.")]
    [MaxLength(100, ErrorMessage = "Почта — не длиннее 100 символов.")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Укажите номер паспорта.")]
    [MaxLength(20, ErrorMessage = "Номер паспорта — не длиннее 20 символов.")]
    public string PassportNumber { get; set; } = string.Empty;
}

public class LoginRequest
{
    [Required(ErrorMessage = "Укажите имя пользователя.")]
    [MaxLength(50, ErrorMessage = "Имя пользователя — не длиннее 50 символов.")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите пароль.")]
    [MaxLength(100, ErrorMessage = "Пароль — не длиннее 100 символов.")]
    public string Password { get; set; } = string.Empty;
}

public record LoginResponse(string Token, DateTime ExpiresAt, string UserName, string Role);

public record UserInfoResponse(int UserId, string UserName, string Role, int? GuestId);

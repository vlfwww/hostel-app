using System.ComponentModel.DataAnnotations;

namespace HotelApi.Dtos;

public class CreateGuestRequest
{
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

public record GuestResponse(
    int Id,
    string FullName,
    string? Phone,
    string? Email,
    string PassportNumber);

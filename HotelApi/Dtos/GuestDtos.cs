using System.ComponentModel.DataAnnotations;

namespace HotelApi.Dtos;

public class CreateGuestRequest
{
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

public record GuestResponse(
    int Id,
    string FullName,
    string? Phone,
    string? Email,
    string PassportNumber);

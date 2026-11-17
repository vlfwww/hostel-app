using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace HotelApi.Dtos;

public class BookingQuery
{
    [FromQuery(Name = "status")]
    public string? Status { get; set; }

    [FromQuery(Name = "roomId")]
    public int? RoomId { get; set; }

    [FromQuery(Name = "guestId")]
    public int? GuestId { get; set; }

    [FromQuery(Name = "sort")]
    public string? Sort { get; set; }

    [FromQuery(Name = "page")]
    public int? Page { get; set; }

    [FromQuery(Name = "pageSize")]
    public int? PageSize { get; set; }
}

public class CreateBookingRequest : IValidatableObject
{
    [Required(ErrorMessage = "Укажите номер.")]
    public int? RoomId { get; set; }

    [Required(ErrorMessage = "Укажите дату заезда.")]
    public DateOnly? CheckIn { get; set; }

    [Required(ErrorMessage = "Укажите дату выезда.")]
    public DateOnly? CheckOut { get; set; }

    public int? GuestId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CheckIn is DateOnly checkIn && CheckOut is DateOnly checkOut && checkOut <= checkIn)
        {
            yield return new ValidationResult(
                "Дата выезда должна быть позже даты заезда.",
                [nameof(CheckOut)]);
        }
    }
}

public class UpdateBookingStatusRequest
{
    [Required(ErrorMessage = "Укажите статус.")]
    [RegularExpression("^(Active|Cancelled|Completed)$",
        ErrorMessage = "Статус должен быть Active, Cancelled или Completed.")]
    public string Status { get; set; } = string.Empty;
}

public record BookingResponse(
    int Id,
    int GuestId,
    string GuestName,
    int RoomId,
    string RoomNumber,
    string RoomType,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int Nights,
    string Status,
    decimal TotalPrice,
    DateTime CreatedAt);

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace HotelApi.Dtos;

public class RoomQuery
{
    [FromQuery(Name = "type")]
    public string? Type { get; set; }

    [FromQuery(Name = "minPrice")]
    public decimal? MinPrice { get; set; }

    [FromQuery(Name = "maxPrice")]
    public decimal? MaxPrice { get; set; }

    [FromQuery(Name = "available")]
    public bool? Available { get; set; }

    [FromQuery(Name = "freeFrom")]
    public DateOnly? FreeFrom { get; set; }

    [FromQuery(Name = "freeTo")]
    public DateOnly? FreeTo { get; set; }

    [FromQuery(Name = "sort")]
    public string? Sort { get; set; }

    [FromQuery(Name = "page")]
    public int? Page { get; set; }

    [FromQuery(Name = "pageSize")]
    public int? PageSize { get; set; }
}

public class UpsertRoomRequest
{
    [Required(ErrorMessage = "Укажите номер комнаты.")]
    [MaxLength(10, ErrorMessage = "Номер комнаты — не длиннее 10 символов.")]
    public string Number { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите тип номера.")]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty;

    [Range(0.01, 1_000_000, ErrorMessage = "Цена за ночь должна быть больше нуля.")]
    public decimal PricePerNight { get; set; }

    [Range(1, 10, ErrorMessage = "Вместимость — от 1 до 10 гостей.")]
    public int Capacity { get; set; }

    public bool IsAvailable { get; set; } = true;
}

public record RoomResponse(
    int Id,
    string Number,
    string Type,
    decimal PricePerNight,
    int Capacity,
    bool IsAvailable);

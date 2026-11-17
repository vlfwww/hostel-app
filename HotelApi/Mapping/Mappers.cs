using HotelApi.Dtos;
using HotelApi.Models;

namespace HotelApi.Mapping;

public static class RoomMapper
{
    public static RoomResponse ToResponse(Room room) =>
        new(room.Id, room.Number, room.Type, room.PricePerNight, room.Capacity, room.IsAvailable);
}

public static class GuestMapper
{
    public static GuestResponse ToResponse(Guest guest) =>
        new(guest.Id, guest.FullName, guest.Phone, guest.Email, guest.PassportNumber);
}

public static class BookingMapper
{
    public static BookingResponse ToResponse(Booking booking)
    {
        var nights = booking.CheckOut.DayNumber - booking.CheckIn.DayNumber;
        return new BookingResponse(
            booking.Id,
            booking.GuestId,
            booking.Guest?.FullName ?? string.Empty,
            booking.RoomId,
            booking.Room?.Number ?? string.Empty,
            booking.Room?.Type ?? string.Empty,
            booking.CheckIn,
            booking.CheckOut,
            nights,
            booking.Status,
            booking.TotalPrice,
            booking.CreatedAt);
    }
}

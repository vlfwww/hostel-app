using HotelApi.Dtos;
using HotelApi.Models;

namespace HotelApi.Repositories;

public enum BookingSort
{
    CheckIn,
    CheckInDesc,
    Price,
    PriceDesc
}

public sealed record BookingSearch(
    string? Status,
    int? RoomId,
    int? GuestId,
    BookingSort Sort,
    int Page,
    int PageSize);

public interface IBookingRepository
{
    Task<PagedResult<Booking>> SearchAsync(BookingSearch search, CancellationToken cancellationToken);

    Task<Booking?> GetByIdAsync(int id, bool tracking, CancellationToken cancellationToken);

    Task<bool> HasOverlapAsync(int roomId, DateOnly checkIn, DateOnly checkOut, int? excludeBookingId, CancellationToken cancellationToken);

    Task<bool> AnyForRoomAsync(int roomId, CancellationToken cancellationToken);

    Task AddAsync(Booking booking, CancellationToken cancellationToken);

    void Remove(Booking booking);
}

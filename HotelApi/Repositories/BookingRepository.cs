using HotelApi.Data;
using HotelApi.Dtos;
using HotelApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelApi.Repositories;

public sealed class BookingRepository : IBookingRepository
{
    private readonly HotelDbContext _db;

    public BookingRepository(HotelDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<Booking>> SearchAsync(BookingSearch search, CancellationToken cancellationToken)
    {
        var query = _db.Bookings
            .AsNoTracking()
            .Include(booking => booking.Guest)
            .Include(booking => booking.Room)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search.Status))
        {
            query = query.Where(booking => booking.Status == search.Status);
        }

        if (search.RoomId is int roomId)
        {
            query = query.Where(booking => booking.RoomId == roomId);
        }

        if (search.GuestId is int guestId)
        {
            query = query.Where(booking => booking.GuestId == guestId);
        }

        query = search.Sort switch
        {
            BookingSort.CheckInDesc => query.OrderByDescending(booking => booking.CheckIn),
            BookingSort.Price => query.OrderBy(booking => (double)booking.TotalPrice),
            BookingSort.PriceDesc => query.OrderByDescending(booking => (double)booking.TotalPrice),
            _ => query.OrderBy(booking => booking.CheckIn)
        };

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((search.Page - 1) * search.PageSize)
            .Take(search.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Booking>(items, search.Page, search.PageSize, total);
    }

    public Task<Booking?> GetByIdAsync(int id, bool tracking, CancellationToken cancellationToken)
    {
        IQueryable<Booking> query = _db.Bookings
            .Include(booking => booking.Guest)
            .Include(booking => booking.Room);

        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return query.FirstOrDefaultAsync(booking => booking.Id == id, cancellationToken);
    }

    public Task<bool> HasOverlapAsync(
        int roomId,
        DateOnly checkIn,
        DateOnly checkOut,
        int? excludeBookingId,
        CancellationToken cancellationToken)
    {
        return _db.Bookings.AnyAsync(booking =>
            booking.RoomId == roomId &&
            booking.Status == BookingStatuses.Active &&
            (excludeBookingId == null || booking.Id != excludeBookingId) &&
            booking.CheckIn < checkOut &&
            booking.CheckOut > checkIn,
            cancellationToken);
    }

    public Task<bool> AnyForRoomAsync(int roomId, CancellationToken cancellationToken) =>
        _db.Bookings.AnyAsync(booking => booking.RoomId == roomId, cancellationToken);

    public async Task AddAsync(Booking booking, CancellationToken cancellationToken) =>
        await _db.Bookings.AddAsync(booking, cancellationToken);

    public void Remove(Booking booking) => _db.Bookings.Remove(booking);
}

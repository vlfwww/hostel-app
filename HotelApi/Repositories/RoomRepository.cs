using HotelApi.Data;
using HotelApi.Dtos;
using HotelApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelApi.Repositories;

public sealed class RoomRepository : IRoomRepository
{
    private readonly HotelDbContext _db;

    public RoomRepository(HotelDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<Room>> SearchAsync(RoomSearch search, CancellationToken cancellationToken)
    {
        var query = _db.Rooms.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search.Type))
        {
            var type = search.Type.Trim().ToLower();
            query = query.Where(room => room.Type.ToLower() == type);
        }

        if (search.MinPrice is decimal minPrice)
        {
            query = query.Where(room => room.PricePerNight >= minPrice);
        }

        if (search.MaxPrice is decimal maxPrice)
        {
            query = query.Where(room => room.PricePerNight <= maxPrice);
        }

        if (search.Available is bool available)
        {
            query = query.Where(room => room.IsAvailable == available);
        }

        if (search.FreeFrom is DateOnly from && search.FreeTo is DateOnly to)
        {
            query = query.Where(room =>
                room.IsAvailable &&
                !room.Bookings.Any(booking =>
                    booking.Status == BookingStatuses.Active &&
                    booking.CheckIn < to &&
                    booking.CheckOut > from));
        }

        query = search.Sort switch
        {
            RoomSort.NumberDesc => query.OrderByDescending(room => room.Number),
            // SQLite не сортирует decimal: в SQL уходит CAST(... AS REAL).
            RoomSort.Price => query.OrderBy(room => (double)room.PricePerNight).ThenBy(room => room.Number),
            RoomSort.PriceDesc => query.OrderByDescending(room => (double)room.PricePerNight).ThenBy(room => room.Number),
            _ => query.OrderBy(room => room.Number)
        };

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((search.Page - 1) * search.PageSize)
            .Take(search.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Room>(items, search.Page, search.PageSize, total);
    }

    public Task<Room?> GetByIdAsync(int id, bool tracking, CancellationToken cancellationToken)
    {
        var query = tracking ? _db.Rooms.AsQueryable() : _db.Rooms.AsNoTracking();
        return query.FirstOrDefaultAsync(room => room.Id == id, cancellationToken);
    }

    public Task<bool> NumberExistsAsync(string number, int? excludeId, CancellationToken cancellationToken)
    {
        var normalized = number.Trim().ToLower();
        return _db.Rooms.AnyAsync(
            room => room.Number.ToLower() == normalized && (excludeId == null || room.Id != excludeId),
            cancellationToken);
    }

    public async Task AddAsync(Room room, CancellationToken cancellationToken) =>
        await _db.Rooms.AddAsync(room, cancellationToken);

    public void Remove(Room room) => _db.Rooms.Remove(room);
}

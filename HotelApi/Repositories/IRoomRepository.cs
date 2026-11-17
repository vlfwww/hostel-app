using HotelApi.Dtos;
using HotelApi.Models;

namespace HotelApi.Repositories;

public enum RoomSort
{
    Number,
    NumberDesc,
    Price,
    PriceDesc
}

public sealed record RoomSearch(
    string? Type,
    decimal? MinPrice,
    decimal? MaxPrice,
    bool? Available,
    DateOnly? FreeFrom,
    DateOnly? FreeTo,
    RoomSort Sort,
    int Page,
    int PageSize);

public interface IRoomRepository
{
    Task<PagedResult<Room>> SearchAsync(RoomSearch search, CancellationToken cancellationToken);

    Task<Room?> GetByIdAsync(int id, bool tracking, CancellationToken cancellationToken);

    Task<bool> NumberExistsAsync(string number, int? excludeId, CancellationToken cancellationToken);

    Task AddAsync(Room room, CancellationToken cancellationToken);

    void Remove(Room room);
}

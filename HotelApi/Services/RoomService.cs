using HotelApi.Common;
using HotelApi.Data;
using HotelApi.Dtos;
using HotelApi.Mapping;
using HotelApi.Models;
using HotelApi.Options;
using HotelApi.Repositories;
using Microsoft.Extensions.Options;

namespace HotelApi.Services;

public interface IRoomService
{
    Task<PagedResult<RoomResponse>> SearchAsync(RoomQuery query, CancellationToken cancellationToken);

    Task<RoomResponse> GetAsync(int id, CancellationToken cancellationToken);

    Task<RoomResponse> CreateAsync(UpsertRoomRequest request, CancellationToken cancellationToken);

    Task<RoomResponse> UpdateAsync(int id, UpsertRoomRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(int id, CancellationToken cancellationToken);
}

public sealed class RoomService : IRoomService
{
    private readonly IRoomRepository _rooms;
    private readonly IBookingRepository _bookings;
    private readonly IUnitOfWork _unitOfWork;
    private readonly PaginationOptions _paging;
    private readonly ILogger<RoomService> _logger;

    public RoomService(
        IRoomRepository rooms,
        IBookingRepository bookings,
        IUnitOfWork unitOfWork,
        IOptions<PaginationOptions> paging,
        ILogger<RoomService> logger)
    {
        _rooms = rooms;
        _bookings = bookings;
        _unitOfWork = unitOfWork;
        _paging = paging.Value;
        _logger = logger;
    }

    public async Task<PagedResult<RoomResponse>> SearchAsync(RoomQuery query, CancellationToken cancellationToken)
    {
        if (query.FreeFrom.HasValue != query.FreeTo.HasValue)
        {
            throw ApiException.BadRequest("Укажите обе даты свободного периода: freeFrom и freeTo.");
        }

        if (query.FreeFrom is DateOnly from && query.FreeTo is DateOnly to && to <= from)
        {
            throw ApiException.BadRequest("Дата freeTo должна быть позже freeFrom.");
        }

        if (query.MinPrice is decimal min && query.MaxPrice is decimal max && max < min)
        {
            throw ApiException.BadRequest("maxPrice не может быть меньше minPrice.");
        }

        var (page, pageSize) = Paging.Normalize(query.Page, query.PageSize, _paging.DefaultPageSize, _paging.MaxPageSize);
        var result = await _rooms.SearchAsync(new RoomSearch(
            query.Type,
            query.MinPrice,
            query.MaxPrice,
            query.Available,
            query.FreeFrom,
            query.FreeTo,
            ParseSort(query.Sort),
            page,
            pageSize), cancellationToken);

        _logger.LogDebug("Выдан список номеров: страница {Page}, всего {Total}", page, result.TotalCount);
        return new PagedResult<RoomResponse>(
            result.Items.Select(RoomMapper.ToResponse).ToList(),
            result.Page,
            result.PageSize,
            result.TotalCount);
    }

    public async Task<RoomResponse> GetAsync(int id, CancellationToken cancellationToken)
    {
        var room = await _rooms.GetByIdAsync(id, tracking: false, cancellationToken)
            ?? throw ApiException.NotFound($"Номер {id} не найден.");
        return RoomMapper.ToResponse(room);
    }

    public async Task<RoomResponse> CreateAsync(UpsertRoomRequest request, CancellationToken cancellationToken)
    {
        var number = request.Number.Trim();
        if (await _rooms.NumberExistsAsync(number, excludeId: null, cancellationToken))
        {
            throw ApiException.Conflict($"Номер {number} уже существует.");
        }

        var room = new Room
        {
            Number = number,
            Type = request.Type.Trim(),
            PricePerNight = request.PricePerNight,
            Capacity = request.Capacity,
            IsAvailable = request.IsAvailable
        };

        await _rooms.AddAsync(room, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Создан номер {Number} ({Type}, {Price})", room.Number, room.Type, room.PricePerNight);
        return RoomMapper.ToResponse(room);
    }

    public async Task<RoomResponse> UpdateAsync(int id, UpsertRoomRequest request, CancellationToken cancellationToken)
    {
        var room = await _rooms.GetByIdAsync(id, tracking: true, cancellationToken)
            ?? throw ApiException.NotFound($"Номер {id} не найден.");

        var number = request.Number.Trim();
        if (await _rooms.NumberExistsAsync(number, excludeId: id, cancellationToken))
        {
            throw ApiException.Conflict($"Номер {number} уже существует.");
        }

        room.Number = number;
        room.Type = request.Type.Trim();
        room.PricePerNight = request.PricePerNight;
        room.Capacity = request.Capacity;
        room.IsAvailable = request.IsAvailable;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Обновлён номер {RoomId} ({Number})", room.Id, room.Number);
        return RoomMapper.ToResponse(room);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var room = await _rooms.GetByIdAsync(id, tracking: true, cancellationToken)
            ?? throw ApiException.NotFound($"Номер {id} не найден.");

        if (await _bookings.AnyForRoomAsync(id, cancellationToken))
        {
            throw ApiException.Conflict("Нельзя удалить номер: по нему есть бронирования.");
        }

        _rooms.Remove(room);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Удалён номер {RoomId} ({Number})", room.Id, room.Number);
    }

    private static RoomSort ParseSort(string? sort) => sort switch
    {
        null or "" or "number" => RoomSort.Number,
        "-number" => RoomSort.NumberDesc,
        "price" => RoomSort.Price,
        "-price" => RoomSort.PriceDesc,
        _ => throw ApiException.BadRequest("Допустимая сортировка: number, -number, price, -price.")
    };
}

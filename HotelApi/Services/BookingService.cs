using HotelApi.Auth;
using HotelApi.Common;
using HotelApi.Data;
using HotelApi.Dtos;
using HotelApi.Mapping;
using HotelApi.Models;
using HotelApi.Options;
using HotelApi.Repositories;
using Microsoft.Extensions.Options;

namespace HotelApi.Services;

public interface IBookingService
{
    Task<PagedResult<BookingResponse>> SearchAsync(BookingQuery query, CancellationToken cancellationToken);

    Task<BookingResponse> GetAsync(int id, CancellationToken cancellationToken);

    Task<BookingResponse> CreateAsync(CreateBookingRequest request, CancellationToken cancellationToken);

    Task<BookingResponse> UpdateStatusAsync(int id, UpdateBookingStatusRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(int id, CancellationToken cancellationToken);
}

public sealed class BookingService : IBookingService
{
    private readonly IBookingRepository _bookings;
    private readonly IRoomRepository _rooms;
    private readonly IGuestRepository _guests;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly PaginationOptions _paging;
    private readonly ILogger<BookingService> _logger;

    public BookingService(
        IBookingRepository bookings,
        IRoomRepository rooms,
        IGuestRepository guests,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClock clock,
        IOptions<PaginationOptions> paging,
        ILogger<BookingService> logger)
    {
        _bookings = bookings;
        _rooms = rooms;
        _guests = guests;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _paging = paging.Value;
        _logger = logger;
    }

    public async Task<PagedResult<BookingResponse>> SearchAsync(BookingQuery query, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(query.Status) && !BookingStatuses.All.Contains(query.Status))
        {
            throw ApiException.BadRequest("Статус должен быть Active, Cancelled или Completed.");
        }

        var guestId = query.GuestId;
        if (!_currentUser.IsAdmin)
        {
            guestId = _currentUser.GuestId
                ?? throw ApiException.Forbidden("Учётная запись гостя не связана с карточкой гостя.");
        }

        var (page, pageSize) = Paging.Normalize(query.Page, query.PageSize, _paging.DefaultPageSize, _paging.MaxPageSize);
        var result = await _bookings.SearchAsync(new BookingSearch(
            query.Status,
            query.RoomId,
            guestId,
            ParseSort(query.Sort),
            page,
            pageSize), cancellationToken);

        return new PagedResult<BookingResponse>(
            result.Items.Select(BookingMapper.ToResponse).ToList(),
            result.Page,
            result.PageSize,
            result.TotalCount);
    }

    public async Task<BookingResponse> GetAsync(int id, CancellationToken cancellationToken)
    {
        var booking = await _bookings.GetByIdAsync(id, tracking: false, cancellationToken)
            ?? throw ApiException.NotFound($"Бронирование {id} не найдено.");
        EnsureCanRead(booking);
        return BookingMapper.ToResponse(booking);
    }

    public Task<BookingResponse> CreateAsync(CreateBookingRequest request, CancellationToken cancellationToken)
    {
        return _unitOfWork.InTransactionAsync(() => CreateCoreAsync(request, cancellationToken), cancellationToken);
    }

    public async Task<BookingResponse> UpdateStatusAsync(int id, UpdateBookingStatusRequest request, CancellationToken cancellationToken)
    {
        var booking = await _bookings.GetByIdAsync(id, tracking: true, cancellationToken)
            ?? throw ApiException.NotFound($"Бронирование {id} не найдено.");

        if (!_currentUser.IsAdmin)
        {
            if (booking.GuestId != _currentUser.GuestId)
            {
                throw ApiException.Forbidden("Можно изменять только свои бронирования.");
            }

            if (request.Status != BookingStatuses.Cancelled)
            {
                throw ApiException.Forbidden("Гость может только отменить бронирование.");
            }

            if (booking.Status != BookingStatuses.Active)
            {
                throw ApiException.Conflict("Отменить можно только активное бронирование.");
            }
        }

        if (request.Status == BookingStatuses.Active && booking.Status != BookingStatuses.Active)
        {
            var overlaps = await _bookings.HasOverlapAsync(
                booking.RoomId, booking.CheckIn, booking.CheckOut, booking.Id, cancellationToken);
            if (overlaps)
            {
                throw ApiException.Conflict("Номер уже занят на даты этого бронирования.");
            }
        }

        booking.Status = request.Status;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Статус бронирования {BookingId} изменён на {Status} пользователем {User}",
            booking.Id, booking.Status, _currentUser.UserName);
        return BookingMapper.ToResponse(booking);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var booking = await _bookings.GetByIdAsync(id, tracking: true, cancellationToken)
            ?? throw ApiException.NotFound($"Бронирование {id} не найдено.");

        _bookings.Remove(booking);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Администратор удалил бронирование {BookingId}", id);
    }

    private async Task<BookingResponse> CreateCoreAsync(CreateBookingRequest request, CancellationToken cancellationToken)
    {
        var guestId = ResolveGuestId(request.GuestId);
        var guest = await _guests.GetByIdAsync(guestId, cancellationToken)
            ?? throw ApiException.NotFound($"Гость {guestId} не найден.");

        var roomId = request.RoomId ?? throw ApiException.BadRequest("Укажите номер.");
        var room = await _rooms.GetByIdAsync(roomId, tracking: false, cancellationToken)
            ?? throw ApiException.NotFound($"Номер {roomId} не найден.");

        if (!room.IsAvailable)
        {
            throw ApiException.Conflict($"Номер {room.Number} снят с продажи.");
        }

        var checkIn = request.CheckIn ?? throw ApiException.BadRequest("Укажите дату заезда.");
        var checkOut = request.CheckOut ?? throw ApiException.BadRequest("Укажите дату выезда.");
        if (checkIn < _clock.Today)
        {
            throw ApiException.BadRequest("Дата заезда не может быть в прошлом.");
        }

        var nights = checkOut.DayNumber - checkIn.DayNumber;
        if (nights > 60)
        {
            throw ApiException.BadRequest("Бронирование длиннее 60 ночей не принимается.");
        }

        if (await _bookings.HasOverlapAsync(room.Id, checkIn, checkOut, excludeBookingId: null, cancellationToken))
        {
            throw ApiException.Conflict($"Номер {room.Number} уже забронирован на выбранные даты.");
        }

        var booking = new Booking
        {
            GuestId = guest.Id,
            RoomId = room.Id,
            CheckIn = checkIn,
            CheckOut = checkOut,
            Status = BookingStatuses.Active,
            TotalPrice = room.PricePerNight * nights,
            CreatedAt = _clock.UtcNow
        };

        await _bookings.AddAsync(booking, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var created = await _bookings.GetByIdAsync(booking.Id, tracking: false, cancellationToken)
            ?? throw ApiException.NotFound("Созданное бронирование не найдено.");

        _logger.LogInformation(
            "Создано бронирование {BookingId}: гость {GuestId}, номер {RoomNumber}, {CheckIn:yyyy-MM-dd}–{CheckOut:yyyy-MM-dd}",
            created.Id, created.GuestId, room.Number, checkIn, checkOut);

        return BookingMapper.ToResponse(created);
    }

    private int ResolveGuestId(int? requestedGuestId)
    {
        if (_currentUser.IsAdmin)
        {
            return requestedGuestId
                ?? throw ApiException.BadRequest("Администратор должен указать guestId.");
        }

        if (requestedGuestId is int otherGuest && otherGuest != _currentUser.GuestId)
        {
            throw ApiException.Forbidden("Гость может создавать бронирования только для себя.");
        }

        return _currentUser.GuestId
            ?? throw ApiException.Forbidden("Учётная запись гостя не связана с карточкой гостя.");
    }

    private void EnsureCanRead(Booking booking)
    {
        if (_currentUser.IsAdmin || booking.GuestId == _currentUser.GuestId)
        {
            return;
        }

        throw ApiException.Forbidden("Нельзя просматривать чужие бронирования.");
    }

    private static BookingSort ParseSort(string? sort) => sort switch
    {
        null or "" or "checkIn" => BookingSort.CheckIn,
        "-checkIn" => BookingSort.CheckInDesc,
        "price" => BookingSort.Price,
        "-price" => BookingSort.PriceDesc,
        _ => throw ApiException.BadRequest("Допустимая сортировка: checkIn, -checkIn, price, -price.")
    };
}

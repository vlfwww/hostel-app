using HotelApi.Common;
using HotelApi.Data;
using HotelApi.Dtos;
using HotelApi.Models;
using HotelApi.Options;
using HotelApi.Repositories;
using HotelApi.Services;
using HotelApi.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace HotelApi.Tests;

public class BookingServiceTests
{
    private static readonly DateOnly Today = new(2026, 6, 1);

    [Theory]
    [InlineData(1, 85)]
    [InlineData(3, 255)]
    [InlineData(10, 850)]
    public async Task Create_multiplies_room_price_by_nights(int nights, int expected)
    {
        var checkIn = Today.AddDays(10);
        var checkOut = checkIn.AddDays(nights);
        var (service, bookings) = Service(guest: true);
        var saved = CaptureCreate(bookings, checkIn, checkOut);

        var response = await service.CreateAsync(Request(checkIn, checkOut), CancellationToken.None);

        Assert.Equal(expected, saved.Value!.TotalPrice);
        Assert.Equal(expected, response.TotalPrice);
        Assert.Equal(BookingStatuses.Active, response.Status);
        Assert.Equal(nights, response.Nights);
    }

    [Fact]
    public async Task Create_rejects_check_in_in_the_past()
    {
        var (service, _) = Service(guest: true);
        PrepareRoomAndGuest();
        var exception = await Assert.ThrowsAsync<ApiException>(() => service.CreateAsync(
            Request(Today.AddDays(-1), Today.AddDays(2)),
            CancellationToken.None));

        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
        Assert.Contains("прошлом", exception.Message);
    }

    [Theory]
    [InlineData(60, false)]
    [InlineData(61, true)]
    public async Task Create_rejects_stay_longer_than_60_nights(int nights, bool rejected)
    {
        var checkIn = Today;
        var checkOut = checkIn.AddDays(nights);
        var (service, bookings) = Service(guest: true);
        PrepareRoomAndGuest();
        if (!rejected)
        {
            CaptureCreate(bookings, checkIn, checkOut);
            var response = await service.CreateAsync(Request(checkIn, checkOut), CancellationToken.None);
            Assert.Equal(60, response.Nights);
            return;
        }

        var exception = await Assert.ThrowsAsync<ApiException>(() => service.CreateAsync(
            Request(checkIn, checkOut),
            CancellationToken.None));
        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
        Assert.Contains("60", exception.Message);
    }

    [Fact]
    public async Task Create_guest_cannot_book_for_another_guest()
    {
        var (service, _) = Service(guest: true);
        var request = Request(Today.AddDays(10), Today.AddDays(12));
        request.GuestId = 9;

        var exception = await Assert.ThrowsAsync<ApiException>(() => service.CreateAsync(request, CancellationToken.None));

        Assert.Equal(StatusCodes.Status403Forbidden, exception.StatusCode);
    }

    [Fact]
    public async Task Create_admin_must_name_the_guest()
    {
        var (service, _) = Service(guest: false);
        var exception = await Assert.ThrowsAsync<ApiException>(() => service.CreateAsync(
            Request(Today.AddDays(10), Today.AddDays(12)),
            CancellationToken.None));

        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
        Assert.Contains("guestId", exception.Message);
    }

    [Fact]
    public async Task Create_conflicts_when_dates_overlap()
    {
        var checkIn = Today.AddDays(10);
        var checkOut = checkIn.AddDays(3);
        var (service, bookings) = Service(guest: true);
        PrepareRoomAndGuest();
        bookings.Setup(repo => repo.HasOverlapAsync(2, checkIn, checkOut, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var exception = await Assert.ThrowsAsync<ApiException>(() => service.CreateAsync(
            Request(checkIn, checkOut),
            CancellationToken.None));

        Assert.Equal(StatusCodes.Status409Conflict, exception.StatusCode);
    }

    [Fact]
    public async Task Search_guest_sees_only_own_bookings()
    {
        var (service, bookings) = Service(guest: true);
        bookings.Setup(repo => repo.SearchAsync(It.IsAny<BookingSearch>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Booking>([], 1, 10, 0));

        await service.SearchAsync(new BookingQuery { GuestId = 99, Page = 1, PageSize = 10 }, CancellationToken.None);

        bookings.Verify(repo => repo.SearchAsync(
            It.Is<BookingSearch>(search => search.GuestId == 5),
            It.IsAny<CancellationToken>()));
    }

    private readonly Mock<IRoomRepository> _rooms = new();
    private readonly Mock<IGuestRepository> _guests = new();

    private (BookingService Service, Mock<IBookingRepository> Bookings) Service(bool guest)
    {
        var bookings = new Mock<IBookingRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(work => work.InTransactionAsync(It.IsAny<Func<Task<BookingResponse>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<Task<BookingResponse>> action, CancellationToken _) => action());
        unitOfWork.Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var user = guest
            ? new TestUser()
            : new TestUser { Role = Roles.Admin, GuestId = null, UserName = "admin" };

        var service = new BookingService(
            bookings.Object,
            _rooms.Object,
            _guests.Object,
            unitOfWork.Object,
            user,
            new FixedClock(Today, new DateTime(2026, 6, 1, 8, 0, 0, DateTimeKind.Utc)),
            Microsoft.Extensions.Options.Options.Create(new PaginationOptions { DefaultPageSize = 10, MaxPageSize = 50 }),
            NullLogger<BookingService>.Instance);

        return (service, bookings);
    }

    private SavedBooking CaptureCreate(Mock<IBookingRepository> bookings, DateOnly checkIn, DateOnly checkOut)
    {
        var (room, guest) = PrepareRoomAndGuest();
        var saved = new SavedBooking();
        bookings.Setup(repo => repo.HasOverlapAsync(room.Id, checkIn, checkOut, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        bookings.Setup(repo => repo.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
            .Callback<Booking, CancellationToken>((booking, _) =>
            {
                booking.Id = 40;
                saved.Value = booking;
            })
            .Returns(Task.CompletedTask);
        bookings.Setup(repo => repo.GetByIdAsync(40, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                saved.Value!.Guest = guest;
                saved.Value.Room = room;
                return saved.Value;
            });
        return saved;
    }

    private sealed class SavedBooking
    {
        public Booking? Value { get; set; }
    }

    private (Room Room, Guest Guest) PrepareRoomAndGuest()
    {
        var room = new Room
        {
            Id = 2,
            Number = "102",
            Type = "Standard",
            PricePerNight = 85,
            Capacity = 2,
            IsAvailable = true
        };
        var guest = new Guest
        {
            Id = 5,
            FullName = "Иванов Алексей",
            PassportNumber = "MP1234567"
        };
        _rooms.Setup(repo => repo.GetByIdAsync(room.Id, false, It.IsAny<CancellationToken>())).ReturnsAsync(room);
        _guests.Setup(repo => repo.GetByIdAsync(guest.Id, It.IsAny<CancellationToken>())).ReturnsAsync(guest);
        return (room, guest);
    }

    private static CreateBookingRequest Request(DateOnly checkIn, DateOnly checkOut) => new()
    {
        RoomId = 2,
        CheckIn = checkIn,
        CheckOut = checkOut
    };
}

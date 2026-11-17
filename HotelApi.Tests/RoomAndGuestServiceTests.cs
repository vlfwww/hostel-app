using HotelApi.Common;
using HotelApi.Data;
using HotelApi.Dtos;
using HotelApi.Models;
using HotelApi.Options;
using HotelApi.Repositories;
using HotelApi.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace HotelApi.Tests;

public class RoomServiceTests
{
    [Fact]
    public async Task Search_rejects_price_range_when_max_is_below_min()
    {
        var service = Create(out _, out _);

        var exception = await Assert.ThrowsAsync<ApiException>(() => service.SearchAsync(
            new RoomQuery { MinPrice = 200, MaxPrice = 50 },
            CancellationToken.None));

        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
        Assert.Contains("maxPrice", exception.Message);
    }

    [Fact]
    public async Task Delete_conflicts_when_room_has_bookings()
    {
        var service = Create(out var rooms, out var bookings);
        rooms.Setup(repo => repo.GetByIdAsync(3, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Room { Id = 3, Number = "201", Type = "Deluxe", PricePerNight = 140, Capacity = 3 });
        bookings.Setup(repo => repo.AnyForRoomAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var exception = await Assert.ThrowsAsync<ApiException>(() => service.DeleteAsync(3, CancellationToken.None));

        Assert.Equal(StatusCodes.Status409Conflict, exception.StatusCode);
        rooms.Verify(repo => repo.Remove(It.IsAny<Room>()), Times.Never);
    }

    private static RoomService Create(out Mock<IRoomRepository> rooms, out Mock<IBookingRepository> bookings)
    {
        rooms = new Mock<IRoomRepository>();
        bookings = new Mock<IBookingRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        return new RoomService(
            rooms.Object,
            bookings.Object,
            unitOfWork.Object,
            Microsoft.Extensions.Options.Options.Create(new PaginationOptions()),
            NullLogger<RoomService>.Instance);
    }
}

public class GuestServiceTests
{
    [Fact]
    public async Task Create_conflicts_when_passport_already_exists()
    {
        var guests = new Mock<IGuestRepository>();
        guests.Setup(repo => repo.PassportExistsAsync("AB9988776", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var service = new GuestService(
            guests.Object,
            new Mock<IBookingRepository>().Object,
            new Mock<IUnitOfWork>().Object,
            NullLogger<GuestService>.Instance);

        var exception = await Assert.ThrowsAsync<ApiException>(() => service.CreateAsync(new CreateGuestRequest
        {
            FullName = "Новый гость",
            PassportNumber = "AB9988776"
        }, CancellationToken.None));

        Assert.Equal(StatusCodes.Status409Conflict, exception.StatusCode);
        guests.Verify(repo => repo.AddAsync(It.IsAny<Guest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Delete_conflicts_when_guest_has_bookings()
    {
        var guests = new Mock<IGuestRepository>();
        var bookings = new Mock<IBookingRepository>();
        guests.Setup(repo => repo.GetTrackedByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Guest { Id = 2, FullName = "Петрова Мария", PassportNumber = "MP7654321" });
        bookings.Setup(repo => repo.AnyForGuestAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var service = new GuestService(
            guests.Object,
            bookings.Object,
            new Mock<IUnitOfWork>().Object,
            NullLogger<GuestService>.Instance);

        var exception = await Assert.ThrowsAsync<ApiException>(() => service.DeleteAsync(2, CancellationToken.None));

        Assert.Equal(StatusCodes.Status409Conflict, exception.StatusCode);
        guests.Verify(repo => repo.Remove(It.IsAny<Guest>()), Times.Never);
    }
}

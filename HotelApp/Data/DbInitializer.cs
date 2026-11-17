using HotelApp.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelApp.Data;

public static class DbInitializer
{
    public static void EnsureSeeded(HotelDbContext db)
    {
        db.Database.Migrate();

        if (db.Rooms.Any())
        {
            return;
        }

        var rooms = new List<Room>
        {
            new() { Number = "101", Type = "Standard", PricePerNight = 80, Capacity = 2, IsAvailable = true },
            new() { Number = "102", Type = "Standard", PricePerNight = 85, Capacity = 2, IsAvailable = true },
            new() { Number = "201", Type = "Deluxe", PricePerNight = 140, Capacity = 3, IsAvailable = true },
            new() { Number = "301", Type = "Suite", PricePerNight = 220, Capacity = 4, IsAvailable = true }
        };

        var guests = new List<Guest>
        {
            new()
            {
                FullName = "Иванов Алексей",
                Phone = "+375291111111",
                Email = "ivanov@mail.by",
                PassportNumber = "MP1234567",
                Profile = new GuestProfile { LoyaltyLevel = "Silver", Notes = "Предпочитает тихие номера" }
            },
            new()
            {
                FullName = "Петрова Мария",
                Phone = "+375292222222",
                Email = "petrova@mail.by",
                PassportNumber = "MP7654321",
                Profile = new GuestProfile { LoyaltyLevel = "Gold", Notes = "Постоянный клиент" }
            },
            new()
            {
                FullName = "Сидоров Павел",
                Phone = "+375293333333",
                Email = "sidorov@mail.by",
                PassportNumber = "AB9988776",
                Profile = new GuestProfile { LoyaltyLevel = "Bronze", Notes = null }
            }
        };

        var services = new List<Service>
        {
            new() { Name = "Завтрак", Price = 15, Description = "Шведский стол" },
            new() { Name = "Парковка", Price = 10, Description = "Охраняемая стоянка" },
            new() { Name = "SPA", Price = 45, Description = "Сауна и бассейн" },
            new() { Name = "Трансфер", Price = 30, Description = "Аэропорт — гостиница" }
        };

        db.Rooms.AddRange(rooms);
        db.Guests.AddRange(guests);
        db.Services.AddRange(services);
        db.SaveChanges();

        var booking1 = new Booking
        {
            GuestId = guests[0].Id,
            RoomId = rooms[0].Id,
            CheckIn = DateTime.Today.AddDays(-1),
            CheckOut = DateTime.Today.AddDays(2),
            Status = "Active",
            TotalPrice = rooms[0].PricePerNight * 3
        };

        var booking2 = new Booking
        {
            GuestId = guests[1].Id,
            RoomId = rooms[2].Id,
            CheckIn = DateTime.Today,
            CheckOut = DateTime.Today.AddDays(4),
            Status = "Active",
            TotalPrice = rooms[2].PricePerNight * 4
        };

        db.Bookings.AddRange(booking1, booking2);
        db.SaveChanges();

        db.BookingServices.AddRange(
            new BookingServiceItem { BookingId = booking1.Id, ServiceId = services[0].Id, Quantity = 2 },
            new BookingServiceItem { BookingId = booking1.Id, ServiceId = services[1].Id, Quantity = 1 },
            new BookingServiceItem { BookingId = booking2.Id, ServiceId = services[2].Id, Quantity = 1 },
            new BookingServiceItem { BookingId = booking2.Id, ServiceId = services[3].Id, Quantity = 1 });

        rooms[0].IsAvailable = false;
        rooms[2].IsAvailable = false;
        db.SaveChanges();
    }
}

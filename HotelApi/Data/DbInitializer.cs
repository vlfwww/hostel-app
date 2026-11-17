using HotelApi.Auth;
using HotelApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelApi.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(HotelDbContext db, IPasswordHasher passwords, ILogger logger, CancellationToken cancellationToken = default)
    {
        await db.Database.MigrateAsync(cancellationToken);

        if (!await db.Rooms.AnyAsync(cancellationToken))
        {
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
                    PassportNumber = "MP1234567"
                },
                new()
                {
                    FullName = "Петрова Мария",
                    Phone = "+375292222222",
                    Email = "petrova@mail.by",
                    PassportNumber = "MP7654321"
                },
                new()
                {
                    FullName = "Сидоров Павел",
                    Phone = "+375293333333",
                    Email = "sidorov@mail.by",
                    PassportNumber = "AB9988776"
                }
            };

            db.Rooms.AddRange(rooms);
            db.Guests.AddRange(guests);
            await db.SaveChangesAsync(cancellationToken);

            var today = DateOnly.FromDateTime(DateTime.Today);
            db.Bookings.AddRange(
                new Booking
                {
                    GuestId = guests[0].Id,
                    RoomId = rooms[0].Id,
                    CheckIn = today,
                    CheckOut = today.AddDays(3),
                    Status = BookingStatuses.Active,
                    TotalPrice = rooms[0].PricePerNight * 3,
                    CreatedAt = DateTime.UtcNow
                },
                new Booking
                {
                    GuestId = guests[1].Id,
                    RoomId = rooms[2].Id,
                    CheckIn = today,
                    CheckOut = today.AddDays(4),
                    Status = BookingStatuses.Active,
                    TotalPrice = rooms[2].PricePerNight * 4,
                    CreatedAt = DateTime.UtcNow
                });

            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("База заполнена тестовыми номерами, гостями и бронированиями.");
        }

        if (!await db.Users.AnyAsync(cancellationToken))
        {
            var ivanov = await db.Guests.SingleAsync(guest => guest.PassportNumber == "MP1234567", cancellationToken);
            var petrova = await db.Guests.SingleAsync(guest => guest.PassportNumber == "MP7654321", cancellationToken);

            db.Users.AddRange(
                new AppUser
                {
                    UserName = "admin",
                    PasswordHash = passwords.Hash("Admin123!"),
                    Role = Roles.Admin
                },
                new AppUser
                {
                    UserName = "ivanov",
                    PasswordHash = passwords.Hash("Guest123!"),
                    Role = Roles.Guest,
                    GuestId = ivanov.Id
                },
                new AppUser
                {
                    UserName = "petrova",
                    PasswordHash = passwords.Hash("Guest123!"),
                    Role = Roles.Guest,
                    GuestId = petrova.Id
                });

            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Созданы учётные записи admin, ivanov и petrova.");
        }
    }
}

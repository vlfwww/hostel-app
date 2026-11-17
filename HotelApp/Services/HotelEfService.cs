using HotelApp.Data;
using HotelApp.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelApp.Services;

public class HotelEfService
{
    private readonly HotelDbContext _db;

    public HotelEfService(HotelDbContext db) => _db = db;

    public void ListRooms()
    {
        Console.WriteLine("\n=== Номера ===");
        var rooms = _db.Rooms.AsNoTracking().OrderBy(r => r.Number).ToList();
        foreach (var r in rooms)
        {
            Console.WriteLine(
                $"#{r.Id}  {r.Number,-6} {r.Type,-10} {r.PricePerNight,8:0.00} BYN  " +
                $"мест: {r.Capacity}  {(r.IsAvailable ? "свободен" : "занят")}");
        }
    }

    public void ListGuestsWithProfiles()
    {
        Console.WriteLine("\n=== Гости ===");
        var guests = _db.Guests
            .AsNoTracking()
            .Include(g => g.Profile)
            .OrderBy(g => g.FullName)
            .ToList();

        foreach (var g in guests)
        {
            Console.WriteLine(
                $"#{g.Id}  {g.FullName,-20} паспорт {g.PassportNumber}  " +
                $"лояльность: {g.Profile?.LoyaltyLevel ?? "-"}");
        }
    }

    public void ListBookingsWithDetails()
    {
        Console.WriteLine("\n=== Бронирования ===");
        var bookings = _db.Bookings
            .AsNoTracking()
            .Include(b => b.Guest)
            .Include(b => b.Room)
            .Include(b => b.BookingServices)
                .ThenInclude(bs => bs.Service)
            .OrderBy(b => b.CheckIn)
            .ToList();

        foreach (var b in bookings)
        {
            Console.WriteLine(
                $"#{b.Id}  {b.Guest.FullName} → номер {b.Room.Number}  " +
                $"{b.CheckIn:dd.MM.yyyy}–{b.CheckOut:dd.MM.yyyy}  " +
                $"{b.Status}  итого {b.TotalPrice:0.00} BYN");

            foreach (var bs in b.BookingServices)
            {
                Console.WriteLine($"     услуга: {bs.Service.Name} x{bs.Quantity} ({bs.Service.Price:0.00})");
            }
        }
    }

    public void CreateGuest()
    {
        Console.WriteLine("\n=== Регистрация гостя ===");
        _db.ChangeTracker.Clear();

        Console.Write("ФИО: ");
        var fullName = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(fullName))
        {
            fullName = "Козлова Анна";
            Console.WriteLine($"(пусто → {fullName})");
        }

        Console.Write("Паспорт: ");
        var passport = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(passport))
        {
            passport = "CD1122334";
            Console.WriteLine($"(пусто → {passport})");
        }

        var existing = _db.Guests.FirstOrDefault(g => g.PassportNumber == passport);
        if (existing is not null)
        {
            Console.WriteLine($"Гость с паспортом {passport} уже зарегистрирован: #{existing.Id} {existing.FullName}");
            return;
        }

        Console.Write("Телефон: ");
        var phone = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(phone))
        {
            phone = "+375294444444";
        }

        var guest = new Guest
        {
            FullName = fullName,
            Phone = phone,
            Email = null,
            PassportNumber = passport,
            Profile = new GuestProfile { LoyaltyLevel = "Bronze", Notes = "Новый гость" }
        };

        _db.Guests.Add(guest);
        _db.SaveChanges();
        Console.WriteLine($"Гость зарегистрирован: #{guest.Id} {guest.FullName}");
    }

    public void UpdateRoomPrice()
    {
        Console.WriteLine("\n=== Изменение цены номера ===");
        Console.Write("Номер комнаты (например 102): ");
        var number = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(number))
        {
            number = "102";
        }

        var room = _db.Rooms.FirstOrDefault(r => r.Number == number);
        if (room is null)
        {
            Console.WriteLine("Номер не найден.");
            return;
        }

        Console.WriteLine($"Текущая цена: {room.PricePerNight:0.00} BYN");
        Console.Write("Новая цена (Enter = +5 BYN): ");
        var input = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(input))
        {
            room.PricePerNight += 5;
        }
        else if (decimal.TryParse(input.Replace(',', '.'), System.Globalization.NumberStyles.Any,
                     System.Globalization.CultureInfo.InvariantCulture, out var price))
        {
            room.PricePerNight = price;
        }
        else
        {
            Console.WriteLine("Некорректная цена.");
            return;
        }

        _db.SaveChanges();
        Console.WriteLine($"Цена номера {room.Number} обновлена: {room.PricePerNight:0.00} BYN");
    }

    public void RaiseStandardPrices()
    {
        Console.WriteLine("\n=== Повышение цен категории Standard ===");
        var affected = _db.Rooms
            .Where(r => r.Type == "Standard")
            .ExecuteUpdate(s => s.SetProperty(r => r.PricePerNight, r => r.PricePerNight + 3));
        Console.WriteLine($"Обновлено номеров: {affected}");
        ListRooms();
    }

    public void DeleteGuestWithoutBookings()
    {
        Console.WriteLine("\n=== Удаление гостя ===");
        Console.Write("Паспорт гостя: ");
        var passport = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(passport))
        {
            passport = "CD1122334";
        }

        var guest = _db.Guests
            .Include(g => g.Bookings)
            .FirstOrDefault(g => g.PassportNumber == passport);

        if (guest is null)
        {
            Console.WriteLine("Гость не найден.");
            return;
        }

        if (guest.Bookings.Count > 0)
        {
            Console.WriteLine("У гостя есть бронирования — удаление невозможно.");
            return;
        }

        var name = guest.FullName;
        _db.Guests.Remove(guest);
        _db.SaveChanges();
        Console.WriteLine($"Гость {name} удалён.");
    }
}

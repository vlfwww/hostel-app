using HotelApp.Data;
using HotelApp.Services;

Console.OutputEncoding = System.Text.Encoding.UTF8;

using var db = new HotelDbContext();
DbInitializer.EnsureSeeded(db);

var hotel = new HotelEfService(db);
var ado = new AdoNetDemo(db.DbPath);

Console.WriteLine("==============================================");
Console.WriteLine("           Гостиница «Северная»");
Console.WriteLine("     Учёт номеров, гостей и бронирований");
Console.WriteLine("==============================================");

while (true)
{
    Console.WriteLine("""

        Меню:
          1  — Список номеров
          2  — Список гостей
          3  — Бронирования и услуги
          4  — Зарегистрировать гостя
          5  — Изменить цену номера
          6  — Поднять цены категории Standard
          7  — Удалить гостя без броней
          8  — Поиск номеров по типу
          9  — Оформить бронирование
         10  — Сводка по бронированиям
          0  — Выход
        """);

    Console.Write("Выбор: ");
    var choice = Console.ReadLine()?.Trim();

    switch (choice)
    {
        case "1":
            hotel.ListRooms();
            break;
        case "2":
            hotel.ListGuestsWithProfiles();
            break;
        case "3":
            hotel.ListBookingsWithDetails();
            break;
        case "4":
            hotel.CreateGuest();
            break;
        case "5":
            hotel.UpdateRoomPrice();
            break;
        case "6":
            hotel.RaiseStandardPrices();
            break;
        case "7":
            hotel.DeleteGuestWithoutBookings();
            break;
        case "8":
            ado.SearchRoomsByType();
            break;
        case "9":
            ado.CreateBookingWithService();
            break;
        case "10":
            ado.ShowBookingsSummary();
            break;
        case "0":
            Console.WriteLine("До свидания.");
            return;
        default:
            Console.WriteLine("Неизвестный пункт. Выберите номер из меню.");
            break;
    }
}

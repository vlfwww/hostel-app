using System.Data;
using System.Data.Common;
using Microsoft.Data.Sqlite;

namespace HotelApp.Services;

public class AdoNetDemo
{
    private readonly string _connectionString;

    public AdoNetDemo(string dbPath)
    {
        _connectionString = $"Data Source={dbPath}";
    }

    public void SearchRoomsByType()
    {
        Console.WriteLine("\n=== Поиск номеров по типу ===");
        Console.Write("Тип номера (Standard / Deluxe / Suite): ");
        var roomType = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(roomType))
        {
            roomType = "Standard";
            Console.WriteLine($"(пусто → «{roomType}»)");
        }

        using DbConnection connection = new SqliteConnection(_connectionString);
        connection.Open();

        using DbCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Id, Number, Type, PricePerNight, Capacity, IsAvailable
            FROM Rooms
            WHERE Type = $type
            ORDER BY Number;
            """;

        var parameter = command.CreateParameter();
        parameter.ParameterName = "$type";
        parameter.Value = roomType;
        command.Parameters.Add(parameter);

        using DbDataReader reader = command.ExecuteReader();
        var found = 0;
        while (reader.Read())
        {
            found++;
            Console.WriteLine(
                $"  #{reader.GetInt32(0)}  {reader.GetString(1),-6} {reader.GetString(2),-10} " +
                $"{reader.GetDecimal(3),8:0.00}  мест:{reader.GetInt32(4)}  " +
                $"{(reader.GetBoolean(5) ? "свободен" : "занят")}");
        }

        if (found == 0)
        {
            Console.WriteLine("  Номеров такого типа не найдено.");
        }
    }

    public void CreateBookingWithService()
    {
        Console.WriteLine("\n=== Оформление бронирования ===");

        using DbConnection connection = new SqliteConnection(_connectionString);
        connection.Open();
        using DbTransaction transaction = connection.BeginTransaction();

        try
        {
            int? roomId = null;
            string? roomNumber = null;
            decimal price = 0;

            using (var findRoom = connection.CreateCommand())
            {
                findRoom.Transaction = transaction;
                findRoom.CommandText =
                    """
                    SELECT Id, Number, PricePerNight
                    FROM Rooms
                    WHERE IsAvailable = 1
                    ORDER BY PricePerNight
                    LIMIT 1;
                    """;

                using var reader = findRoom.ExecuteReader();
                if (!reader.Read())
                {
                    Console.WriteLine("Свободных номеров нет.");
                    transaction.Rollback();
                    return;
                }

                roomId = reader.GetInt32(0);
                roomNumber = reader.GetString(1);
                price = reader.GetDecimal(2);
            }

            int guestId;
            string guestName;
            using (var findGuest = connection.CreateCommand())
            {
                findGuest.Transaction = transaction;
                findGuest.CommandText = "SELECT Id, FullName FROM Guests ORDER BY Id LIMIT 1;";
                using var reader = findGuest.ExecuteReader();
                if (!reader.Read())
                {
                    Console.WriteLine("Нет зарегистрированных гостей.");
                    transaction.Rollback();
                    return;
                }

                guestId = reader.GetInt32(0);
                guestName = reader.GetString(1);
            }

            long bookingId;
            using (var insertBooking = connection.CreateCommand())
            {
                insertBooking.Transaction = transaction;
                insertBooking.CommandText =
                    """
                    INSERT INTO Bookings (GuestId, RoomId, CheckIn, CheckOut, Status, TotalPrice)
                    VALUES ($guestId, $roomId, $checkIn, $checkOut, $status, $total);
                    SELECT last_insert_rowid();
                    """;

                AddParam(insertBooking, "$guestId", guestId);
                AddParam(insertBooking, "$roomId", roomId!);
                AddParam(insertBooking, "$checkIn", DateTime.Today.ToString("yyyy-MM-dd HH:mm:ss"));
                AddParam(insertBooking, "$checkOut", DateTime.Today.AddDays(2).ToString("yyyy-MM-dd HH:mm:ss"));
                AddParam(insertBooking, "$status", "Active");
                AddParam(insertBooking, "$total", price * 2);
                bookingId = (long)(insertBooking.ExecuteScalar() ?? 0L);
            }

            using (var insertService = connection.CreateCommand())
            {
                insertService.Transaction = transaction;
                insertService.CommandText =
                    """
                    INSERT INTO BookingServices (BookingId, ServiceId, Quantity)
                    SELECT $bookingId, Id, 1 FROM Services WHERE Name = 'Завтрак' LIMIT 1;
                    """;
                AddParam(insertService, "$bookingId", bookingId);
                insertService.ExecuteNonQuery();
            }

            using (var updateRoom = connection.CreateCommand())
            {
                updateRoom.Transaction = transaction;
                updateRoom.CommandText = "UPDATE Rooms SET IsAvailable = 0 WHERE Id = $id;";
                AddParam(updateRoom, "$id", roomId!);
                updateRoom.ExecuteNonQuery();
            }

            transaction.Commit();
            Console.WriteLine(
                $"Бронирование #{bookingId} оформлено: гость {guestName}, номер {roomNumber}, " +
                $"на 2 ночи, услуга «Завтрак» добавлена. Итого: {price * 2:0.00} BYN.");
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            Console.WriteLine($"Не удалось оформить бронирование: {ex.Message}");
        }
    }

    public void ShowBookingsSummary()
    {
        Console.WriteLine("\n=== Сводка по бронированиям ===");

        var table = new DataTable();
        table.Columns.Add("Guest", typeof(string));
        table.Columns.Add("Room", typeof(string));
        table.Columns.Add("Status", typeof(string));
        table.Columns.Add("TotalPrice", typeof(decimal));

        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText =
                """
                SELECT g.FullName AS Guest, r.Number AS Room, b.Status, b.TotalPrice
                FROM Bookings b
                INNER JOIN Guests g ON g.Id = b.GuestId
                INNER JOIN Rooms r ON r.Id = b.RoomId
                ORDER BY b.Id;
                """;

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                table.Rows.Add(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetDecimal(3));
            }
        }

        if (table.Rows.Count == 0)
        {
            Console.WriteLine("Бронирований пока нет.");
            return;
        }

        foreach (DataRow row in table.Rows)
        {
            Console.WriteLine(
                $"  {row["Guest"],-20} номер {row["Room"],-4} {row["Status"],-8} {Convert.ToDecimal(row["TotalPrice"]):0.00} BYN");
        }
    }

    private static void AddParam(DbCommand command, string name, object value)
    {
        var p = command.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        command.Parameters.Add(p);
    }
}

using System.Globalization;
using HotelBlazor.Models;

namespace HotelBlazor.Services;

public static class HotelText
{
    private static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");

    public static string Money(decimal value) => value.ToString("0.##", Ru) + " BYN";

    public static string Day(DateOnly value) => value.ToString("d MMMM", Ru);

    public static string When(DateTime value) => value.ToLocalTime().ToString("d MMMM, HH:mm", Ru);

    public static string Role(string? role) => role switch
    {
        "Admin" => "администратор",
        "Guest" => "гость",
        _ => "посетитель"
    };

    public static string Status(string status) => status switch
    {
        "Active" => "Активна",
        "Cancelled" => "Отменена",
        "Completed" => "Завершена",
        _ => status
    };

    public static string Tone(string type) => type.Trim().ToLowerInvariant() switch
    {
        "deluxe" => "tone-deluxe",
        "suite" => "tone-suite",
        _ => "tone-standard"
    };

    public static string Nights(int nights) => nights switch
    {
        1 => "1 ночь",
        >= 2 and <= 4 => $"{nights} ночи",
        _ => $"{nights} ночей"
    };

    public static int CountNights(DateOnly checkIn, DateOnly checkOut) =>
        checkOut > checkIn ? checkOut.DayNumber - checkIn.DayNumber : 0;

    public static decimal Quote(decimal pricePerNight, DateOnly checkIn, DateOnly checkOut) =>
        pricePerNight * CountNights(checkIn, checkOut);

    public static Viewer AsViewer(AuthSession session) =>
        new(session.IsAuthenticated, session.IsAdmin, session.UserName);
}

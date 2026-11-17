namespace HotelApi.Models;

public static class BookingStatuses
{
    public const string Active = "Active";
    public const string Cancelled = "Cancelled";
    public const string Completed = "Completed";

    public static readonly string[] All = [Active, Cancelled, Completed];
}

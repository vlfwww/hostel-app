using HotelApi.Auth;
using HotelApi.Common;
using HotelApi.Models;

namespace HotelApi.Tests.Support;

internal sealed class TestUser : ICurrentUser
{
    public bool IsAuthenticated => true;

    public int UserId { get; init; } = 1;

    public string UserName { get; init; } = "ivanov";

    public string Role { get; init; } = Roles.Guest;

    public bool IsAdmin => Role == Roles.Admin;

    public int? GuestId { get; init; } = 5;
}

internal sealed class FixedClock : IClock
{
    public FixedClock(DateOnly today, DateTime utcNow)
    {
        Today = today;
        UtcNow = utcNow;
    }

    public DateTime UtcNow { get; }

    public DateOnly Today { get; }
}

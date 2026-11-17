namespace HotelBlazor.Models;

public sealed class LoginResult
{
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
}

public sealed class UserProfile
{
    public int UserId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public int? GuestId { get; set; }
}

public sealed class RoomModel
{
    public int Id { get; set; }

    public string Number { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public decimal PricePerNight { get; set; }

    public int Capacity { get; set; }

    public bool IsAvailable { get; set; }
}

public sealed class GuestModel
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string PassportNumber { get; set; } = string.Empty;
}

public sealed class BookingModel
{
    public int Id { get; set; }

    public int GuestId { get; set; }

    public string GuestName { get; set; } = string.Empty;

    public int RoomId { get; set; }

    public string RoomNumber { get; set; } = string.Empty;

    public string RoomType { get; set; } = string.Empty;

    public DateOnly CheckIn { get; set; }

    public DateOnly CheckOut { get; set; }

    public int Nights { get; set; }

    public string Status { get; set; } = string.Empty;

    public decimal TotalPrice { get; set; }

    public DateTime CreatedAt { get; set; }
}

public sealed class Paged<T>
{
    public List<T> Items { get; set; } = [];

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public int TotalPages { get; set; }
}

public sealed class RoomFilter
{
    public string? Type { get; set; }

    public decimal? MinPrice { get; set; }

    public decimal? MaxPrice { get; set; }

    public bool? Available { get; set; }

    public string? Sort { get; set; } = "number";

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 6;
}

public sealed class BookingFilter
{
    public string? Status { get; set; }

    public string? Sort { get; set; } = "-checkIn";

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}

public sealed record StatusChange(int Id, string Status);

public sealed record Viewer(bool IsAuthenticated, bool IsAdmin, string? Name);

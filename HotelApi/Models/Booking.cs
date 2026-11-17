using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelApi.Models;

[Table("Bookings")]
public class Booking
{
    public int Id { get; set; }

    public int GuestId { get; set; }

    public Guest Guest { get; set; } = null!;

    public int RoomId { get; set; }

    public Room Room { get; set; } = null!;

    public DateOnly CheckIn { get; set; }

    public DateOnly CheckOut { get; set; }

    [MaxLength(30)]
    public string Status { get; set; } = BookingStatuses.Active;

    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalPrice { get; set; }

    public DateTime CreatedAt { get; set; }
}

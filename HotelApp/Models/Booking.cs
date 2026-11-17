using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelApp.Models;

[Table("Bookings")]
public class Booking
{
    [Key]
    public int Id { get; set; }

    public int GuestId { get; set; }

    [ForeignKey(nameof(GuestId))]
    public Guest Guest { get; set; } = null!;

    public int RoomId { get; set; }

    [ForeignKey(nameof(RoomId))]
    public Room Room { get; set; } = null!;

    public DateTime CheckIn { get; set; }

    public DateTime CheckOut { get; set; }

    [Required, MaxLength(30)]
    public string Status { get; set; } = "Active";

    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalPrice { get; set; }

    public ICollection<BookingServiceItem> BookingServices { get; set; } = new List<BookingServiceItem>();
}

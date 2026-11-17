using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelApp.Models;

[Table("BookingServices")]
public class BookingServiceItem
{
    [Key]
    public int Id { get; set; }

    public int BookingId { get; set; }

    [ForeignKey(nameof(BookingId))]
    public Booking Booking { get; set; } = null!;

    public int ServiceId { get; set; }

    [ForeignKey(nameof(ServiceId))]
    public Service Service { get; set; } = null!;

    public int Quantity { get; set; } = 1;
}

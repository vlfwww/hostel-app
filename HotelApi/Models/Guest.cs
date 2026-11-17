using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelApi.Models;

[Table("Guests")]
public class Guest
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? Phone { get; set; }

    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string PassportNumber { get; set; } = string.Empty;

    public AppUser? User { get; set; }

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}

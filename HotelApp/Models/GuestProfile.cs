using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelApp.Models;

[Table("GuestProfiles")]
public class GuestProfile
{
    [Key]
    public int Id { get; set; }

    public int GuestId { get; set; }

    [ForeignKey(nameof(GuestId))]
    public Guest Guest { get; set; } = null!;

    [MaxLength(50)]
    public string? LoyaltyLevel { get; set; }

    public string? Notes { get; set; }

    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
}

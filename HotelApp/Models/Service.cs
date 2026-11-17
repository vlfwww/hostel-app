using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelApp.Models;

[Table("Services")]
public class Service
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }

    [MaxLength(250)]
    public string? Description { get; set; }

    public ICollection<BookingServiceItem> BookingServices { get; set; } = new List<BookingServiceItem>();
}

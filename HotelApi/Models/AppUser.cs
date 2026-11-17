using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelApi.Models;

[Table("Users")]
public class AppUser
{
    public int Id { get; set; }

    [MaxLength(50)]
    public string UserName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Role { get; set; } = Roles.Guest;

    public int? GuestId { get; set; }

    public Guest? Guest { get; set; }
}

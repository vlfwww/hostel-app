using HotelApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelApi.Data;

public class HotelDbContext : DbContext
{
    public HotelDbContext(DbContextOptions<HotelDbContext> options)
        : base(options)
    {
    }

    public DbSet<Room> Rooms => Set<Room>();

    public DbSet<Guest> Guests => Set<Guest>();

    public DbSet<Booking> Bookings => Set<Booking>();

    public DbSet<AppUser> Users => Set<AppUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Room>()
            .HasIndex(room => room.Number)
            .IsUnique();

        modelBuilder.Entity<Guest>()
            .HasIndex(guest => guest.PassportNumber)
            .IsUnique();

        modelBuilder.Entity<AppUser>()
            .HasIndex(user => user.UserName)
            .IsUnique();

        modelBuilder.Entity<AppUser>()
            .HasOne(user => user.Guest)
            .WithOne(guest => guest.User)
            .HasForeignKey<AppUser>(user => user.GuestId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Booking>()
            .HasOne(booking => booking.Guest)
            .WithMany(guest => guest.Bookings)
            .HasForeignKey(booking => booking.GuestId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Booking>()
            .HasOne(booking => booking.Room)
            .WithMany(room => room.Bookings)
            .HasForeignKey(booking => booking.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Booking>()
            .HasIndex(booking => new { booking.RoomId, booking.Status });

        modelBuilder.Entity<Room>()
            .Property(room => room.PricePerNight)
            .HasPrecision(10, 2);

        modelBuilder.Entity<Booking>()
            .Property(booking => booking.TotalPrice)
            .HasPrecision(10, 2);
    }
}

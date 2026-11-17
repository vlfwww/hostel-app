using HotelApp.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelApp.Data;

public class HotelDbContext : DbContext
{
    public string DbPath { get; }

    public HotelDbContext()
    {
        var folder = AppContext.BaseDirectory;
        DbPath = Path.Combine(folder, "hotel.db");
    }

    public HotelDbContext(DbContextOptions<HotelDbContext> options) : base(options)
    {
        DbPath = string.Empty;
    }

    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Guest> Guests => Set<Guest>();
    public DbSet<GuestProfile> GuestProfiles => Set<GuestProfile>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<BookingServiceItem> BookingServices => Set<BookingServiceItem>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite($"Data Source={DbPath}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // One-to-one: Guest ↔ GuestProfile
        modelBuilder.Entity<Guest>()
            .HasOne(g => g.Profile)
            .WithOne(p => p.Guest)
            .HasForeignKey<GuestProfile>(p => p.GuestId)
            .OnDelete(DeleteBehavior.Cascade);

        // One-to-many: Guest → Bookings
        modelBuilder.Entity<Booking>()
            .HasOne(b => b.Guest)
            .WithMany(g => g.Bookings)
            .HasForeignKey(b => b.GuestId)
            .OnDelete(DeleteBehavior.Restrict);

        // One-to-many: Room → Bookings
        modelBuilder.Entity<Booking>()
            .HasOne(b => b.Room)
            .WithMany(r => r.Bookings)
            .HasForeignKey(b => b.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        // Many-to-many через явную сущность BookingServiceItem
        modelBuilder.Entity<BookingServiceItem>()
            .HasOne(bs => bs.Booking)
            .WithMany(b => b.BookingServices)
            .HasForeignKey(bs => bs.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BookingServiceItem>()
            .HasOne(bs => bs.Service)
            .WithMany(s => s.BookingServices)
            .HasForeignKey(bs => bs.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Room>()
            .HasIndex(r => r.Number)
            .IsUnique();

        modelBuilder.Entity<Guest>()
            .HasIndex(g => g.PassportNumber)
            .IsUnique();
    }
}

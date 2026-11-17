using HotelApi.Data;
using HotelApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelApi.Repositories;

public sealed class GuestRepository : IGuestRepository
{
    private readonly HotelDbContext _db;

    public GuestRepository(HotelDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Guest>> ListAsync(CancellationToken cancellationToken) =>
        await _db.Guests.AsNoTracking().OrderBy(guest => guest.FullName).ToListAsync(cancellationToken);

    public Task<Guest?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        _db.Guests.AsNoTracking().FirstOrDefaultAsync(guest => guest.Id == id, cancellationToken);

    public Task<bool> PassportExistsAsync(string passportNumber, CancellationToken cancellationToken)
    {
        var passport = passportNumber.Trim().ToLower();
        return _db.Guests.AnyAsync(guest => guest.PassportNumber.ToLower() == passport, cancellationToken);
    }

    public async Task AddAsync(Guest guest, CancellationToken cancellationToken) =>
        await _db.Guests.AddAsync(guest, cancellationToken);
}

public sealed class UserRepository : IUserRepository
{
    private readonly HotelDbContext _db;

    public UserRepository(HotelDbContext db)
    {
        _db = db;
    }

    public Task<AppUser?> GetByUserNameAsync(string userName, CancellationToken cancellationToken)
    {
        var name = userName.Trim().ToLower();
        return _db.Users.FirstOrDefaultAsync(user => user.UserName.ToLower() == name, cancellationToken);
    }

    public Task<bool> UserNameExistsAsync(string userName, CancellationToken cancellationToken)
    {
        var name = userName.Trim().ToLower();
        return _db.Users.AnyAsync(user => user.UserName.ToLower() == name, cancellationToken);
    }

    public async Task AddAsync(AppUser user, CancellationToken cancellationToken) =>
        await _db.Users.AddAsync(user, cancellationToken);
}

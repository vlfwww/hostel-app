using HotelApi.Models;

namespace HotelApi.Repositories;

public interface IGuestRepository
{
    Task<IReadOnlyList<Guest>> ListAsync(CancellationToken cancellationToken);

    Task<Guest?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<bool> PassportExistsAsync(string passportNumber, CancellationToken cancellationToken);

    Task AddAsync(Guest guest, CancellationToken cancellationToken);
}

public interface IUserRepository
{
    Task<AppUser?> GetByUserNameAsync(string userName, CancellationToken cancellationToken);

    Task<bool> UserNameExistsAsync(string userName, CancellationToken cancellationToken);

    Task AddAsync(AppUser user, CancellationToken cancellationToken);
}

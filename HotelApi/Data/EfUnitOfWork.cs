namespace HotelApi.Data;

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<T> InTransactionAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default);
}

public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly HotelDbContext _db;

    public EfUnitOfWork(HotelDbContext db)
    {
        _db = db;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);

    public async Task<T> InTransactionAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        if (_db.Database.CurrentTransaction is not null)
        {
            return await action();
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var result = await action();
        await transaction.CommitAsync(cancellationToken);
        return result;
    }
}

namespace Forma.Infrastructure.Persistence;

/// <summary>
/// Runs work in one database transaction. Every repository of the request shares its <see cref="FormaDbContext"/>, so
/// their <c>SaveChangesAsync</c> calls join the transaction: it commits when the work completes and rolls back when it throws.
/// </summary>
public interface ITransactionRunner
{
    /// <summary>
    /// Runs <paramref name="work"/> in a transaction. It commits when <paramref name="commit"/> says so for the result
    /// (always when it is <c>null</c>), and rolls back otherwise or when the work throws.
    /// </summary>
    Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> work, Func<T, bool>? commit = null, CancellationToken cancellationToken = default);
}

public class TransactionRunner(FormaDbContext db) : ITransactionRunner
{
    public async Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> work, Func<T, bool>? commit = null, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        T result;

        try
        {
            result = await work(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            db.ChangeTracker.Clear();
            throw;
        }

        if (commit is null || commit(result))
            await transaction.CommitAsync(cancellationToken);
        else
            await transaction.RollbackAsync(cancellationToken);

        return result;
    }
}

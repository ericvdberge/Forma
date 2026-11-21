using Forma.Infrastructure.Daos;
using Forma.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Forma.Infrastructure.Repositories.PageBindingUsages;

public interface IPageBindingUsageRepository
{
    Task<IReadOnlyList<PageBindingUsageDao>> GetPageBindingUsagesAsync(PageBindingUsageFilter filter, CancellationToken cancellationToken = default);

    Task AddAsync(IReadOnlyList<PageBindingUsageDao> usages, CancellationToken cancellationToken = default);

    /// <summary>Deletes the usages matching <paramref name="filter"/>, and returns how many there were.</summary>
    Task<int> DeleteAsync(PageBindingUsageFilter filter, CancellationToken cancellationToken = default);
}

public class PageBindingUsageRepository(FormaDbContext db) : IPageBindingUsageRepository
{
    public async Task<IReadOnlyList<PageBindingUsageDao>> GetPageBindingUsagesAsync(PageBindingUsageFilter filter, CancellationToken cancellationToken = default) =>
        await db.PageBindingUsages
            .AsNoTracking()
            .ApplyFilter(filter)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(IReadOnlyList<PageBindingUsageDao> usages, CancellationToken cancellationToken = default)
    {
        db.PageBindingUsages.AddRange(usages);
        await db.SaveChangesAsync(cancellationToken);
        db.ChangeTracker.Clear();
    }

    public async Task<int> DeleteAsync(PageBindingUsageFilter filter, CancellationToken cancellationToken = default) =>
        await db.PageBindingUsages.ApplyFilter(filter).ExecuteDeleteAsync(cancellationToken);
}

using Forma.Application.Entities;
using Forma.Application.Mappers;
using Forma.Infrastructure.Repositories.PageBindingUsages;

namespace Forma.Application.Services;

/// <summary>Creates, reads and deletes the collection fields pages show. Rules and workflows belong to the callers.</summary>
public interface IPageBindingUsageCoreService
{
    Task<IReadOnlyList<PageBindingUsage>> GetPageBindingUsagesAsync(PageBindingUsageFilter filter, CancellationToken cancellationToken = default);

    Task CreateAsync(IReadOnlyList<PageBindingUsage> usages, CancellationToken cancellationToken = default);

    /// <summary>Deletes every usage of the page, and returns how many there were.</summary>
    Task<int> DeleteAsync(Guid pageId, CancellationToken cancellationToken = default);
}

public class PageBindingUsageCoreService(IPageBindingUsageRepository repository) : IPageBindingUsageCoreService
{
    public async Task<IReadOnlyList<PageBindingUsage>> GetPageBindingUsagesAsync(PageBindingUsageFilter filter, CancellationToken cancellationToken = default)
    {
        var usages = await repository.GetPageBindingUsagesAsync(filter, cancellationToken);
        return usages.ToEntities();
    }

    public async Task CreateAsync(IReadOnlyList<PageBindingUsage> usages, CancellationToken cancellationToken = default)
    {
        if (usages.Count == 0)
            return;

        await repository.AddAsync(usages.Select(u => u.ToDao()).ToList(), cancellationToken);
    }

    public async Task<int> DeleteAsync(Guid pageId, CancellationToken cancellationToken = default) =>
        await repository.DeleteAsync(new PageBindingUsageFilter { PageId = pageId }, cancellationToken);
}

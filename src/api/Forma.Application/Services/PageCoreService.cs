using Forma.Application.Entities;
using Forma.Application.Mappers;

using Forma.Infrastructure.Repositories.Pages;

namespace Forma.Application.Services;

public interface IPageCoreService
{
    Task<IReadOnlyList<Page>> GetPagesAsync(PageFilter filter, CancellationToken cancellationToken = default);

    /// <summary>The first page matching <paramref name="filter"/>, or <c>null</c> when there is none.</summary>
    Task<Page?> GetPageAsync(PageFilter filter, CancellationToken cancellationToken = default);
}

public class PageCoreService(IPageRepository pageRepository) : IPageCoreService
{
    public async Task<IReadOnlyList<Page>> GetPagesAsync(PageFilter filter, CancellationToken cancellationToken = default)
    {
        var pages = await pageRepository.GetPagesAsync(filter, cancellationToken);
        return pages.ToEntities();
    }

    public async Task<Page?> GetPageAsync(PageFilter filter, CancellationToken cancellationToken = default)
    {
        var pages = await pageRepository.GetPagesAsync(filter, cancellationToken);
        return pages.FirstOrDefault()?.ToEntity();
    }
}

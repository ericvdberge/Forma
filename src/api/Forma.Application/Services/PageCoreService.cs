using Forma.Application.Entities;
using Forma.Application.Mappers;

using Forma.Infrastructure.Repositories.Pages;

namespace Forma.Application.Services;

public interface IPageCoreService
{
    Task<IReadOnlyList<Page>> GetPagesAsync(Guid websiteId, CancellationToken cancellationToken = default);
}

public class PageCoreService(IPageRepository pageRepository) : IPageCoreService
{
    public async Task<IReadOnlyList<Page>> GetPagesAsync(Guid websiteId, CancellationToken cancellationToken = default)
    {
        var filter = new PageFilter { WebsiteId = websiteId };
        var pages = await pageRepository.GetPagesAsync(filter, cancellationToken);
        return pages.ToEntities();
    }
}

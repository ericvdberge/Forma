using Forma.Application.Entities.Pages;
using Forma.Application.Repositories.Pages;

namespace Forma.Application.Services.Pages;

public interface IPageCoreService
{
    Task<IReadOnlyList<Page>> GetPagesAsync(Guid websiteId, CancellationToken cancellationToken = default);
}

public class PageCoreService(IPageRepository pageRepository) : IPageCoreService
{
    public Task<IReadOnlyList<Page>> GetPagesAsync(Guid websiteId, CancellationToken cancellationToken = default)
    {
        var filter = new PageFilter { WebsiteId = websiteId };
        return pageRepository.GetPagesAsync(filter, cancellationToken);
    }
}

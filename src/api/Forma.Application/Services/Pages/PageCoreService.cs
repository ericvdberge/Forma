using Forma.Application.Entities.Pages;
using Forma.Application.Filters.Pages;
using Forma.Application.Repositories.Pages;

namespace Forma.Application.Services.Pages;

public interface IPageCoreService
{
    Task<IReadOnlyList<Page>> GetPagesAsync(PageFilter filter, CancellationToken cancellationToken = default);
}

public class PageCoreService(IPageRepository pageRepository) : IPageCoreService
{
    public Task<IReadOnlyList<Page>> GetPagesAsync(PageFilter filter, CancellationToken cancellationToken = default) =>
        pageRepository.GetPagesAsync(filter, cancellationToken);
}

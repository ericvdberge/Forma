using Forma.Infrastructure.DataAccessObjects.Pages;

namespace Forma.Infrastructure.Repositories.Pages;

public interface IPageRepository
{
    Task<IReadOnlyList<PageDao>> GetPagesAsync(PageFilter filter, CancellationToken cancellationToken = default);
}

using Forma.Infrastructure.Daos;

namespace Forma.Infrastructure.Repositories.Pages;

public interface IPageRepository
{
    Task<IReadOnlyList<PageDao>> GetPagesAsync(PageFilter filter, CancellationToken cancellationToken = default);
}

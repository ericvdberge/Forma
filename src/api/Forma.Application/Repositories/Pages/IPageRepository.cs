using Forma.Application.Entities.Pages;
using Forma.Application.Filters.Pages;

namespace Forma.Application.Repositories.Pages;

public interface IPageRepository
{
    Task<IReadOnlyList<Page>> GetPagesAsync(PageFilter filter, CancellationToken cancellationToken = default);
}

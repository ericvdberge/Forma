using Forma.Application.Entities.Pages;

namespace Forma.Application.Repositories.Pages;

public interface IPageRepository
{
    Task<IReadOnlyList<Page>> GetPagesAsync(PageFilter filter, CancellationToken cancellationToken = default);
}

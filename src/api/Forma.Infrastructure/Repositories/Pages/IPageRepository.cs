using Forma.Infrastructure.Daos;

namespace Forma.Infrastructure.Repositories.Pages;

public interface IPageRepository
{
    Task<IReadOnlyList<PageDao>> GetPagesAsync(PageFilter filter, CancellationToken cancellationToken = default);

    Task AddAsync(PageDao page, CancellationToken cancellationToken = default);

    Task UpdateAsync(PageDao page, CancellationToken cancellationToken = default);

    /// <summary>Deletes the page and its content. Returns <c>false</c> when it does not exist.</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

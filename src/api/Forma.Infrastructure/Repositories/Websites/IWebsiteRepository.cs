using Forma.Infrastructure.DataAccessObjects.Websites;

namespace Forma.Infrastructure.Repositories.Websites;

public interface IWebsiteRepository
{
    Task<IReadOnlyList<WebsiteDao>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}

using Forma.Application.Entities.Websites;

namespace Forma.Application.Repositories.Websites;

public interface IWebsiteRepository
{
    Task<IReadOnlyList<Website>> GetAllAsync(CancellationToken cancellationToken = default);
}

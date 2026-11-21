using Forma.Infrastructure.Daos;

namespace Forma.Infrastructure.Repositories.Websites;

public interface IWebsiteRepository
{
    Task<IReadOnlyList<WebsiteDao>> GetWebsitesAsync(WebsiteFilter filter, CancellationToken cancellationToken = default);
}

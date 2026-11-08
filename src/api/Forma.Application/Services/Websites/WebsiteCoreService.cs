using Forma.Application.Entities.Websites;
using Forma.Application.Repositories.Websites;

namespace Forma.Application.Services.Websites;

public interface IWebsiteCoreService
{
    Task<IReadOnlyList<Website>> GetWebsitesAsync(CancellationToken cancellationToken = default);
}

public class WebsiteCoreService(IWebsiteRepository websiteRepository) : IWebsiteCoreService
{
    public Task<IReadOnlyList<Website>> GetWebsitesAsync(CancellationToken cancellationToken = default) =>
        websiteRepository.GetAllAsync(cancellationToken);
}

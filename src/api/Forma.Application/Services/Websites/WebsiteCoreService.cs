using Forma.Application.Entities.Websites;
using Forma.Application.Mappers.Websites;
using Forma.Infrastructure.Repositories.Websites;

namespace Forma.Application.Services.Websites;

public interface IWebsiteCoreService
{
    Task<IReadOnlyList<Website>> GetWebsitesAsync(CancellationToken cancellationToken = default);
}

public class WebsiteCoreService(IWebsiteRepository websiteRepository) : IWebsiteCoreService
{
    public async Task<IReadOnlyList<Website>> GetWebsitesAsync(CancellationToken cancellationToken = default)
    {
        var websites = await websiteRepository.GetAllAsync(cancellationToken);
        return websites.ToEntities();
    }
}

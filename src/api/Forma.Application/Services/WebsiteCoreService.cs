using Forma.Application.Entities;
using Forma.Application.Mappers;

using Forma.Infrastructure.Repositories.Websites;

namespace Forma.Application.Services;

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

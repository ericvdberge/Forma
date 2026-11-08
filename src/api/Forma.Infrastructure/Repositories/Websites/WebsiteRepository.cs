using Forma.Application.Entities.Websites;
using Forma.Application.Repositories.Websites;
using Forma.Infrastructure.Mappers.Websites;
using Forma.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Forma.Infrastructure.Repositories.Websites;

public class WebsiteRepository(FormaDbContext db) : IWebsiteRepository
{
    public async Task<IReadOnlyList<Website>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var websites = await db.Websites
            .AsNoTracking()
            .OrderByDescending(w => w.UpdatedAt)
            .ToListAsync(cancellationToken);

        return websites.Select(w => w.ToEntity()).ToList();
    }
}

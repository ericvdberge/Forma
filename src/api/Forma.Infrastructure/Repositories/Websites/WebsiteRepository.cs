using Forma.Infrastructure.Daos;
using Forma.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Forma.Infrastructure.Repositories.Websites;

public class WebsiteRepository(FormaDbContext db) : IWebsiteRepository
{
    public async Task<IReadOnlyList<WebsiteDao>> GetWebsitesAsync(WebsiteFilter filter, CancellationToken cancellationToken = default) =>
        await db.Websites
            .AsNoTracking()
            .ApplyFilter(filter)
            .OrderByDescending(w => w.UpdatedAt)
            .ToListAsync(cancellationToken);
}

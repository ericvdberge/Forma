using Forma.Infrastructure.DataAccessObjects.Websites;
using Forma.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Forma.Infrastructure.Repositories.Websites;

public class WebsiteRepository(FormaDbContext db) : IWebsiteRepository
{
    public async Task<IReadOnlyList<WebsiteDao>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await db.Websites
            .AsNoTracking()
            .OrderByDescending(w => w.UpdatedAt)
            .ToListAsync(cancellationToken);
}

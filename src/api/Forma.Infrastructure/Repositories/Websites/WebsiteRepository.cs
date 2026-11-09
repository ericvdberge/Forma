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

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) =>
        await db.Websites.AnyAsync(w => w.Id == id, cancellationToken);
}

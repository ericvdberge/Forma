using Forma.Infrastructure.Daos;
using Forma.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Forma.Infrastructure.Repositories.Pages;

public class PageRepository(FormaDbContext db) : IPageRepository
{
    public async Task<IReadOnlyList<PageDao>> GetPagesAsync(PageFilter filter, CancellationToken cancellationToken = default) =>
        await db.Pages
            .AsNoTracking()
            .ApplyFilter(filter)
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync(cancellationToken);
}

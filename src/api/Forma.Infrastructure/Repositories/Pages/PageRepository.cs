using Forma.Application.Entities.Pages;
using Forma.Application.Repositories.Pages;
using Forma.Infrastructure.Mappers.Pages;
using Forma.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Forma.Infrastructure.Repositories.Pages;

public class PageRepository(FormaDbContext db) : IPageRepository
{
    public async Task<IReadOnlyList<Page>> GetPagesAsync(PageFilter filter, CancellationToken cancellationToken = default)
    {
        var query = db.Pages.AsNoTracking();

        if (filter.WebsiteId is { } websiteId)
        {
            query = query.Where(p => p.WebsiteId == websiteId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Title))
        {
            var pattern = $"%{filter.Title.Trim()}%";
            query = query.Where(p => EF.Functions.Like(p.Title, pattern));
        }

        if (!string.IsNullOrWhiteSpace(filter.Slug))
        {
            var slug = filter.Slug.Trim();
            query = query.Where(p => p.Slug == slug);
        }

        var pages = await query
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync(cancellationToken);

        return pages.Select(p => p.ToEntity()).ToList();
    }
}

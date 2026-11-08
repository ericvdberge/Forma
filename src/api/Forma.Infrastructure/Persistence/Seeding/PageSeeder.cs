using Forma.Infrastructure.DataAccessObjects.Pages;
using Microsoft.EntityFrameworkCore;

namespace Forma.Infrastructure.Persistence.Seeding;

internal static class PageSeeder
{
    private static readonly (string Title, string Slug, PageDaoStatus Status)[] DefaultPages =
    [
        ("Home", "home", PageDaoStatus.Published),
        ("About", "about", PageDaoStatus.Published),
        ("Contact", "contact", PageDaoStatus.Draft),
    ];

    public static async Task SeedAsync(FormaDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.Pages.AnyAsync(cancellationToken))
        {
            return;
        }

        var websiteIds = await db.Websites.Select(w => w.Id).ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;

        foreach (var websiteId in websiteIds)
        {
            db.Pages.AddRange(DefaultPages.Select((page, index) => new PageDao
            {
                Id = Guid.CreateVersion7(),
                WebsiteId = websiteId,
                Title = page.Title,
                Slug = page.Slug,
                Status = page.Status,
                CreatedAt = now.AddDays(-7),
                UpdatedAt = now.AddHours(-index),
            }));
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}

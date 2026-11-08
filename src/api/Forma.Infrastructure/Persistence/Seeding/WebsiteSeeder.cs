using Forma.Application.Entities.Websites;
using Forma.Infrastructure.DataAccessObjects.Websites;
using Microsoft.EntityFrameworkCore;

namespace Forma.Infrastructure.Persistence.Seeding;

internal static class WebsiteSeeder
{
    public static async Task SeedAsync(FormaDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.Websites.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = DateTime.UtcNow;

        db.Websites.AddRange(
            new WebsiteDao
            {
                Id = Guid.CreateVersion7(),
                Name = "Forma Studio",
                Domain = "studio.forma.dev",
                Description = "A modern website built with Forma.",
                Status = WebsiteStatus.Live,
                CreatedAt = now.AddDays(-30),
                UpdatedAt = now.AddMinutes(-2),
            },
            new WebsiteDao
            {
                Id = Guid.CreateVersion7(),
                Name = "Agency Pro",
                Domain = "agency.forma.dev",
                Description = "Case studies and services for a small design agency.",
                Status = WebsiteStatus.Draft,
                CreatedAt = now.AddDays(-14),
                UpdatedAt = now.AddHours(-3),
            },
            new WebsiteDao
            {
                Id = Guid.CreateVersion7(),
                Name = "Portfolio",
                Domain = "portfolio.forma.dev",
                Description = "Selected work, 2019 – today.",
                Status = WebsiteStatus.Draft,
                CreatedAt = now.AddDays(-7),
                UpdatedAt = now.AddDays(-2),
            });

        await db.SaveChangesAsync(cancellationToken);
    }
}

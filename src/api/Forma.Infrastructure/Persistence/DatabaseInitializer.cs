using Forma.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Forma.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    /// <summary>Applies pending migrations and, when <paramref name="seedSampleData"/> is set, adds sample websites and pages to an empty database.</summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, bool seedSampleData, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FormaDbContext>();

        await db.Database.MigrateAsync(cancellationToken);
        if (!seedSampleData)
        {
            return;
        }

        await WebsiteSeeder.SeedAsync(db, cancellationToken);
        await PageSeeder.SeedAsync(db, cancellationToken);
    }
}

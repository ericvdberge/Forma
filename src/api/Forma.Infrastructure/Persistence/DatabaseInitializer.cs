using Forma.Infrastructure.DataAccessObjects.Users;
using Forma.Infrastructure.Persistence.Seeding;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Forma.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    /// <summary>Applies pending migrations and seeds development data. Development only.</summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FormaDbContext>();

        await db.Database.MigrateAsync(cancellationToken);
        await WebsiteSeeder.SeedAsync(db, cancellationToken);
        await PageSeeder.SeedAsync(db, cancellationToken);
        await UserSeeder.SeedAsync(
            scope.ServiceProvider.GetRequiredService<UserManager<UserDao>>(),
            scope.ServiceProvider.GetRequiredService<IConfiguration>());
    }
}

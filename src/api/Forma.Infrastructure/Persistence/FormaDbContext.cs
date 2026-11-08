using Forma.Infrastructure.DataAccessObjects.Pages;
using Forma.Infrastructure.DataAccessObjects.Users;
using Forma.Infrastructure.DataAccessObjects.Websites;
using Forma.Infrastructure.Persistence.Converters;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Forma.Infrastructure.Persistence;

/// <summary>
/// Forma's database. Inherits the ASP.NET Core Identity user tables (users, claims, logins, tokens);
/// roles aren't used yet.
/// </summary>
public class FormaDbContext(DbContextOptions<FormaDbContext> options) : IdentityUserContext<UserDao, Guid>(options)
{
    public DbSet<WebsiteDao> Websites => Set<WebsiteDao>();
    public DbSet<PageDao> Pages => Set<PageDao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Identity maps its own tables first; Forma's configurations then rename them and add the rest.
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FormaDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }
}

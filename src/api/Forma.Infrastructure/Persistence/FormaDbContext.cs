using Forma.Infrastructure.Daos;
using Forma.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;

namespace Forma.Infrastructure.Persistence;

public class FormaDbContext(DbContextOptions<FormaDbContext> options) : DbContext(options)
{
    public DbSet<WebsiteDao> Websites => Set<WebsiteDao>();
    public DbSet<PageDao> Pages => Set<PageDao>();
    public DbSet<MediaItemDao> Media => Set<MediaItemDao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FormaDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }
}

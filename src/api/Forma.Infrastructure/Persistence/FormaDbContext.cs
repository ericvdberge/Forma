using Forma.Infrastructure.Daos;
using Forma.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;

namespace Forma.Infrastructure.Persistence;

public class FormaDbContext(DbContextOptions<FormaDbContext> options) : DbContext(options)
{
    public DbSet<WebsiteDao> Websites => Set<WebsiteDao>();
    public DbSet<PageDao> Pages => Set<PageDao>();
    public DbSet<MediaItemDao> Media => Set<MediaItemDao>();
    public DbSet<PageContentDao> PageContents => Set<PageContentDao>();
    public DbSet<CollectionDao> Collections => Set<CollectionDao>();
    public DbSet<CollectionItemDao> CollectionItems => Set<CollectionItemDao>();
    public DbSet<PageBindingUsageDao> PageBindingUsages => Set<PageBindingUsageDao>();
    public DbSet<ScheduledPublishDao> ScheduledPublishes => Set<ScheduledPublishDao>();
    public DbSet<ScheduledPublishEntryDao> ScheduledPublishEntries => Set<ScheduledPublishEntryDao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FormaDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }
}

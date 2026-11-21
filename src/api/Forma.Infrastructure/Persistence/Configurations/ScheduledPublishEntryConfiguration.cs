using Forma.Infrastructure.Daos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Forma.Infrastructure.Persistence.Configurations;

public class ScheduledPublishEntryConfiguration : IEntityTypeConfiguration<ScheduledPublishEntryDao>
{
    /// <summary>An entry is exactly one collection, page or item.</summary>
    private const string OneEntityCheck =
        """("CollectionId" IS NOT NULL) + ("PageId" IS NOT NULL) + ("CollectionItemId" IS NOT NULL) = 1""";

    public void Configure(EntityTypeBuilder<ScheduledPublishEntryDao> builder)
    {
        builder.ToTable("ScheduledPublishEntries", t => t.HasCheckConstraint("CK_ScheduledPublishEntries_OneEntity", OneEntityCheck));

        builder.HasKey(e => e.Id);

        builder.HasOne(e => e.ScheduledPublish)
            .WithMany()
            .HasForeignKey(e => e.ScheduledPublishId)
            .OnDelete(DeleteBehavior.Cascade);

        // Deleting a collection, page or item takes it out of its scheduled publish.
        builder.HasOne(e => e.Collection)
            .WithMany()
            .HasForeignKey(e => e.CollectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Page)
            .WithMany()
            .HasForeignKey(e => e.PageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.CollectionItem)
            .WithMany()
            .HasForeignKey(e => e.CollectionItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.ScheduledPublishId);
        builder.HasIndex(e => e.CollectionId);
        builder.HasIndex(e => e.PageId);
        builder.HasIndex(e => e.CollectionItemId);
    }
}

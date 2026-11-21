using Forma.Infrastructure.Daos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Forma.Infrastructure.Persistence.Configurations;

public class CollectionItemConfiguration : IEntityTypeConfiguration<CollectionItemDao>
{
    public void Configure(EntityTypeBuilder<CollectionItemDao> builder)
    {
        builder.ToTable("CollectionItems");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Slug).HasMaxLength(200).IsRequired();
        builder.Property(i => i.Values).IsRequired();
        builder.Property(i => i.DraftSlug).HasMaxLength(200);

        builder.HasOne(i => i.Collection)
            .WithMany()
            .HasForeignKey(i => i.CollectionId)
            .OnDelete(DeleteBehavior.Cascade);

        // A slug is unique within a collection; this index also serves the CollectionId filter.
        builder.HasIndex(i => new { i.CollectionId, i.Slug }).IsUnique();
    }
}

using Forma.Infrastructure.Daos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Forma.Infrastructure.Persistence.Configurations;

public class PageBindingUsageConfiguration : IEntityTypeConfiguration<PageBindingUsageDao>
{
    public void Configure(EntityTypeBuilder<PageBindingUsageDao> builder)
    {
        builder.ToTable("PageBindingUsages");

        builder.HasKey(u => new { u.PageId, u.CollectionId, u.FieldId, u.Kind });

        builder.Property(u => u.FieldId).HasMaxLength(40).IsRequired();
        builder.Property(u => u.Kind).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(u => u.Page)
            .WithMany()
            .HasForeignKey(u => u.PageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(u => u.Collection)
            .WithMany()
            .HasForeignKey(u => u.CollectionId)
            .OnDelete(DeleteBehavior.Cascade);

        // "Used by…" in the field editor reads by collection.
        builder.HasIndex(u => new { u.CollectionId, u.FieldId });
    }
}

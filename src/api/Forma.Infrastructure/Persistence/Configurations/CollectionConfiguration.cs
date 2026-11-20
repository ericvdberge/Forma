using Forma.Infrastructure.Daos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Forma.Infrastructure.Persistence.Configurations;

public class CollectionConfiguration : IEntityTypeConfiguration<CollectionDao>
{
    public void Configure(EntityTypeBuilder<CollectionDao> builder)
    {
        builder.ToTable("Collections");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.Property(c => c.SingularName).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Slug).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Fields).IsRequired();

        builder.HasOne(c => c.Website)
            .WithMany()
            .HasForeignKey(c => c.WebsiteId)
            .OnDelete(DeleteBehavior.Cascade);

        // A slug is unique within a website; this index also serves the WebsiteId filter.
        builder.HasIndex(c => new { c.WebsiteId, c.Slug }).IsUnique();
    }
}

using Forma.Infrastructure.Daos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Forma.Infrastructure.Persistence.Configurations;

public class PageConfiguration : IEntityTypeConfiguration<PageDao>
{
    /// <summary>A page of its own has no collection; a detail page always has one.</summary>
    private const string KindCheck =
        """("Kind" = 'Static' AND "CollectionId" IS NULL) OR ("Kind" = 'CollectionTemplate' AND "CollectionId" IS NOT NULL)""";

    public void Configure(EntityTypeBuilder<PageDao> builder)
    {
        builder.ToTable("Pages", t => t.HasCheckConstraint("CK_Pages_Kind", KindCheck));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Title).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Slug).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Kind).HasConversion<string>().HasMaxLength(30);

        builder.HasOne(p => p.Website)
            .WithMany()
            .HasForeignKey(p => p.WebsiteId)
            .OnDelete(DeleteBehavior.Cascade);

        // A detail page is on its collection's website. Deleting a collection deletes its detail page.
        builder.HasOne(p => p.Collection)
            .WithMany()
            .HasForeignKey(p => new { p.WebsiteId, p.CollectionId })
            .HasPrincipalKey(c => new { c.WebsiteId, c.Id })
            .OnDelete(DeleteBehavior.Cascade);

        // A collection has at most one detail page.
        builder.HasIndex(p => p.CollectionId).IsUnique();

        // A slug is unique within a website; this index also serves the WebsiteId filter.
        builder.HasIndex(p => new { p.WebsiteId, p.Slug }).IsUnique();
    }
}

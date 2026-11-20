using Forma.Infrastructure.Daos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Forma.Infrastructure.Persistence.Configurations;

public class PageDocumentConfiguration : IEntityTypeConfiguration<PageDocumentDao>
{
    public void Configure(EntityTypeBuilder<PageDocumentDao> builder)
    {
        builder.ToTable("PageDocuments");

        // One row per version (draft, published) of a page; the key also serves lookups by page.
        builder.HasKey(d => new { d.PageId, d.Kind });

        builder.Property(d => d.Kind).HasConversion<string>().HasMaxLength(20);
        builder.Property(d => d.Content).IsRequired();
        builder.Property(d => d.ContentHash).HasMaxLength(64).IsRequired();

        builder.HasOne(d => d.Page)
            .WithMany()
            .HasForeignKey(d => d.PageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

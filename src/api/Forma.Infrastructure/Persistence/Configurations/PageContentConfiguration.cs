using Forma.Infrastructure.Daos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Forma.Infrastructure.Persistence.Configurations;

public class PageContentConfiguration : IEntityTypeConfiguration<PageContentDao>
{
    public void Configure(EntityTypeBuilder<PageContentDao> builder)
    {
        builder.ToTable("PageContents");

        // A page has one content row; the key also serves lookups by page.
        builder.HasKey(c => c.PageId);

        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.Content).IsRequired();

        builder.HasOne(c => c.Page)
            .WithOne()
            .HasForeignKey<PageContentDao>(c => c.PageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

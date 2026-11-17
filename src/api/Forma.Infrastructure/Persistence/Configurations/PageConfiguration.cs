using Forma.Infrastructure.Daos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Forma.Infrastructure.Persistence.Configurations;

public class PageConfiguration : IEntityTypeConfiguration<PageDao>
{
    public void Configure(EntityTypeBuilder<PageDao> builder)
    {
        builder.ToTable("Pages");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Title).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Slug).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(p => p.Website)
            .WithMany()
            .HasForeignKey(p => p.WebsiteId)
            .OnDelete(DeleteBehavior.Cascade);

        // A slug is unique within a website; this index also serves the WebsiteId filter.
        builder.HasIndex(p => new { p.WebsiteId, p.Slug }).IsUnique();
    }
}

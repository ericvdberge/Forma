using Forma.Infrastructure.DataAccessObjects.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Forma.Infrastructure.Persistence.Configurations.Media;

public class MediaItemConfiguration : IEntityTypeConfiguration<MediaItemDao>
{
    public void Configure(EntityTypeBuilder<MediaItemDao> builder)
    {
        builder.ToTable("Media");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.FileName).HasMaxLength(255).IsRequired();
        builder.Property(m => m.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(m => m.FilePath).HasMaxLength(500).IsRequired();
        builder.Property(m => m.ThumbnailPath).HasMaxLength(500).IsRequired();

        builder.HasOne(m => m.Website)
            .WithMany()
            .HasForeignKey(m => m.WebsiteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => m.WebsiteId);
    }
}

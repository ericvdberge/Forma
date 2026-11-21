using Forma.Infrastructure.Daos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Forma.Infrastructure.Persistence.Configurations;

public class ScheduledPublishConfiguration : IEntityTypeConfiguration<ScheduledPublishDao>
{
    public void Configure(EntityTypeBuilder<ScheduledPublishDao> builder)
    {
        builder.ToTable("ScheduledPublishes");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(p => p.Website)
            .WithMany()
            .HasForeignKey(p => p.WebsiteId)
            .OnDelete(DeleteBehavior.Cascade);

        // The worker reads the pending publishes that are due.
        builder.HasIndex(p => new { p.Status, p.PublishAt });

        builder.HasIndex(p => p.WebsiteId);
    }
}

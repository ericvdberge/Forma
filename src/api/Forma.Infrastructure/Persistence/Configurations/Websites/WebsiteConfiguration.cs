using Forma.Infrastructure.DataAccessObjects.Websites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Forma.Infrastructure.Persistence.Configurations.Websites;

public class WebsiteConfiguration : IEntityTypeConfiguration<WebsiteDao>
{
    public void Configure(EntityTypeBuilder<WebsiteDao> builder)
    {
        builder.ToTable("Websites");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Name).HasMaxLength(200).IsRequired();
        builder.Property(w => w.Domain).HasMaxLength(253).IsRequired();
        builder.Property(w => w.Description).HasMaxLength(2000);
        builder.Property(w => w.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(w => w.Domain).IsUnique();
    }
}

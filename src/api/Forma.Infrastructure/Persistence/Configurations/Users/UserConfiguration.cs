using Forma.Infrastructure.DataAccessObjects.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Forma.Infrastructure.Persistence.Configurations.Users;

public class UserConfiguration : IEntityTypeConfiguration<UserDao>
{
    public void Configure(EntityTypeBuilder<UserDao> builder)
    {
        builder.ToTable("Users");

        builder.Property(u => u.DisplayName).HasMaxLength(200);
    }
}

public class UserClaimConfiguration : IEntityTypeConfiguration<IdentityUserClaim<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserClaim<Guid>> builder) => builder.ToTable("UserClaims");
}

public class UserLoginConfiguration : IEntityTypeConfiguration<IdentityUserLogin<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserLogin<Guid>> builder) => builder.ToTable("UserLogins");
}

public class UserTokenConfiguration : IEntityTypeConfiguration<IdentityUserToken<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserToken<Guid>> builder) => builder.ToTable("UserTokens");
}

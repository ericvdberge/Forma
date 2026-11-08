using Forma.Infrastructure.DataAccessObjects.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace Forma.Infrastructure.Persistence.Seeding;

/// <summary>
/// Creates the development admin account from the <c>Seed:Admin</c> configuration section.
/// Does nothing when no password is configured.
/// </summary>
internal static class UserSeeder
{
    public static async Task SeedAsync(UserManager<UserDao> userManager, IConfiguration configuration)
    {
        var section = configuration.GetSection("Seed:Admin");
        var userName = section["UserName"];
        var password = section["Password"];

        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password)
            || await userManager.FindByNameAsync(userName) is not null)
        {
            return;
        }

        var user = new UserDao
        {
            Id = Guid.CreateVersion7(),
            UserName = userName,
            DisplayName = section["DisplayName"] ?? userName,
            Email = section["Email"],
            CreatedAt = DateTime.UtcNow,
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = string.Join(" ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Could not seed the admin user '{userName}': {errors}");
        }
    }
}

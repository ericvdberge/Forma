using Microsoft.AspNetCore.Identity;

namespace Forma.Infrastructure.DataAccessObjects.Users;

/// <summary>
/// A user who can sign in to the Forma app. ASP.NET Core Identity owns the password hash,
/// lockout and security stamp columns; Forma only adds what it shows in the app.
/// </summary>
public class UserDao : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

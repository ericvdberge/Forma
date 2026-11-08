using System.Threading.RateLimiting;
using Forma.Infrastructure.DataAccessObjects.Users;
using Forma.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;

namespace Forma.Api.Authentication;

public static class AuthenticationExtensions
{
    /// <summary>Rate-limiting policy applied to the anonymous auth endpoints (login, refresh).</summary>
    public const string AuthRateLimitPolicy = "auth";

    /// <summary>
    /// Username/password sign-in with ASP.NET Core Identity, issuing short-lived bearer access tokens
    /// and longer-lived refresh tokens. Every endpoint requires a signed-in user unless it opts out
    /// with <c>AllowAnonymous()</c>.
    /// </summary>
    public static IServiceCollection AddFormaAuthentication(this IServiceCollection services)
    {
        services
            .AddIdentityApiEndpoints<UserDao>(options =>
            {
                // NIST 800-63B: length matters, composition rules don't.
                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

                options.User.RequireUniqueEmail = false;
            })
            .AddEntityFrameworkStores<FormaDbContext>();

        services.Configure<BearerTokenOptions>(IdentityConstants.BearerScheme, options =>
        {
            options.BearerTokenExpiration = TimeSpan.FromMinutes(15);
            options.RefreshTokenExpiration = TimeSpan.FromDays(14);
        });

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build());

        // Slow down password guessing across accounts; Identity's lockout only protects one account at a time.
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(AuthRateLimitPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
        });

        return services;
    }
}

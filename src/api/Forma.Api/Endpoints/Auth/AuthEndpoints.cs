using System.Security.Claims;
using Forma.Api.Abstractions;
using Forma.Api.Authentication;
using Forma.Api.Dtos.Auth;
using Forma.Infrastructure.DataAccessObjects.Users;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.Extensions.Options;

namespace Forma.Api.Endpoints.Auth;

/// <summary>
/// Username/password sign-in. Login and refresh return an <see cref="AccessTokenResponse"/>:
/// a 15-minute bearer access token plus a 14-day refresh token. There is no public sign-up;
/// accounts are created by an admin (and seeded in development).
/// </summary>
public class AuthEndpoints : IEndpoints
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth").WithTags("Auth");

        group.MapPost("/login", LoginAsync)
            .WithName("Login")
            .AllowAnonymous()
            .RequireRateLimiting(AuthenticationExtensions.AuthRateLimitPolicy);

        group.MapPost("/refresh", RefreshAsync)
            .WithName("RefreshToken")
            .AllowAnonymous()
            .RequireRateLimiting(AuthenticationExtensions.AuthRateLimitPolicy);

        group.MapPost("/logout", LogoutAsync).WithName("Logout");

        group.MapGet("/me", GetCurrentUserAsync).WithName("GetCurrentUser");
    }

    private static async Task<Results<Ok<AccessTokenResponse>, EmptyHttpResult, ProblemHttpResult>> LoginAsync(
        LoginRequestDto request,
        SignInManager<UserDao> signInManager)
    {
        signInManager.AuthenticationScheme = IdentityConstants.BearerScheme;

        var result = await signInManager.PasswordSignInAsync(
            request.UserName, request.Password, isPersistent: false, lockoutOnFailure: true);

        // One message for wrong password, unknown user and locked-out account, so the response
        // doesn't reveal which usernames exist.
        if (!result.Succeeded)
        {
            return TypedResults.Problem("Invalid username or password.", statusCode: StatusCodes.Status401Unauthorized);
        }

        // The bearer handler has already written the AccessTokenResponse body.
        return TypedResults.Empty;
    }

    private static async Task<Results<Ok<AccessTokenResponse>, SignInHttpResult, ChallengeHttpResult>> RefreshAsync(
        RefreshRequestDto request,
        SignInManager<UserDao> signInManager,
        IOptionsMonitor<BearerTokenOptions> bearerTokenOptions,
        TimeProvider timeProvider)
    {
        var protector = bearerTokenOptions.Get(IdentityConstants.BearerScheme).RefreshTokenProtector;
        var ticket = protector.Unprotect(request.RefreshToken);

        // Expired, tampered with, or revoked (the security stamp changes on logout and password change).
        if (ticket?.Properties.ExpiresUtc is not { } expiresUtc
            || timeProvider.GetUtcNow() >= expiresUtc
            || await signInManager.ValidateSecurityStampAsync(ticket.Principal) is not { } user)
        {
            return TypedResults.Challenge();
        }

        var principal = await signInManager.CreateUserPrincipalAsync(user);
        return TypedResults.SignIn(principal, authenticationScheme: IdentityConstants.BearerScheme);
    }

    /// <summary>
    /// Revokes every refresh token the user holds (on all devices). Access tokens already issued stay
    /// valid until they expire, at most 15 minutes; the client discards its own.
    /// </summary>
    private static async Task<NoContent> LogoutAsync(ClaimsPrincipal principal, UserManager<UserDao> userManager)
    {
        if (await userManager.GetUserAsync(principal) is { } user)
        {
            await userManager.UpdateSecurityStampAsync(user);
        }

        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<CurrentUserDto>, UnauthorizedHttpResult>> GetCurrentUserAsync(
        ClaimsPrincipal principal,
        UserManager<UserDao> userManager)
    {
        if (await userManager.GetUserAsync(principal) is not { } user)
        {
            return TypedResults.Unauthorized();
        }

        return TypedResults.Ok(new CurrentUserDto(user.Id, user.UserName!, user.DisplayName, user.Email));
    }
}

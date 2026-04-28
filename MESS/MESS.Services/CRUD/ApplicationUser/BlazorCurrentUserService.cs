using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace MESS.Services.CRUD.ApplicationUser;

/// <summary>
/// Blazor Server implementation of <see cref="ICurrentUserService"/>.
/// 
/// Resolves the current user from the Blazor authentication state and
/// exposes identity information in a simplified, domain-friendly format.
/// </summary>
/// <remarks>
/// This implementation:
/// - Uses AuthenticationStateProvider as the source of truth
/// - Extracts identity from ClaimsPrincipal
/// - Provides safe fallbacks for unauthenticated or background execution contexts
/// 
/// IMPORTANT:
/// AuthenticationStateProvider is UI-scoped.
/// </remarks>
public sealed class BlazorCurrentUserService : ICurrentUserService
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;

    private const string SYSTEM_USER_ID = "system";
    private const string SYSTEM_USER_NAME = "MESS System";
    private const string SYSTEM_EMAIL = "system@mess.local";

    /// <summary>
    /// Initializes a new instance of the <see cref="BlazorCurrentUserService"/> class.
    /// </summary>
    /// <param name="authenticationStateProvider">
    /// Provides access to the current Blazor authentication state used to resolve
    /// the identity of the active user.
    /// </param>
    public BlazorCurrentUserService(AuthenticationStateProvider authenticationStateProvider)
    {
        _authenticationStateProvider = authenticationStateProvider;
    }

    /// <inheritdoc />
    public string GetUserId()
    {
        var user = GetClaimsPrincipal();

        return user.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? SYSTEM_USER_ID;
    }

    /// <inheritdoc />
    public string GetUserName()
    {
        var user = GetClaimsPrincipal();

        // Prefer Name claim, then fallback to identity name
        return user.FindFirstValue(ClaimTypes.Name)
               ?? user.Identity?.Name
               ?? SYSTEM_USER_NAME;
    }

    /// <inheritdoc />
    public string GetEmail()
    {
        var user = GetClaimsPrincipal();

        return user.FindFirstValue(ClaimTypes.Email)
               ?? SYSTEM_EMAIL;
    }

    /// <summary>
    /// Resolves the current ClaimsPrincipal from the authentication state.
    /// Falls back to an empty principal if no user is available.
    /// </summary>
    private ClaimsPrincipal GetClaimsPrincipal()
    {
        try
        {
            // In Blazor Server, this is safe to call synchronously
            var task = _authenticationStateProvider.GetAuthenticationStateAsync();
            var state = task.GetAwaiter().GetResult();

            return state.User;
        }
        catch
        {
            // If called outside a valid Blazor circuit (e.g., background job),
            // return an anonymous principal so fallback values are used.
            return new ClaimsPrincipal(new ClaimsIdentity());
        }
    }
}
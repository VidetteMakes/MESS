namespace MESS.Services.CRUD.ApplicationUser;

/// <summary>
/// Provides access to the identity of the user currently executing an operation.
/// 
/// This abstraction decouples domain and infrastructure services from UI-specific
/// authentication mechanisms such as ASP.NET Core Identity or Blazor's
/// AuthenticationStateProvider.
/// 
/// It is primarily used for auditing, logging, and attribution purposes
/// (e.g., Git commit authorship, production log tracking).
/// </summary>
/// <remarks>
/// DESIGN GOALS:
/// - Hide authentication framework details from domain services
/// - Provide a consistent "actor identity" for all operations
/// - Support both interactive (user-driven) and background/system execution contexts
/// 
/// IMPLEMENTATION NOTES:
/// - In Blazor apps, implementations typically resolve identity via
///   AuthenticationStateProvider.
/// - In background services, a system user or service identity may be used.
/// </remarks>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the unique identifier of the current user.
    /// </summary>
    /// <returns>
    /// A string representing the user's unique ID, or a system identifier
    /// if executed outside a user context.
    /// </returns>
    string GetUserId();

    /// <summary>
    /// Gets the display name or username of the current user.
    /// </summary>
    /// <returns>
    /// The user's display name, or a fallback system name if unavailable.
    /// </returns>
    string GetUserName();

    /// <summary>
    /// Gets the email address of the current user, if available.
    /// </summary>
    /// <returns>
    /// The user's email address, or null if not present or not applicable.
    /// </returns>
    string GetEmail();
}
using MESS.Services.CRUD.ApplicationUser;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace MESS.Blazor.Controllers.Auth;

/// <summary>
/// The `AuthController` class handles authentication-related operations,
/// such as user login and logout.
/// </summary>
[ApiController]
[Route("api/auth/")]
public class AuthController : ControllerBase
{
    private readonly IApplicationUserService _applicationUserService;
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthController"/> class.
    /// </summary>
    /// <param name="applicationUserService">
    /// The service responsible for handling application user operations.
    /// </param>
    public AuthController(IApplicationUserService applicationUserService)
    {
        _applicationUserService = applicationUserService;
    }

    /// <summary>
    /// Handles the login process for a user.
    /// </summary>
    /// <param name="email">The email address of the user attempting to log in.</param>
    /// <returns>
    /// A redirection to the production log page if login is successful, 
    /// or back to the login page if unsuccessful or an error occurs.
    /// </returns>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromForm] string email)
    {
        try
        {
            var result = await _applicationUserService.SignInAsync(email);
            if (result)
            {
                Log.Information("User successfully logged in: {Email}", email);
                return Redirect("/production-log");
            }
            
            Log.Information("Unsuccessful sign-in attempt");
            return Redirect("/auth/Login");
        }
        catch (Exception ex)
        {
            // Pass exception to Serilog for stack trace and structured properties (not only ex.Message).
            Log.Warning(ex, "Login failed for submitted identity {Email}", email);
            return Redirect("/auth/Login");
        }
    }

    /// <summary>
    /// Logs out the currently authenticated user.
    /// </summary>
    /// <returns>
    /// A redirection to the root URL ("/") after the user is logged out.
    /// </returns>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _applicationUserService.SignOutAsync();
        return Redirect("/");
    }
}
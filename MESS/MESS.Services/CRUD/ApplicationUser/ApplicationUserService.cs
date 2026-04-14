using System.Transactions;
using MESS.Data.Context;
using MESS.Services.Files.ApplicationUsers;
using Microsoft.AspNetCore.Identity;
using Serilog;

namespace MESS.Services.CRUD.ApplicationUser;
using Data.Models;
using Microsoft.EntityFrameworkCore;

/// <inheritdoc cref="IApplicationUserService"/>
public class ApplicationUserService : IApplicationUserService
{
    private readonly ApplicationContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IApplicationUserFileService _fileService;

    const string DEFAULT_PASSWORD = "";
    const string DEFAULT_ROLE = "Operator";

    /// <inheritdoc cref="IApplicationUserService"/>
    public ApplicationUserService(ApplicationContext context, UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,  RoleManager<IdentityRole> roleManager, IApplicationUserFileService fileService)
    {
        _context = context;
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _fileService = fileService;
    }

    /// <inheritdoc />
    public async Task SignOutAsync()
    {
        try
        {
            await _signInManager.SignOutAsync();
        }
        catch (Exception e)
        {
            Log.Warning("Unable to SignOutAsync in ApplicationUserService: Exception thrown: {Exception}", e.ToString());
        }
    }
    
    /// <inheritdoc />
    public async Task<bool> SignInAsync(string username)
    {
        try
        {
            var user = await _userManager.FindByNameAsync(username);

            if (user != null) await _signInManager.SignInAsync(user, isPersistent: false);
            return true;
        }
        catch (Exception e)
        {
            Log.Warning("Unable to SignInAsync with UserName: {username} in ApplicationUserService: Exception thrown: {Exception}", username, e.ToString());
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<List<ApplicationUser>> GetUsersByRoleAsync(string roleName)
    {
        try
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);
            return usersInRole.ToList();
        }
        catch (Exception e)
        {
            Log.Warning("Unable to GetUsersByRoleAsync with RoleName: {roleName} in ApplicationUserService: Exception thrown: {Exception}", roleName, e.ToString());
            return [];
        }
    }

    /// <inheritdoc />
    public async Task<List<ApplicationUser>> GetAllAsync()
    {
        try
        {
            return await _context.Users.ToListAsync();
        }
        catch (Exception e)
        {
            Log.Warning("Unable to GetAllAsync in ApplicationUserService: Exception thrown: {Exception}", e.ToString());
            return [];
        }
    }

    /// <inheritdoc />
    public async Task<ApplicationUser?> GetByIdAsync(string id)
    {
        var applicationUser = await _context.Users.FindAsync(id);
        if (applicationUser != null)
        {
            Log.Information("Retrieved Application User: {ApplicationUser}", applicationUser.ToString());
        }
        else
        {
            Log.Information("Unable to retrieve Application User with ID: {userId}", id);
        }
        return applicationUser;
    }

    /// <inheritdoc />
    public async Task<bool?> IsNewUser(ApplicationUser user)
    {
        try
        {
            if (string.IsNullOrEmpty(user.UserName) && string.IsNullOrEmpty(user.Email))
            {
                return null;
            }
            
            _context.ChangeTracker.Clear();

            var existingUser = await _context.Users.FindAsync(user.Id);

            return existingUser == null;
        }
        catch (Exception e)
        {
            Log.Warning("Unable to determine if user is new: {Exception}", e.ToString());
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<ApplicationUser?> GetByLastNameAsync(string lastName)
    {
        try
        {
            var applicationUser = await _context.Users.FirstOrDefaultAsync(n => n.LastName == lastName);
            return applicationUser;
        }
        catch (Exception e)
        {
            Log.Warning("Unable to retrieve user by last name: {LastName}. Exception Thrown: {Exception}", lastName, e.ToString());
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<ApplicationUser?> GetByUserNameAsync(string userName)
    {
        try
        {
            var applicationUser = await _context.Users.FirstOrDefaultAsync(n => n.UserName == userName);
            return applicationUser;
        }
        catch (Exception e)
        {
            Log.Warning("Unable to retrieve user with UserName: {Username}. Exception Thrown: {Exception}", userName, e);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<ApplicationUser?> GetByEmailAsync(string email)
    {
        try
        {
            var applicationUser = await _context.Users.FirstOrDefaultAsync(n => n.Email == email);
            return applicationUser;
        }
        catch (Exception e)
        {
            Log.Warning("Unable to find user with Email: {Email}. Exception Thrown: {Exception}", email, e.ToString());
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<IdentityResult> AddApplicationUser(ApplicationUser applicationUser)
    {
        try
        {
            var result = await _userManager.CreateAsync(applicationUser);
            
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(applicationUser, DEFAULT_ROLE);
                Log.Information("Added ApplicationUser with ID {id}", applicationUser.Id);
                return IdentityResult.Success;
            }
            
            Log.Warning("Unable to create ApplicationUser with ID {id}", applicationUser.Id);
            return result;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not add ApplicationUser");
            return IdentityResult.Failed();
        }
    }

    /// <inheritdoc />
    public async Task<bool> UpdateApplicationUser(ApplicationUser applicationUser)
    {
        try
        {
            var strategy = _context.Database.CreateExecutionStrategy();
        
            return await strategy.ExecuteAsync(async () =>
            {
                using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            
                var existingUser = await _context.Users.FirstOrDefaultAsync(
                    u => u.Id == applicationUser.Id);

                if (existingUser == null)
                {
                    Log.Error("Could not find user with ID {id}", applicationUser.Id);
                    return false;
                }

                applicationUser.NormalizedEmail = applicationUser.Email?.ToUpper();
                applicationUser.NormalizedUserName = applicationUser.UserName?.ToUpper();

                _context.Entry(existingUser).CurrentValues.SetValues(applicationUser);

                await _context.SaveChangesAsync();
                scope.Complete();

                Log.Information("Updated user with ID {id}", applicationUser.Id);
                return true;
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating user with ID {id}", applicationUser.Id);
            return false;
        }
    }
    
    /// <inheritdoc />
     public async Task<(List<string> Errors, int ImportedCount)> ImportUsersFromCsvAsync(string csvData)
    {
        var errors = new List<string>();
        int importedCount = 0;

        // Step 1: Validate CSV
        var validationErrors = _fileService.ValidateCsv(csvData);
        if (validationErrors.Any())
        {
            errors.AddRange(validationErrors);
            return (errors, importedCount);
        }

        // Step 2: Parse CSV
        var users = _fileService.ImportFromCsv(csvData, out var userRoles);

        foreach (var user in users)
        {
            // Step 3: Check if user already exists
            var existingUser = await _userManager.FindByNameAsync(user.UserName!);
            if (existingUser != null)
            {
                errors.Add($"User '{user.UserName}' already exists in the database.");
                continue;
            }

            // Step 4: Add user
            var result = await _userManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                errors.Add($"Failed to create user '{user.UserName}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
                continue;
            }

            // Step 5: Assign default role if none provided
            if (!userRoles.TryGetValue(user.UserName!, out var roles) || roles.Count == 0)
            {
                roles = new List<string> { "Operator" }; // default role
            }

            // Step 6: Ensure roles exist and assign
            foreach (var roleName in roles)
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    var roleResult = await _roleManager.CreateAsync(new IdentityRole(roleName));
                    if (!roleResult.Succeeded)
                    {
                        errors.Add($"Failed to create role '{roleName}' for user '{user.UserName}'.");
                        continue;
                    }
                }

                await _userManager.AddToRoleAsync(user, roleName);
            }

            importedCount++;
        }

        return (errors, importedCount);
    }
    
    /// <inheritdoc />
    public async Task<string> ExportUsersToCsvAsync()
    {
        // Step 1: Load all users from database
        var users = await _context.Users.ToListAsync();

        // Step 2: Load roles for each user
        var userRoles = new Dictionary<string, IEnumerable<string>>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userRoles[user.UserName ?? user.Email ?? user.Id] = roles;
        }

        // Step 3: Call the file service to generate CSV
        var csv = _fileService.ExportToCsv(users, userRoles);

        return csv;
    }

    /// <inheritdoc />
    public async Task<bool> UpdateDarkModePreferenceAsync(string userId, bool isDarkMode)
    {
        try
        {
            var updated = await _context.Users
                .Where(u => u.Id == userId)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.DarkMode, isDarkMode));
            return updated > 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating dark mode preference for user {UserId}", userId);
            return false;
        }
    }
}
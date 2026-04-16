using Microsoft.AspNetCore.Identity;

namespace MESS.Services.CRUD.ApplicationUser;
using Data.Models;

/// <summary>
/// Interface for managing application user-related operations.
/// Provides methods for user authentication, retrieval, and management.
/// </summary>
public interface IApplicationUserService
{
    /// <summary>
    /// Signs out the current user.
    /// </summary>
    public Task SignOutAsync();
    /// <summary>
    /// Signs in a user by username.
    /// </summary>
    /// <param name="username">The username of the user.</param>
    /// <returns>True if sign-in was successful, otherwise false.</returns>
    public Task<bool> SignInAsync(string username);
    /// <summary>
    /// Gets a list of users by role.
    /// </summary>
    /// <param name="roleName">The name of the role.</param>
    /// <returns>A list of users in the specified role.</returns>
    public Task<List<ApplicationUser>> GetUsersByRoleAsync(string roleName);
    ///<summary>
    /// Retrieves a list of all ApplicationUsers currently registered
    ///</summary>
    ///<returns>List of ApplicationUser objects</returns>
    public Task<List<ApplicationUser>> GetAllAsync();

    /// <summary>
    /// Users with <c>LockoutEnd</c> null (AspNetUsers). Any non-null date is treated as locked for the login picker.
    /// </summary>
    public Task<List<ApplicationUser>> GetUsersForLoginDropdownAsync();
    
    ///<summary>
    /// Retrieves a ApplicationUser by id number
    ///</summary>
    ///<returns>ApplicationUser object</returns>
    public Task<ApplicationUser?> GetByIdAsync(string id);

    /// <summary>
    /// Determines if the given user is currently persisted within the database.
    /// </summary>
    /// <param name="user">An <see cref="ApplicationUser"/> object. </param>
    /// <returns></returns>
    public Task<bool?> IsNewUser(ApplicationUser user);
    
    /// <summary>
    /// Retrieves an ApplicationUser by last name.
    /// </summary>
    /// <param name="lastName">The last name of the user.</param>
    /// <returns>An ApplicationUser object if found, otherwise null.</returns>
    public Task<ApplicationUser?> GetByLastNameAsync(string lastName);

    /// <summary>
    /// Retrieves an ApplicationUser by username.
    /// </summary>
    /// <param name="userName">The username of the user.</param>
    /// <returns>An ApplicationUser object if found, otherwise null.</returns>
    public Task<ApplicationUser?> GetByUserNameAsync(string userName);

    /// <summary>
    /// Retrieves an ApplicationUser by email.
    /// </summary>
    /// <param name="email">The email of the user.</param>
    /// <returns>An ApplicationUser object if found, otherwise null.</returns>
    public Task<ApplicationUser?> GetByEmailAsync(string email);
    
    ///<summary>
    /// Creates a ApplicationUser object and saves it to the database
    ///</summary>
    ///<returns>ApplicationUser object</returns>
    public Task<IdentityResult> AddApplicationUser(ApplicationUser ApplicationUser);
    
    ///<summary>
    /// Updates a ApplicationUser currently in the database
    ///</summary>
    ///<returns>Updated ApplicationUser object</returns>
    public Task<bool> UpdateApplicationUser(ApplicationUser ApplicationUser);
    
    /// <summary>
    /// Validates, imports, and assigns roles from a CSV string.
    /// Returns a list of error messages and the count of successfully imported users.
    /// </summary>
    /// <param name="csvData">CSV data containing users. Columns: UserName, Email, FirstName, LastName, IsActive, Roles.</param>
    /// <returns>
    /// A tuple containing:
    /// - <c>Errors</c>: A list of errors encountered during validation or import.
    /// - <c>ImportedCount</c>: The number of users successfully imported.
    /// </returns>
    Task<(List<string> Errors, int ImportedCount)> ImportUsersFromCsvAsync(string csvData);
    
    /// <summary>
    /// Exports all application users and their roles to a CSV string.
    /// Roles are included in a single semicolon-separated column per user.
    /// </summary>
    /// <returns>A CSV string representing all users and their roles.</returns>
    Task<string> ExportUsersToCsvAsync();

    /// <summary>
    /// Updates the dark mode preference for a user.
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <param name="isDarkMode">True to enable dark mode; false to disable it.</param>
    /// <returns>True if successful; otherwise false.</returns>
    Task<bool> UpdateDarkModePreferenceAsync(string userId, bool isDarkMode);
}
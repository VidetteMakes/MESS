using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Microsoft.AspNetCore.Identity;

namespace MESS.Data.Models;

/// <summary>
/// Represents an application user with additional properties.
/// Inherits from IdentityUser.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// Gets or sets the first name of the user.
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Gets or sets the last name of the user.
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user is active.
    /// </summary>
    public bool IsActive { get; set; }
    
    /// <summary>
    /// Gets or sets the list of production log IDs associated with the user.
    /// </summary>
    public List<int>? ProductionLogIds { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the user prefers dark mode.
    /// </summary>
    public bool DarkMode { get; set; }

    /// <summary>
    /// Gets the full name of the user by combining the first and last names.
    /// </summary>
    public string FullName => $"{FirstName} {LastName}";
}

/// <summary>
/// Validator for the ApplicationUser class.
/// </summary>
public class ApplicationUserValidator : AbstractValidator<ApplicationUser>
{
    /// <summary>
    /// Initializes a new instance of the ApplicationUserValidator class.
    /// Defines validation rules for ApplicationUser properties.
    /// </summary>
    public ApplicationUserValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().Length(0, 1024);
        RuleFor(x => x.LastName).NotEmpty().Length(0, 1024);
    }
}
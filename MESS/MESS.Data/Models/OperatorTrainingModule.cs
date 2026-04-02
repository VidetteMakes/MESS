using System.ComponentModel.DataAnnotations;

namespace MESS.Data.Models;

/// <summary>
/// Represents a training module that operators can read and administrators can manage.
/// </summary>
public class OperatorTrainingModule : AuditableEntity
{
    /// <summary>
    /// Gets or sets the primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the title shown in the training list.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a short overview of the module.
    /// </summary>
    [MaxLength(500)]
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the training content body.
    /// </summary>
    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display order in the UI.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the module is visible to non-admin users.
    /// </summary>
    public bool IsPublished { get; set; } = true;
}

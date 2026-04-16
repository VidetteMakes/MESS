using System.ComponentModel.DataAnnotations;

namespace MESS.Data.Models;

/// <summary>
/// Represents a step within a training module with optional image support.
/// </summary>
public class TrainingStep : AuditableEntity
{
    /// <summary>
    /// Gets or sets the primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the title of this step.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description/body content of this step.
    /// </summary>
    [Required]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display order of this step within the module.
    /// </summary>
    public int StepOrder { get; set; }

    /// <summary>
    /// Gets or sets the relative path to the image file associated with this step (optional).
    /// Images are stored in wwwroot/TrainingImages/
    /// </summary>
    [MaxLength(500)]
    public string? ImagePath { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the parent OperatorTrainingModule.
    /// </summary>
    public int OperatorTrainingModuleId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the parent OperatorTrainingModule.
    /// </summary>
    public virtual OperatorTrainingModule? OperatorTrainingModule { get; set; }
}

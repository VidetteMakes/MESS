namespace MESS.Services.DTOs.WorkInstructions.File;

/// <summary>
/// Represents audit metadata captured for a work instruction when serialized
/// to or from a file.
/// 
/// This structure mirrors the audit fields defined in <see cref="MESS.Data.Models.AuditableEntity"/>,
/// providing a portable representation of creation and modification history.
/// 
/// The data is intended for traceability and informational purposes. During import,
/// these values may be ignored or conditionally applied depending on system configuration,
/// and should not be assumed to override system-managed audit fields.
/// </summary>
public class AuditFileDTO
{
    /// <summary>
    /// Gets or sets the identifier of the user who originally created
    /// the work instruction.
    /// </summary>
    public string CreatedBy { get; set; } = "";

    /// <summary>
    /// Gets or sets the date and time when the work instruction
    /// was originally created.
    /// </summary>
    public DateTimeOffset CreatedOn { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who last modified
    /// the work instruction.
    /// </summary>
    public string LastModifiedBy { get; set; } = "";

    /// <summary>
    /// Gets or sets the date and time when the work instruction
    /// was last modified.
    /// </summary>
    public DateTimeOffset LastModifiedOn { get; set; }
}
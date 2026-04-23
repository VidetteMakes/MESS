using MESS.Data.Models;
using MESS.Services.DTOs.WorkInstructions.Nodes.File;

namespace MESS.Services.DTOs.WorkInstructions.File;

/// <summary>
/// Represents a file-safe data transfer object for importing and exporting
/// <see cref="WorkInstruction"/> data.
/// 
/// This DTO is designed to be persistence-agnostic and stable across file formats.
/// It contains only the data necessary to reconstruct a work instruction,
/// without exposing database-specific identifiers or EF Core navigation properties.
/// </summary>
public class WorkInstructionFileDTO
{
    /// <summary>
    /// Gets or sets the title of the work instruction.
    /// This value is required and should match the original instruction title.
    /// </summary>
    public string Title { get; set; } = "";

    /// <summary>
    /// Gets or sets the version label of the work instruction.
    /// This value is optional and may be null if versioning is not used.
    /// </summary>
    public string? Version { get; set; }
    
    /// <summary>
    /// Gets or sets the audit metadata associated with this work instruction.
    /// 
    /// This data mirrors the audit fields of <see cref="AuditableEntity"/> and
    /// represents the creation and modification history at the time the file
    /// was exported.
    /// 
    /// The values are intended for traceability and documentation purposes.
    /// During import, they may be ignored or conditionally applied depending
    /// on system configuration, and should not be assumed to override
    /// system-managed audit fields.
    /// </summary>
    public AuditFileDTO? Audit { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a QR code should be generated
    /// when a production log is completed for this work instruction.
    /// </summary>
    public bool ShouldGenerateQrCode { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the part produced by this
    /// work instruction is serialized.
    /// </summary>
    public bool PartProducedIsSerialized { get; set; }

    /// <summary>
    /// Gets or sets the name of the part produced by this work instruction.
    /// 
    /// This value is used instead of a database identifier to keep the file
    /// representation independent of the underlying persistence layer.
    /// </summary>
    public string? ProducedPartName { get; set; }

    /// <summary>
    /// Gets or sets the collection of product names associated with
    /// this work instruction.
    /// 
    /// Product names are used instead of database identifiers to allow
    /// import resolution in different environments.
    /// </summary>
    public List<string> AssociatedProductNames { get; set; } = [];

    /// <summary>
    /// Gets or sets the ordered collection of nodes that define
    /// the structure and steps of this work instruction.
    /// </summary>
    public List<WorkInstructionNodeFileDTO> Nodes { get; set; } = [];
}
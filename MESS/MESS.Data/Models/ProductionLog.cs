using System.ComponentModel.DataAnnotations.Schema;
using FluentValidation;

namespace MESS.Data.Models;

/// <summary>
/// Represents a production log that tracks steps and details of a production process.
/// </summary>
public class ProductionLog : AuditableEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the production log.
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// Gets or sets the list of steps associated with the production log.
    /// </summary>
    public List<ProductionLogStep> LogSteps { get; set; } = [];
    
    // Foreign Keys + Navigation

    /// <summary>
    /// Gets or sets the identifier of the operator associated with the production log.
    /// </summary>
    [ForeignKey("UserId")]
    public string? OperatorId { get; set; }

    /// <summary>
    /// Gets or sets the product ID associated with the production log.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the product associated with the production log.
    /// </summary>
    public Product? Product { get; set; }

    /// <summary>
    /// Gets or sets the work instruction ID associated with the production log.
    /// </summary>
    public int WorkInstructionId { get; set; }

    /// <summary>
    /// Gets or sets the work instruction associated with the production log.
    /// </summary>
    public WorkInstruction? WorkInstruction { get; set; }
    
    /// <summary>
    /// Gets or sets an unsigned integer value specifying the size of the batch this production log was made from.
    /// A value of 1 indicates that the log was made from a single piece flow.
    /// </summary>
    public int FromBatchOf { get; set; }

    /// <summary>
    /// The original ID from a source system when this log was created via import.
    /// Null for logs created natively. Used for duplicate detection on re-import.
    /// </summary>
    public int? ExternalId { get; set; }

    /// <summary>
    /// The username of the person who imported this log. Null for natively created logs.
    /// </summary>
    public string? ImportedBy { get; set; }

    /// <summary>
    /// The timestamp when this log was imported. Null for natively created logs.
    /// </summary>
    public DateTimeOffset? ImportedOn { get; set; }
}
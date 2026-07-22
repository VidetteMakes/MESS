namespace MESS.Services.DTOs.ProductionLogs.Archive;

/// <summary>
/// Row displayed in the Log Archives table.
/// </summary>
public sealed class ProductionLogArchiveRowDTO
{
    /// <summary>Production log id, used for opening details.</summary>
    public int ProductionLogId { get; set; }

    /// <summary>Identity GUID of the operator who executed this production log.</summary>
    public string? OperatorId { get; set; }

    /// <summary>Most recent production log step attempt id for this log.</summary>
    public int? AttemptId { get; set; }

    /// <summary>Computed status from attempts in this production log.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Product name.</summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>Database ID of the work instruction linked to this production log.</summary>
    public int WorkInstructionId { get; set; }

    /// <summary>Work instruction title.</summary>
    public string WorkInstructionName { get; set; } = string.Empty;

    /// <summary>Part produced by the work instruction.</summary>
    public string PartProducedName { get; set; } = string.Empty;

    /// <summary>Serial number of the part produced by this production log.</summary>
    public string ProducedPartSerialNumber { get; set; } = string.Empty;

    /// <summary>Date/time the production log was created.</summary>
    public DateTimeOffset CreatedOn { get; set; }

    /// <summary>User that created the production log.</summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>Date/time the production log was last modified.</summary>
    public DateTimeOffset LastModifiedOn { get; set; }

    /// <summary>User that last modified the production log.</summary>
    public string LastModifiedBy { get; set; } = string.Empty;
}

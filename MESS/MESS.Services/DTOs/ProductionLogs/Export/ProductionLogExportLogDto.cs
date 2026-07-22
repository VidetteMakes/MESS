namespace MESS.Services.DTOs.ProductionLogs.Export;

/// <summary>A single production log in the export file.</summary>
public sealed class ProductionLogExportLogDto
{
    /// <summary>The original DB primary key — stored as ExternalId on import.</summary>
    public int ExternalId { get; set; }

    /// <summary>Identity GUID of the operator who ran the log.</summary>
    public string? OperatorId { get; set; }

    /// <summary>Email address of the operator who ran the log.</summary>
    public string? OperatorEmail { get; set; }

    /// <summary>Database ID of the associated product.</summary>
    public int ProductId { get; set; }

    /// <summary>Human-readable name of the associated product's part definition.</summary>
    public string? ProductName { get; set; }

    /// <summary>Database ID of the associated work instruction.</summary>
    public int WorkInstructionId { get; set; }

    /// <summary>Title of the associated work instruction.</summary>
    public string? WorkInstructionTitle { get; set; }

    /// <summary>Batch size this log was created from (1 = single piece flow).</summary>
    public int FromBatchOf { get; set; }

    /// <summary>Username of the person who created the log.</summary>
    public string? CreatedBy { get; set; }

    /// <summary>UTC timestamp when the log was created.</summary>
    public DateTimeOffset CreatedOn { get; set; }

    /// <summary>Username of the person who last modified the log.</summary>
    public string? LastModifiedBy { get; set; }

    /// <summary>UTC timestamp when the log was last modified.</summary>
    public DateTimeOffset LastModifiedOn { get; set; }

    /// <summary>Step results recorded during this log.</summary>
    public List<ProductionLogExportStepDto> Steps { get; set; } = [];

    /// <summary>Parts produced, installed, or removed during this log.</summary>
    public List<ProductionLogExportPartDto> Parts { get; set; } = [];
}

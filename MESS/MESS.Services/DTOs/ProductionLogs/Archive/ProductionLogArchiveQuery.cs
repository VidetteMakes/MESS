namespace MESS.Services.DTOs.ProductionLogs.Archive;

/// <summary>
/// Server-side query parameters for Log Archives.
/// </summary>
public sealed class ProductionLogArchiveQuery
{
    /// <summary>One-based page number.</summary>
    public int Page { get; set; } = 1;

    /// <summary>Number of rows per page.</summary>
    public int PageSize { get; set; } = 50;

    /// <summary>Global search text across archive columns.</summary>
    public string? Search { get; set; }

    /// <summary>Sort column key.</summary>
    public string? SortBy { get; set; } = "createdOn";

    /// <summary>Sort direction, asc or desc.</summary>
    public string? SortDir { get; set; } = "desc";

    /// <summary>Attempt id filter.</summary>
    public int? FilterAttemptId { get; set; }

    /// <summary>Production log id filter.</summary>
    public int? FilterLogId { get; set; }

    /// <summary>Status filter.</summary>
    public string? FilterStatus { get; set; }

    /// <summary>Product filter.</summary>
    public string? FilterProduct { get; set; }

    /// <summary>Work instruction filter.</summary>
    public string? FilterWorkInstruction { get; set; }

    /// <summary>Exact work instruction ID filter — takes precedence over <see cref="FilterWorkInstruction"/> when set.</summary>
    public int? FilterWorkInstructionId { get; set; }

    /// <summary>Produced part filter.</summary>
    public string? FilterPartProduced { get; set; }

    /// <summary>Produced part serial number filter.</summary>
    public string? FilterProducedPartSerialNumber { get; set; }

    /// <summary>Created date range start.</summary>
    public DateTimeOffset? FilterCreatedOnFrom { get; set; }

    /// <summary>Created date range end.</summary>
    public DateTimeOffset? FilterCreatedOnTo { get; set; }

    /// <summary>Created by filter.</summary>
    public string? FilterCreatedBy { get; set; }

    /// <summary>Modified date range start.</summary>
    public DateTimeOffset? FilterModifiedOnFrom { get; set; }

    /// <summary>Modified date range end.</summary>
    public DateTimeOffset? FilterModifiedOnTo { get; set; }

    /// <summary>Modified by filter.</summary>
    public string? FilterModifiedBy { get; set; }

    /// <summary>Exact operator (user) ID filter. When set, only logs belonging to this operator are returned.</summary>
    public string? FilterOperatorId { get; set; }
}

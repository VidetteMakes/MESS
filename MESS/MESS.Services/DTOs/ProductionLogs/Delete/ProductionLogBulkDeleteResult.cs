namespace MESS.Services.DTOs.ProductionLogs.Delete;

/// <summary>Result returned after a bulk deletion of production logs.</summary>
public sealed class ProductionLogBulkDeleteResult
{
    /// <summary>Number of logs deleted.</summary>
    public int DeletedCount { get; set; }

    /// <summary>Primary key of the created ProductionLogDeletionAudit record.</summary>
    public int AuditId { get; set; }

    /// <summary>Non-null when the operation failed (IDs not found, etc.).</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>True when no error occurred.</summary>
    public bool Success => ErrorMessage == null;
}

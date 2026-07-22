namespace MESS.Data.Models;

/// <summary>
/// Audit record written whenever production logs are bulk-deleted.
/// Captures who deleted, when, how many logs, and the export filename used as the deletion gate.
/// </summary>
public class ProductionLogDeletionAudit
{
    /// <summary>Gets or sets the primary key.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the username of the person who performed the deletion.</summary>
    public string DeletedBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC timestamp when the deletion occurred.</summary>
    public DateTimeOffset DeletedOn { get; set; }

    /// <summary>Gets or sets the number of logs deleted in this operation.</summary>
    public int LogCount { get; set; }

    /// <summary>Gets or sets the snapshot of the deleted production log IDs.</summary>
    public List<int> LogIds { get; set; } = [];

    /// <summary>Gets or sets the filename of the export file downloaded before deletion.</summary>
    public string ExportFile { get; set; } = string.Empty;
}

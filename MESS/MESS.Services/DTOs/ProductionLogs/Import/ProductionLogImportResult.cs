namespace MESS.Services.DTOs.ProductionLogs.Import;

/// <summary>Result returned after a successful production log import operation.</summary>
public sealed class ProductionLogImportResult
{
    /// <summary>Number of logs successfully created.</summary>
    public int CreatedCount { get; set; }

    /// <summary>Number of logs skipped because their ExternalId already existed.</summary>
    public int SkippedDuplicateCount { get; set; }

    /// <summary>ExternalIds of the skipped duplicate logs.</summary>
    public List<int> SkippedExternalIds { get; set; } = [];
}

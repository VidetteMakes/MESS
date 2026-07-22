namespace MESS.Services.DTOs.ProductionLogs.Import;

/// <summary>Result of a dry-run validation pass before importing production logs.</summary>
public sealed class ProductionLogImportValidationResult
{
    /// <summary>True when no errors were found and the import can proceed.</summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>All validation errors found. Empty means the file is ready to import.</summary>
    public List<ProductionLogImportValidationError> Errors { get; set; } = [];
}

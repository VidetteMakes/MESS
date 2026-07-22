namespace MESS.Services.DTOs.ProductionLogs.Import;

/// <summary>A single referential-integrity error found during import validation.</summary>
public sealed class ProductionLogImportValidationError
{
    /// <summary>One-based row index in the import file.</summary>
    public int Row { get; set; }

    /// <summary>The field that failed validation (e.g. "Product", "WorkInstruction").</summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>The value that was looked up and not found.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>Human-readable description of why the validation failed.</summary>
    public string Reason { get; set; } = string.Empty;
}

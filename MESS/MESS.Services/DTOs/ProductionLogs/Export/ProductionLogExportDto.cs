namespace MESS.Services.DTOs.ProductionLogs.Export;

/// <summary>Top-level envelope written to the export JSON file.</summary>
public sealed class ProductionLogExportDto
{
    /// <summary>UTC timestamp when this export was generated.</summary>
    public DateTimeOffset ExportedAt { get; set; }

    /// <summary>Username of the user who triggered the export.</summary>
    public string ExportedBy { get; set; } = string.Empty;

    /// <summary>Human-readable summary of the filters applied when exporting.</summary>
    public ProductionLogExportFilterSummaryDto FilterSummary { get; set; } = new();

    /// <summary>The exported production logs.</summary>
    public List<ProductionLogExportLogDto> Logs { get; set; } = [];
}

/// <summary>Filter context recorded in the export envelope for traceability.</summary>
public sealed class ProductionLogExportFilterSummaryDto
{
    /// <summary>Start of the created-on date range filter, if applied.</summary>
    public string? DateFrom { get; set; }

    /// <summary>End of the created-on date range filter, if applied.</summary>
    public string? DateTo { get; set; }

    /// <summary>Product name filter, if applied.</summary>
    public string? Product { get; set; }

    /// <summary>Work instruction name filter, if applied.</summary>
    public string? WorkInstruction { get; set; }

    /// <summary>Global search term, if applied.</summary>
    public string? Search { get; set; }
}

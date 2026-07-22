namespace MESS.Services.DTOs.ProductionLogs.Export;

/// <summary>A step within an exported production log.</summary>
public sealed class ProductionLogExportStepDto
{
    /// <summary>Database ID of the work instruction step this entry corresponds to.</summary>
    public int WorkInstructionStepId { get; set; }

    /// <summary>Name of the work instruction step at the time of export.</summary>
    public string? WorkInstructionStepName { get; set; }

    /// <summary>All attempts recorded for this step.</summary>
    public List<ProductionLogExportAttemptDto> Attempts { get; set; } = [];
}

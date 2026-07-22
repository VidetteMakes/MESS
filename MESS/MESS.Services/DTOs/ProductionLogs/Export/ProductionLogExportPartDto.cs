using MESS.Data.Models;

namespace MESS.Services.DTOs.ProductionLogs.Export;

/// <summary>A part entry within an exported production log.</summary>
public sealed class ProductionLogExportPartDto
{
    /// <summary>The type of operation performed on this part (Produced, Installed, Removed).</summary>
    public PartOperationType OperationType { get; set; }

    /// <summary>Database ID of the part definition.</summary>
    public int PartDefinitionId { get; set; }

    /// <summary>Name of the part definition, used as a fallback during import resolution.</summary>
    public string? PartDefinitionName { get; set; }

    /// <summary>Serial number of the serializable part instance, if any.</summary>
    public string? SerialNumber { get; set; }
}

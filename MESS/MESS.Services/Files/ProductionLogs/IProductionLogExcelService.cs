using MESS.Services.DTOs.ProductionLogs.Export;

namespace MESS.Services.Files.ProductionLogs;

/// <summary>
/// Generates Excel workbooks from production log export data.
/// </summary>
public interface IProductionLogExcelService
{
    /// <summary>
    /// Builds an Excel workbook from <paramref name="export"/> and returns it as a <see cref="MemoryStream"/>.
    /// The stream is positioned at the beginning.
    /// Three sheets are produced: Logs, Steps, Parts.
    /// </summary>
    MemoryStream GenerateWorkbook(ProductionLogExportDto export);
}

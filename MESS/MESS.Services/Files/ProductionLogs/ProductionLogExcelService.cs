using ClosedXML.Excel;
using MESS.Services.DTOs.ProductionLogs.Export;

namespace MESS.Services.Files.ProductionLogs;

/// <inheritdoc />
public sealed class ProductionLogExcelService : IProductionLogExcelService
{
    /// <inheritdoc />
    public MemoryStream GenerateWorkbook(ProductionLogExportDto export)
    {
        using var workbook = new XLWorkbook();

        BuildLogsSheet(workbook, export);
        BuildStepsSheet(workbook, export);
        BuildPartsSheet(workbook, export);

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    private static void BuildLogsSheet(XLWorkbook workbook, ProductionLogExportDto export)
    {
        var ws = workbook.Worksheets.Add("Logs");

        string[] headers =
        [
            "ExternalId", "OperatorId", "OperatorEmail",
            "ProductId", "ProductName",
            "WorkInstructionId", "WorkInstructionTitle",
            "FromBatchOf",
            "CreatedBy", "CreatedOn",
            "LastModifiedBy", "LastModifiedOn"
        ];

        for (var i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        ws.Row(1).Style.Font.Bold = true;

        var row = 2;
        foreach (var log in export.Logs)
        {
            ws.Cell(row, 1).Value = log.ExternalId;
            ws.Cell(row, 2).Value = log.OperatorId ?? string.Empty;
            ws.Cell(row, 3).Value = log.OperatorEmail ?? string.Empty;
            ws.Cell(row, 4).Value = log.ProductId;
            ws.Cell(row, 5).Value = log.ProductName ?? string.Empty;
            ws.Cell(row, 6).Value = log.WorkInstructionId;
            ws.Cell(row, 7).Value = log.WorkInstructionTitle ?? string.Empty;
            ws.Cell(row, 8).Value = log.FromBatchOf;
            ws.Cell(row, 9).Value = log.CreatedBy ?? string.Empty;
            ws.Cell(row, 10).Value = log.CreatedOn.ToString("yyyy-MM-dd HH:mm:ss zzz");
            ws.Cell(row, 11).Value = log.LastModifiedBy ?? string.Empty;
            ws.Cell(row, 12).Value = log.LastModifiedOn.ToString("yyyy-MM-dd HH:mm:ss zzz");
            row++;
        }

        ws.Columns().AdjustToContents();
    }

    private static void BuildStepsSheet(XLWorkbook workbook, ProductionLogExportDto export)
    {
        var ws = workbook.Worksheets.Add("Steps");

        string[] headers =
        [
            "LogExternalId", "StepId", "StepName",
            "Success", "Notes", "SubmitTime",
            "FailureNoun", "FailureAdjective"
        ];

        for (var i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        ws.Row(1).Style.Font.Bold = true;

        var row = 2;
        foreach (var log in export.Logs)
        {
            foreach (var step in log.Steps)
            {
                foreach (var attempt in step.Attempts)
                {
                    ws.Cell(row, 1).Value = log.ExternalId;
                    ws.Cell(row, 2).Value = step.WorkInstructionStepId;
                    ws.Cell(row, 3).Value = step.WorkInstructionStepName ?? string.Empty;
                    ws.Cell(row, 4).Value = attempt.Success?.ToString() ?? string.Empty;
                    ws.Cell(row, 5).Value = attempt.Notes;
                    ws.Cell(row, 6).Value = attempt.SubmitTime.ToString("yyyy-MM-dd HH:mm:ss zzz");
                    ws.Cell(row, 7).Value = attempt.FailureNoun ?? string.Empty;
                    ws.Cell(row, 8).Value = attempt.FailureAdjective ?? string.Empty;
                    row++;
                }
            }
        }

        ws.Columns().AdjustToContents();
    }

    private static void BuildPartsSheet(XLWorkbook workbook, ProductionLogExportDto export)
    {
        var ws = workbook.Worksheets.Add("Parts");

        string[] headers =
        [
            "LogExternalId", "OperationType",
            "PartDefinitionId", "PartDefinitionName", "SerialNumber"
        ];

        for (var i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        ws.Row(1).Style.Font.Bold = true;

        var row = 2;
        foreach (var log in export.Logs)
        {
            foreach (var part in log.Parts)
            {
                ws.Cell(row, 1).Value = log.ExternalId;
                ws.Cell(row, 2).Value = part.OperationType.ToString();
                ws.Cell(row, 3).Value = part.PartDefinitionId;
                ws.Cell(row, 4).Value = part.PartDefinitionName ?? string.Empty;
                ws.Cell(row, 5).Value = part.SerialNumber ?? string.Empty;
                row++;
            }
        }

        ws.Columns().AdjustToContents();
    }
}

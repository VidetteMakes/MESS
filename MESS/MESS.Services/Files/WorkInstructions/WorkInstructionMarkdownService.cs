using System.Text;
using MESS.Services.DTOs.WorkInstructions.File;
using MESS.Services.DTOs.WorkInstructions.Nodes.File;
using MESS.Services.DTOs.WorkInstructions.Nodes.PartNodes.File;
using MESS.Services.DTOs.WorkInstructions.Nodes.StepNodes.File;

namespace MESS.Services.Files.WorkInstructions;

/// <inheritdoc />
public class WorkInstructionMarkdownService : IWorkInstructionMarkdownService
{
    
    /// <inheritdoc />
    public WorkInstructionFileDTO Parse(string markdown)
    {
        var (yaml, body) = SplitFrontMatter(markdown);

        var dto = ParseYaml(yaml);
        dto.Nodes = ParseNodes(body);

        return dto;
    }
    
    private static (string yaml, string body) SplitFrontMatter(string content)
    {
        if (!content.TrimStart().StartsWith("---"))
            return ("", content);

        var parts = content.Split(["---"], 3, StringSplitOptions.None);

        return parts.Length < 3 ? throw new FormatException("Invalid front matter format.") : 
            (parts[1].Trim(), parts[2].Trim());
    }
    
    private static WorkInstructionFileDTO ParseYaml(string yaml)
    {
        var dto = new WorkInstructionFileDTO();

        if (string.IsNullOrWhiteSpace(yaml))
            return dto;

        var lines = yaml.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            if (line.StartsWith("title:"))
                dto.Title = GetValue(line);

            else if (line.StartsWith("version:"))
                dto.Version = GetValue(line);

            else if (line.StartsWith("shouldGenerateQrCode:"))
                dto.ShouldGenerateQrCode = bool.Parse(GetValue(line));

            else if (line.StartsWith("collectsProducedPartSerialNumber:"))
                dto.PartProducedIsSerialized = bool.Parse(GetValue(line));

            else if (line.StartsWith("producedPartName:"))
                dto.ProducedPartName = GetValue(line);

            else if (line.StartsWith("associatedProducts:"))
            {
                i++;
                while (i < lines.Length && lines[i].Trim().StartsWith("-"))
                {
                    dto.AssociatedProductNames.Add(
                        lines[i].Trim().Substring(1).Trim()
                    );
                    i++;
                }
                i--;
            }

            else if (line.StartsWith("audit:"))
            {
                var audit = new AuditFileDTO();

                i++;
                while (i < lines.Length && lines[i].StartsWith("  "))
                {
                    var auditLine = lines[i].Trim();

                    if (auditLine.StartsWith("createdBy:"))
                        audit.CreatedBy = GetValue(auditLine);

                    else if (auditLine.StartsWith("createdOn:"))
                        audit.CreatedOn = DateTimeOffset.Parse(GetValue(auditLine));

                    else if (auditLine.StartsWith("modifiedBy:"))
                        audit.LastModifiedBy = GetValue(auditLine);

                    else if (auditLine.StartsWith("modifiedOn:"))
                        audit.LastModifiedOn = DateTimeOffset.Parse(GetValue(auditLine));

                    i++;
                }

                dto.Audit = audit;
                i--;
            }
        }

        return dto;
    }
    
    private static string GetValue(string line)
    {
        return line.Split(':', 2)[1].Trim();
    }
    
    private static List<WorkInstructionNodeFileDTO> ParseNodes(string body)
    {
        var nodes = new List<WorkInstructionNodeFileDTO>();

        var lines = body.Split('\n');
        int i = 0;

        while (i < lines.Length)
        {
            var line = lines[i].Trim();

            if (line.TrimStart().StartsWith("<!-- MESS:PARTS"))
            {
                var (parts, consumed) = ParsePartsTable(lines, i);

                nodes.AddRange(parts);
                i += consumed;
            }
            else if (line.StartsWith("## "))
            {
                var (step, consumed) = ParseStep(lines, i);
                nodes.Add(step);
                i += consumed;
            }
            else
            {
                i++;
            }
        }

        return nodes;
    }
    
    private static (List<PartNodeFileDTO> nodes, int nextIndex) ParsePartsTable(string[] lines, int start)
    {
        var parts = new List<PartNodeFileDTO>();

        var i = start + 1;

        // Skip blank lines immediately after marker
        while (i < lines.Length && string.IsNullOrWhiteSpace(lines[i]))
            i++;
        
        // Must be header row
        if (i >= lines.Length)
            return (parts, Math.Max(1, i - start));

        if (!lines[i].Trim().StartsWith('|'))
            return (parts, Math.Max(1, i - start));

        // skip header row
        i++;

        // optional separator row
        if (i < lines.Length && lines[i].Contains("---"))
            i++;

        // Parse rows
        while (i < lines.Length)
        {
            var line = lines[i];

            if (string.IsNullOrWhiteSpace(line))
                break;

            if (!line.StartsWith('|'))
                break;

            var cols = line.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            if (cols.Length > 0)
            {
                parts.Add(new PartNodeFileDTO
                {
                    PartName = cols[0],
                    PartNumber = cols.ElementAtOrDefault(1)
                });
            }

            i++;
        }

        return (parts, i - start);
    }
    
    private static (StepNodeFileDTO node, int nextIndex) ParseStep(string[] lines, int start)
    {
        var step = new StepNodeFileDTO
        {
            Name = lines[start].Substring(3).Trim()
        };

        var bodyLines = new List<string>();
        var detailLines = new List<string>();

        var i = start + 1;
        var section = "body";

        while (i < lines.Length)
        {
            var raw = lines[i];
            var line = raw.Trim();

            if (line.StartsWith("## "))
                break;
            
            if (line.StartsWith("<!-- MESS:PARTS"))
                break;

            if (line.Equals("### Details", StringComparison.OrdinalIgnoreCase))
            {
                section = "details";
                i++;
                continue;
            }

            if (line.Equals("### Secondary Media", StringComparison.OrdinalIgnoreCase))
            {
                section = "secondary";
                i++;
                continue;
            }

            if (IsImage(line))
            {
                var src = ExtractImage(line);

                if (section == "secondary")
                    step.SecondaryMedia.Add(src);
                else
                    step.PrimaryMedia.Add(src);
            }
            else
            {
                if (section == "details")
                    detailLines.Add(raw);
                else
                    bodyLines.Add(raw);
            }

            i++;
        }

        step.Body = string.Join("\n", bodyLines).Trim();
        step.DetailedBody = detailLines.Count > 0
            ? string.Join("\n", detailLines).Trim()
            : null;

        return (step, i - start);
    }
    
    private static bool IsImage(string line)
    {
        return line.StartsWith("![");
    }

    private static string ExtractImage(string line)
    {
        var start = line.IndexOf('(');
        var end = line.IndexOf(')', start + 1);

        if (start < 0 || end <= start)
            return "";

        return line.Substring(start + 1, end - start - 1);
    }
    
    /// <inheritdoc />
    public string Serialize(WorkInstructionFileDTO dto)
    {
        var sb = new StringBuilder();

        WriteFrontMatter(sb, dto);
        sb.AppendLine();

        WriteTitle(sb, dto);

        var i = 0;

        while (i < dto.Nodes.Count)
        {
            var node = dto.Nodes[i];

            if (node is StepNodeFileDTO step)
            {
                WriteStep(sb, step);
                i++;
                continue;
            }

            var parts = new List<PartNodeFileDTO>();

            while (i < dto.Nodes.Count && dto.Nodes[i] is PartNodeFileDTO p)
            {
                parts.Add(p);
                i++;
            }

            if (parts.Count > 0)
            {
                WritePartsTable(sb, parts);
            }
        }

        return sb.ToString().TrimEnd();
    }
    
    private static void WriteFrontMatter(StringBuilder sb, WorkInstructionFileDTO dto)
    {
        sb.AppendLine("---");

        sb.AppendLine($"title: {dto.Title}");

        if (!string.IsNullOrWhiteSpace(dto.Version))
            sb.AppendLine($"version: {dto.Version}");
        
        sb.AppendLine($"shouldGenerateQrCode: {dto.ShouldGenerateQrCode.ToString().ToLower()}");
        sb.AppendLine($"collectsProducedPartSerialNumber: {dto.PartProducedIsSerialized.ToString().ToLower()}");

        if (!string.IsNullOrWhiteSpace(dto.ProducedPartName))
            sb.AppendLine($"producedPartName: {dto.ProducedPartName}");

        if (dto.AssociatedProductNames.Any())
        {
            sb.AppendLine();
            sb.AppendLine("associatedProducts:");
            foreach (var product in dto.AssociatedProductNames)
                sb.AppendLine($"  - {product}");
        }

        if (dto.Audit != null)
        {
            sb.AppendLine();
            sb.AppendLine("audit:");
            sb.AppendLine($"  createdBy: {dto.Audit.CreatedBy}");
            sb.AppendLine($"  createdOn: {dto.Audit.CreatedOn:O}");
            sb.AppendLine($"  modifiedBy: {dto.Audit.LastModifiedBy}");
            sb.AppendLine($"  modifiedOn: {dto.Audit.LastModifiedOn:O}");
        }

        sb.AppendLine("---");
    }
    
    private static void WriteTitle(StringBuilder sb, WorkInstructionFileDTO dto)
    {
        sb.AppendLine($"# {dto.Title}");
    }
    
    private static void WritePartsTable(StringBuilder sb, List<PartNodeFileDTO> parts)
    {
        if (parts.Count == 0)
            return;

        sb.AppendLine("<!-- MESS:PARTS -->");
        sb.AppendLine();
        sb.AppendLine("| Part Name | Part Number |");
        sb.AppendLine("|-----------|-------------|");

        foreach (var part in parts)
        {
            sb.Append("| ");
            sb.Append(part.PartName);
            sb.Append(" | ");
            sb.Append(part.PartNumber ?? "");
            sb.AppendLine(" |");
        }
    }
    
    private static void WriteStep(StringBuilder sb, StepNodeFileDTO step)
    {
        sb.AppendLine($"## {step.Name}");
        sb.AppendLine();

        WriteStepBody(sb, step);

        if (!string.IsNullOrWhiteSpace(step.DetailedBody))
        {
            sb.AppendLine();
            sb.AppendLine("### Details");
            sb.AppendLine(step.DetailedBody.Trim());
        }

        if (step.SecondaryMedia.Count == 0) return;
        sb.AppendLine();
        sb.AppendLine("### Secondary Media");

        foreach (var media in step.SecondaryMedia)
            sb.AppendLine($"![Image]({media})");
    }
    
    private static void WriteStepBody(StringBuilder sb, StepNodeFileDTO step)
    {
        if (!string.IsNullOrWhiteSpace(step.Body))
        {
            sb.AppendLine(step.Body.Trim());
            sb.AppendLine();
        }

        foreach (var media in step.PrimaryMedia)
            sb.AppendLine($"![Image]({media})");
    }
}
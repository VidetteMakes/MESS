namespace MESS.Services.Files.WorkInstructions;

/// <inheritdoc />
public class WorkInstructionMarkdownLinter : IWorkInstructionMarkdownLinter
{
    /// <inheritdoc />
    public List<string> Lint(string markdown)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(markdown))
            return ["Markdown is empty."];

        // ------------------------
        // FRONT MATTER CHECK
        // ------------------------
        if (!markdown.StartsWith("---"))
            errors.Add("Missing YAML front matter.");

        if (!markdown.Contains("# "))
            errors.Add("Missing work instruction title (H1).");

        var lines = markdown.Split('\n');

        // ------------------------
        // PARTS TABLE CHECK (NEW)
        // ------------------------
        ValidatePartsTable(lines, errors);

        // ------------------------
        // STEP + IMAGE CHECKS
        // ------------------------
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            if (line.StartsWith("## "))
            {
                var nextNonEmpty = GetNextNonEmpty(lines, i + 1);

                if (nextNonEmpty != null && nextNonEmpty.StartsWith("## "))
                    errors.Add($"Step '{line}' has no body content.");
            }

            // IMAGE VALIDATION
            if (!line.StartsWith('!')) continue;

            if (!line.StartsWith("![") || !line.Contains("](") || !line.EndsWith(')'))
            {
                errors.Add($"Malformed image syntax: '{line}'");
            }
        }

        return errors;
    }

    // ------------------------
    // PARTS TABLE VALIDATION
    // ------------------------
    private static void ValidatePartsTable(string[] lines, List<string> errors)
    {
        var hasPartsBlock = lines.Any(l => l.Contains("<!-- MESS:PARTS"));

        // If table exists but no marker → invalid
        var hasTable = lines.Any(l => l.TrimStart().StartsWith("|"));

        if (hasTable && !hasPartsBlock)
        {
            errors.Add("Parts table found but missing <!-- MESS:PARTS --> block.");
            return;
        }

        if (!hasPartsBlock)
            return;

        // Find table start
        var startIndex = Array.FindIndex(lines, l => l.Contains("<!-- MESS:PARTS"));
        if (startIndex < 0)
            return;

        var i = startIndex + 1;

        // Skip until table header
        while (i < lines.Length && !lines[i].Contains('|'))
            i++;

        if (i >= lines.Length)
        {
            errors.Add("Parts block has no table.");
            return;
        }

        // Validate header
        var header = lines[i].Trim();

        if (!header.Contains("Part Name"))
            errors.Add("Parts table missing 'Part Name' column.");

        i++;

        // Validate separator row
        if (i >= lines.Length || !lines[i].Contains("---"))
        {
            errors.Add("Parts table missing separator row.");
            return;
        }

        i++;

        // Validate rows
        for (; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            if (string.IsNullOrWhiteSpace(line))
                break;

            if (!line.StartsWith("|"))
                break;

            var cols = line.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            if (cols.Length < 1)
            {
                errors.Add($"Invalid parts row: '{line}'");
                continue;
            }

            var partName = cols.ElementAtOrDefault(0);

            if (string.IsNullOrWhiteSpace(partName))
            {
                errors.Add($"Part row missing Part Name: '{line}'");
            }
        }
    }

    // ------------------------
    // STEP UTIL
    // ------------------------
    private static string? GetNextNonEmpty(string[] lines, int start)
    {
        for (var i = start; i < lines.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(lines[i]))
                return lines[i].Trim();
        }

        return null;
    }
}
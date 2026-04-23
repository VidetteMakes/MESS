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

        // ------------------------
        // PART NODE CHECK
        // ------------------------
        if (markdown.Contains("PartName") && !markdown.Contains("<!-- MESS:PART"))
            errors.Add("Part node missing MESS:PART block.");

        // ------------------------
        // STEP CHECKS
        // ------------------------
        var lines = markdown.Split('\n');

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            if (line.StartsWith("## "))
            {
                if (i + 1 >= lines.Length)
                    continue;

                var nextNonEmpty = GetNextNonEmpty(lines, i + 1);

                if (nextNonEmpty != null && nextNonEmpty.StartsWith("## "))
                    errors.Add($"Step '{line}' has no body content.");
            }

            // ------------------------
            // IMAGE VALIDATION
            // ------------------------
            if (!line.StartsWith('!')) continue;
            if (!line.StartsWith("![" ) || !line.Contains("](") || !line.EndsWith(')'))
            {
                errors.Add($"Malformed image syntax: '{line}'");
            }
        }

        return errors;
    }

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
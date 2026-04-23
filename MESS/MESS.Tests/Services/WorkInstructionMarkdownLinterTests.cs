using MESS.Services.Files.WorkInstructions;

namespace MESS.Tests.Services;

public class WorkInstructionMarkdownLinterTests
{
    private readonly WorkInstructionMarkdownLinter _linter = new();

    [Fact]
    public void Lint_Should_Pass_For_ValidMarkdown()
    {
        const string markdown = """
                                ---
                                title: Test
                                ---

                                # Test WI

                                <!-- MESS:PARTS -->

                                | Part Name | Part Number |
                                |-----------|-------------|
                                | Housing   |             |

                                ## Step 1
                                Do something

                                ### Details
                                More info
                                """;

        var result = _linter.Lint(markdown);

        Assert.Empty(result);
    }

    [Fact]
    public void Lint_Should_Detect_Missing_FrontMatter()
    {
        var markdown = "# Title only";

        var result = _linter.Lint(markdown);

        Assert.Contains("Missing YAML front matter.", result);
    }

    [Fact]
    public void Lint_Should_Detect_Missing_Title()
    {
        const string markdown = """
                                ---
                                title: Test
                                ---
                                """;

        var result = _linter.Lint(markdown);

        Assert.Contains("Missing work instruction title (H1).", result);
    }

    [Fact]
    public void Lint_Should_Detect_Malformed_Image()
    {
        var markdown = """
                       # Test

                       ## Step
                       !broken-image
                       """;

        var result = _linter.Lint(markdown);

        Assert.Contains(result, e => e.Contains("Malformed image syntax"));
    }
}
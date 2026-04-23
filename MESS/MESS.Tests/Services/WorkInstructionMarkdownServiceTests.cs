using MESS.Services.DTOs.WorkInstructions.File;
using MESS.Services.DTOs.WorkInstructions.Nodes.PartNodes.File;
using MESS.Services.DTOs.WorkInstructions.Nodes.StepNodes.File;
using MESS.Services.Files.WorkInstructions;

namespace MESS.Tests.Services;

public class WorkInstructionMarkdownServiceTests
{
    private readonly WorkInstructionMarkdownService _service = new();

    // ----------------------------
    // ROUND TRIP TEST (CRITICAL)
    // ----------------------------
    [Fact]
    public void Serialize_Then_Parse_Should_ReturnEquivalentModel()
    {
        var original = CreateSampleDto();

        var markdown = _service.Serialize(original);
        var result = _service.Parse(markdown);

        Assert.Equal(original.Title, result.Title);
        Assert.Equal(original.Version, result.Version);
        Assert.Equal(original.ShouldGenerateQrCode, result.ShouldGenerateQrCode);
        Assert.Equal(original.PartProducedIsSerialized, result.PartProducedIsSerialized);
        Assert.Equal(original.ProducedPartName, result.ProducedPartName);

        Assert.Equal(original.AssociatedProductNames, result.AssociatedProductNames);

        Assert.NotNull(result.Audit);
        Assert.Equal(original.Audit!.CreatedBy, result.Audit!.CreatedBy);
        Assert.Equal(original.Audit.CreatedOn, result.Audit.CreatedOn);
        Assert.Equal(original.Audit.LastModifiedBy, result.Audit.LastModifiedBy);
        Assert.Equal(original.Audit.LastModifiedOn, result.Audit.LastModifiedOn);

        Assert.Equal(original.Nodes.Count, result.Nodes.Count);
    }

    // ----------------------------
    // SERIALIZE BASIC STRUCTURE
    // ----------------------------
    [Fact]
    public void Serialize_ShouldContainFrontMatterAndTitle()
    {
        var dto = CreateSampleDto();

        var markdown = _service.Serialize(dto);

        Assert.Contains("---", markdown);
        Assert.Contains($"title: {dto.Title}", markdown);
        Assert.Contains($"version: {dto.Version}", markdown);
    }

    // ----------------------------
    // STEP NODE TEST
    // ----------------------------
    [Fact]
    public void Serialize_ShouldIncludeStepSections()
    {
        var dto = new WorkInstructionFileDTO
        {
            Title = "Test WI",
            Nodes =
            {
                new StepNodeFileDTO
                {
                    Name = "Install Part",
                    Body = "Step body text",
                    DetailedBody = "Detailed instructions",
                    PrimaryMedia = { "media/a.png" },
                    SecondaryMedia = { "media/b.png" }
                }
            }
        };

        var markdown = _service.Serialize(dto);

        Assert.Contains("## Install Part", markdown);
        Assert.Contains("Step body text", markdown);
        Assert.Contains("### Details", markdown);
        Assert.Contains("Detailed instructions", markdown);
        Assert.Contains("### Secondary Media", markdown);
        Assert.Contains("media/a.png", markdown);
        Assert.Contains("media/b.png", markdown);
    }

    // ----------------------------
    // PART NODE TEST
    // ----------------------------
    [Fact]
    public void Serialize_ShouldIncludePartNodeComment()
    {
        var dto = new WorkInstructionFileDTO
        {
            Title = "Test WI",
            Nodes =
            {
                new PartNodeFileDTO
                {
                    PartName = "Housing",
                    PartNumber = "HS-100"
                }
            }
        };

        var markdown = _service.Serialize(dto);

        Assert.Contains("<!-- MESS:PART", markdown);
        Assert.Contains("name: Housing", markdown);
        Assert.Contains("number: HS-100", markdown);
    }

    // ----------------------------
    // EDGE CASE: EMPTY DTO
    // ----------------------------
    [Fact]
    public void Serialize_ShouldHandleEmptyDto()
    {
        var dto = new WorkInstructionFileDTO
        {
            Title = "Empty WI"
        };

        var markdown = _service.Serialize(dto);

        Assert.Contains("title: Empty WI", markdown);
        Assert.Contains("# Empty WI", markdown);
    }

    // ----------------------------
    // SAMPLE DATA FACTORY
    // ----------------------------
    private static WorkInstructionFileDTO CreateSampleDto()
    {
        return new WorkInstructionFileDTO
        {
            Title = "Bicycle Drivetrain",
            Version = "1.2",
            ShouldGenerateQrCode = false,
            PartProducedIsSerialized = true,
            ProducedPartName = "Widget A",

            AssociatedProductNames = { "Bicycle" },

            Audit = new AuditFileDTO
            {
                CreatedBy = "sthyen",
                CreatedOn = new DateTimeOffset(2026, 4, 20, 14, 32, 0, TimeSpan.Zero),
                LastModifiedBy = "sthyen",
                LastModifiedOn = new DateTimeOffset(2026, 4, 21, 9, 10, 0, TimeSpan.Zero)
            },

            Nodes =
            {
                new PartNodeFileDTO
                {
                    PartName = "Housing",
                    PartNumber = "HS-100"
                },
                new StepNodeFileDTO
                {
                    Name = "Install Bottom Bracket",
                    Body = "This is the step body",
                    DetailedBody = "This is the step details",
                    PrimaryMedia = { "media/bracket.png" },
                    SecondaryMedia = { "media/torque.png" }
                }
            }
        };
    }
}
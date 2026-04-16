using MESS.Data.Models;
using MESS.Services.CRUD.PartDefinitions;
using MESS.Services.CRUD.WorkInstructions;
using MESS.Services.DTOs.PartDefinitions;
using MESS.Services.DTOs.WorkInstructions.Form;
using MESS.Services.DTOs.WorkInstructions.Nodes.PartNodes.Form;
using MESS.Services.DTOs.WorkInstructions.Nodes.StepNodes.Form;
using MESS.Services.Media.WorkInstructions;
using MESS.Services.UI.WorkInstructionEditor;
using Moq;

namespace MESS.Tests.Services;

public class WorkInstructionEditorServiceTests
{
    private readonly Mock<IWorkInstructionService> _mockWorkInstructionService;
    private readonly Mock<IWorkInstructionImageService> _mockImageService;
    private readonly Mock<IPartDefinitionService> _mockPartDefinitionService;
    private readonly WorkInstructionEditorService _sut;

    public WorkInstructionEditorServiceTests()
    {
        _mockWorkInstructionService = new Mock<IWorkInstructionService>();
        _mockImageService = new Mock<IWorkInstructionImageService>();
        _mockPartDefinitionService = new Mock<IPartDefinitionService>();
        _sut = new WorkInstructionEditorService(_mockWorkInstructionService.Object, _mockImageService.Object, _mockPartDefinitionService.Object);
    }

    [Fact]
    public void StartNew_ShouldInitializeNewInstruction()
    {
        // Act
        _sut.StartNew("Test WI");

        // Assert
        Assert.NotNull(_sut.Current);
        Assert.Equal("Test WI", _sut.Current!.Title);
        Assert.Equal("1.0", _sut.Current.Version);
        Assert.Equal(EditorMode.CreateNew, _sut.Mode);
        Assert.True(_sut.IsDirty);
    }

    [Fact]
    public async Task StartNewFromCurrent_ShouldCloneFromCurrent()
    {
        // Arrange
        _sut.StartNew("Base WI");
        _sut.Current!.ShouldGenerateQrCode = true;
        _sut.Current.Nodes.Add(new StepNodeFormDTO
        {
            Name = "Step 1",
            Body = "Body 1"
        });

        _mockImageService.Setup(s => s.SaveImageFileAsync(It.IsAny<string>()))
            .ReturnsAsync("cloned.png");

        // Act
        await _sut.StartNewFromCurrent("Cloned WI");

        // Assert
        Assert.NotNull(_sut.Current);
        Assert.Equal("Cloned WI", _sut.Current!.Title);
        Assert.Single(_sut.Current.Nodes);
        Assert.Equal(EditorMode.CreateNew, _sut.Mode);
    }

    [Fact]
    public async Task LoadForEditAsync_ShouldLoadExistingInstruction()
    {
        // Arrange
        var wi = new WorkInstructionFormDTO { Id = 42, Title = "Existing" };
        _mockWorkInstructionService.Setup(s => s.GetFormByIdAsync(42)).ReturnsAsync(wi);

        // Act
        await _sut.LoadForEditAsync(42);

        // Assert
        Assert.Equal(wi, _sut.Current);
        Assert.Equal(EditorMode.EditExisting, _sut.Mode);
        Assert.False(_sut.IsDirty);
    }

    [Fact]
    public async Task LoadForEditAsync_NotFound_ShouldThrow()
    {
        _mockWorkInstructionService
            .Setup(s => s.GetFormByIdAsync(99))
            .ReturnsAsync((WorkInstructionFormDTO?)null);

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.LoadForEditAsync(99));

        Assert.Equal("Work instruction with id 99 was not found.", ex.Message);
    }

    [Fact]
    public async Task SaveAsync_CreateNew_ShouldCallCreate()
    {
        // Arrange
        _sut.StartNew("New WI");

        _mockWorkInstructionService
            .Setup(s => s.CreateAsync(It.IsAny<WorkInstructionFormDTO>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.SaveAsync();

        // Assert
        Assert.True(result);
        Assert.False(_sut.IsDirty);
        Assert.Equal(EditorMode.EditExisting, _sut.Mode);

        _mockWorkInstructionService.Verify(
            s => s.CreateAsync(It.IsAny<WorkInstructionFormDTO>()),
            Times.Once);
    }
    
    [Fact]
    public async Task SaveAsync_WithQueuedNodes_ShouldDeleteAfterSave()
    {
        // Arrange
        _sut.StartNew("New WI");
        _sut.QueueNodeForDeletion(1);
        _sut.QueueNodeForDeletion(2);

        _mockWorkInstructionService
            .Setup(s => s.CreateAsync(It.IsAny<WorkInstructionFormDTO>()))
            .ReturnsAsync(true);

        List<int>? capturedIds = null;

        _mockWorkInstructionService
            .Setup(s => s.DeleteNodesAsync(It.IsAny<IEnumerable<int>>()))
            .Callback<IEnumerable<int>>(ids =>
                capturedIds = ids.ToList())
            .ReturnsAsync(true);

        // Act
        var result = await _sut.SaveAsync();

        // Assert
        Assert.True(result);
        Assert.False(_sut.IsDirty);
        Assert.Equal(EditorMode.EditExisting, _sut.Mode);

        _mockWorkInstructionService.Verify(
            s => s.DeleteNodesAsync(It.IsAny<IEnumerable<int>>()),
            Times.Once);

        Assert.NotNull(capturedIds);
        Assert.Equal(2, capturedIds!.Count);
        Assert.Contains(1, capturedIds);
        Assert.Contains(2, capturedIds);

        Assert.Empty(_sut.NodesQueuedForDeletionIds);
    }

    [Fact]
    public void QueueNodeForDeletion_ShouldAddNodeId()
    {
        var nodeId = 123;

        _sut.QueueNodeForDeletion(nodeId);

        Assert.Contains(nodeId, _sut.NodesQueuedForDeletionIds);
    }

    [Fact]
    public void ToggleActive_ShouldFlipAndMarkDirty()
    {
        _sut.StartNew("WI");
        var before = _sut.Current!.IsActive;

        _sut.ToggleActive();

        Assert.NotEqual(before, _sut.Current.IsActive);
        Assert.True(_sut.IsDirty);
    }

    [Fact]
    public void CurrentHasPartsOrSteps_ShouldReturnExpected()
    {
        _sut.StartNew("WI");
        Assert.False(_sut.CurrentHasParts());
        Assert.False(_sut.CurrentHasSteps());

        _sut.Current!.Nodes.Add(new PartNodeFormDTO{ Name = "Some Part", Number = "Some Number"});
        _sut.Current!.Nodes.Add(new StepNodeFormDTO { Name = "Some name", Body = "Some body" });

        Assert.True(_sut.CurrentHasParts());
        Assert.True(_sut.CurrentHasSteps());
    }
}
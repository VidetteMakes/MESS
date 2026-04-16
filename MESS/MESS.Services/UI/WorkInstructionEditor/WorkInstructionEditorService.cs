using MESS.Services.CRUD.PartDefinitions;
using MESS.Services.CRUD.WorkInstructions;
using MESS.Services.DTOs.WorkInstructions.Form;
using MESS.Services.DTOs.WorkInstructions.Nodes.Form;
using MESS.Services.DTOs.WorkInstructions.Nodes.PartNodes.Form;
using MESS.Services.DTOs.WorkInstructions.Nodes.StepNodes.Form;
using MESS.Services.Media.WorkInstructions;
using Serilog;

namespace MESS.Services.UI.WorkInstructionEditor;
using Data.Models;
using System.Threading.Tasks;

/// <inheritdoc />
public class WorkInstructionEditorService : IWorkInstructionEditorService
{
    private readonly IWorkInstructionService _workInstructionService;
    private readonly IWorkInstructionImageService _workInstructionImageService;
    private readonly IPartDefinitionService _partDefinitionService;
    
    /// <inheritdoc />
    public WorkInstructionFormDTO? Current { get; private set; }
    
    private readonly HashSet<int> _nodesQueuedForDeletionIds = [];
    
    /// <inheritdoc />
    public IReadOnlyCollection<int> NodesQueuedForDeletionIds => _nodesQueuedForDeletionIds;
    
    /// <inheritdoc />
    public bool CurrentHasParts()
    {
        if (Current == null) return false;

        if (Current.Nodes.Count == 0) return false;

        return Current.Nodes.OfType<PartNodeFormDTO>().Any();
    }
    
    /// <inheritdoc />
    public bool CurrentHasSteps()
    {
        if (Current == null) return false;

        if (Current.Nodes.Count == 0) return false;

        return Current.Nodes.OfType<StepNodeFormDTO>().Any();
    }
    
    /// <inheritdoc />
    public bool IsDirty { get; private set; }
    /// <inheritdoc />
    public EditorMode Mode { get; private set; } = EditorMode.None;

    /// <inheritdoc />
    public event Action? OnChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkInstructionEditorService"/> class.
    /// </summary>
    /// <param name="workInstructionService">
    /// The service used to retrieve, create, update, and clone <see cref="WorkInstruction"/> 
    /// entities during the editing process.
    /// </param>
    /// <param name="workInstructionImageService">
    /// The service used to manipulate work instruction images.
    /// </param>
    /// <param name ="partDefinitionService">
    /// The service used to manage part definitions.
    /// </param>
    public WorkInstructionEditorService(IWorkInstructionService workInstructionService, 
        IWorkInstructionImageService workInstructionImageService,
        IPartDefinitionService partDefinitionService)
    {
        _workInstructionService = workInstructionService;
        _workInstructionImageService = workInstructionImageService;
        _partDefinitionService = partDefinitionService;
    }

    private void NotifyChanged()
    {
        OnChanged?.Invoke();
    }

    /// <inheritdoc />
    public void MarkDirty()
    {
        IsDirty = true;
        NotifyChanged();
    }

    /// <inheritdoc />
    public void StartNew(string? title = null, List<string>? products = null)
    {
        Current = new WorkInstructionFormDTO
        {
            Title = title ?? "",
            Version = "1.0",
            IsActive = false,
            IsLatest = true,
            ShouldGenerateQrCode = false,
            PartProducedIsSerialized = false,
            ProductNames = products?.Select(p => p.Trim())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList() ?? [],
            Nodes = []
        };

        Mode = EditorMode.CreateNew;
        IsDirty = true;
        NotifyChanged();
    }
    
    /// <inheritdoc />
    public async Task StartNewFromCurrent(string? title = null, List<string>? products = null)
    {
        if (Current == null)
            throw new InvalidOperationException("Cannot start a new work instruction from current because it is null.");

        var newInstruction = new WorkInstructionFormDTO
        {
            Title = title ?? Current.Title,
            Version = "1.0",
            IsActive = false,
            IsLatest = true,
            ShouldGenerateQrCode = Current.ShouldGenerateQrCode,
            PartProducedIsSerialized = Current.PartProducedIsSerialized,
            ProducedPartName = Current.ProducedPartName,
            ProductNames = Current.ProductNames?.ToList() ?? [],
            Nodes = await CloneNodesAsync(Current.Nodes)
        };

        Current = newInstruction;
        Mode = EditorMode.CreateNew;
        IsDirty = true;
        NotifyChanged();
    }
    
    /// <inheritdoc />
    public async Task LoadForEditAsync(int id)
    {
        var wi = await _workInstructionService.GetFormByIdAsync(id);

        // KeyNotFoundException: typed failure for missing entity (avoid generic Exception / clearer catch boundaries).
        Current = wi ?? throw new KeyNotFoundException($"Work instruction with id {id} was not found.");
        Mode = EditorMode.EditExisting;
        IsDirty = false;
        NotifyChanged();
    }
    
    /// <inheritdoc />
    public async Task LoadForNewVersionFromCurrentAsync()
    {
        if (Current != null)
        {
          Current = await CloneForNewVersion(Current);
          Mode = EditorMode.CreateNewVersion;
          IsDirty = true;
          NotifyChanged();          
        }
    }
    
    /// <inheritdoc />
    public async Task LoadForNewVersionAsync(int originalId)
    {
        var latestSummaries = await _workInstructionService.GetAllSummariesAsync();
        
        // Finds the latest version in the same version chain as the given originalId.
        // Matches any work instruction that is marked as IsLatest and belongs to the chain
        // identified by originalId. We allow matching either OriginalId or Id because
        // the very first version in a chain has OriginalId == Id.
        var templateSummary = latestSummaries
            .FirstOrDefault(w => w.IsLatest && (w.OriginalId == originalId || w.Id == originalId));

        if (templateSummary == null)
            throw new KeyNotFoundException($"No latest work instruction version found for original id {originalId}.");

        var templateForm = await _workInstructionService.GetFormByIdAsync(templateSummary.Id);

        if (templateForm == null)
            throw new InvalidOperationException($"Could not load work instruction data for id {templateSummary.Id}.");

        Current = await CloneForNewVersion(templateForm);
        Mode = EditorMode.CreateNewVersion;
        IsDirty = true;
        NotifyChanged();
    }
    
    /// <inheritdoc />
    public async Task LoadForNewVersionFromVersionAsync(int versionId)
    {
        // Load the version to restore by ID
        var oldVersion = await _workInstructionService.GetFormByIdAsync(versionId);
        if (oldVersion == null)
            throw new KeyNotFoundException($"Work instruction version {versionId} was not found.");

        // Clone it
        var newVersion = await CloneForNewVersion(oldVersion);

        // Assign OriginalId from the old version
        newVersion.OriginalId = oldVersion.OriginalId;

        // Set mode and mark as dirty
        Current = newVersion;
        Mode = EditorMode.CreateNewVersion;
        IsDirty = true;

        NotifyChanged();
    }
    
    /// <summary>
    /// Loads an imported <see cref="WorkInstructionFormDTO"/> into the editor service.
    /// This instruction has not been saved to the database yet.
    /// </summary>
    /// <param name="imported">The imported work instruction DTO.</param>
    public async Task LoadImportedAsync(WorkInstructionFormDTO imported)
    {
        if (imported == null) throw new ArgumentNullException(nameof(imported));

        // Optionally clone nodes to avoid accidental shared references
        var clonedNodes = await CloneNodesAsync(imported.Nodes);

        Current = new WorkInstructionFormDTO
        {
            Title = imported.Title,
            Version = imported.Version,
            OriginalId = null,
            IsActive = imported.IsActive,
            IsLatest = true,
            ShouldGenerateQrCode = imported.ShouldGenerateQrCode,
            PartProducedIsSerialized = imported.PartProducedIsSerialized,
            ProducedPartName = imported.ProducedPartName,
            ProductNames = imported.ProductNames?.ToList() ?? new List<string>(),
            Nodes = clonedNodes
        };

        Mode = EditorMode.CreateNew;
        IsDirty = true;

        NotifyChanged();
    }

    
    private async Task<WorkInstructionFormDTO> CloneForNewVersion(WorkInstructionFormDTO template)
    {
        // New DB row must use a version string not already taken for this title (IX_WorkInstructions_Title_Version).
        return new WorkInstructionFormDTO
        {
            Title = template.Title,
            Version = IncrementVersion(template.Version),
            OriginalId = template.OriginalId ?? template.Id,
            IsActive = false,
            IsLatest = true,
            ShouldGenerateQrCode = template.ShouldGenerateQrCode,
            PartProducedIsSerialized = template.PartProducedIsSerialized,
            ProducedPartName = template.ProducedPartName,
            ProductNames = template.ProductNames?.ToList() ?? [],
            Nodes = await CloneNodesAsync(template.Nodes)
        };
    }

    private async Task<List<WorkInstructionNodeFormDTO>> CloneNodesAsync(List<WorkInstructionNodeFormDTO> nodes)
    {
        if (nodes.Count == 0)
        {
            return nodes;
        }

        var clone = new List<WorkInstructionNodeFormDTO>();
        foreach (var node in nodes)
        {
            clone.Add(await CloneNodeAsync(node));
        }

        return clone?.ToList() ?? new List<WorkInstructionNodeFormDTO>();
    }

    private async Task<WorkInstructionNodeFormDTO> CloneNodeAsync(WorkInstructionNodeFormDTO node)
    {
        return node switch
        {
            PartNodeFormDTO partNode => new PartNodeFormDTO
            {
                // New unsaved node
                ClientId = Guid.NewGuid(),
                NodeType = WorkInstructionNodeType.Part,
                Position = partNode.Position,
                Name = partNode.Name,
                Number = partNode.Number,
                IsSerialNumberUnique = partNode.IsSerialNumberUnique,
                InputType = partNode.InputType
            },
            StepNodeFormDTO stepNode => new StepNodeFormDTO
            {
                ClientId = Guid.NewGuid(), // Assign a new ClientId for the cloned node
                NodeType = WorkInstructionNodeType.Step,
                Name = stepNode.Name,
                Body = stepNode.Body,
                Position = stepNode.Position,
                DetailedBody = stepNode.DetailedBody,
                PrimaryMedia = (await CloneImages(stepNode.PrimaryMedia))?.ToList() ?? [],
                SecondaryMedia = (await CloneImages(stepNode.SecondaryMedia))?.ToList() ?? []
            },
            _ => throw new NotSupportedException("Unknown WorkInstructionNode type")
        };
    }

    private async Task<List<string>> CloneImages(List<string> Images)
    {

        if (Images.Count == 0)
        {
            return Images;
        }

        var clone = new List<string>();
        foreach (var image in Images) 
        {
            clone.Add( await _workInstructionImageService.SaveImageFileAsync(image));
        }


        return clone?.ToList() ?? new List<string>();
    }

    private string IncrementVersion(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return "1.0";

        var parts = version.Split('.');
        if (parts.Length == 2 && int.TryParse(parts[1], out int minor))
        {
            return $"{parts[0]}.{minor + 1}";
        }

        return version + ".1";
    }

    /// <inheritdoc />
    public async Task<bool> SaveAsync()
    {
        if (Current == null)
            return false;

        bool success;

        // -----------------------------
        // Handle save based on editor mode
        // -----------------------------
        switch (Mode)
        {
            case EditorMode.CreateNew:
                Current.OriginalId = null;
                success = await _workInstructionService.CreateAsync(Current);
                break;

            case EditorMode.EditExisting:
                success = await _workInstructionService.UpdateWorkInstructionAsync(Current);
                break;

            case EditorMode.CreateNewVersion:
                if (Current.OriginalId == null)
                    throw new InvalidOperationException("OriginalId is required for versioning.");

                // Create new version; already handles marking old versions inactive
                var newVersion = await _workInstructionService.CreateNewVersionAsync(Current);
                if (newVersion == null)
                    return false;

                // Update Current with the newly created version's ID, version label, and status
                Current.Id = newVersion.Id;
                Current.Version = newVersion.Version;
                Current.IsLatest = newVersion.IsLatest;
                Current.IsActive = newVersion.IsActive;
                success = true;
                break;

            case EditorMode.None:
            default:
                return false;
        }

        if (!success)
            return false;

        // -----------------------------
        // Post-save editor state
        // -----------------------------
        IsDirty = false;
        Mode = EditorMode.EditExisting;
        NotifyChanged();

        // -----------------------------
        // Cleanup queued deletions
        // -----------------------------
        if (_nodesQueuedForDeletionIds.Any())
        {
            var deleted = await _workInstructionService.DeleteNodesAsync(_nodesQueuedForDeletionIds);
            if (deleted)
            {
                _nodesQueuedForDeletionIds.Clear();
            }
            else
            {
                Log.Warning(
                    "Failed to delete {Count} queued nodes after saving WorkInstruction {Id}",
                    _nodesQueuedForDeletionIds.Count,
                    Current.Id);
            }
        }

        return true;
    }

    /// <inheritdoc />
    public void Reset()
    {
        Current = null;
        IsDirty = false;
        Mode = EditorMode.None;
        NotifyChanged();
    }
    
    /// <inheritdoc />
    public void ToggleActive()
    {
        if (Current == null) return;
        Current.IsActive = !Current.IsActive;
        MarkDirty();
    }
    
    /// <inheritdoc />
    public void QueueNodeForDeletion(int nodeId)
    {
        _nodesQueuedForDeletionIds.Add(nodeId); // no duplicates allowed
    }

    
    /// <inheritdoc />
    public void SetProducedPartName(string? name, bool markDirty = true)
    {
        if (Current == null)
            return;

        Current.ProducedPartName = name?.Trim();

        if (markDirty)
            MarkDirty();
    }

}

using MESS.Services.DTOs.Products.Detail;
using MESS.Services.DTOs.WorkInstructions.Form;

namespace MESS.Services.UI.WorkInstructionEditor;

using Data.Models;


/// <summary>
/// Represents the editing mode of the <see cref="IWorkInstructionEditorService"/>,
/// indicating the context in which a WorkInstruction is being edited.
/// </summary>
public enum EditorMode
{
    /// <summary>
    /// No editing mode is currently active. No WorkInstruction is being edited.
    /// </summary>
    None,

    /// <summary>
    /// A brand new WorkInstruction is being created from scratch.
    /// The resulting instruction will not belong to any version chain.
    /// </summary>
    CreateNew,

    /// <summary>
    /// An existing WorkInstruction is being edited in place.
    /// Typically only allowed if the instruction has no associated production logs.
    /// </summary>
    EditExisting,

    /// <summary>
    /// A new version is being created based on the latest WorkInstruction in an existing version chain.
    /// The new version will receive an incremented version number and be marked as the latest.
    /// </summary>
    CreateNewVersion
}


/// <summary>
/// Represents a client-side editor service for managing the state of a single WorkInstruction being edited in the UI.
/// Supports creating new instructions, editing existing ones, and branching new versions in a version chain.
/// Tracks in-memory editing state, dirty flags, and editing modes.
/// </summary>
public interface IWorkInstructionEditorService
{
    /// <summary>
    /// Gets the WorkInstruction currently being edited in the UI.
    /// May be null if no editing session is active.
    /// </summary>
    WorkInstructionFormDTO? Current { get; }

    /// <summary>
    /// Determines if the current work instruction has parts
    /// </summary>
    /// <returns></returns>
    public bool CurrentHasParts();

    /// <summary>
    /// Determines if the current work instruction has steps
    /// </summary>
    /// <returns></returns>
    public bool CurrentHasSteps();

    /// <summary>
    /// Gets a value indicating whether the current WorkInstruction has unsaved changes.
    /// This flag is used to enable or disable Save buttons and warn users about unsaved work.
    /// </summary>
    bool IsDirty { get; }

    /// <summary>
    /// Gets the current editing mode of the service, indicating whether
    /// the user is creating a new instruction, editing an existing one,
    /// or creating a new version in an existing version chain.
    /// </summary>
    EditorMode Mode { get; }

    /// <summary>
    /// True when the loaded instruction has associated production logs and the editor is in
    /// <see cref="EditorMode.EditExisting"/> mode. Structural changes (add/delete/reorder nodes)
    /// are disabled; non-structural Step field edits remain allowed.
    /// Reset to <c>false</c> on any load other than <see cref="LoadForEditAsync"/>.
    /// </summary>
    bool IsMinimalEditingMode { get; }
    
    /// <summary>
    /// Gets a read-only collection of IDs representing the work instruction nodes
    /// that have been queued for deletion but have not yet been removed from the database.
    /// </summary>
    /// <remarks>
    /// These IDs correspond to the database IDs of the nodes and can be used
    /// by the implementing service to delete the nodes when <see cref="SaveAsync"/> or
    /// another deletion operation is performed. Duplicate IDs are automatically ignored.
    /// </remarks>
    IReadOnlyCollection<int> NodesQueuedForDeletionIds { get; }

    /// <summary>
    /// Loads an existing WorkInstruction from the database for editing.
    /// Overwrites the current editing session with the retrieved data.
    /// </summary>
    /// <param name="id">The ID of the WorkInstruction to load for editing.</param>
    /// <returns>
    /// A task that completes when the WorkInstruction has been loaded.
    /// Throws an exception if the WorkInstruction cannot be found.
    /// </returns>
    Task LoadForEditAsync(int id);

    /// <summary>
    /// Loads the current work instruction to serve as a template
    /// for creating a new version. The resulting WorkInstruction is not yet saved
    /// to the database and remains in memory for user editing.
    /// </summary>
    /// <returns>
    /// A task that completes when the new version template is prepared.
    /// </returns>
    Task LoadForNewVersionFromCurrentAsync();
    
    /// <summary>
    /// Loads the latest version of a WorkInstruction chain to serve as a template
    /// for creating a new version. The resulting WorkInstruction is not yet saved
    /// to the database and remains in memory for user editing.
    /// </summary>
    /// <param name="originalId">
    /// The ID of the original WorkInstruction that defines the version chain.
    /// </param>
    /// <returns>
    /// A task that completes when the new version template is prepared.
    /// Throws an exception if the latest version cannot be found.
    /// </returns>
    Task LoadForNewVersionAsync(int originalId);
    
    /// <summary>
    /// Loads a specific existing WorkInstruction version by its ID and creates a new editable version
    /// based on it. The new version will copy the version string from the old version.
    /// allowing the technician to modify it before saving. The new version is marked as dirty and
    /// set to <see cref="EditorMode.CreateNewVersion"/> mode.
    /// </summary>
    /// <param name="versionId">The ID of the WorkInstruction version to clone as a new version.</param>
    /// <returns>A task that completes when the new version is loaded and ready for editing.</returns>
    Task LoadForNewVersionFromVersionAsync(int versionId);

    /// <summary>
    /// Loads an imported <see cref="WorkInstructionFormDTO"/> into the editor service.
    /// This instruction has not been saved to the database yet.
    /// </summary>
    /// <param name="imported">The imported work instruction DTO.</param>
    public Task LoadImportedAsync(WorkInstructionFormDTO imported);
    
    /// <summary>
    /// Initializes a new WorkInstruction in memory for user editing.
    /// Sets the editing mode to CreateNew and marks the editing state as dirty.
    /// Optionally pre-populates the WorkInstruction with a list of products.
    /// </summary>
    /// <param name="title">An optional title for the new work instruction. If not provided, the title will be an empty string.</param>
    /// <param name="products">Optional list of product ids to prefill the WorkInstruction.</param>
    void StartNew(string? title = null, List<string>? products = null);

    /// <summary>
    /// Creates a new work instruction based on the current one, resets versioning and status flags,
    /// copies all child nodes, and optionally overrides the title and associated products.
    /// </summary>
    /// <param name="title">An optional title for the new work instruction. If not provided, the title from the current instruction is used.</param>
    /// <param name="products">An optional list of <see cref="Product"/>s to associate with the new instruction. 
    /// If not provided, the products from the current instruction will be cloned.</param>
    /// <exception cref="InvalidOperationException">Thrown if the current work instruction is null.</exception>
    /// <remarks>
    /// This method deep-copies all nodes from the current instruction using the <c>CloneNode</c> method,
    /// resets the version to "1.0", and marks the instruction as inactive and the latest version.
    /// </remarks>
    public Task StartNewFromCurrent(string? title = null, List<string>? products = null);

    /// <summary>
    /// Marks the current editing session as having unsaved changes.
    /// Typically called by UI components when the user modifies a field.
    /// Raises the OnChanged event to notify listeners of state changes.
    /// </summary>
    void MarkDirty();

    /// <summary>
    /// Resets the current editing session, clearing any in-memory
    /// WorkInstruction and resetting the dirty flag and editing mode.
    /// </summary>
    void Reset();

    /// <summary>
    /// Persists the current WorkInstruction to the database based on its editing mode.
    /// - In CreateNew mode: Creates a brand new WorkInstruction with no OriginalId.
    /// - In EditExisting mode: Updates the existing WorkInstruction in the database.
    /// - In CreateNewVersion mode: Creates a new WorkInstruction in the version chain,
    ///   updates other versions to IsLatest = false, and assigns an incremented version.
    /// Clears the dirty flag if save is successful.
    /// </summary>
    /// <returns>
    /// A boolean indicating true if the save operation succeeded; otherwise, false.
    /// </returns>
    Task<bool> SaveAsync();

    /// <summary>
    /// An event raised whenever the editing state changes.
    /// UI components can subscribe to this event to refresh themselves
    /// when the Current model, dirty flag, or mode changes.
    /// </summary>
    event Action? OnChanged;

    /// <summary>
    /// Toggles the <c>IsActive</c> property of the currently loaded work instruction in memory.
    /// Marks the editor state as dirty to indicate that changes need to be saved.
    /// This does not persist the change to the database immediately; the update will be applied when <see cref="SaveAsync"/> is called.
    /// </summary>
    void ToggleActive();

    /// <summary>
    /// Queues a node for deletion by its ID. Duplicate IDs are ignored.
    /// </summary>
    /// <param name="nodeId">The ID of the node to delete.</param>
    public void QueueNodeForDeletion(int nodeId);
    
    /// <summary>
    /// Sets the name of the part that the current work instruction produces.
    /// </summary>
    /// <param name="name">
    /// The name of the produced part. This may be <c>null</c> or empty if no part has been specified.
    /// </param>
    /// <param name="markDirty">
    /// If <c>true</c>, the editor state will be marked as having unsaved changes. 
    /// This should typically be <c>true</c> for user-initiated edits and <c>false</c> when initializing
    /// or restoring editor state (e.g., when loading an existing work instruction).
    /// </param>
    void SetProducedPartName(string? name, bool markDirty = true);
}


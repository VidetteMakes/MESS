using MESS.Services.DTOs.WorkInstructions.Form;
using MESS.Services.DTOs.WorkInstructions.Summary;
using MESS.Services.DTOs.WorkInstructions.Version;

namespace MESS.Services.CRUD.WorkInstructions;
using Data.Models;

/// <summary>
/// Interface for managing work instructions, including operations such as export, import, 
/// retrieval, creation, deletion, and updates. See the WorkInstructionEditor service for more Phoebe
/// specific work instruction editing functions.
/// </summary>
public interface IWorkInstructionService
{
    /// <summary>
    /// Determines whether the specified work instruction is editable.
    /// A work instruction is considered non-editable if it has associated production logs.
    /// </summary>
    /// <param name="workInstruction">The work instruction to evaluate.</param>
    /// <returns>
    /// A boolean value indicating whether the work instruction is editable.
    /// True if editable (i.e., no production logs exist for it); otherwise, false.
    /// </returns>
    public Task<bool> IsEditable(WorkInstruction workInstruction);
    
    /// <summary>
    /// Determines whether a given WorkInstruction is unique based on its properties and contents.
    /// </summary>
    /// <param name="workInstruction">The WorkInstruction to check for uniqueness.</param>
    /// <returns>
    /// True if the WorkInstruction is unique by counting the instances in the database. If there
    /// are 0 or 1 instances of the Title, Version combination it is unique; otherwise, false.
    /// </returns>
    public Task<bool> IsUnique(WorkInstruction workInstruction);

    /// <summary>
    /// Retrieves a List of WorkInstruction objects asynchronously
    /// </summary>
    /// <returns>List of WorkInstruction objects</returns>
    public Task<List<WorkInstruction>> GetAllAsync();
    
    /// <summary>
    /// Retrieves only the latest versions of all work instructions (IsLatest = true).
    /// </summary>
    Task<List<WorkInstruction>> GetAllLatestAsync();

    /// <summary>
    /// Asynchronously retrieves summaries of only the latest versions of all work instructions,
    /// using caching for performance.
    /// </summary>
    /// <returns>List of <see cref="WorkInstructionSummaryDTO"/> for latest work instructions.</returns>
    /// <remarks>
    /// Results are cached for 15 minutes to improve performance.
    /// </remarks>
    public Task<List<WorkInstructionSummaryDTO>> GetAllLatestSummariesAsync();

    /// <summary>
    /// Asynchronously retrieves summaries of all work instructions,
    /// including historical versions, using caching for performance.
    /// </summary>
    /// <returns>
    /// A list of <see cref="WorkInstructionSummaryDTO"/> objects representing all work instructions.
    /// Each DTO includes the work instruction details and associated products in summary form.
    /// </returns>
    /// <remarks>
    /// Results are cached for 15 minutes to improve performance.
    /// </remarks>
    public Task<List<WorkInstructionSummaryDTO>> GetAllSummariesAsync();

    /// <summary>
    /// Asynchronously retrieves the full version history for a given work instruction lineage,
    /// identified by its root instruction identifier.
    /// Results are ordered by LastModifiedOn descending (most recent edits first).
    /// </summary>
    /// <param name="originalId">
    /// The root identifier of the work instruction lineage to retrieve.
    /// This should be the Id of the original (first) version in the chain.
    /// </param>
    /// <returns>
    /// A list of <see cref="WorkInstructionVersionDTO"/> objects representing
    /// all versions in the lineage, ordered by LastModifiedOn descending.
    /// </returns>
    public Task<List<WorkInstructionVersionDTO>> GetVersionHistoryAsync(int originalId);
    
    /// <summary>
    /// Retrieves a WorkInstruction by its title.
    /// </summary>
    /// <param name="title">The title of the WorkInstruction to retrieve.</param>
    /// <returns>The WorkInstruction if found; otherwise, <c>null</c>.</returns>
    public WorkInstruction? GetByTitle(string title);
    /// <summary>
    /// Retrieves a WorkInstruction by its ID.
    /// </summary>
    /// <param name="id">The ID of the WorkInstruction to retrieve.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the WorkInstruction if found; otherwise, <c>null</c>.</returns>
    public Task<WorkInstruction?> GetByIdAsync(int id);

    /// <summary>
    /// Asynchronously retrieves a work instruction by its ID and maps it to a <see cref="WorkInstructionFormDTO"/>.
    /// Includes related products, nodes, and part information required for editing in the UI.
    /// </summary>
    /// <param name="id">The ID of the work instruction to retrieve.</param>
    /// <returns>
    /// A <see cref="WorkInstructionFormDTO"/> representing the work instruction if found; otherwise, <c>null</c>.
    /// </returns>
    public Task<WorkInstructionFormDTO?> GetFormByIdAsync(int id);

    /// <summary>
    /// Creates a new work instruction from the provided form DTO.
    /// </summary>
    /// <param name="dto">
    /// The <see cref="WorkInstructionFormDTO"/> containing scalar values,
    /// related product IDs, produced part ID, and node definitions.
    /// </param>
    /// <returns>
    /// <c>true</c> if creation was successful; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// Related entities are explicitly resolved from the database to ensure proper
    /// EF Core tracking and to prevent detached entity conflicts. The new work
    /// instruction is created as inactive by default.
    /// </remarks>
    public Task<bool> CreateAsync(WorkInstructionFormDTO dto);

    /// <summary>
    /// Updates an existing work instruction using the provided form DTO.
    /// </summary>
    /// <param name="dto">
    /// The <see cref="WorkInstructionFormDTO"/> containing updated scalar values,
    /// associated product IDs, and node definitions.
    /// </param>
    /// <returns>
    /// <c>true</c> if the work instruction was successfully updated; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// The existing entity is loaded and tracked by the DbContext before applying updates.
    /// Related entities such as products, produced part, and nodes are explicitly
    /// resolved from the database to ensure correct EF Core tracking and to prevent
    /// detached graph conflicts. Cache entries are invalidated after a successful update.
    /// </remarks>
    public Task<bool> UpdateWorkInstructionAsync(WorkInstructionFormDTO dto);
    
    /// <summary>
    /// Deletes a WorkInstruction from the database.
    /// </summary>
    /// <param name="id">The ID of the desired WorkInstruction.</param>
    /// <returns>A boolean value indicating true for success or false for failure.</returns>
    public Task<bool> DeleteByIdAsync(int id);

    /// <summary>
    /// Deletes all versions of a WorkInstruction associated with an id from the database.
    /// </summary>
    /// <param name="id">the id of the starting instruction</param>
    /// <returns></returns>
    public Task<bool> DeleteAllVersionsByIdAsync(int id);

    /// <summary>
    /// Deletes images and other media files associated with the specified <paramref name="nodes"/>.
    /// </summary>
    /// <param name="nodes">The collection of <see cref="WorkInstructionNode"/> entities whose images should be deleted.</param>
    public Task<bool> DeleteNodesAsync(IEnumerable<WorkInstructionNode> nodes);

    /// <summary>
    /// Deletes the <see cref="WorkInstructionNode"/> entities with the specified IDs,
    /// along with any associated images, from the database.
    /// </summary>
    /// <param name="nodeIds">The IDs of the nodes to delete.</param>
    /// <returns>
    /// <c>true</c> if the deletion was successful or if no matching nodes were found; 
    /// otherwise <c>false</c> if an exception occurred.
    /// </returns>
    public Task<bool> DeleteNodesAsync(IEnumerable<int> nodeIds);

    /// <summary>
    /// Creates a new version of a work instruction from the provided form DTO.
    /// </summary>
    /// <param name="dto">
    /// The form DTO containing the updated work instruction data, including
    /// associated product IDs and node definitions.
    /// </param>
    /// <returns>
    /// The newly created <see cref="WorkInstruction"/> marked as the latest
    /// and active version in the version chain, or <c>null</c> if the operation fails.
    /// </returns>
    /// <remarks>
    /// Existing versions in the chain are marked inactive and not latest.
    /// Related entities (such as products and produced part) are resolved
    /// from the database to ensure proper EF Core tracking before saving.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <see cref="WorkInstructionFormDTO.OriginalId"/> is not provided.
    /// </exception>
    public Task<WorkInstruction?> CreateNewVersionAsync(WorkInstructionFormDTO dto);
    
    /// <summary>
    /// Associates additional work instructions with the specified product,
    /// without removing existing associations.
    /// </summary>
    /// <param name="productId">The ID of the product to update.</param>
    /// <param name="workInstructionIds">A list of work instruction IDs to associate.</param>
    Task AddWorkInstructionsToProductAsync(int productId, List<int> workInstructionIds);

    /// <summary>
    /// Removes specific work instruction associations from a product.
    /// </summary>
    /// <param name="productId">The ID of the product to update.</param>
    /// <param name="workInstructionIds">A list of work instruction IDs to remove.</param>
    Task RemoveWorkInstructionsFromProductAsync(int productId, List<int> workInstructionIds);
}
using MESS.Data.Context;
using MESS.Data.Models;
using MESS.Services.DTOs.PartDefinitions;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace MESS.Services.CRUD.PartDefinitions;

/// <inheritdoc />
public class PartDefinitionService : IPartDefinitionService
{
    private readonly IDbContextFactory<ApplicationContext> _contextFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="PartDefinitionService"/> class.
    /// </summary>
    /// <param name="contextFactory">
    /// The <see cref="IDbContextFactory{TContext}"/> used to create instances of the
    /// <see cref="ApplicationContext"/> for database operations.
    /// </param>
    /// <remarks>
    /// This constructor sets up the service for performing create-or-retrieve operations
    /// on <see cref="PartDefinition"/> entities. The provided <paramref name="contextFactory"/>
    /// ensures that each operation uses its own DbContext instance, improving performance
    /// and avoiding tracking conflicts in multithreaded or scoped environments.
    /// </remarks>
    public PartDefinitionService(
        IDbContextFactory<ApplicationContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }
    
    /// <inheritdoc />
    public async Task<PartDefinition?> GetOrAddPartAsync(PartDefinition partDefinitionToAdd)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var existingPart = await context.PartDefinitions.FirstOrDefaultAsync(p =>
                p.Name == partDefinitionToAdd.Name &&
                p.Number == partDefinitionToAdd.Number);


            if (existingPart != null)
            {
                // Detach from context so that EF Core does not attempt to re-add it to the database
                context.Entry(existingPart).State = EntityState.Unchanged;
                Log.Information("GetOrAddPart: Successfully found pre-existing Part with ID: {ExistingPartID}", existingPart.Id);
                return existingPart;
            }
            
            // If a part does not exist in the database create it here
            // and return with database generated ID
            await context.PartDefinitions.AddAsync(partDefinitionToAdd);
            await context.SaveChangesAsync();
            Log.Information("Successfully created a new Part with Name: {PartName}, and Number: {PartNumber}", partDefinitionToAdd.Name, partDefinitionToAdd.Number);
            return partDefinitionToAdd;
        }
        catch (Exception e)
        {
            Log.Warning("Exception when adding part: {Exception}", e.ToString());
            return null;
        }
    }
    
    /// <inheritdoc />
    public async Task<PartDefinition?> CreateAsync(PartDefinition part)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            part.Id = 0; // defensive: force identity generation
            context.PartDefinitions.Add(part);

            await context.SaveChangesAsync();

            Log.Information(
                "Created PartDefinition '{Name}' (ID {Id})",
                part.Name,
                part.Id);

            return part;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating PartDefinition '{Name}'", part.Name);
            return null;
        }
    }

    
    /// <inheritdoc />
    public async Task<PartDefinition?> GetOrCreateByNameAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            Log.Warning("GetOrCreateByNameAsync called with empty name.");
            return null;
        }

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            // Case-insensitive match on Name
            var existing = await context.PartDefinitions
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Name.ToLower() == name.ToLower());

            if (existing != null)
            {
                Log.Information("Found existing PartDefinition '{Name}' (ID {Id})", name, existing.Id);
                return existing;
            }

            // Create a new PartDefinition with only the Name
            var newPart = new PartDefinition
            {
                Name = name,
                Number = null
            };

            context.PartDefinitions.Add(newPart);
            await context.SaveChangesAsync();

            Log.Information("Created new PartDefinition '{Name}' (ID {Id})", name, newPart.Id);

            // Return a clean, detached instance
            context.Entry(newPart).State = EntityState.Detached;
            return newPart;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in GetOrCreateByNameAsync for name '{Name}'", name);
            return null;
        }
    }
    
    /// <inheritdoc />
    public async Task<PartDefinition?> UpdateAsync(PartDefinition updated)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var existing = await context.PartDefinitions.FindAsync(updated.Id);
            if (existing == null)
            {
                Log.Warning("Update failed: PartDefinition ID {Id} not found", updated.Id);
                return null;
            }

            existing.Name = updated.Name;
            existing.Number = updated.Number;

            await context.SaveChangesAsync();

            Log.Information(
                "Updated PartDefinition '{Name}' (ID {Id})",
                existing.Name,
                existing.Id);

            return existing;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating PartDefinition ID {Id}", updated.Id);
            return null;
        }
    }
    
    /// <summary>
    /// Retrieves a list of <see cref="PartDefinition"/> entities that are
    /// common between two specified <see cref="WorkInstruction"/> objects.
    /// </summary>
    /// <param name="workInstructionIdA">
    /// The unique identifier of the first <see cref="WorkInstruction"/> to compare.
    /// </param>
    /// <param name="workInstructionIdB">
    /// The unique identifier of the second <see cref="WorkInstruction"/> to compare.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains
    /// a list of <see cref="PartDefinition"/> objects that are referenced by both
    /// work instructions through their <see cref="PartNode"/> elements.
    /// </returns>
    /// <remarks>
    /// This method identifies common parts based solely on the <see cref="PartNode.PartDefinition"/>
    /// associations within each work instruction. It does <b>not</b> include parts
    /// referenced by the <see cref="WorkInstruction.PartProduced"/> property.
    /// </remarks>
    public async Task<List<PartDefinition>> GetCommonPartDefinitionsAsync(int workInstructionIdA, int workInstructionIdB)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Load PartNodes and their PartDefinitions for both work instructions
        var workInstructionA = await context.WorkInstructions
            .Include(wi => wi.Nodes.OfType<PartNode>())
            .ThenInclude(pn => pn.PartDefinition)
            .FirstOrDefaultAsync(wi => wi.Id == workInstructionIdA);

        var workInstructionB = await context.WorkInstructions
            .Include(wi => wi.Nodes.OfType<PartNode>())
            .ThenInclude(pn => pn.PartDefinition)
            .FirstOrDefaultAsync(wi => wi.Id == workInstructionIdB);

        // Handle nulls defensively
        if (workInstructionA is null || workInstructionB is null)
            return [];

        // Extract all PartDefinitions used in each work instruction, ignoring nulls
        var partsA = workInstructionA.Nodes
            .OfType<PartNode>()
            .Select(pn => pn.PartDefinition)
            .Where(pd => pd != null)   
            .ToList();                

        var partsB = workInstructionB.Nodes
            .OfType<PartNode>()
            .Select(pn => pn.PartDefinition)
            .Where(pd => pd != null)
            .ToList();

        // Return only common parts (matched by ID)
        var commonParts = partsA
            .Where(pA => partsB.Any(pB => pB!.Id == pA!.Id))  
            .DistinctBy(p => p!.Id)                             
            .ToList();

        return commonParts!;
    }
    
    /// <summary>
    /// Retrieves all <see cref="PartDefinition"/> entities in the database.
    /// </summary>
    /// <returns>A task that returns a list of all <see cref="PartDefinition"/> objects.</returns>
    public async Task<List<PartDefinition>> GetAllAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            // Use AsNoTracking for performance since we don't intend to update these
            var parts = await context.PartDefinitions
                .AsNoTracking()
                .OrderBy(p => p.Name) // optional: sort by Name by default
                .ToListAsync();

            Log.Information("Retrieved {Count} PartDefinitions from the database.", parts.Count);

            return parts;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving all PartDefinitions.");
            return [];
        }
    }
    
    /// <summary>
    /// Retrieves all <see cref="PartDefinition"/> entities that are referenced
    /// by <see cref="PartNode"/> elements within a specified <see cref="WorkInstruction"/>.
    /// </summary>
    /// <param name="workInstructionId">
    /// The unique identifier of the <see cref="WorkInstruction"/> whose part definitions should be retrieved.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains
    /// a list of <see cref="PartDefinition"/> objects referenced by part nodes
    /// in the specified work instruction.
    /// </returns>
    /// <remarks>
    /// This method returns only part definitions associated with <see cref="PartNode"/> elements.
    /// It does <b>not</b> include the part defined in <see cref="WorkInstruction.PartProduced"/>.
    /// </remarks>
    public async Task<List<PartDefinition>> GetByWorkInstructionIdAsync(int workInstructionId)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var workInstruction = await context.WorkInstructions
                .Include(wi => wi.Nodes.OfType<PartNode>())
                    .ThenInclude(pn => pn.PartDefinition)
                .FirstOrDefaultAsync(wi => wi.Id == workInstructionId);

            if (workInstruction is null)
            {
                Log.Warning("WorkInstruction with ID {WorkInstructionId} not found.", workInstructionId);
                return [];
            }

            var partDefinitions = workInstruction.Nodes
                .OfType<PartNode>()
                .Select(pn => pn.PartDefinition ?? throw new InvalidOperationException(
                    $"PartNode {pn.Id} has no PartDefinition loaded"))
                .DistinctBy(pd => pd.Id)
                .ToList();

            Log.Information("Retrieved {Count} part definitions for WorkInstruction ID {WorkInstructionId}.",
                partDefinitions.Count, workInstructionId);

            return partDefinitions;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving part definitions for WorkInstruction ID {WorkInstructionId}.", workInstructionId);
            return [];
        }
    }
    
    /// <summary>
    /// Retrieves a single <see cref="PartDefinition"/> entity by its part number.
    /// </summary>
    /// <param name="number">
    /// The part number of the <see cref="PartDefinition"/> to retrieve.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains
    /// the matching <see cref="PartDefinition"/> if found; otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    /// This method performs a case-insensitive lookup based on the <see cref="PartDefinition.Number"/> property.
    /// </remarks>
    public async Task<PartDefinition?> GetByNumberAsync(string number)
    {
        if (string.IsNullOrWhiteSpace(number))
        {
            Log.Warning("GetByNumberAsync called with null or empty part number.");
            return null;
        }

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var part = await context.PartDefinitions
                .FirstOrDefaultAsync(p => (p.Number ?? "").Equals(number, StringComparison.CurrentCultureIgnoreCase));

            if (part is null)
            {
                Log.Information("No PartDefinition found with Number: {PartNumber}", number);
                return null;
            }

            Log.Information("Retrieved PartDefinition with ID {PartId} for Number {PartNumber}", part.Id, number);
            return part;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving PartDefinition with Number {PartNumber}", number);
            return null;
        }
    }
    
    /// <summary>
    /// Retrieves a single <see cref="PartDefinition"/> entity by its unique identifier.
    /// </summary>
    /// <param name="id">
    /// The unique identifier of the <see cref="PartDefinition"/> to retrieve.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains
    /// the matching <see cref="PartDefinition"/> if found; otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    /// This method performs a direct primary key lookup using the database context
    /// for efficiency. The returned entity is not tracked beyond the scope of the query.
    /// </remarks>
    public async Task<PartDefinition?> GetByIdAsync(int id)
    {
        if (id <= 0)
        {
            Log.Warning("GetByIdAsync called with an invalid ID: {PartId}", id);
            return null;
        }

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var part = await context.PartDefinitions
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (part is null)
            {
                Log.Information("No PartDefinition found with ID: {PartId}", id);
                return null;
            }

            Log.Information("Retrieved PartDefinition with ID: {PartId} and Number: {PartNumber}", part.Id, part.Number);
            return part;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving PartDefinition with ID: {PartId}", id);
            return null;
        }
    }
    
    /// <summary>
    /// Retrieves a list of <see cref="PartDefinition"/> entities that match the provided identifiers.
    /// </summary>
    /// <param name="ids">
    /// A collection of unique identifiers corresponding to the <see cref="PartDefinition"/> records to retrieve.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains
    /// a list of <see cref="PartDefinition"/> entities that match the provided identifiers.
    /// </returns>
    /// <remarks>
    /// This method performs a batched lookup for all provided IDs in a single database query.
    /// The returned entities are retrieved using <see cref="EntityFrameworkQueryableExtensions.AsNoTracking{TEntity}(IQueryable{TEntity})"/>
    /// for performance optimization. Duplicate or invalid IDs are automatically ignored.
    /// </remarks>
    public async Task<List<PartDefinition>> GetByIdsAsync(IEnumerable<int> ids)
    {
        var enumerable = ids.ToList();
        if (enumerable.Count == 0)
        {
            Log.Warning("GetByIdsAsync called with empty ID list.");
            return [];
        }

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var distinctIds = enumerable.Distinct().Where(id => id > 0).ToList();

            var parts = await context.PartDefinitions
                .AsNoTracking()
                .Where(p => distinctIds.Contains(p.Id))
                .ToListAsync();

            Log.Information("Retrieved {Count} PartDefinitions for {RequestedCount} requested IDs.",
                parts.Count, distinctIds.Count);

            return parts;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving PartDefinitions for provided ID collection.");
            return [];
        }
    }
    
    ///  <inheritdoc/>
    public async Task<bool> ExistsAsync(string name, string? number = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            Log.Warning("ExistsAsync called with empty name.");
            return false;
        }

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var query = context.PartDefinitions.AsNoTracking().Where(p => p.Name.ToLower() == name.ToLower());

            if (!string.IsNullOrWhiteSpace(number))
                query = query.Where(p => (p.Number ?? "").Equals(number, StringComparison.OrdinalIgnoreCase));

            return await query.AnyAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error checking existence of PartDefinition '{Name}'", name);
            return false;
        }
    }
    
    /// <inheritdoc />
    public async Task<DeletePartDefinitionResponse> DeleteAsync(PartDefinition partDefinition)
    {
        if (partDefinition.Id <= 0)
            return new DeletePartDefinitionResponse { Result = DeletePartDefinitionResult.NotFound };

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var entity = await context.PartDefinitions.FindAsync(partDefinition.Id);
            if (entity is null)
                return new DeletePartDefinitionResponse { Result = DeletePartDefinitionResult.NotFound };

            var usages = await context.Set<WorkInstructionNode>()
                .OfType<PartNode>()
                .Where(pn => pn.PartDefinitionId == partDefinition.Id)
                .Join(
                    context.WorkInstructions,
                    pn => EF.Property<int>(pn, "WorkInstructionId"),
                    wi => wi.Id,
                    (pn, wi) => new PartDefinitionUsage
                    {
                        PartNodeId = pn.Id,
                        NodePosition = pn.Position,
                        WorkInstructionId = wi.Id,
                        WorkInstructionTitle = wi.Title
                    })
                .ToListAsync();

            if (usages.Count > 0)
            {
                Log.Warning(
                    "Cannot delete PartDefinition '{Name}' (ID {Id}); referenced by {UsageCount} part nodes.",
                    entity.Name,
                    entity.Id,
                    usages.Count);

                return new DeletePartDefinitionResponse
                {
                    Result = DeletePartDefinitionResult.InUse,
                    Usages = usages
                };
            }

            context.PartDefinitions.Remove(entity);
            await context.SaveChangesAsync();

            Log.Information(
                "Deleted PartDefinition '{Name}' (ID {Id})",
                entity.Name,
                entity.Id);

            return new DeletePartDefinitionResponse { Result = DeletePartDefinitionResult.Success };
        }
        catch (Exception ex)
        {
            Log.Error(
                ex,
                "Error deleting PartDefinition '{Name}' (ID {Id})",
                partDefinition.Name,
                partDefinition.Id);

            return new DeletePartDefinitionResponse { Result = DeletePartDefinitionResult.Error };
        }
    }
    
    /// <inheritdoc />
    public async Task<bool> MergePartDefinitionsAsync(PartDefinition target, PartDefinition source)
    {
        if (target == null) throw new ArgumentNullException(nameof(target));
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (target.Id == source.Id)
        {
            Log.Warning("MergePartDefinitionsAsync called with identical PartDefinition IDs {Id}", target.Id);
            return false;
        }

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            // Use the execution strategy for retries
            var strategy = context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                // Wrap everything in a transaction inside the strategy
                await using var transaction = await context.Database.BeginTransactionAsync();

                // Reload tracked entities
                var targetTracked = await context.PartDefinitions.FindAsync(target.Id);
                var sourceTracked = await context.PartDefinitions.FindAsync(source.Id);

                if (targetTracked == null || sourceTracked == null)
                {
                    Log.Warning("One of the PartDefinitions not found. Target ID {TargetId}, Source ID {SourceId}", target.Id, source.Id);
                    return false;
                }

                // --- Fill missing fields on target ---
                if (string.IsNullOrWhiteSpace(targetTracked.Number) && !string.IsNullOrWhiteSpace(sourceTracked.Number))
                    targetTracked.Number = sourceTracked.Number;

                if (string.IsNullOrWhiteSpace(targetTracked.Name) && !string.IsNullOrWhiteSpace(sourceTracked.Name))
                    targetTracked.Name = sourceTracked.Name;

                // --- Update all foreign keys referencing source ---
                var partNodes = await context.PartNodes
                    .Where(pn => pn.PartDefinitionId == sourceTracked.Id)
                    .ToListAsync();
                partNodes.ForEach(pn => pn.PartDefinitionId = targetTracked.Id);

                var products = await context.Products
                    .Where(p => p.PartDefinitionId == sourceTracked.Id)
                    .ToListAsync();
                products.ForEach(p => p.PartDefinitionId = targetTracked.Id);

                var serializableParts = await context.SerializableParts
                    .Where(sp => sp.PartDefinitionId == sourceTracked.Id)
                    .ToListAsync();
                serializableParts.ForEach(sp => sp.PartDefinitionId = targetTracked.Id);

                var workInstructions = await context.WorkInstructions
                    .Where(wi => wi.PartProducedId == sourceTracked.Id)
                    .ToListAsync();
                workInstructions.ForEach(wi => wi.PartProducedId = targetTracked.Id);

                // --- Remove the source part ---
                context.PartDefinitions.Remove(sourceTracked);

                // --- Save all changes ---
                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                Log.Information(
                    "Merged PartDefinition ID {SourceId} into ID {TargetId}. Updated {PN} PartNodes, {P} Products, {SP} SerializableParts, {WI} WorkInstructions.",
                    sourceTracked.Id, targetTracked.Id,
                    partNodes.Count, products.Count, serializableParts.Count, workInstructions.Count);

                return true;
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error merging PartDefinition ID {SourceId} into ID {TargetId}", source.Id, target.Id);
            return false;
        }
    }
    
    /// <inheritdoc />
    public async Task<int> BatchMergeByNameAsync(IProgress<int>? progress = null)
    {
        int mergeCount = 0;

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            // Load all PartDefinitions
            var allParts = await context.PartDefinitions
                .OrderBy(p => p.Name) // optional
                .ToListAsync();

            // Group by Name (case-insensitive) and filter duplicates
            var grouped = allParts
                .GroupBy(p => p.Name?.ToLower())
                .Where(g => g.Count() > 1)
                .ToList();

            int totalGroups = grouped.Count;
            int currentGroup = 0;

            foreach (var group in grouped)
            {
                // Prefer a target that has both Name and Number
                var target = group
                    .Where(p => !string.IsNullOrWhiteSpace(p.Number))
                    .OrderBy(p => p.Id)
                    .FirstOrDefault() ?? group.OrderBy(p => p.Id).First();

                foreach (var source in group.Where(p => p.Id != target.Id))
                {
                    bool merged = await MergePartDefinitionsAsync(target, source);
                    if (merged) mergeCount++;
                }

                // Update progress after processing each group
                currentGroup++;
                int percent = (int)((double)currentGroup / totalGroups * 100);
                progress?.Report(percent);
            }

            Log.Information("Batch merge complete. Total merges performed: {Count}", mergeCount);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error performing batch merge of PartDefinitions by Name.");
        }

        return mergeCount;
    }
}
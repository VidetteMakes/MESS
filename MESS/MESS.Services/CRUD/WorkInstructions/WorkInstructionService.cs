using MESS.Data.Context;
using MESS.Services.CRUD.PartDefinitions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Serilog;
using MESS.Services.CRUD.ProductionLogs;
using MESS.Services.CRUD.Products;
using MESS.Services.DTOs.WorkInstructions.File;
using MESS.Services.DTOs.WorkInstructions.Form;
using MESS.Services.DTOs.WorkInstructions.Summary;
using MESS.Services.DTOs.WorkInstructions.Version;
using MESS.Services.Git;
using MESS.Services.Media.WorkInstructions;

namespace MESS.Services.CRUD.WorkInstructions;
using Data.Models;

/// <inheritdoc />
public class WorkInstructionService : IWorkInstructionService
{
    private readonly IWorkInstructionGitSyncService _gitSyncService;
    private readonly IProductionLogService _productionLogService;
    private readonly IWorkInstructionImageService _imageService;
    private readonly IMemoryCache _cache;
    private readonly IWorkInstructionUpdater _workInstructionUpdater;
    private readonly IProductResolver _productResolver;
    private readonly IPartNodeResolver _partNodeResolver;
    private readonly IPartDefinitionResolver _partDefinitionResolver;
    private readonly IDbContextFactory<ApplicationContext> _contextFactory;
    
    private const string WORK_INSTRUCTION_CACHE_KEY = "AllWorkInstructions";
    private const string WORK_INSTRUCTION_LATEST_CACHE_KEY = "AllLatestWorkInstructions";
    private const string WORK_INSTRUCTION_LATEST_SUMMARY_CACHE_KEY = "AllLatestWorkInstructionSummaries";
    private const string WORK_INSTRUCTION_SUMMARY_CACHE_KEY = "AllWorkInstructionSummaries";
    
    /// <summary>
    /// Initializes a new instance of the <see cref="WorkInstructionService"/> class.
    /// </summary>
    /// <param name="gitSyncService">The service called to manage Markdown work instructions from a Git repository.</param>
    /// <param name="productionLogService">The service for managing product-related operations.</param>
    /// <param name="imageService">The service for managing work instruction image-related operations.</param>
    /// <param name="cache">The memory cache for caching work instructions.</param>
    /// <param name="workInstructionUpdater"> The service responsible for applying updates from form DTOs to existing work instruction entities.</param>
    /// <param name="productResolver"> The service responsible for resolving products.</param>
    /// <param name="partNodeResolver"> The service responsible for resolving part definitions for part nodes when updating a work instruction.</param>
    /// <param name="partDefinitionResolver"> The service responsible for resolving part definitions.</param>
    /// <param name="contextFactory">The factory for creating database contexts.</param>
    public WorkInstructionService(
        IWorkInstructionGitSyncService gitSyncService,
        IProductionLogService productionLogService, 
        IWorkInstructionImageService imageService, IMemoryCache cache, 
        IWorkInstructionUpdater workInstructionUpdater,
        IProductResolver productResolver,
        IPartNodeResolver partNodeResolver,
        IPartDefinitionResolver partDefinitionResolver,
        IDbContextFactory<ApplicationContext> contextFactory)
    {
        _gitSyncService = gitSyncService;
        _productionLogService = productionLogService;
        _cache = cache;
        _imageService = imageService;
        _workInstructionUpdater = workInstructionUpdater;
        _productResolver = productResolver;
        _partNodeResolver = partNodeResolver;
        _partDefinitionResolver = partDefinitionResolver;
        _contextFactory = contextFactory;
        
    }
    
    /// <inheritdoc />
    public async Task<bool> IsEditable(WorkInstruction workInstruction)
    {
        if (workInstruction is not { Id: > 0 })
        {
            Log.Warning("Cannot check editability: Work instruction is null or has invalid ID");
            return false;
        }
        
        try
        {
            await using var localContext = await _contextFactory.CreateDbContextAsync();
            
            // Check if any production logs reference this work instruction. If so the Instruction is not editable.
            var hasProductionLogs = await localContext.ProductionLogs
                .AsNoTracking()
                .AnyAsync(p => p.WorkInstruction != null && p.WorkInstruction.Id == workInstruction.Id);

            return !hasProductionLogs;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Unable to determine if work instruction {WorkInstructionId} ({Title}) is editable", workInstruction.Id, workInstruction.Title);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsUnique(WorkInstruction workInstruction)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var title = workInstruction.Title;
            var version = workInstruction.Version;

            var isUniqueCount = await context.WorkInstructions
                .CountAsync(w => w.Title == title && w.Version == version);

            if (isUniqueCount >= 1)
            {
                return false;
            }
            
            if (isUniqueCount is 0)
            {
                return true;
            }

            return false;
        }
        catch (Exception e)
        {
            Log.Warning("Unable to determine if WorkInstruction with Title: {Title}, and Version: {Version} is unique. Exception thrown: {Exception}", workInstruction.Title, workInstruction.Version, e.ToString());
            return false;
        }
    }

    /// <summary>
    /// Asynchronously retrieves all work instructions, using caching for performance.
    /// </summary>
    /// <returns>List of work instructions.</returns>
    /// <remarks>
    /// Results are cached for 15 minutes to improve performance.
    /// </remarks>
    public async Task<List<WorkInstruction>> GetAllAsync()
    {
        if (_cache.TryGetValue(WORK_INSTRUCTION_CACHE_KEY, out List<WorkInstruction>? cachedWorkInstructionList) &&
            cachedWorkInstructionList != null)
        {
            return cachedWorkInstructionList;
        }
        
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var workInstructions = await context.WorkInstructions
                .Include(w => w.Products)
                .Include(w => w.Nodes)
                .Include(w => w.PartProduced)
                .ToListAsync();
            
            // Load PartDefinition for all PartNodes (derived entities)
            await context.Set<PartNode>()
                .Include(pn => pn.PartDefinition)
                .LoadAsync();

            // Cache data for 15 minutes
            _cache.Set(WORK_INSTRUCTION_CACHE_KEY, workInstructions, TimeSpan.FromMinutes(15));

            Log.Information("GetAllAsync Successfully retrieved a List of {WorkInstructionCount} WorkInstructions", workInstructions.Count);
            return workInstructions;
        }
        catch (Exception e)
        {
            Log.Warning("Exception thrown when attempting to GetAllAsync Work Instructions, in WorkInstructionService. Exception: {Exception}", e.ToString());
            return [];
        }
    }

    /// <summary>
    /// Asynchronously retrieves only the latest versions of all work instructions,
    /// using caching for performance.
    /// </summary>
    /// <returns>List of work instructions where IsLatest is true.</returns>
    /// <remarks>
    /// Results are cached for 15 minutes to improve performance.
    /// </remarks>
    public async Task<List<WorkInstruction>> GetAllLatestAsync()
    {
        if (_cache.TryGetValue(WORK_INSTRUCTION_LATEST_CACHE_KEY, out List<WorkInstruction>? cachedLatestList) &&
            cachedLatestList != null)
        {
            return cachedLatestList;
        }

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
           
            // Load all latest work instructions and their base relationships
            var latestWorkInstructions = await context.WorkInstructions
                .Where(w => w.IsLatest)
                .Include(w => w.Products)
                .Include(w => w.Nodes)
                .Include(w => w.PartProduced)
                .ToListAsync();

            // Explicitly load PartDefinition for derived PartNodes
            await context.Set<PartNode>()
                .Include(pn => pn.PartDefinition)
                .LoadAsync();

            // Cache data for 15 minutes
            _cache.Set(WORK_INSTRUCTION_LATEST_CACHE_KEY, latestWorkInstructions, TimeSpan.FromMinutes(15));

            Log.Information("GetAllLatestAsync successfully retrieved {WorkInstructionCount} latest WorkInstructions", latestWorkInstructions.Count);
            return latestWorkInstructions;
        }
        catch (Exception e)
        {
            Log.Warning("Exception thrown in GetAllLatestAsync in WorkInstructionService. Exception: {Exception}", e.ToString());
            return [];
        }
    }
    
    /// <inheritdoc />
    public async Task<List<WorkInstructionSummaryDTO>> GetAllSummariesAsync()
    {
        if (_cache.TryGetValue(WORK_INSTRUCTION_SUMMARY_CACHE_KEY, out List<WorkInstructionSummaryDTO>? cachedSummaries) &&
            cachedSummaries != null)
        {
            return cachedSummaries;
        }

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            // Load all work instructions with related entities needed for summaries
            var allWorkInstructions = await context.WorkInstructions
                .Include(w => w.Products)        // Needed for ProductSummaryDTO
                .Include(w => w.PartProduced)    // Needed for PartProducedName/Number
                .ToListAsync();

            // Map entities to summary DTOs
            var summaries = allWorkInstructions
                .Select(wi => wi.ToSummaryDTO())
                .ToList();

            // Cache data for 15 minutes
            _cache.Set(WORK_INSTRUCTION_SUMMARY_CACHE_KEY, summaries, TimeSpan.FromMinutes(15));

            Log.Information("GetAllSummariesAsync successfully retrieved {Count} WorkInstruction summaries", summaries.Count);
            return summaries;
        }
        catch (Exception e)
        {
            Log.Warning("Exception thrown in GetAllSummariesAsync in WorkInstructionService. Exception: {Exception}", e.ToString());
            return new List<WorkInstructionSummaryDTO>();
        }
    }
    
    /// <inheritdoc />
    public async Task<List<WorkInstructionSummaryDTO>> GetAllLatestSummariesAsync()
    {
        const string cacheKey = WORK_INSTRUCTION_LATEST_SUMMARY_CACHE_KEY;

        if (_cache.TryGetValue(cacheKey, out List<WorkInstructionSummaryDTO>? cachedSummaries) &&
            cachedSummaries != null)
        {
            return cachedSummaries;
        }

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            // Load all latest work instructions with related entities needed for summaries
            var latestWorkInstructions = await context.WorkInstructions
                .Where(w => w.IsLatest)
                .Include(w => w.Products)        // Needed for ProductSummaryDTO
                .Include(w => w.PartProduced)    // Needed for PartProducedName/Number
                .ToListAsync();

            // Map entities to summary DTOs
            var summaries = latestWorkInstructions
                .Select(wi => wi.ToSummaryDTO())
                .ToList();

            // Cache data for 15 minutes
            _cache.Set(cacheKey, summaries, TimeSpan.FromMinutes(15));

            Log.Information("GetAllLatestSummariesAsync successfully retrieved {Count} WorkInstruction summaries", summaries.Count);
            return summaries;
        }
        catch (Exception e)
        {
            Log.Warning("Exception thrown in GetAllLatestSummariesAsync in WorkInstructionService. Exception: {Exception}", e.ToString());
            return [];
        }
    }
    
    /// <summary>
    /// Retrieves a work instruction by its title.
    /// </summary>
    /// <param name="title">The title of the work instruction to retrieve.</param>
    /// <returns>The work instruction if found, null otherwise.</returns>
    public WorkInstruction? GetByTitle(string title)
    {
        try
        {
            using var context =  _contextFactory.CreateDbContext();
            var workInstruction = context.WorkInstructions
                .FirstOrDefault(w => w.Title.ToLower() == title.ToLower());

            Log.Information("Successfully retrieved a WorkInstruction by title: {Title}", title);
            return workInstruction;
        }
        catch (Exception e)
        {
            Log.Warning("Exception: {exceptionType} thrown when attempting to GetByTitle with Title: {title}, in WorkInstructionService", e.GetBaseException().ToString(), title);
            return null;
        }
    }
    
    /// <summary>
    /// Asynchronously retrieves a work instruction by its ID, including related nodes and parts.
    /// </summary>
    /// <param name="id">The ID of the work instruction to retrieve.</param>
    /// <returns>The work instruction with its related data if found, null otherwise.</returns>
    public async Task<WorkInstruction?> GetByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
            {
                return null;
            }
            
            await using var context = await _contextFactory.CreateDbContextAsync();
            var workInstruction = await context.WorkInstructions
                .Include(w => w.Products)
                .ThenInclude(p => p.PartDefinition)
                .Include(w => w.Nodes)
                .ThenInclude(w => ((PartNode)w).PartDefinition)
                .Include(w => w.PartProduced)
                .FirstOrDefaultAsync(w => w.Id == id);
            
            SortNodesByPosition(workInstruction);
            
            return workInstruction;
        }
        catch (Exception e)
        {
            Log.Warning("Exception thrown when attempting to GetByIdAsync with ID: {id}, in WorkInstructionService. Exception: {Exception}", id, e.ToString());
            return null;
        }
    }
    
    /// <summary>
    /// Asynchronously retrieves a work instruction by its ID and maps it to a <see cref="WorkInstructionFormDTO"/>.
    /// Includes related products, nodes, and part information required for editing in the UI.
    /// </summary>
    /// <param name="id">The ID of the work instruction to retrieve.</param>
    /// <returns>
    /// A <see cref="WorkInstructionFormDTO"/> representing the work instruction if found; otherwise, <c>null</c>.
    /// </returns>
    public async Task<WorkInstructionFormDTO?> GetFormByIdAsync(int id)
    {
        if (id <= 0)
            return null;

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            // Load work instruction with related entities needed for the form
            var workInstruction = await context.WorkInstructions
                .Include(w => w.Products)
                .ThenInclude(p => p.PartDefinition)// Needed for ProductNames
                .Include(w => w.Nodes)         // Needed for node DTOs
                .ThenInclude(n => ((PartNode)n).PartDefinition) // Needed if nodes reference PartDefinition
                .Include(w => w.PartProduced)  // Needed for PartProducedId / serialized info
                .FirstOrDefaultAsync(w => w.Id == id);

            if (workInstruction == null)
                return null;

            // Sort nodes by position (just in case)
            SortNodesByPosition(workInstruction);

            // Map to Form DTO using your existing mapper
            var dto = workInstruction.ToFormDTO();

            Log.Debug("Form DTO Products: {ProductNames}", string.Join(", ", dto.ProductNames));

            return dto;
        }
        catch (Exception e)
        {
            Log.Warning(
                "Exception thrown when attempting to GetFormByIdAsync with ID: {id} in WorkInstructionService. Exception: {Exception}",
                id,
                e.ToString()
            );

            return null;
        }
    }

    
    /// <summary>
    /// Ensures that the WorkInstruction's Nodes are ordered by their Position property.
    /// </summary>
    private static void SortNodesByPosition(WorkInstruction? workInstruction)
    {
        if (workInstruction?.Nodes == null || workInstruction.Nodes.Count == 0)
            return;

        workInstruction.Nodes = workInstruction.Nodes
            .OrderBy(n => n.Position)
            .ToList();
    }
    
    /// <inheritdoc />
    public async Task<List<WorkInstructionVersionDTO>> GetVersionHistoryAsync(int originalId)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var versionHistory = await context.WorkInstructions
                .AsNoTracking()
                .Where(w => w.OriginalId == originalId || w.Id == originalId)
                .OrderByDescending(w => w.LastModifiedOn)
                .Select(w => new WorkInstructionVersionDTO
                {
                    Id = w.Id,
                    RootInstructionId = w.OriginalId ?? w.Id,
                    Version = w.Version,
                    Title = w.Title,
                    LastModifiedOn = w.LastModifiedOn,
                    LastModifiedBy = w.LastModifiedBy
                })
                .ToListAsync();

            Log.Information(
                "GetVersionHistoryAsync successfully retrieved {Count} versions for RootInstructionId {RootInstructionId}",
                versionHistory.Count,
                originalId
            );

            return versionHistory;
        }
        catch (Exception e)
        {
            Log.Warning(
                "Exception thrown in GetVersionHistoryAsync in WorkInstructionService. Exception: {Exception}",
                e.ToString()
            );

            return [];
        }
    }
    
    /// <inheritdoc />
    public async Task<bool> CreateAsync(WorkInstructionFormDTO dto)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        try
        {
            // Build base entity from mapper
            var workInstruction = dto.ToNewEntity();

            // Validate entity
            var validator = new WorkInstructionValidator();
            var validationResult = await validator.ValidateAsync(workInstruction);

            if (!validationResult.IsValid)
                return false;

            // Resolve produced part
            workInstruction.PartProduced =
                await _partDefinitionResolver.ResolveAsync(context, dto.ProducedPartName, null);

            // Resolve Products (internally calls the part definition resolver)
            workInstruction.Products =
                await _productResolver.ResolveProductsAsync(context, dto.ProductNames);

            // Resolve all PartNodes via PartNodeResolver
            await _partNodeResolver.ResolvePendingNodesAsync(context, workInstruction.Nodes);

            // Default new WorkInstructions to active and latest since this is a new creation (not a version update)
            workInstruction.IsActive = true;
            workInstruction.IsLatest = true;

            await context.WorkInstructions.AddAsync(workInstruction);
            await context.SaveChangesAsync();
            
            // Git commit AFTER DB success
            var dtoForGit = workInstruction.ToFileDTO();

            var commitMessage = $"Create WorkInstruction: {workInstruction.Title}";

            await _gitSyncService.CommitAsync(
                dto: dtoForGit,
                commitMessage: commitMessage,
                originalTitle: null); // new file

            ClearWorkInstructionCaches();

            Log.Information(
                "Successfully created WorkInstruction with ID: {Id}",
                workInstruction.Id);

            return true;
        }
        catch (Exception e)
        {
            Log.Warning(
                "Exception thrown when creating WorkInstruction. Exception: {Exception}",
                e);

            return false;
        }
    }
    
    /// <inheritdoc />
    public async Task<WorkInstruction?> CreateNewVersionAsync(WorkInstructionFormDTO dto)
    {
        if (dto.OriginalId == null)
            throw new InvalidOperationException("OriginalId is required to create a new version.");

        await using var context = await _contextFactory.CreateDbContextAsync();
        var strategy = context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync();

            // Resolve the referenced work instruction to determine the true root
            var referenced = await context.WorkInstructions
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.Id == dto.OriginalId.Value);

            if (referenced == null)
                throw new InvalidOperationException("Referenced work instruction not found.");

            var rootId = referenced.OriginalId ?? referenced.Id;

            // Load full version chain
            var versions = await context.WorkInstructions
                .Where(w => w.Id == rootId || w.OriginalId == rootId)
                .ToListAsync();

            if (versions.Count == 0)
                throw new InvalidOperationException("No existing version chain found.");

            // Deactivate all existing versions
            foreach (var wi in versions)
            {
                wi.IsActive = false;
                wi.IsLatest = false;
            }

            // Build new entity from DTO
            var workInstruction = dto.ToNewEntity();

            // Force insert (never reuse existing id)
            workInstruction.Id = 0;

            // Unique (Title, Version): prior rows stay in the table (only flags updated), so the new row
            // must use a version string not present for this title — bump until free even if UI pre-bumped once.
            var usedVersions = versions
                .Select(w => w.Version)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var candidate = string.IsNullOrWhiteSpace(workInstruction.Version)
                ? "1.0"
                : workInstruction.Version;
            while (usedVersions.Contains(candidate))
            {
                candidate = BumpWorkInstructionVersionString(candidate);
            }

            workInstruction.Version = candidate;

            // Ensure correct root linkage
            workInstruction.OriginalId = rootId;

            workInstruction.PartProduced =
                await _partDefinitionResolver.ResolveAsync(context, dto.ProducedPartName, null);

            workInstruction.Products = await _productResolver.ResolveProductsAsync(context, dto.ProductNames);
            
            await _partNodeResolver.ResolvePendingNodesAsync(context, workInstruction.Nodes);

            // Mark new version active/latest
            workInstruction.IsLatest = true;
            workInstruction.IsActive = true;

            await context.WorkInstructions.AddAsync(workInstruction);
            await context.SaveChangesAsync();

            await transaction.CommitAsync();

            ClearWorkInstructionCaches();

            Log.Information(
                "Created new WorkInstruction version {Id} for RootId {RootId}",
                workInstruction.Id,
                rootId);
            try
            {
                // AFTER DB commit → write to Git
                await _gitSyncService.CommitAsync(
                        workInstruction.ToFileDTO(),
                        $"New version {workInstruction.Version} created from UI",
                        originalTitle: referenced.Title);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Git sync failed for WorkInstruction {Id}", workInstruction.Id);
            }

            return workInstruction;
        });
    }
    
    /// <summary>
    /// Deletes a Work Instruction from the database if there are no Production log relationships.
    /// </summary>
    /// <param name="id">The ID of the work instruction to delete.</param>
    /// <returns>True if deletion was successful, false otherwise.</returns>
    /// <remarks>
    /// The cache is invalidated after successful deletion.
    /// </remarks>
    public async Task<bool> DeleteByIdAsync(int id)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var workInstruction = await context.WorkInstructions
                .Include(w => w.Nodes)
                .FirstOrDefaultAsync(w => w.Id == id);
            
            if (workInstruction == null)
            {
                return false;
            }

            if (!await IsEditable(workInstruction))
            {
                return false;
            }
            
            // Remove associated Work Instruction Node Images first
            await _imageService.DeleteImagesByWorkInstructionAsync(workInstruction);
            
            // Remove associated Work Instruction Nodes first
            context.WorkInstructionNodes.RemoveRange(workInstruction.Nodes);

            context.WorkInstructions.Remove(workInstruction);
            await context.SaveChangesAsync();
            
            // Invalidate cache so that on next request users retrieve the latest data
            ClearWorkInstructionCaches();
            
            Log.Information("Successfully deleted WorkInstruction with ID: {workInstructionID}", workInstruction.Id);

            return true;
        }
        catch (Exception e)
        {
            Log.Warning("Exception thrown when attempting to Delete a work instruction with ID: {id}, in WorkInstructionService. Exception: {Exception}", id, e.ToString());
            return false;
        }
    }
    
    /// <inheritdoc />
    public async Task<bool> DeleteNodesAsync(IEnumerable<WorkInstructionNode> nodes)
    {
        var nodeList = nodes.ToList();

        if (!nodeList.Any())
            return true;

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var nodeIds = nodeList.Select(n => n.Id).ToList();

            var nodesToDelete = await context.WorkInstructionNodes
                .Where(n => nodeIds.Contains(n.Id))
                .ToListAsync();

            if (!nodesToDelete.Any())
                return true;

            // Delegate image deletion to the ImageService
            await _imageService.DeleteImagesByNodesAsync(nodesToDelete);

            context.WorkInstructionNodes.RemoveRange(nodesToDelete);
            await context.SaveChangesAsync();

            ClearWorkInstructionCaches();

            Log.Information("Deleted {Count} WorkInstructionNodes and their images successfully.", nodesToDelete.Count);
            return true;
        }
        catch (Exception e)
        {
            Log.Warning("Exception thrown when attempting to delete WorkInstructionNodes and images. Exception: {Exception}", e);
            return false;
        }
    }
    
    /// <inheritdoc />
    public async Task<bool> DeleteNodesAsync(IEnumerable<int> nodeIds)
    {
        var idsList = nodeIds.ToList();

        if (!idsList.Any())
            return true;

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var nodesToDelete = await context.WorkInstructionNodes
                .Where(n => idsList.Contains(n.Id))
                .ToListAsync();

            if (!nodesToDelete.Any())
                return true;

            // Delegate image deletion to the ImageService
            await _imageService.DeleteImagesByNodesAsync(nodesToDelete);

            context.WorkInstructionNodes.RemoveRange(nodesToDelete);
            await context.SaveChangesAsync();

            ClearWorkInstructionCaches();

            Log.Information("Deleted {Count} WorkInstructionNodes and their images successfully.", nodesToDelete.Count);
            return true;
        }
        catch (Exception e)
        {
            Log.Warning("Exception thrown when attempting to delete WorkInstructionNodes and images. Exception: {Exception}", e);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAllVersionsByIdAsync(int id)
    {
        try
        {
            // assuming the input work instruction is the original
            await using var context = await _contextFactory.CreateDbContextAsync();
            var originalWorkInstruction = await context.WorkInstructions
                .Include(w => w.Nodes)
                .FirstOrDefaultAsync(w => w.Id == id);
            
            if (originalWorkInstruction == null)
            {
                return false;
            }


            // if input work instruction is not the original, find it
            if (originalWorkInstruction.OriginalId != null)
            {
                var ogId = originalWorkInstruction.OriginalId;

                originalWorkInstruction = await context.WorkInstructions
                    .Include(w => w.Nodes)
                    .FirstOrDefaultAsync(w => w.Id == ogId);

                if (originalWorkInstruction == null)
                {
                    return false;
                }
            }
            
            // record original id to get the rest or the versions but delete this one now
            var originalId =  originalWorkInstruction.Id;
            
            // query for all work instructions associated with the original
            var otherVersions = await context.WorkInstructions
                .Where(w => w.OriginalId == originalId)
                .Include(w => w.Nodes)
                .ThenInclude(w => ((PartNode)w).PartDefinition)
                .ToListAsync();
            
            otherVersions.Add(originalWorkInstruction);
            
            // delete each one
            foreach (var version in otherVersions)
            {
                // Remove all production logs associated with a work instruction
                await  _productionLogService.DeleteByWorkInstructionAsync(version);
                await _imageService.DeleteImagesByWorkInstructionAsync(version);
               
                // Remove associated Work Instruction Nodes first
                context.WorkInstructionNodes.RemoveRange(version.Nodes);
                context.WorkInstructions.Remove(version);
            }

            //save
            await context.SaveChangesAsync();

            // Invalidate cache so that on next request users retrieve the latest data
            ClearWorkInstructionCaches();

            Log.Information("Successfully deleted all versions associated with WorkInstruction ID: {workInstructionID}", originalWorkInstruction.Id);

            return true;
        }
        catch (Exception e)
        {
            Log.Warning("Exception thrown when attempting to Delete all versions associated with WorkInstruction ID: {id}, in WorkInstructionService. Exception: {Exception}", id, e.ToString());
            return false;
        }
    }
    
    /// <inheritdoc />
    public async Task<bool> UpdateWorkInstructionAsync(WorkInstructionFormDTO dto)
    {
        Log.Information("Beginning update for WorkInstruction {Id}", dto.Id);

        if (dto.Id is null or 0)
            return false;

        await using var context = await _contextFactory.CreateDbContextAsync();
        var strategy = context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                var existing = await context.WorkInstructions
                    .Include(w => w.Products)
                    .Include(w => w.Nodes)
                    .FirstOrDefaultAsync(w => w.Id == dto.Id.Value);

                if (existing == null)
                    return false;

                if (!await ValidateUniquenessAsync(dto, existing))
                    return false;
                
                await _workInstructionUpdater.ApplyAsync(dto, existing, context);
                
                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Git commit AFTER DB success
                var dtoForGit = existing.ToFileDTO();

                var commitMessage = $"Update WorkInstruction: {existing.Title}";

                await _gitSyncService.CommitAsync(
                    dto: dtoForGit,
                    commitMessage: commitMessage,
                    originalTitle: existing.Title);

                ClearWorkInstructionCaches();

                Log.Information("Successfully updated WorkInstruction {Id}", existing.Id);

                return true;
            }
            catch (Exception ex)
            {
                Log.Warning("Error updating WorkInstruction {Id}: {Exception}", dto.Id, ex);
                return false;
            }
        });
    }
    
    private async Task<bool> ValidateUniquenessAsync(
        WorkInstructionFormDTO dto,
        WorkInstruction existing)
    {
        if (string.Equals(existing.Title, dto.Title, StringComparison.OrdinalIgnoreCase)
            && string.Equals(existing.Version, dto.Version, StringComparison.OrdinalIgnoreCase))
            return true;

        var uniquenessCheck = dto.ToNewEntity();
        uniquenessCheck.Id = existing.Id;

        return await IsUnique(uniquenessCheck);
    }
    
    /// <summary>
    /// Associates additional work instructions with the specified product,
    /// without removing existing associations.
    /// </summary>
    /// <param name="productId">The ID of the product to update.</param>
    /// <param name="workInstructionIds">A list of work instruction IDs to associate.</param>
    public async Task AddWorkInstructionsToProductAsync(int productId, List<int> workInstructionIds)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var product = await context.Products
                .Include(p => p.WorkInstructions)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product is null)
            {
                Log.Warning("Product not found for ID {ProductId} while adding work instructions.", productId);
                return;
            }

            // Defensive initialization to avoid null references
            product.WorkInstructions ??= [];

            var existingIds = product.WorkInstructions.Select(wi => wi.Id).ToHashSet();

            foreach (var id in workInstructionIds)
            {
                if (!existingIds.Contains(id))
                {
                    var wi = await context.WorkInstructions.FindAsync(id);
                    if (wi != null)
                    {
                        product.WorkInstructions.Add(wi);
                    }
                }
            }

            await context.SaveChangesAsync();
            ClearWorkInstructionCaches();
            
            Log.Information("Associated {Count} work instructions with product ID {ProductId}.", workInstructionIds.Count, productId);
        }
        catch (Exception e)
        {
            Log.Warning(e, "Exception while associating work instructions with product ID {ProductId}.", productId);
        }
    }
    
    /// <summary>
    /// Removes specific work instruction associations from a product.
    /// </summary>
    /// <param name="productId">The ID of the product to update.</param>
    /// <param name="workInstructionIds">A list of work instruction IDs to remove.</param>
    public async Task RemoveWorkInstructionsFromProductAsync(int productId, List<int> workInstructionIds)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var product = await context.Products
                .Include(p => p.WorkInstructions)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product is null)
            {
                Log.Warning("Product not found for ID {ProductId} while removing work instructions.", productId);
                return;
            }

            // Defensive initialization in case WorkInstructions is null
            product.WorkInstructions ??= new List<WorkInstruction>();

            // Remove only the matching work instructions by ID
            product.WorkInstructions.RemoveAll(wi => workInstructionIds.Contains(wi.Id));

            await context.SaveChangesAsync();
            ClearWorkInstructionCaches();
            
            Log.Information("Removed {Count} work instructions from product ID {ProductId}.", workInstructionIds.Count, productId);
        }
        catch (Exception e)
        {
            Log.Warning(e, "Exception while removing work instructions from product ID {ProductId}.", productId);
        }
    }

    /// <summary>
    /// Increments a simple `major.minor` version label for new work instruction rows (matches editor logic).
    /// </summary>
    private static string BumpWorkInstructionVersionString(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return "1.0";
        }

        var parts = version.Split('.');
        if (parts.Length == 2 && int.TryParse(parts[1], out var minor))
        {
            return $"{parts[0]}.{minor + 1}";
        }

        return version + ".1";
    }

    private void ClearWorkInstructionCaches()
    {
        _cache.Remove(WORK_INSTRUCTION_CACHE_KEY);
        _cache.Remove(WORK_INSTRUCTION_SUMMARY_CACHE_KEY);
        _cache.Remove(WORK_INSTRUCTION_LATEST_CACHE_KEY);
        _cache.Remove(WORK_INSTRUCTION_LATEST_SUMMARY_CACHE_KEY);
    }
}

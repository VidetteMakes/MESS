using MESS.Data.Context;
using MESS.Data.Models;
using MESS.Services.CRUD.Tags;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace MESS.Services.CRUD.SerializableParts;

/// <summary>
/// Provides CRUD operations for <see cref="SerializablePart"/> entities.
/// </summary>
public class SerializablePartService : ISerializablePartService
{
    private readonly IDbContextFactory<ApplicationContext> _contextFactory;
    private readonly ITagService _tagService;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="SerializablePartService"/> class.
    /// </summary>
    /// <param name="contextFactory">
    /// The <see cref="IDbContextFactory{TContext}"/> used to create instances of the <see cref="ApplicationContext"/>.
    /// This factory provides scoped database contexts for performing CRUD operations on <see cref="SerializablePart"/> entities.
    /// </param>
    /// <param name ="tagService">
    /// The <see cref="ITagService"/> used to resolve tags when retrieving <see cref="SerializablePart"/> entities by tag code.
    /// </param>
    public SerializablePartService(IDbContextFactory<ApplicationContext> contextFactory, ITagService tagService)
    {
        _contextFactory = contextFactory;
        _tagService = tagService;
    }
    
    /// <summary>
    /// Creates a new <see cref="SerializablePart"/> record for the specified 
    /// <see cref="PartDefinition"/> and optional serial number.
    /// </summary>
    /// <param name="definition">
    /// The <see cref="PartDefinition"/> associated with the serialized part.
    /// </param>
    /// <param name="serialNumber">
    /// The serial number identifying this specific part instance, or <c>null</c>
    /// /empty if the part has no serial number.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// The task result contains the newly created <see cref="SerializablePart"/> entity,
    /// or <c>null</c> if creation failed.
    /// </returns>
    /// <remarks>
    /// - Serial numbers are optional and may be <c>null</c> or empty.
    /// - Duplicate serial numbers are allowed; duplicates trigger warnings when a non-null
    ///   serial number is provided.
    /// - The returned entity is not tracked once the context is disposed.
    /// </remarks>
    public async Task<SerializablePart?> CreateAsync(PartDefinition definition, string? serialNumber)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            // Only check duplicates when a serial number is supplied
            if (!string.IsNullOrWhiteSpace(serialNumber))
            {
                var existing = await context.SerializableParts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(sp =>
                        sp.PartDefinitionId == definition.Id &&
                        sp.SerialNumber == serialNumber);

                if (existing != null)
                {
                    Log.Warning(
                        "A SerializablePart with serial number {SerialNumber} already exists " +
                        "for PartDefinition ID {PartDefinitionId}. A new record will still be created.",
                        serialNumber, definition.Id);
                }
            }
            else
            {
                Log.Information(
                    "Creating SerializablePart for PartDefinition ID {PartDefinitionId} with no serial number.",
                    definition.Id);
            }

            var newPart = new SerializablePart
            {
                PartDefinitionId = definition.Id,
                SerialNumber = serialNumber, // may be null or empty
                PartDefinition = null        // avoid EF tracking conflicts
            };

            await context.SerializableParts.AddAsync(newPart);
            await context.SaveChangesAsync();

            Log.Information(
                "Created new SerializablePart (ID: {Id}) for PartDefinition ID {PartDefinitionId} with SerialNumber {SerialNumber}.",
                newPart.Id, definition.Id, serialNumber);

            return newPart;
        }
        catch (Exception ex)
        {
            Log.Error(
                ex,
                "Error creating SerializablePart for PartDefinition ID {PartDefinitionId} with SerialNumber {SerialNumber}.",
                definition.Id, serialNumber);

            return null;
        }
    }
     
    /// <summary>
    /// Retrieves a <see cref="SerializablePart"/> entity that matches the specified serial number.
    /// </summary>
    /// <param name="serialNumber">
    /// The serial number of the serialized part to retrieve. This value is compared case-insensitively.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains the matching
    /// <see cref="SerializablePart"/> entity if found; otherwise, <see langword="null"/>.
    /// </returns>
    /// <remarks>
    /// This method performs a case-insensitive lookup for the specified serial number and
    /// includes the associated <see cref="PartDefinition"/> for convenience.
    /// It does not throw an exception if multiple serialized parts share the same serial number.
    /// Instead, it returns the first matching instance as determined by the database.
    /// </remarks>
    public async Task<SerializablePart?> GetBySerialNumberAsync(string serialNumber)
    {
        if (string.IsNullOrWhiteSpace(serialNumber))
        {
            Log.Warning("GetBySerialNumberAsync called with null or empty serial number.");
            return null;
        }

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var part = await context.SerializableParts
                .AsNoTracking()
                .Include(sp => sp.PartDefinition)
                .FirstOrDefaultAsync(sp => sp.SerialNumber != null &&
                                           sp.SerialNumber.ToLower() == serialNumber.ToLower());

            if (part is null)
            {
                Log.Information("No SerializablePart found for serial number {SerialNumber}.", serialNumber);
            }
            else
            {
                Log.Information("Found SerializablePart with ID {Id} for serial number {SerialNumber}.", part.Id, serialNumber);
            }

            return part;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving SerializablePart with serial number {SerialNumber}.", serialNumber);
            return null;
        }
    }
    
    /// <summary>
    /// Retrieves all <see cref="SerializablePart"/> entities associated with the specified
    /// <see cref="PartDefinition"/> identifier.
    /// </summary>
    /// <param name="partDefinitionId">
    /// The unique identifier of the <see cref="PartDefinition"/> whose serialized instances should be retrieved.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains a list of
    /// <see cref="SerializablePart"/> entities linked to the specified part definition.
    /// If no matching records are found, an empty list is returned.
    /// </returns>
    /// <remarks>
    /// This method performs a single database query to retrieve all serialized parts linked to the provided
    /// part definition. The query uses <see cref="EntityFrameworkQueryableExtensions.AsNoTracking{TEntity}(IQueryable{TEntity})"/>
    /// for performance optimization, as the returned entities are intended for read-only scenarios.
    /// </remarks>
    public async Task<List<SerializablePart>> GetByPartDefinitionIdAsync(int partDefinitionId)
    {
        if (partDefinitionId <= 0)
        {
            Log.Warning("GetByPartDefinitionIdAsync called with invalid PartDefinitionId: {PartDefinitionId}.", partDefinitionId);
            return [];
        }

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var parts = await context.SerializableParts
                .AsNoTracking()
                .Include(sp => sp.PartDefinition)
                .Where(sp => sp.PartDefinitionId == partDefinitionId)
                .ToListAsync();

            Log.Information("Retrieved {Count} SerializableParts for PartDefinitionId {PartDefinitionId}.",
                parts.Count, partDefinitionId);

            return parts;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving SerializableParts for PartDefinitionId {PartDefinitionId}.", partDefinitionId);
            return [];
        }
    }
    
    /// <summary>
    /// Retrieves all <see cref="SerializablePart"/> entities that were installed
    /// during a specified <see cref="ProductionLog"/> event.
    /// </summary>
    /// <param name="productionLogId">
    /// The unique identifier of the <see cref="ProductionLog"/> to filter by.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains a list of
    /// <see cref="SerializablePart"/> entities that were installed in the specified production log.
    /// If no matching records are found, an empty list is returned.
    /// </returns>
    /// <remarks>
    /// Only <see cref="ProductionLogPart"/> entries with <see cref="PartOperationType.Installed"/>
    /// are considered. Each returned <see cref="SerializablePart"/> includes the associated
    /// <see cref="PartDefinition"/> for context. Duplicate serializable parts (if any) are preserved.
    /// </remarks>
    public async Task<List<SerializablePart>> GetInstalledForProductionLogAsync(int productionLogId)
    {
        if (productionLogId <= 0)
        {
            Log.Warning("GetInstalledForProductionLogAsync called with invalid ProductionLogId: {ProductionLogId}.", productionLogId);
            return [];
        }

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var installedParts = await context.ProductionLogParts
                .AsNoTracking()
                .Where(plp => plp.ProductionLogId == productionLogId &&
                              plp.OperationType == PartOperationType.Installed)
                .Include(plp => plp.SerializablePart)              
                .ThenInclude(sp => sp!.PartDefinition)          
                .Select(plp => plp.SerializablePart)
                .Where(sp => sp != null)
                .Cast<SerializablePart>()                  
                .ToListAsync();

            Log.Information("Retrieved {Count} installed SerializableParts for ProductionLogId {ProductionLogId}.",
                installedParts.Count, productionLogId);

            return installedParts;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving installed SerializableParts for ProductionLogId {ProductionLogId}.", productionLogId);
            return [];
        }
    }
    
    /// <inheritdoc/>
    public async Task<SerializablePart?> GetProducedForProductionLogAsync(int productionLogId)
    {
        if (productionLogId <= 0)
        {
            Log.Warning("GetProducedForProductionLogAsync called with invalid ProductionLogId: {ProductionLogId}.", productionLogId);
            return null;
        }

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            // Look for the produced part for this log
            var producedPart = await context.ProductionLogParts
                .AsNoTracking()
                .Where(plp =>
                    plp.ProductionLogId == productionLogId &&
                    plp.OperationType == PartOperationType.Produced)
                .Include(plp => plp.SerializablePart)
                .ThenInclude(sp => sp!.PartDefinition)
                .Select(plp => plp.SerializablePart) 
                .FirstOrDefaultAsync();

            if (producedPart is null)
            {
                Log.Information("No produced SerializablePart found for ProductionLogId {ProductionLogId}.", productionLogId);
            }
            else
            {
                Log.Information(
                    "Found produced SerializablePart with ID {Id} for ProductionLogId {ProductionLogId}.",
                    producedPart.Id, productionLogId);
            }

            return producedPart;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving produced SerializablePart for ProductionLogId {ProductionLogId}.", productionLogId);
            return null;
        }
    }
    
    /// <summary>
    /// Updates the serial number of an existing <see cref="SerializablePart"/>.
    /// </summary>
    /// <param name="serializablePartId">
    /// The unique identifier of the <see cref="SerializablePart"/> to update.
    /// </param>
    /// <param name="newSerialNumber">
    /// The new serial number to assign to the <see cref="SerializablePart"/>.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The task completes once the serial number has been updated.
    /// </returns>
    /// <remarks>
    /// This method will directly update the serial number of the specified entity in the database.
    /// It does not check for duplicate serial numbers — multiple <see cref="SerializablePart"/> records may share the same serial number.
    /// </remarks>
    public async Task UpdateSerialNumberAsync(int serializablePartId, string newSerialNumber)
    {
        if (serializablePartId <= 0)
        {
            Log.Warning("UpdateSerialNumberAsync called with invalid SerializablePartId: {SerializablePartId}.", serializablePartId);
            return;
        }

        if (string.IsNullOrWhiteSpace(newSerialNumber))
        {
            Log.Warning("UpdateSerialNumberAsync called with null or empty new serial number for SerializablePartId: {SerializablePartId}.", serializablePartId);
            return;
        }

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var part = await context.SerializableParts
                .FirstOrDefaultAsync(sp => sp.Id == serializablePartId);

            if (part is null)
            {
                Log.Warning("SerializablePart with ID {SerializablePartId} not found.", serializablePartId);
                return;
            }

            part.SerialNumber = newSerialNumber;
            await context.SaveChangesAsync();

            Log.Information("Updated SerializablePart ID {SerializablePartId} with new serial number: {NewSerialNumber}.", serializablePartId, newSerialNumber);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating serial number for SerializablePart ID {SerializablePartId}.", serializablePartId);
        }
    }
    
    /// <summary>
    /// Checks whether a <see cref="SerializablePart"/> exists with the specified
    /// <see cref="PartDefinition"/> ID and serial number.
    /// </summary>
    /// <param name="partDefinitionId">
    /// The unique identifier of the <see cref="PartDefinition"/> to match.
    /// </param>
    /// <param name="serialNumber">
    /// The serial number to match.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result is <c>true</c>
    /// if a matching <see cref="SerializablePart"/> exists; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This method performs a case-insensitive comparison on the serial number.
    /// Multiple <see cref="SerializablePart"/> entries may share the same serial number,
    /// but this method will return <c>true</c> if at least one exists.
    /// </remarks>
    public async Task<bool> ExistsAsync(int partDefinitionId, string serialNumber)
    {
        if (partDefinitionId <= 0)
        {
            Log.Warning("ExistsAsync called with invalid PartDefinitionId: {PartDefinitionId}.", partDefinitionId);
            return false;
        }

        if (string.IsNullOrWhiteSpace(serialNumber))
        {
            Log.Warning("ExistsAsync called with null or empty serial number for PartDefinitionId: {PartDefinitionId}.", partDefinitionId);
            return false;
        }

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var exists = await context.SerializableParts
                .AsNoTracking()
                .AnyAsync(sp => sp.PartDefinitionId == partDefinitionId &&
                                sp.SerialNumber!.ToLower() == serialNumber.ToLower());

            Log.Information("Checked existence of SerializablePart with PartDefinitionId {PartDefinitionId} and SerialNumber {SerialNumber}: {Exists}.",
                partDefinitionId, serialNumber, exists);

            return exists;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error checking existence of SerializablePart with PartDefinitionId {PartDefinitionId} and SerialNumber {SerialNumber}.",
                partDefinitionId, serialNumber);
            return false;
        }
    }
    
    /// <inheritdoc/>
    public async Task<SerializablePart?> GetByTagCodeAsync(string tagCode)
    {
        if (string.IsNullOrWhiteSpace(tagCode))
        {
            Log.Warning("GetByTagCodeAsync called with null or empty tag code.");
            return null;
        }

        try
        {
            // Resolve the tag
            var tag = await _tagService.GetByCodeAsync(tagCode);
            if (tag == null)
            {
                Log.Information("No tag found with code {TagCode}.", tagCode);
                return null;
            }

            if (tag.SerializablePartId == null)
            {
                Log.Information("Tag {TagCode} exists but has no assigned SerializablePart.", tagCode);
                return null;
            }

            await using var context = await _contextFactory.CreateDbContextAsync();

            // Load the SerializablePart with PartDefinition included
            var part = await context.SerializableParts
                .AsNoTracking()
                .Include(sp => sp.PartDefinition)
                .FirstOrDefaultAsync(sp => sp.Id == tag.SerializablePartId);

            if (part == null)
            {
                Log.Warning("SerializablePart with ID {SerializablePartId} referenced by tag {TagCode} was not found.", tag.SerializablePartId, tagCode);
            }
            else
            {
                Log.Information("Retrieved SerializablePart ID {SerializablePartId} for tag {TagCode}.", part.Id, tagCode);
            }

            return part;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving SerializablePart for tag code {TagCode}.", tagCode);
            return null;
        }
    }
    
    ///<inheritdoc/>
    public async Task<int?> TryResolveTagAsync(string tagCode, int expectedPartDefinitionId)
    {
        if (string.IsNullOrWhiteSpace(tagCode))
        {
            Log.Warning("TryResolveTagAsync called with null or empty tag code.");
            return null;
        }

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            
            var tag = await _tagService.GetByCodeAsync(tagCode);
            if (tag == null)
            {
                Log.Information("No tag found with code {TagCode}.", tagCode);
                return null;
            }

            if (tag.SerializablePartId == null)
            {
                Log.Information("Tag {TagCode} exists but has no assigned SerializablePart.", tagCode);
                return null;
            }
            
            var serializablePart = await context.SerializableParts
                .Where(sp => sp.Id == tag.SerializablePartId)
                .Select(sp => new { sp.Id, sp.PartDefinitionId })
                .FirstOrDefaultAsync();

            if (serializablePart == null)
            {
                Log.Warning("SerializablePart {Id} not found.", tag.SerializablePartId);
                return null;
            }
            
            if (serializablePart.PartDefinitionId != expectedPartDefinitionId)
            {
                Log.Information(
                    "Tag {TagCode} resolved to SerializablePart {Id}, but PartDefinition mismatch. Expected {Expected}, got {Actual}.",
                    tagCode,
                    serializablePart.Id,
                    expectedPartDefinitionId,
                    serializablePart.PartDefinitionId);

                return null;
            }

            Log.Information(
                "Resolved tag {TagCode} to SerializablePart ID {SerializablePartId}.",
                tagCode,
                serializablePart.Id);

            return serializablePart.Id;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error resolving SerializablePart ID for tag code {TagCode}.", tagCode);
            return null;
        }
    }
}
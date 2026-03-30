using MESS.Data.Context;
using MESS.Data.Models;
using MESS.Services.DTOs.ProductionLogs.Form;
using MESS.Services.UI.PartTraceability;
using Microsoft.EntityFrameworkCore;

namespace MESS.Services.CRUD.PartTraceability;

/// <inheritdoc />
public class PartTraceabilityPersistenceService : IPartTraceabilityPersistenceService
{
    private readonly IDbContextFactory<ApplicationContext> _dbContextFactory;
    private readonly IPartResolver _partResolver;

    /// <summary>
    /// Constructs a new instance of <see cref="PartTraceabilityPersistenceService"/> with the provided database context factory.
    /// </summary>
    /// <param name="dbContextFactory"></param>
    /// <param name="partResolver"></param>
    public PartTraceabilityPersistenceService(IDbContextFactory<ApplicationContext> dbContextFactory, IPartResolver partResolver)
    {
        _dbContextFactory = dbContextFactory;
        _partResolver = partResolver;
    }
    
    /// <inheritdoc />
    public List<PartTraceabilityOperation> BuildOperations(
        IEnumerable<PartTraceabilitySnapshot> snapshots,
        IReadOnlyDictionary<int, int> logIndexToProductionLogId)
    {
        ArgumentNullException.ThrowIfNull(snapshots);
        ArgumentNullException.ThrowIfNull(logIndexToProductionLogId);

        var operations = new List<PartTraceabilityOperation>();

        foreach (var snapshot in snapshots)
        {
            if (!logIndexToProductionLogId.TryGetValue(snapshot.LogIndex, out var productionLogId))
            {
                throw new InvalidOperationException(
                    $"No ProductionLogId mapping found for log index {snapshot.LogIndex}.");
            }

            var operation = new PartTraceabilityOperation
            {
                ProductionLogId = productionLogId,
                ProducedPartSerialNumber = snapshot.ProducedPartSerialNumber,
                ProducedPartTagCode = snapshot.ProducedPartTagCode,
                ShouldProducePart = snapshot.ShouldProducePart,
                Entries = snapshot.Entries
                    .Select(e => new PartTraceabilityOperation.PartEntryDTO
                    {
                        PartNodeId = e.PartNodeId,
                        SerialNumber = e.SerialNumber,
                        TagCode = e.TagCode,
                        SerializablePartId = e.SerializablePartId
                    })
                    .ToList()
            };

            operations.Add(operation);
        }

        return operations;
    }
    
    ///<inheritdoc/>
   public async Task PersistOperationBatchedAsync(PartTraceabilityOperation operation)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var strategy = db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            ArgumentNullException.ThrowIfNull(operation);

            // --- Validation ---
            if (!operation.ShouldProducePart &&
                !string.IsNullOrWhiteSpace(operation.ProducedPartSerialNumber))
            {
                throw new InvalidOperationException(
                    "ProducedPartSerialNumber was provided but ShouldProducePart is false.");
            }

            await using var tx = await db.Database.BeginTransactionAsync();

            // --- Load core ---
            var productionLog = await db.ProductionLogs
                .Include(pl => pl.WorkInstruction)
                .FirstOrDefaultAsync(pl => pl.Id == operation.ProductionLogId);

            if (productionLog == null)
                throw new InvalidOperationException($"ProductionLog {operation.ProductionLogId} not found.");

            var workInstruction = productionLog.WorkInstruction;

            // --- Load resolution data ---
            var data = await LoadResolutionDataAsync(db, operation, workInstruction);

            // --- Build context ---
            var context = BuildResolutionContext(data);

            // --- Resolve produced part ---
            SerializablePart? producedPart = null;

            if (operation.ShouldProducePart)
            {
                if (workInstruction?.PartProducedId == null)
                    throw new InvalidOperationException("WorkInstruction does not define a produced part.");

                var defId = workInstruction.PartProducedId.Value;

                if (!string.IsNullOrWhiteSpace(operation.ProducedPartSerialNumber))
                {
                    // Resolve via resolver (handles uniqueness correctly)
                    producedPart = _partResolver.ResolveProducedPart(
                        operation.ProducedPartSerialNumber,
                        defId,
                        context);
                }
                else
                {
                    // No serial → still create the part
                    producedPart = new SerializablePart
                    {
                        PartDefinitionId = defId
                    };

                    context.PartsToAdd.Add(producedPart);
                }

                db.ProductionLogParts.Add(new ProductionLogPart
                {
                    ProductionLogId = operation.ProductionLogId,
                    SerializablePart = producedPart,
                    OperationType = PartOperationType.Produced
                });
            }

            // Assign these to the context for the resolver
            context.ProducedPart = producedPart;
            context.ProducedPartTagCode = operation.ProducedPartTagCode;
            
            if (producedPart != null)
            {
                await db.SaveChangesAsync(); // ensures producedPart.Id is generated
                
                var key = (producedPart.SerialNumber!, producedPart.PartDefinitionId);
                
                // Ensure the resolver data sources are updated
                context.PartsBySerial[key] = producedPart;
                context.PartsById[producedPart.Id] = producedPart;

                if (!string.IsNullOrWhiteSpace(operation.ProducedPartTagCode))
                {
                    var tagCode = operation.ProducedPartTagCode!;
                    if (!context.TagsByCode.TryGetValue(tagCode, out var tag))
                        throw new InvalidOperationException($"Produced part tag '{tagCode}' not found.");

                    if (tag.Status != TagStatus.Available)
                        throw new InvalidOperationException($"Produced part tag '{tagCode}' is already assigned.");
                    
                    tag.SerializablePartId = producedPart.Id;
                    tag.Status = TagStatus.Assigned;
                    tag.History.Add(new TagHistory
                    {
                        TagId = tag.Id,
                        SerializablePartId = producedPart.Id,
                        EventType = TagEventType.Assigned,
                        Timestamp = DateTimeOffset.UtcNow
                    });
                }
            }

            // --- Resolve entries ---
            var entryPartMap = new Dictionary<PartTraceabilityOperation.PartEntryDTO, SerializablePart>();

            foreach (var entry in operation.Entries)
            {
                var part = _partResolver.Resolve(entry, context);
                if (part != null)
                    entryPartMap[entry] = part;
            }

            // Only add brand-new parts to EF for insertion
            db.SerializableParts.AddRange(context.PartsToAdd.Where(p => p.Id == 0));

            // --- Persist ---
            foreach (var entry in operation.Entries)
            {
                if (!entryPartMap.TryGetValue(entry, out var part))
                    continue;

                // Add ProductionLogPart for installation
                db.ProductionLogParts.Add(new ProductionLogPart
                {
                    ProductionLogId = operation.ProductionLogId,
                    SerializablePart = part,
                    OperationType = PartOperationType.Installed
                });

                if (producedPart == null) continue;

                // Check if an existing relationship exists
                SerializablePartRelationship? existingRel = null;

                if (part.Id != 0)
                {
                    existingRel = await db.SerializablePartRelationships
                        .FirstOrDefaultAsync(r => r.ChildPartId == part.Id);
                }

                if (existingRel != null)
                {
                    // Only log removal if the parent is actually changing
                    if (existingRel.ParentPartId != producedPart.Id)
                    {
                        db.ProductionLogParts.Add(new ProductionLogPart
                        {
                            ProductionLogId = operation.ProductionLogId,
                            SerializablePart = part,
                            OperationType = PartOperationType.Removed
                        });

                        db.SerializablePartRelationships.Remove(existingRel);
                    }
                    // else: existing parent is the same, no removal needed
                }

                // Add the new relationship only if it's not already the same
                if (existingRel == null || existingRel.ParentPartId != producedPart.Id)
                {
                    db.SerializablePartRelationships.Add(new SerializablePartRelationship
                    {
                        ParentPart = producedPart,
                        ChildPart = part,
                        LastUpdated = DateTimeOffset.UtcNow
                    });
                }
            }
            
            await db.SaveChangesAsync();
            await tx.CommitAsync();
        });
    }
    
    private async Task<PartResolutionData> LoadResolutionDataAsync(
        ApplicationContext db,
        PartTraceabilityOperation operation,
        WorkInstruction? workInstruction)
    {
        // --- PartNodes ---
        var partNodeIds = operation.Entries.Select(e => e.PartNodeId).Distinct().ToList();

        var partNodes = await db.PartNodes
            .Where(n => partNodeIds.Contains(n.Id))
            .Select(n => new { n.Id, n.PartDefinitionId })
            .ToListAsync();

        var nodeToDefId = partNodes.ToDictionary(n => n.Id, n => n.PartDefinitionId);

        // --- Definitions ---
        var partDefinitionIds = partNodes
            .Select(n => n.PartDefinitionId)
            .Distinct()
            .ToList();

        if (workInstruction?.PartProducedId != null)
            partDefinitionIds.Add(workInstruction.PartProducedId.Value);

        partDefinitionIds = partDefinitionIds.Distinct().ToList();

        var definitions = await db.PartDefinitions
            .Where(pd => partDefinitionIds.Contains(pd.Id))
            .ToDictionaryAsync(pd => pd.Id);

        // --- Parts by ID ---
        var idsToLookup = operation.Entries
            .Where(e => e.SerializablePartId.HasValue)
            .Select(e => e.SerializablePartId!.Value)
            .Distinct()
            .ToList();

        var partsById = await db.SerializableParts
            .Where(p => idsToLookup.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        // --- Serial preload ---
        var serialNumbers = operation.Entries
            .Where(e => !string.IsNullOrWhiteSpace(e.SerialNumber))
            .Select(e => e.SerialNumber!)
            .ToList();

        if (!string.IsNullOrWhiteSpace(operation.ProducedPartSerialNumber))
            serialNumbers.Add(operation.ProducedPartSerialNumber);

        serialNumbers = serialNumbers.Distinct().ToList();

        var uniqueDefIds = definitions.Values
            .Where(d => d.IsSerialNumberUnique)
            .Select(d => d.Id)
            .ToList();

        var partsBySerial = await db.SerializableParts
            .Where(p => serialNumbers.Contains(p.SerialNumber!) &&
                        uniqueDefIds.Contains(p.PartDefinitionId))
            .ToDictionaryAsync(p => (p.SerialNumber!, p.PartDefinitionId));

        // --- Tags ---
        // Collect tag codes from entries
        var tagCodesFromEntries = operation.Entries
            .Where(e => !string.IsNullOrWhiteSpace(e.TagCode))
            .Select(e => e.TagCode!)
            .Distinct()
            .ToList();

        // Include ProducedPartTagCode explicitly
        var producedPartTagCodes = string.IsNullOrWhiteSpace(operation.ProducedPartTagCode)
            ? new List<string>()
            : new List<string> { operation.ProducedPartTagCode! };

        var allRelevantTagCodes = tagCodesFromEntries
            .Concat(producedPartTagCodes)
            .Distinct()
            .ToList();

        // Load all tags that are either referenced or available
        var tagsByCode = await db.Tags
            .Include(t => t.SerializablePart)
            .Where(t => allRelevantTagCodes.Contains(t.Code) || t.Status == TagStatus.Available)
            .ToDictionaryAsync(t => t.Code);

        return new PartResolutionData
        {
            NodeToDefinitionId = nodeToDefId,
            Definitions = definitions,
            PartsById = partsById,
            TagsByCode = tagsByCode,
            PartsBySerial = partsBySerial
        };
    }
    
    private PartResolutionContext BuildResolutionContext(PartResolutionData data)
    {
        return new PartResolutionContext
        {
            NodeToDefinitionId = data.NodeToDefinitionId,
            Definitions = data.Definitions,
            PartsById = data.PartsById,
            TagsByCode = data.TagsByCode,
            PartsBySerial = data.PartsBySerial,
            PartsToAdd = new List<SerializablePart>(),
            UsedPartIds = new HashSet<int>()
        };
    }
    
    private static void DumpSerializableParts(DbContext db, string label)
    {
        Console.WriteLine($"\n==== DEBUG: {label} ====");

        var entries = db.ChangeTracker.Entries<SerializablePart>();

        foreach (var e in entries)
        {
            var entity = e.Entity;

            Console.WriteLine(
                $"State={e.State,-10} | Id={entity.Id,-5} | DefId={entity.PartDefinitionId,-5} | Serial={entity.SerialNumber ?? "NULL"}");
        }

        Console.WriteLine("==== END DEBUG ====\n");
    }
}

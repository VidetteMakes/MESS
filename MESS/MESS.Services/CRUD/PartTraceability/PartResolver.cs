using MESS.Data.Models;
using MESS.Services.DTOs.ProductionLogs.Form;

namespace MESS.Services.CRUD.PartTraceability;

///<inheritdoc/>
public class PartResolver : IPartResolver
{
    ///<inheritdoc/>
    public SerializablePart? Resolve(
        PartTraceabilityOperation.PartEntryDTO entry,
        PartResolutionContext ctx)
    {
        // Skip empty
        if (string.IsNullOrWhiteSpace(entry.SerialNumber) &&
            string.IsNullOrWhiteSpace(entry.TagCode) &&
            !entry.SerializablePartId.HasValue)
        {
            return null;
        }

        var defId = ctx.NodeToDefinitionId[entry.PartNodeId];
        var definition = ctx.Definitions[defId];

        SerializablePart? part = null;

        // --- 1. Resolve by ID ---
        if (entry.SerializablePartId.HasValue)
        {
            if (!ctx.PartsById.TryGetValue(entry.SerializablePartId.Value, out part))
                throw new InvalidOperationException(
                    $"SerializablePartId '{entry.SerializablePartId}' not found.");

            ValidateDefinition(part, defId, entry);
        }

        // --- 2. Resolve by Tag ---
        // Currently Unused.
        else if (!string.IsNullOrWhiteSpace(entry.TagCode))
        {
            throw new InvalidOperationException(
                $"Tag resolution is temporarily disabled. Encountered TagCode '{entry.TagCode}'.");
            
            /*
            if (!ctx.TagsByCode.TryGetValue(entry.TagCode!, out var tag))
                throw new InvalidOperationException($"Tag '{entry.TagCode}' not found.");

            if (tag.Status == TagStatus.Retired)
                throw new InvalidOperationException($"Tag '{entry.TagCode}' is retired.");

            if (tag.Status != TagStatus.Assigned || tag.SerializablePart == null)
                throw new InvalidOperationException($"Tag '{entry.TagCode}' is not assigned.");

            part = tag.SerializablePart;

            ValidateDefinition(part, defId, entry);

            if (!ctx.UsedPartIds.Add(part.Id))
                throw new InvalidOperationException(
                    $"Tag '{entry.TagCode}' is already used in this operation.");

            tag.History.Add(new TagHistory
            {
                TagId = tag.Id,
                SerializablePartId = part.Id,
                EventType = TagEventType.Assigned,
                Timestamp = DateTimeOffset.UtcNow
            });
            */
        }

        // --- 3. Resolve by Serial ---
        else if (!string.IsNullOrWhiteSpace(entry.SerialNumber))
        {
            var normalizedSerial = NormalizeSerial(entry.SerialNumber);
            var key = (normalizedSerial, defId);

            if (definition.IsSerialNumberUnique)
            {
                if (!ctx.PartsBySerial.TryGetValue(key, out part))
                {
                    part = new SerializablePart
                    {
                        SerialNumber = normalizedSerial,
                        PartDefinitionId = defId
                    };
                    ctx.PartsToAdd.Add(part);
                    ctx.PartsBySerial[key] = part;
                }
            }
            else
            {
                part = new SerializablePart
                {
                    SerialNumber = normalizedSerial,
                    PartDefinitionId = defId
                };
                ctx.PartsToAdd.Add(part);
            }
        }

        return part;
    }

    private static void ValidateDefinition(
        SerializablePart part,
        int expectedDefId,
        PartTraceabilityOperation.PartEntryDTO entry)
    {
        if (part.PartDefinitionId != expectedDefId)
        {
            throw new InvalidOperationException(
                $"Part has PartDefinitionId {part.PartDefinitionId} " +
                $"but PartNode {entry.PartNodeId} expects {expectedDefId}.");
        }
    }
    
    ///<inheritdoc/>
    public SerializablePart ResolveProducedPart(string serialNumber, int defId, PartResolutionContext ctx)
    {
        var normalizedSerial = NormalizeSerial(serialNumber);
        var definition = ctx.Definitions[defId];

        SerializablePart? existingPart = null;

        if (definition.IsSerialNumberUnique)
        {
            var key = (normalizedSerial, defId);

            // Check if we already have it in memory
            if (ctx.PartsBySerial.TryGetValue(key, out existingPart))
                return existingPart;

            // Check if it exists in DB (already loaded into PartsById)
            existingPart = ctx.PartsById.Values
                .FirstOrDefault(p => p.PartDefinitionId == defId && p.SerialNumber == normalizedSerial);

            if (existingPart != null)
            {
                ctx.PartsBySerial[key] = existingPart; // cache it
                return existingPart;
            }
        }

        // Always create new part if non-unique or doesn't exist yet
        var newPart = new SerializablePart
        {
            SerialNumber = normalizedSerial,
            PartDefinitionId = defId
        };

        ctx.PartsToAdd.Add(newPart);     // only new parts go here
        if (definition.IsSerialNumberUnique)
            ctx.PartsBySerial[(normalizedSerial, defId)] = newPart;

        return newPart;
    }
    
    private static string NormalizeSerial(string serial) =>
        serial?.Trim().ToUpperInvariant() ?? throw new ArgumentNullException(nameof(serial));
}
using MESS.Data.Models;
using MESS.Services.DTOs.ProductionLogs.Form;

namespace MESS.Services.CRUD.PartTraceability;

/// <summary>
/// Defines the contract for resolving <see cref="SerializablePart"/> entities
/// from part traceability entries or produced part information.
/// </summary>
public interface IPartResolver
{
    /// <summary>
    /// Resolves a <see cref="SerializablePart"/> for a given <see cref="PartTraceabilityOperation.PartEntryDTO"/> 
    /// within the context of a <see cref="PartResolutionContext"/>.
    /// </summary>
    /// <param name="entry">
    /// The part traceability entry containing a serial number, tag code, or resolved part ID.
    /// </param>
    /// <param name="context">
    /// The resolution context containing cached data, mappings, and tracking information
    /// needed to resolve parts.
    /// </param>
    /// <returns>
    /// The resolved <see cref="SerializablePart"/> instance if one could be determined; 
    /// otherwise, <c>null</c> if no part could be resolved from the entry.
    /// </returns>
    SerializablePart? Resolve(
        PartTraceabilityOperation.PartEntryDTO entry,
        PartResolutionContext context);

    /// <summary>
    /// Resolves or creates a produced <see cref="SerializablePart"/> given a serial number 
    /// and a part definition ID within the provided <see cref="PartResolutionContext"/>.
    /// </summary>
    /// <param name="serialNumber">
    /// The serial number of the produced part. Must not be <c>null</c> or empty.
    /// </param>
    /// <param name="defId">
    /// The ID of the <see cref="PartDefinition"/> associated with the produced part.
    /// </param>
    /// <param name="ctx">
    /// The resolution context containing cached data and tracking information for the operation.
    /// </param>
    /// <returns>
    /// The resolved or newly created <see cref="SerializablePart"/> corresponding to the produced part.
    /// </returns>
    public SerializablePart ResolveProducedPart(
        string serialNumber,
        int defId,
        PartResolutionContext ctx);
}
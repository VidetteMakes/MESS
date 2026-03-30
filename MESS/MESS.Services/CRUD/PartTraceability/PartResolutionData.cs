using MESS.Data.Models;

namespace MESS.Services.CRUD.PartTraceability;

/// <summary>
/// Holds preloaded data necessary for resolving <see cref="SerializablePart"/> instances
/// in a part traceability operation.
/// </summary>
/// <remarks>
/// This class contains cached dictionaries for quick lookup of parts, part definitions,
/// and tags during resolution. It is intended to be loaded once per operation and
/// passed to <see cref="PartResolutionContext"/> or <see cref="IPartResolver"/> methods.
/// </remarks>
public class PartResolutionData
{
    /// <summary>
    /// Maps each <c>PartNode</c> ID to its corresponding <see cref="PartDefinition"/> ID.
    /// </summary>
    public required Dictionary<int, int> NodeToDefinitionId { get; init; }

    /// <summary>
    /// Maps <see cref="PartDefinition"/> IDs to their corresponding <see cref="PartDefinition"/> objects.
    /// </summary>
    public required Dictionary<int, PartDefinition> Definitions { get; init; }

    /// <summary>
    /// Maps <see cref="SerializablePart"/> IDs to their corresponding <see cref="SerializablePart"/> objects.
    /// </summary>
    public required Dictionary<int, SerializablePart> PartsById { get; init; }

    /// <summary>
    /// Maps tag codes to <see cref="Tag"/> objects for fast lookup during resolution.
    /// </summary>
    public required Dictionary<string, Tag> TagsByCode { get; init; }

    /// <summary>
    /// Maps a tuple of (<c>SerialNumber</c>, <c>PartDefinitionId</c>) to a <see cref="SerializablePart"/>.
    /// </summary>
    /// <remarks>
    /// Used for quickly finding parts by serial number within definitions that enforce uniqueness.
    /// </remarks>
    public required Dictionary<(string Serial, int DefId), SerializablePart> PartsBySerial { get; init; }
}
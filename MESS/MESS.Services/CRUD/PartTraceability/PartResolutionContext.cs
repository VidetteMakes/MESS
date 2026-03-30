using MESS.Data.Models;

namespace MESS.Services.CRUD.PartTraceability
{
    /// <summary>
    /// Represents the operational context used by <see cref="IPartResolver"/> to resolve
    /// <see cref="SerializablePart"/> instances during a part traceability operation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Unlike <see cref="PartResolutionData"/>, which is a read-only snapshot of preloaded
    /// dictionaries for quick lookup, this class is mutable and maintains state throughout
    /// a resolution operation. It tracks newly created parts, parts already used in the
    /// current operation, and optional produced part information for tag assignment.
    /// </para>
    /// <para>
    /// It is intended to be passed to resolver methods so they can update parts, mark usage,
    /// and prepare new parts for insertion into the database.
    /// </para>
    /// </remarks>
    public class PartResolutionContext
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

        /// <summary>
        /// A list of newly created <see cref="SerializablePart"/> instances that should be added
        /// to the database at the end of the operation.
        /// </summary>
        public required List<SerializablePart> PartsToAdd { get; init; }

        /// <summary>
        /// Tracks <see cref="SerializablePart"/> IDs already used in the current operation
        /// to prevent duplicate usage.
        /// </summary>
        public required HashSet<int> UsedPartIds { get; init; }

        /// <summary>
        /// Optional reference to the produced part in this operation, if one exists.
        /// This is used for linking installed parts and for assigning a tag to the produced part.
        /// </summary>
        public SerializablePart? ProducedPart { get; set; }

        /// <summary>
        /// Optional tag code to assign to the <see cref="ProducedPart"/> during this operation.
        /// </summary>
        public string? ProducedPartTagCode { get; set; }
    }
}
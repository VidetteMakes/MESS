namespace MESS.Services.DTOs.ProductionLogs.Form
{
    /// <summary>
    /// Represents a flattened snapshot of part traceability for a single production log,
    /// suitable for submission to the persistence layer.
    /// </summary>
    /// <remarks>
    /// This DTO captures the current state of all part entries and the produced part serial number
    /// for one production log. It is intended to be immutable once constructed.
    /// </remarks>
    public class PartTraceabilityOperation
    {
        /// <summary>
        /// Gets the identifier of the production log this snapshot belongs to.
        /// </summary>
        public int ProductionLogId { get; init; }

        /// <summary>
        /// Gets the serial number of the produced part, if any.
        /// </summary>
        public string? ProducedPartSerialNumber { get; init; }
        
        /// <summary>
        /// Gets the tag code for the produced part, if any.
        /// This allows an unassigned tag to be linked to the produced part at creation.
        /// </summary>
        public string? ProducedPartTagCode { get; init; }
        
        /// <summary>
        /// Gets a value indicating whether this log is expected to produce a part.
        /// When set to <c>false</c>, the persistence layer should not create a produced part
        /// even if other data (such as serial numbers) is present.
        /// This is typically controlled by an operator when failures occur.
        /// </summary>
        public bool ShouldProducePart { get; init; }


        /// <summary>
        /// Gets all part entries for this production log in a flat list.
        /// </summary>
        /// <remarks>
        /// Each entry represents a single part node and contains either a serial number, a tag code,
        /// or a resolved <see cref="PartEntryDTO.SerializablePartId"/>.
        /// </remarks>
        public List<PartEntryDTO> Entries { get; init; } = new();

        /// <summary>
        /// Represents a single part entry (serial number, tag code, and optional resolved part ID)
        /// for a production log.
        /// </summary>
        public class PartEntryDTO
        {
            /// <summary>
            /// Gets the identifier of the associated part node.
            /// </summary>
            public int PartNodeId { get; init; }

            /// <summary>
            /// Gets the serial number for this entry, if any.
            /// </summary>
            public string? SerialNumber { get; init; }

            /// <summary>
            /// Gets the tag code for this entry, if any.
            /// </summary>
            public string? TagCode { get; init; }

            /// <summary>
            /// Gets the resolved ID of the corresponding <c>SerializablePart</c>, if available.
            /// </summary>
            /// <remarks>
            /// This allows the backend to skip additional lookups if a part has already been resolved
            /// from a tag code or serial number.
            /// </remarks>
            public int? SerializablePartId { get; init; }
        }
    }
}
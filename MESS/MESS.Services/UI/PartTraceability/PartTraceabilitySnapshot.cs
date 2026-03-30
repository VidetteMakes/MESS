namespace MESS.Services.UI.PartTraceability
{
    /// <summary>
    /// Represents a flattened, UI-level snapshot of part traceability data
    /// for a single production log (identified by log index, not database ID).
    /// </summary>
    public class PartTraceabilitySnapshot
    {
        /// <summary>
        /// Gets the UI log index this snapshot belongs to.
        /// </summary>
        public int LogIndex { get; init; }

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
        /// Gets all part entries for this log.
        /// </summary>
        public List<PartEntrySnapshot> Entries { get; init; } = new();

        /// <summary>
        /// Represents a single part entry snapshot from the UI.
        /// </summary>
        public class PartEntrySnapshot
        {
            /// <summary>
            /// Gets the associated part node ID.
            /// </summary>
            public int PartNodeId { get; init; }

            /// <summary>
            /// Gets the entered serial number, if any.
            /// </summary>
            public string? SerialNumber { get; init; }

            /// <summary>
            /// Gets the entered tag code, if any.
            /// </summary>
            public string? TagCode { get; init; }

            /// <summary>
            /// Gets the resolved serializable part ID, if already looked up.
            /// </summary>
            public int? SerializablePartId { get; init; }
        }
    }
}
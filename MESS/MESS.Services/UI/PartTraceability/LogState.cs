namespace MESS.Services.UI.PartTraceability;

/// <summary>
/// Represents the UI state for a single log index in the part traceability system.
/// Encapsulates all part entry data associated with the log, along with any
/// additional metadata such as the produced part serial number.
/// </summary>
public class LogState
{
    /// <summary>
    /// Gets or sets the UI-level identifier for this log.
    /// This corresponds to the index used to group part entries in the interface.
    /// </summary>
    public int LogIndex { get; set; }

    /// <summary>
    /// Gets or sets the collection of part entries for this log,
    /// keyed by <c>PartNodeId</c> for fast lookup.
    /// </summary>
    public Dictionary<int, PartEntryState> Entries { get; set; } = new();

    /// <summary>
    /// Gets or sets the serial number of the produced part for this log.
    /// This represents the output of the traceability process.
    /// </summary>
    public string? ProducedPartSerialNumber { get; set; }
    
    /// <summary>
    /// Gets or sets a tag code for the produced part for this log. This tag should have an "Available" status and will
    /// be assigned later during persistence.
    /// </summary>
    public string? ProducedPartTagCode { get; set; }
    
    /// <summary>
    /// An optional resolved serializable part ID for the produced part, if it has already been looked up. Most of the
    /// time it will not be, only during part rework
    /// </summary>
    public int? ProducedPartSerializablePartId { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether this log is expected to produce a part.
    /// When set to <c>false</c>, the persistence layer should not create a produced part
    /// even if other data (such as serial numbers) is present.
    /// This is typically controlled by an operator when failures occur.
    /// </summary>
    public bool ShouldProducePart { get; set; } = true;
}
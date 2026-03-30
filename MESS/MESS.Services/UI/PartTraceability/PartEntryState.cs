namespace MESS.Services.UI.PartTraceability;

/// <summary>
/// Represents the in-memory UI state for a single part entry associated with a work instruction node.
/// This model captures user input and any resolved data needed during the interaction,
/// but is not persisted to the database.
/// </summary>
public class PartEntryState
{
    /// <summary>
    /// Gets or sets the identifier of the associated part node from the work instruction.
    /// This is used to map the UI state to the correct node during rendering.
    /// </summary>
    public int PartNodeId { get; set; }

    /// <summary>
    /// Gets or sets the serial number entered by the user for serialized parts.
    /// This value is optional and may be null if a tag code is used instead.
    /// </summary>
    public string? SerialNumber { get; set; }

    /// <summary>
    /// Gets or sets the tag code entered by the user for reusable parts.
    /// This value is optional and may be null if a serial number is used instead.
    /// </summary>
    public string? TagCode { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the resolved serializable part, if a lookup has been performed.
    /// This value is optional and may be null if no matching part has been found or no lookup has occurred.
    /// </summary>
    public int? SerializablePartId { get; set; }

    /// <summary>
    /// Gets a value indicating whether the user has entered any input for this entry.
    /// </summary>
    public bool HasInput =>
        !string.IsNullOrWhiteSpace(SerialNumber) ||
        !string.IsNullOrWhiteSpace(TagCode);

    /// <summary>
    /// Clears all user input and resolved data associated with this entry.
    /// </summary>
    public void Clear()
    {
        SerialNumber = null;
        TagCode = null;
        SerializablePartId = null;
    }
}
namespace MESS.Data.Models;

/// <summary>
/// A model representing a physical tag (QR code or barcode) that can be assigned to a serializable part for traceability purposes.
/// </summary>
public class Tag
{
    /// <summary>
    /// A unique internal identifier for tags.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Human-readable tag code (QR or barcode value).
    /// Example: TAG-000123
    /// </summary>
    public string Code { get; set; } = null!;

    /// <summary>
    /// Current status of the tag.
    /// </summary>
    public TagStatus Status { get; set; } = TagStatus.Available;

    /// <summary>
    /// When the tag was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Serializable part currently assigned to this tag.
    /// Null when tag is available.
    /// </summary>
    public int? SerializablePartId { get; set; }

    /// <summary>
    /// A navigation property to the serializable part currently assigned to this tag.
    /// </summary>
    public SerializablePart? SerializablePart { get; set; }

    /// <summary>
    /// Navigation to tag history events.
    /// </summary>
    public ICollection<TagHistory> History { get; set; } = new List<TagHistory>();
}

/// <summary>
/// Represents the current state of a tag in the system.
/// </summary>
public enum TagStatus
{
    /// <summary>
    /// The tag exists in the system but is not yet assigned to a part.
    /// It is available to be printed or assigned.
    /// </summary>
    Available,

    /// <summary>
    /// The tag has been assigned to a serializable part and is currently in use.
    /// </summary>
    Assigned,

    /// <summary>
    /// The tag is permanently removed from use.
    /// This could be due to being lost, damaged, replaced, or otherwise retired.
    /// </summary>
    Retired
}
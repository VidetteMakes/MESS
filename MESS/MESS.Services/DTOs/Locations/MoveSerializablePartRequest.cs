namespace MESS.Services.DTOs.Locations;

/// <summary>
/// Represents a request to move a serialized part to a new location.
/// When the specified serialized part represents an assembly or subassembly,
/// all nested serialized parts associated with it will also have their
/// locations updated accordingly.
/// </summary>
public class MoveSerializablePartRequest
{
    /// <summary>
    /// Gets or sets the identifier of the root serialized part to move.
    /// This part may represent an individual component or an assembly
    /// containing other serialized parts.
    /// </summary>
    public int SerializablePartId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the destination location where
    /// the serialized part and any nested parts should be moved.
    /// </summary>
    public int LocationId { get; set; }
}
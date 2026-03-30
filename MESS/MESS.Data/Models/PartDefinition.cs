namespace MESS.Data.Models;

/// <summary>
/// Represents a part entity with an ID, part number, and part name.
/// </summary>
public class PartDefinition
{
    /// <summary>
    /// Gets or sets the unique identifier for the part.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the part number. This field is optional.
    /// </summary>
    public string? Number { get; set; }

    /// <summary>
    /// Gets or sets the name of the part. This field is required.
    /// </summary>
    public required string Name { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether instances (serializable parts) of this part definition
    /// can be uniquely identified by their serial number.
    /// </summary>
    /// <remarks>
    /// When set to <c>true</c>, each serial number is expected to be unique for this part
    /// definition and may be used as a sole identifier for individual part instances.
    /// When <c>false</c>, serial numbers are not guaranteed to be unique and additional
    /// identifying information may be required.
    /// </remarks>
    public bool IsSerialNumberUnique { get; set; } = true;
}
namespace MESS.Data.Models;

/// <summary>
/// Represents an individual serialized part instance derived from a part definition.
/// Serialized parts can be nested within one another to represent assemblies.
/// </summary>
public class SerializablePart
{
    /// <summary>
    /// Gets or sets the unique identifier for the serialized part.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the base part definition.
    /// </summary>
    public int PartDefinitionId { get; set; }

    /// <summary>
    /// Gets or sets the base part definition.
    /// </summary>
    public PartDefinition? PartDefinition { get; set; }

    /// <summary>
    /// Gets or sets the serial number identifying this particular instance.
    /// </summary>
    public string? SerialNumber { get; set; }

    /// <summary>
    /// Gets or sets the collection of production log associations for this serialized part.
    /// </summary>
    public List<ProductionLogPart> ProductionLogParts { get; set; } = [];
    
    /// <summary>
    /// Gets or sets the location id where this part is currently stored.
    /// </summary>
    public int? LocationId { get; set; }

    /// <summary>
    /// Gets or sets the location where this part is currently stored.
    /// </summary>
    public Location? Location { get; set; }
}
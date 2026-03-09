using System.ComponentModel.DataAnnotations;

namespace MESS.Data.Models;

/// <summary>
/// Represents a physical or logical location where serialized parts can be stored.
/// A location can contain multiple serialized parts.
/// </summary>
public class Location
{
    /// <summary>
    /// Gets or sets the unique identifier for the location.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the unique name of the location.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the serialized parts currently located here.
    /// </summary>
    public List<SerializablePart> SerializableParts { get; set; } = [];
}
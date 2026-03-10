namespace MESS.Services.DTOs.Locations;

/// <summary>
/// Represents a data transfer object containing summary information about a location
/// where serialized parts can be stored.
/// </summary>
public class LocationDTO
{
    /// <summary>
    /// Gets or sets the unique identifier of the location.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the unique name of the location.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of serialized parts currently assigned to this location.
    /// </summary>
    public int PartCount { get; set; }
}
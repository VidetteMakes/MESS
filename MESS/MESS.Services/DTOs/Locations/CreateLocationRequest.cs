namespace MESS.Services.DTOs.Locations;

/// <summary>
/// Represents a request to create a new location where serialized parts can be stored.
/// </summary>
public class CreateLocationRequest
{
    /// <summary>
    /// Gets or sets the name of the location to create.
    /// The name must be unique among all locations.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
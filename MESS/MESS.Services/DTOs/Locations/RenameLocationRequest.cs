namespace MESS.Services.DTOs.Locations;

/// <summary>
/// Represents a request to rename an existing location.
/// </summary>
public class RenameLocationRequest
{
    /// <summary>
    /// Gets or sets the identifier of the location to rename.
    /// </summary>
    public int LocationId { get; set; }

    /// <summary>
    /// Gets or sets the new name that should be assigned to the location.
    /// The name must be unique among all locations.
    /// </summary>
    public string NewName { get; set; } = string.Empty;
}
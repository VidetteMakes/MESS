namespace MESS.Services.DTOs.Locations;

/// <summary>
/// Represents a request to delete an existing location.
/// </summary>
public class DeleteLocationRequest
{
    /// <summary>
    /// Gets or sets the unique identifier of the location to delete.
    /// </summary>
    public int LocationId { get; set; }
}
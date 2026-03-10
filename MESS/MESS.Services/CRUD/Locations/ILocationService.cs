using MESS.Services.DTOs.Locations;

namespace MESS.Services.CRUD.Locations;

/// <summary>
/// Defines operations for managing locations and moving serialized parts
/// between locations within the system.
/// </summary>
public interface ILocationService
{
    /// <summary>
    /// Creates a new location.
    /// </summary>
    /// <param name="request">The request containing the name of the location to create.</param>
    /// <returns>A DTO representing the newly created location.</returns>
    Task<LocationDTO> CreateLocationAsync(CreateLocationRequest request);

    /// <summary>
    /// Renames an existing location.
    /// </summary>
    /// <param name="request">The request containing the location identifier and the new name.</param>
    Task RenameLocationAsync(RenameLocationRequest request);

    /// <summary>
    /// Deletes an existing location.
    /// </summary>
    /// <param name="request">The request containing the identifier of the location to delete.</param>
    Task DeleteLocationAsync(DeleteLocationRequest request);

    /// <summary>
    /// Retrieves a location by its unique identifier.
    /// </summary>
    /// <param name="id">The identifier of the location.</param>
    /// <returns>
    /// A <see cref="LocationDTO"/> representing the location if found; otherwise <c>null</c>.
    /// </returns>
    Task<LocationDTO?> GetByIdAsync(int id);

    /// <summary>
    /// Retrieves a location by its unique name.
    /// </summary>
    /// <param name="name">The name of the location.</param>
    /// <returns>
    /// A <see cref="LocationDTO"/> representing the location if found; otherwise <c>null</c>.
    /// </returns>
    Task<LocationDTO?> GetByNameAsync(string name);

    /// <summary>
    /// Moves a serialized part to a new location. If the specified part represents
    /// an assembly or subassembly, all nested serialized parts associated with it
    /// will also have their locations updated.
    /// </summary>
    /// <param name="request">
    /// The request containing the serialized part to move and the destination location.
    /// </param>
    Task MoveSerializablePartAsync(MoveSerializablePartRequest request);
    
    /// <summary>
    /// Retrieves all locations in the system.
    /// </summary>
    /// <returns>
    /// A collection of <see cref="LocationDTO"/> objects representing all
    /// locations currently defined in the system.
    /// </returns>
    Task<List<LocationDTO>> GetAllAsync();
    
    /// <summary>
    /// Creates multiple new locations in bulk according to a numbering scheme and optional prefix.
    /// </summary>
    /// <param name="scheme">
    /// The numbering scheme to use for generating location names. 
    /// Supported schemes are <see cref="LocationNumberingScheme.Decimal"/>, 
    /// <see cref="LocationNumberingScheme.Hexadecimal"/>, and 
    /// <see cref="LocationNumberingScheme.Alphanumeric"/>.
    /// </param>
    /// <param name="count">The number of locations to create. Must be greater than zero.</param>
    /// <param name="prefix">
    /// An optional string prefix to prepend to each generated location name. 
    /// For example, a prefix of "Rack-" would produce names like "Rack-1", "Rack-2", etc.
    /// </param>
    /// <returns>
    /// A <see cref="List{LocationDTO}"/> containing the newly created locations with assigned IDs
    /// and zero <see cref="LocationDTO.PartCount"/>. Names that already exist are skipped to prevent duplicates.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="count"/> is less than or equal to zero.</exception>
    Task<List<LocationDTO>> CreateLocationsAsync(
        LocationNumberingScheme scheme, 
        int count, 
        string? prefix = null);
}
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
}
using MESS.Data.Context;
using MESS.Data.Models;

namespace MESS.Services.CRUD.PartDefinitions;

/// <summary>
/// Provides functionality for resolving <see cref="PartDefinition"/> entities
/// from user-provided part information.
/// </summary>
/// <remarks>
/// Implementations should not call <c>SaveChanges</c>; persistence is the
/// responsibility of the calling service.
/// </remarks>
public interface IPartDefinitionResolver
{
    /// <summary>
    /// Resolves a <see cref="PartDefinition"/> using the provided name and optional number.
    /// Traceability (<see cref="PartDefinition.InputType"/>) and ID type
    /// (<see cref="PartDefinition.IsSerialNumberUnique"/>) are owned by the part definition
    /// and are not supplied or validated from work-instruction save paths.
    /// </summary>
    Task<PartDefinition?> ResolveAsync(
        ApplicationContext context,
        string? name,
        string? number);
}

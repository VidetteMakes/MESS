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
    /// <remarks>
    /// This method creates a new <see cref="PartDefinition"/> when no match is found. It is used by
    /// the update and new-version save paths. The work-instruction import/create path uses
    /// <see cref="LookupAsync"/> instead, which never creates.
    /// </remarks>
    Task<PartDefinition?> ResolveAsync(
        ApplicationContext context,
        string? name,
        string? number);

    /// <summary>
    /// Looks up an existing <see cref="PartDefinition"/> by case-insensitive name without creating one.
    /// Returns <c>null</c> when no match exists or the name is blank.
    /// </summary>
    /// <param name="context">The active context used for the lookup.</param>
    /// <param name="name">The part name to match (case-insensitive, trimmed).</param>
    Task<PartDefinition?> LookupAsync(ApplicationContext context, string? name);
}

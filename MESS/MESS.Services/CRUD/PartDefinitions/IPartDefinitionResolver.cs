using MESS.Data.Context;
using MESS.Data.Models;

namespace MESS.Services.CRUD.PartDefinitions;

/// <summary>
/// Provides functionality for resolving <see cref="PartDefinition"/> entities
/// from user-provided part information.
/// </summary>
/// <remarks>
/// This resolver is responsible for locating existing parts in the database
/// using normalized values (trimmed and case-insensitive comparisons).
/// If a matching part does not exist, a new <see cref="PartDefinition"/> entity
/// may be created and added to the provided <see cref="ApplicationContext"/>.
/// <para>
/// Implementations should not call <c>SaveChanges</c>; persistence is the
/// responsibility of the calling service.
/// </para>
/// </remarks>
public interface IPartDefinitionResolver
{
    /// <summary>
    /// Resolves a <see cref="PartDefinition"/> using the provided name and number.
    /// </summary>
    /// <param name="context">
    /// The active <see cref="ApplicationContext"/> used for querying and tracking entities.
    /// </param>
    /// <param name="name">
    /// The name of the part to resolve. Leading and trailing whitespace may be ignored.
    /// </param>
    /// <param name="number">
    /// The optional part number used to further distinguish the part.
    /// </param>
    /// <param name="isSerialNumberUnique">
    /// The value to set for the <see cref="PartDefinition.IsSerialNumberUnique"/> property
    /// </param>
    /// <returns>
    /// A task that resolves to an existing or newly created
    /// <see cref="PartDefinition"/> entity, or <c>null</c> if the
    /// provided name is empty or invalid.
    /// </returns>
    Task<PartDefinition?> ResolveAsync(
        ApplicationContext context,
        string? name,
        string? number,
        bool isSerialNumberUnique = true);
}
using MESS.Data.Context;
using MESS.Data.Models;
using MESS.Services.DTOs.WorkInstructions.Form;

namespace MESS.Services.CRUD.WorkInstructions;

/// <summary>
/// Defines behavior for applying changes from a 
/// <see cref="WorkInstructionFormDTO"/> to an existing
/// tracked <see cref="WorkInstruction"/> entity.
/// </summary>
/// <remarks>
/// Implementations are responsible for synchronizing scalar properties
/// and related collections (such as nodes and products) while respecting
/// Entity Framework Core tracking rules.
/// 
/// Implementations must not call <c>SaveChanges</c>; callers are responsible
/// for persisting changes.
/// </remarks>
public interface IWorkInstructionUpdater
{
    /// <summary>
    /// Applies the values from a <see cref="WorkInstructionFormDTO"/> to an
    /// existing tracked <see cref="WorkInstruction"/> entity.
    /// </summary>
    /// <remarks>
    /// The provided <paramref name="entity"/> must already be loaded from
    /// the database with its relevant navigation properties included and
    /// tracked by the supplied <paramref name="context"/>.
    /// 
    /// This method mutates the entity in place but does not persist changes.
    /// </remarks>
    /// <param name="dto">
    /// The DTO containing the desired state of the work instruction.
    /// </param>
    /// <param name="entity">
    /// The tracked entity to update.
    /// </param>
    /// <param name="context">
    /// The active <see cref="ApplicationContext"/> used for resolving
    /// related entities and tracking mutations.
    /// </param>
    /// <param name="minimalMode">
    /// When <c>true</c>, only scalar Step fields are updated; node structure (add, delete, reorder)
    /// and PartNode associations are left unchanged. Pass <c>true</c> when the work instruction
    /// has associated production logs.
    /// </param>
    /// <returns>
    /// A task that completes when the mutation process has finished.
    /// </returns>
    Task ApplyAsync(
        WorkInstructionFormDTO dto,
        WorkInstruction entity,
        ApplicationContext context,
        bool minimalMode = false);
}
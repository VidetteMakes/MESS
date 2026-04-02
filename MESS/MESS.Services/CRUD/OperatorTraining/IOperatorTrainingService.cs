using MESS.Data.Models;

namespace MESS.Services.CRUD.OperatorTraining;

/// <summary>
/// Provides CRUD operations for operator training modules.
/// </summary>
public interface IOperatorTrainingService
{
    /// <summary>
    /// Gets all training modules.
    /// </summary>
    /// <param name="includeDrafts">True to include unpublished modules.</param>
    Task<IReadOnlyList<OperatorTrainingModule>> GetAllAsync(bool includeDrafts = false);

    /// <summary>
    /// Creates a new training module.
    /// </summary>
    Task<OperatorTrainingModule> CreateAsync(OperatorTrainingModule module);

    /// <summary>
    /// Updates an existing training module.
    /// </summary>
    Task<OperatorTrainingModule?> UpdateAsync(OperatorTrainingModule module);

    /// <summary>
    /// Deletes a training module by id.
    /// </summary>
    Task DeleteAsync(int id);
}

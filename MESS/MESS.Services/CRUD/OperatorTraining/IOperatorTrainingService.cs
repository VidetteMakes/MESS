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

    /// <summary>
    /// Gets all training steps for the supplied modules.
    /// </summary>
    Task<IReadOnlyList<TrainingStep>> GetStepsAsync(IEnumerable<int>? moduleIds = null);

    /// <summary>
    /// Creates a new training step.
    /// </summary>
    Task<TrainingStep> CreateStepAsync(TrainingStep step);

    /// <summary>
    /// Updates an existing training step.
    /// </summary>
    Task<TrainingStep?> UpdateStepAsync(TrainingStep step);

    /// <summary>
    /// Deletes a training step by id.
    /// </summary>
    Task DeleteStepAsync(int id);
}

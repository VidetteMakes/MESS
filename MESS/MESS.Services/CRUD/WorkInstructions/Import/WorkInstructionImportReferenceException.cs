namespace MESS.Services.CRUD.WorkInstructions.Import;

/// <summary>
/// Thrown when saving a work instruction references Products or PartDefinitions that do not
/// already exist in the target database. Carries the structured <see cref="Result"/> so the UI
/// can render every missing reference grouped by category.
/// </summary>
public class WorkInstructionImportReferenceException(WorkInstructionImportResolutionResult result)
    : Exception("Imported work instruction references entities that do not exist.")
{
    /// <summary>The aggregated set of missing references that caused this exception.</summary>
    public WorkInstructionImportResolutionResult Result { get; } = result;
}

#pragma warning disable CS1591
namespace MESS.Services.TrainingMatrix;

/// <summary>
/// Represents the training state of a user for a module.
/// </summary>
public enum TrainingMatrixStatus
{
    NotStarted = 0,
    InProgress = 1,
    Completed = 2,
    Expired = 3
}
#pragma warning restore CS1591

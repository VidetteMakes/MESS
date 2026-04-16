#pragma warning disable CS1591
namespace MESS.Services.TrainingMatrix;

public interface ITrainingMatrixService
{
    Task<TrainingMatrixSnapshot> GetSnapshotAsync(bool includeDrafts, CancellationToken cancellationToken = default);
}
#pragma warning restore CS1591

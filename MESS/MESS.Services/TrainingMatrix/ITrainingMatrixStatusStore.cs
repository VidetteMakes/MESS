#pragma warning disable CS1591
namespace MESS.Services.TrainingMatrix;

public interface ITrainingMatrixStatusStore
{
    IReadOnlyCollection<TrainingMatrixScoreRecord> GetRecords();
    TrainingMatrixMqttState GetMqttState();
    ValueTask UpsertAsync(TrainingMatrixScoreRecord record);
    void SetMqttState(TrainingMatrixMqttState state);
}
#pragma warning restore CS1591

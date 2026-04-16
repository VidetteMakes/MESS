#pragma warning disable CS1591
using System.Collections.Concurrent;

namespace MESS.Services.TrainingMatrix;

public sealed class TrainingMatrixStatusStore : ITrainingMatrixStatusStore
{
    private readonly ConcurrentDictionary<string, TrainingMatrixScoreRecord> _records = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _stateLock = new();
    private TrainingMatrixMqttState _mqttState = new();

    public IReadOnlyCollection<TrainingMatrixScoreRecord> GetRecords()
    {
        return _records.Values.ToList();
    }

    public TrainingMatrixMqttState GetMqttState()
    {
        lock (_stateLock)
        {
            return _mqttState;
        }
    }

    public ValueTask UpsertAsync(TrainingMatrixScoreRecord record)
    {
        var key = $"{record.UserId ?? record.UserName ?? "unknown-user"}::{record.StepId?.ToString() ?? record.StepTitle ?? "unknown-step"}";
        _records[key] = record;
        return ValueTask.CompletedTask;
    }

    public void SetMqttState(TrainingMatrixMqttState state)
    {
        lock (_stateLock)
        {
            _mqttState = state;
        }
    }
}
#pragma warning restore CS1591

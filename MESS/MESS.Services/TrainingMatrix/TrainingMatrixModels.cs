#pragma warning disable CS1591
using MESS.Data.Models;

namespace MESS.Services.TrainingMatrix;

public sealed record TrainingMatrixScoreRecord
{
    public string? UserId { get; init; }
    public string? UserName { get; init; }
    public int? ModuleId { get; init; }
    public string? ModuleTitle { get; init; }
    public int? StepId { get; init; }
    public string? StepTitle { get; init; }
    public int Score { get; init; }
    public DateTimeOffset LastUpdatedOnUtc { get; init; }
    public string Source { get; init; } = "mqtt";
}

public sealed record TrainingMatrixCell
{
    public int Score { get; init; }
    public DateTimeOffset? LastUpdatedOnUtc { get; init; }
    public string? Source { get; init; }
}

public sealed record TrainingMatrixOperator
{
    public required string UserId { get; init; }
    public required string UserName { get; init; }
    public required string DisplayName { get; init; }
    public bool IsActive { get; init; }
}

public sealed record TrainingMatrixCompetencyRow
{
    public required int StepId { get; init; }
    public required int ModuleId { get; init; }
    public required string ModuleTitle { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public int StepOrder { get; init; }
    public double Coverage { get; init; }
    public required IReadOnlyDictionary<string, TrainingMatrixCell> Cells { get; init; }
}

public sealed record TrainingMatrixMqttState
{
    public bool IsEnabled { get; init; }
    public bool IsConnected { get; init; }
    public string Broker { get; init; } = string.Empty;
    public string Topic { get; init; } = string.Empty;
    public DateTimeOffset? LastMessageOnUtc { get; init; }
    public string? LastError { get; init; }
}

public sealed record TrainingMatrixSnapshot
{
    public required IReadOnlyList<OperatorTrainingModule> Modules { get; init; }
    public required IReadOnlyList<TrainingMatrixOperator> Operators { get; init; }
    public required IReadOnlyList<TrainingMatrixCompetencyRow> Competencies { get; init; }
    public required TrainingMatrixMqttState MqttState { get; init; }
}

public sealed record MqttTrainingMatrixUpdateMessage
{
    public string? UserId { get; init; }
    public string? UserName { get; init; }
    public int? ModuleId { get; init; }
    public string? ModuleTitle { get; init; }
    public int? StepId { get; init; }
    public string? StepTitle { get; init; }
    public int? Score { get; init; }
    public string? Source { get; init; }
}
#pragma warning restore CS1591

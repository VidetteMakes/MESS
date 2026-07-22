namespace MESS.Services.DTOs.ProductionLogs.Export;

/// <summary>A single attempt within an exported step.</summary>
public sealed class ProductionLogExportAttemptDto
{
    /// <summary>True = success, false = failure, null = not yet recorded.</summary>
    public bool? Success { get; set; }

    /// <summary>Operator notes recorded for this attempt.</summary>
    public string Notes { get; set; } = string.Empty;

    /// <summary>UTC timestamp when the attempt was submitted.</summary>
    public DateTimeOffset SubmitTime { get; set; }

    /// <summary>Name of the failure noun (defect location), if a failure was recorded.</summary>
    public string? FailureNoun { get; set; }

    /// <summary>Name of the failure adjective (defect type), if a failure was recorded.</summary>
    public string? FailureAdjective { get; set; }

    /// <summary>Database ID of the failure noun, used for resolution on import.</summary>
    public int? FailureNounId { get; set; }

    /// <summary>Database ID of the failure adjective, used for resolution on import.</summary>
    public int? FailureAdjectiveId { get; set; }
}

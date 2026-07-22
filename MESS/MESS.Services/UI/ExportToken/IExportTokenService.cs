namespace MESS.Services.UI.ExportToken;

/// <summary>
/// Scoped per-circuit service that tracks whether the current user has performed a download in the
/// last 15 minutes. Used to gate bulk deletion: a delete is only allowed after a confirmed export download.
/// </summary>
public interface IExportTokenService
{
    /// <summary>How long a recorded export token remains valid (15 minutes).</summary>
    static readonly TimeSpan TokenValidity = TimeSpan.FromMinutes(15);

    /// <summary>Records that the user has just downloaded an export with the given filename.</summary>
    void RecordExport(string filename);

    /// <summary>
    /// Returns true and the export filename when a valid (non-expired) export token exists;
    /// otherwise returns false.
    /// </summary>
    bool TryGetValidToken(out string? filename);

    /// <summary>Clears the token, e.g. after it has been consumed by a delete.</summary>
    void Invalidate();
}

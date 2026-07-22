namespace MESS.Services.UI.ExportToken;

/// <inheritdoc />
public sealed class ExportTokenService : IExportTokenService
{
    private string? _filename;
    private DateTimeOffset? _recordedAt;

    /// <inheritdoc />
    public void RecordExport(string filename)
    {
        _filename = filename;
        _recordedAt = DateTimeOffset.UtcNow;
    }

    /// <inheritdoc />
    public bool TryGetValidToken(out string? filename)
    {
        if (_recordedAt is not null && _filename is not null
            && DateTimeOffset.UtcNow - _recordedAt.Value <= IExportTokenService.TokenValidity)
        {
            filename = _filename;
            return true;
        }

        filename = null;
        return false;
    }

    /// <inheritdoc />
    public void Invalidate()
    {
        _filename = null;
        _recordedAt = null;
    }
}

namespace MESS.Services.Git;

/// <summary>
/// Represents metadata about a Git commit.
/// </summary>
public class GitCommitInfo
{
    /// <summary>
    /// The full SHA identifier of the commit.
    /// </summary>
    public string Sha { get; set; } = string.Empty;

    /// <summary>
    /// The commit message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The name of the commit author.
    /// </summary>
    public string AuthorName { get; set; } = string.Empty;

    /// <summary>
    /// The email of the commit author.
    /// </summary>
    public string AuthorEmail { get; set; } = string.Empty;

    /// <summary>
    /// The timestamp of the commit.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
}
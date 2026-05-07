namespace MESS.Data.Models;

/// <summary>
/// Represents the global Git configuration for the MESS instance.
/// This configuration defines the remote repository, target branch,
/// and authentication details used when interacting with a Git provider.
/// </summary>
/// <remarks>
/// This entity is intended to be a single-row table (Id = 1),
/// representing instance-wide configuration rather than per-user or per-entity settings.
/// </remarks>
public class GitConfiguration
{
    /// <summary>
    /// Primary key for the configuration record.
    /// This value is fixed to enforce a single-row configuration (typically Id = 1).
    /// </summary>
    public int Id { get; set; } = 1; // enforce single row

    /// <summary>
    /// The remote Git repository URL.
    /// Supports HTTPS and SSH formats (e.g., https://... or git@...).
    /// </summary>
    public string RemoteUrl { get; set; } = null!;

    /// <summary>
    /// The branch to which commits will be pushed.
    /// Defaults to "main".
    /// </summary>
    public string Branch { get; set; } = "main";

    /// <summary>
    /// The UTC timestamp indicating when this configuration was last updated.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The identifier (e.g., username) of the user who last updated this configuration.
    /// </summary>
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// The authentication method used for the remote repository.
    /// </summary>
    public GitAuthType AuthType { get; set; } = GitAuthType.None;
    
    /// <summary>
    /// Reference to the stored credential (NOT the secret itself).
    /// This value should point to a secure location such as an environment variable,
    /// key vault entry, or protected storage key used to retrieve the credential at runtime.
    /// </summary>
    public string? CredentialReference { get; set; }
}

/// <summary>
/// Defines the authentication mechanism used for accessing a remote Git repository.
/// </summary>
public enum GitAuthType
{
    /// <summary>
    /// No authentication is used.
    /// Typically only valid for public repositories.
    /// </summary>
    None = 0,

    /// <summary>
    /// Personal Access Token authentication over HTTPS.
    /// </summary>
    PersonalAccessToken = 1,

    /// <summary>
    /// SSH key-based authentication.
    /// </summary>
    Ssh = 2
}
using MESS.Services.DTOs.WorkInstructions.File;

namespace MESS.Services.Git;

/// <summary>
/// Provides synchronization between WorkInstruction domain data (stored in Postgres)
/// and the Git-based versioned representation used for history, auditing, and diffing.
/// 
/// This service acts as the orchestration boundary between:
/// - WorkInstruction persistence (source of truth in Postgres)
/// - Markdown serialization (portable file format)
/// - Git storage (version history and audit trail)
/// </summary>
/// <remarks>
/// DESIGN PRINCIPLES:
/// - Markdown files contain NO database identifiers
/// - WorkInstructions are identified by business data (Title + Product associations)
/// - File paths are derived internally using deterministic rules
/// - The first associated product determines the primary folder
/// - All commits are explicitly created on user-driven save actions
/// 
/// IDENTITY MODEL:
/// - User identity is resolved via ICurrentUserService
/// - The service does not directly depend on ASP.NET Identity or AuthenticationStateProvider
/// - A consistent "actor identity" is used for all Git commits (author name/email)
/// 
/// INFRASTRUCTURE:
/// - Repository path is configured at application level (single repo per deployment)
/// - This service is called after successful WorkInstruction persistence in Postgres
/// </remarks>
public interface IWorkInstructionGitSyncService
{
    /// <summary>
    /// Commits a work instruction to the Git repository by serializing it to Markdown and writing it
    /// to a file based on its title. If the title has changed, the existing file is moved so that the
    /// rename and content update are captured in a single commit.
    /// </summary>
    /// <param name="dto">The work instruction to commit.</param>
    /// <param name="commitMessage">The commit message describing the change.</param>
    /// <param name="originalTitle">
    /// The previous title of the work instruction. If provided and different from the current title,
    /// the underlying file will be renamed accordingly.
    /// </param>
    /// <returns>The SHA of the created commit.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="dto"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if the title or commit message is null or whitespace.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if a rename is attempted and a file with the new title already exists.
    /// </exception>
    /// <remarks>
    /// The file path is derived solely from the work instruction title. When a rename occurs,
    /// the file move is staged prior to committing so that Git can properly track the rename.
    /// This method performs both the rename (if applicable) and content update in a single commit.
    /// </remarks>
    Task<string> CommitAsync(
        WorkInstructionFileDTO dto,
        string commitMessage,
        string? originalTitle = null);

    /// <summary>
    /// Retrieves the latest version of a WorkInstruction from Git using its business identity.
    /// </summary>
    /// <param name="title">
    /// The WorkInstruction title (used as the primary identity key).
    /// </param>
    /// <returns>
    /// The latest WorkInstruction snapshot, or null if not found.
    /// </returns>
    Task<WorkInstructionFileDTO?> GetLatestFromGitAsync(
        string title);

    /// <summary>
    /// Retrieves the Git commit history for a WorkInstruction identified by its title.
    /// </summary>
    /// <param name="title">
    /// The unique WorkInstruction title used to resolve the corresponding Git file.
    /// </param>
    /// <param name="maxCount">
    /// Optional maximum number of commits to return, ordered from newest to oldest.
    /// </param>
    /// <returns>
    /// A list of Git commit metadata representing the version history of the WorkInstruction.
    /// </returns>
    Task<IReadOnlyList<GitCommitInfo>> GetHistoryAsync(
        string title,
        int? maxCount = null);

    /// <summary>
    /// Retrieves a specific version of a WorkInstruction from Git.
    /// </summary>
    /// <param name="title">
    /// The WorkInstruction title (used as the primary identity key).
    /// </param>
    /// <param name="commitSha">
    /// The Git commit SHA to restore.
    /// </param>
    /// <returns>
    /// The WorkInstruction at the specified commit, or null if not found.
    /// </returns>
    Task<WorkInstructionFileDTO?> GetVersionFromGitAsync(
        string title,
        string commitSha);
}
namespace MESS.Services.Git;

/// <summary>
/// Provides low-level Git repository operations for working with versioned file content.
/// This service abstracts LibGit2Sharp and exposes a simplified API for committing,
/// retrieving, and inspecting file history within a Git repository.
/// </summary>
/// <remarks>
/// This service is domain-agnostic and operates purely on file paths and content.
/// Folder structure (e.g., grouping files by product) is defined by higher-level services,
/// such as a WorkInstruction Git store.
/// 
/// Expected usage in MESS:
/// - A single repository contains all work instructions.
/// - Files are organized into folders (e.g., per product) for human readability.
/// - Each commit represents an explicit save action performed by a user.
/// - All operations target the main branch unless otherwise specified.
/// </remarks>
public interface IGitRepositoryService
{
    /// <summary>
    /// Initializes a Git repository at the specified path if one does not already exist.
    /// </summary>
    /// <param name="repositoryPath">The root directory of the Git repository.</param>
    void InitializeRepository(string repositoryPath);

    /// <summary>
    /// Determines whether a valid Git repository exists at the specified path.
    /// </summary>
    /// <param name="repositoryPath">The path to check.</param>
    /// <returns>True if the path contains a valid Git repository; otherwise false.</returns>
    bool RepositoryExists(string repositoryPath);

    /// <summary>
    /// Commits a file to the repository on the main branch.
    /// The file will be written (or overwritten), staged, and committed as a new version.
    /// </summary>
    /// <param name="repositoryPath">The root repository path.</param>
    /// <param name="relativeFilePath">
    /// The file path relative to the repository root, including any folder structure.
    /// Example: "ProductA/WI-123.md".
    /// </param>
    /// <param name="content">The full file content to write.</param>
    /// <param name="commitMessage">The commit message describing the change.</param>
    /// <param name="authorName">The name of the commit author (MESS user).</param>
    /// <param name="authorEmail">The email of the commit author.</param>
    /// <returns>The SHA of the created commit.</returns>
    /// <remarks>
    /// This method assumes:
    /// - The repository is already initialized.
    /// - The main branch is the active branch.
    /// - Parent directories will be created if they do not exist.
    /// 
    /// This method does not enforce any naming or folder conventions.
    /// </remarks>
    Task<string> CommitFileAsync(
        string repositoryPath,
        string relativeFilePath,
        string content,
        string commitMessage,
        string authorName,
        string authorEmail);

    /// <summary>
    /// Retrieves the content of a file from the latest commit (HEAD) on the main branch.
    /// </summary>
    /// <param name="repositoryPath">The root repository path.</param>
    /// <param name="relativeFilePath">
    /// The file path relative to the repository root (e.g., "ProductA/WI-123.md").
    /// </param>
    /// <returns>
    /// The file content if found; otherwise null if the file does not exist in HEAD.
    /// </returns>
    Task<string?> GetFileAtHeadAsync(
        string repositoryPath,
        string relativeFilePath);

    /// <summary>
    /// Retrieves the content of a file from a specific commit.
    /// </summary>
    /// <param name="repositoryPath">The root repository path.</param>
    /// <param name="relativeFilePath">
    /// The file path relative to the repository root.
    /// </param>
    /// <param name="commitSha">The SHA identifier of the commit.</param>
    /// <returns>
    /// The file content if found; otherwise null if the file does not exist in that commit.
    /// </returns>
    Task<string?> GetFileAtCommitAsync(
        string repositoryPath,
        string relativeFilePath,
        string commitSha);

    /// <summary>
    /// Retrieves the commit history for a specific file.
    /// </summary>
    /// <param name="repositoryPath">The root repository path.</param>
    /// <param name="relativeFilePath">
    /// The file path relative to the repository root.
    /// </param>
    /// <param name="maxCount">Optional limit on the number of commits returned.</param>
    /// <returns>
    /// A list of commit metadata ordered from newest to oldest.
    /// </returns>
    /// <remarks>
    /// Only commits that include changes to the specified file are returned.
    /// </remarks>
    Task<IReadOnlyList<GitCommitInfo>> GetFileHistoryAsync(
        string repositoryPath,
        string relativeFilePath,
        int? maxCount = null);

    /// <summary>
    /// Generates a unified diff between two commits for a specific file.
    /// </summary>
    /// <param name="repositoryPath">The root repository path.</param>
    /// <param name="relativeFilePath">
    /// The file path relative to the repository root.
    /// </param>
    /// <param name="olderCommitSha">The base (older) commit SHA.</param>
    /// <param name="newerCommitSha">The target (newer) commit SHA.</param>
    /// <returns>
    /// A unified diff string representing the changes between the two commits.
    /// </returns>
    /// <remarks>
    /// The diff output is in standard Git patch format and can be used for display
    /// or further parsing.
    /// </remarks>
    Task<string> GetFileDiffAsync(
        string repositoryPath,
        string relativeFilePath,
        string olderCommitSha,
        string newerCommitSha);

    /// <summary>
    /// Moves a file within the Git repository and stages the change so it will be included
    /// in the next commit. This enables Git to detect the operation as a rename when committed.
    /// </summary>
    /// <param name="repositoryPath">The full path to the Git repository.</param>
    /// <param name="oldRelativePath">The current relative path of the file within the repository.</param>
    /// <param name="newRelativePath">The new relative path of the file within the repository.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the specified repository path is not a valid Git repository.
    /// </exception>
    /// <exception cref="FileNotFoundException">
    /// Thrown if the file at <paramref name="oldRelativePath"/> does not exist.
    /// </exception>
    Task MoveFileAsync(
        string repositoryPath,
        string oldRelativePath,
        string newRelativePath);
}
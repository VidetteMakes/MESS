using LibGit2Sharp;

namespace MESS.Services.Git;

/// <inheritdoc />
public class GitRepositoryService : IGitRepositoryService
{
    /// <inheritdoc />
    public void InitializeRepository(string repositoryPath)
    {
        if (string.IsNullOrWhiteSpace(repositoryPath))
            throw new ArgumentException("Repository path cannot be null or empty.", nameof(repositoryPath));

        // Normalize the path
        repositoryPath = Path.GetFullPath(repositoryPath);

        // Ensure the directory exists
        if (!Directory.Exists(repositoryPath))
        {
            Directory.CreateDirectory(repositoryPath);
        }

        // If it's already a valid Git repository, do nothing
        if (Repository.IsValid(repositoryPath))
        {
            return;
        }

        // Initialize the repository
        Repository.Init(repositoryPath);

        // Optional but recommended: create an initial commit so HEAD exists
        using var repo = new Repository(repositoryPath);

        if (repo.Head.Tip != null) return;
        
        var readmePath = Path.Combine(repositoryPath, ".gitkeep");

        // Create a placeholder file so the first commit has content
        File.WriteAllText(readmePath, "Initialized repository");

        Commands.Stage(repo, ".gitkeep");

        var signature = new Signature(
            "MESS System",
            "mess@local",
            DateTimeOffset.UtcNow
        );

        repo.Commit("Initial commit", signature, signature);
    }

    /// <inheritdoc />
    public bool RepositoryExists(string repositoryPath)
    {
        if (string.IsNullOrWhiteSpace(repositoryPath))
            return false;

        try
        {
            // Normalize path to avoid relative path issues
            repositoryPath = Path.GetFullPath(repositoryPath);

            // Quick check: directory must exist
            return Directory.Exists(repositoryPath) &&
                   // Let LibGit2Sharp validate the repo structure
                   Repository.IsValid(repositoryPath);
        }
        catch
        {
            // If anything goes wrong (invalid path, permissions, etc.),
            // treat it as "repository does not exist"
            return false;
        }
    }

    /// <inheritdoc />
    public Task<string> CommitFileAsync(
        string repositoryPath,
        string relativeFilePath,
        string content,
        string commitMessage,
        string authorName,
        string authorEmail)
    {
        if (string.IsNullOrWhiteSpace(repositoryPath))
            throw new ArgumentException("Repository path cannot be null or empty.", nameof(repositoryPath));

        if (string.IsNullOrWhiteSpace(relativeFilePath))
            throw new ArgumentException("Relative file path cannot be null or empty.", nameof(relativeFilePath));

        if (content is null)
            throw new ArgumentNullException(nameof(content));

        if (string.IsNullOrWhiteSpace(commitMessage))
            throw new ArgumentException("Commit message cannot be null or empty.", nameof(commitMessage));

        if (string.IsNullOrWhiteSpace(authorName))
            throw new ArgumentException("Author name cannot be null or empty.", nameof(authorName));

        if (string.IsNullOrWhiteSpace(authorEmail))
            throw new ArgumentException("Author email cannot be null or empty.", nameof(authorEmail));

        // Normalize paths
        repositoryPath = Path.GetFullPath(repositoryPath);

        if (!Repository.IsValid(repositoryPath))
            throw new InvalidOperationException($"No valid Git repository found at '{repositoryPath}'.");

        // Ensure we are using forward slashes for Git paths
        relativeFilePath = relativeFilePath.Replace('\\', '/');

        var fullFilePath = Path.Combine(repositoryPath, relativeFilePath);

        // Ensure directory structure exists (e.g., ProductA/)
        var directory = Path.GetDirectoryName(fullFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var repo = new Repository(repositoryPath);

        // Write file content (overwrite is expected)
        File.WriteAllText(fullFilePath, content);

        // Stage the file
        Commands.Stage(repo, relativeFilePath);

        // Check if there are actually changes (prevents empty commits)
        var status = repo.RetrieveStatus(new StatusOptions());
        if (!status.IsDirty)
        {
            // Nothing changed—return current HEAD commit SHA
            return Task.FromResult(repo.Head.Tip?.Sha ?? string.Empty);
        }

        // Create commit signature
        var signature = new Signature(
            authorName,
            authorEmail,
            DateTimeOffset.UtcNow
        );

        // Commit changes
        var commit = repo.Commit(commitMessage, signature, signature);

        return Task.FromResult(commit.Sha);
    }

    /// <inheritdoc />
    public Task<string?> GetFileAtHeadAsync(string repositoryPath, string relativeFilePath)
    {
        if (string.IsNullOrWhiteSpace(repositoryPath))
            throw new ArgumentException("Repository path cannot be null or empty.", nameof(repositoryPath));

        if (string.IsNullOrWhiteSpace(relativeFilePath))
            throw new ArgumentException("Relative file path cannot be null or empty.", nameof(relativeFilePath));

        // Normalize paths
        repositoryPath = Path.GetFullPath(repositoryPath);
        relativeFilePath = relativeFilePath.Replace('\\', '/');

        if (!Repository.IsValid(repositoryPath))
            throw new InvalidOperationException($"No valid Git repository found at '{repositoryPath}'.");

        using var repo = new Repository(repositoryPath);

        // If no commits exist yet
        var head = repo.Head?.Tip;
        if (head == null)
            return Task.FromResult<string?>(null);

        // Try to get the file from the commit tree
        var entry = head[relativeFilePath];

        if (entry == null || entry.TargetType != TreeEntryTargetType.Blob)
            return Task.FromResult<string?>(null);

        var blob = (Blob)entry.Target;

        // Read file content from Git object database (not filesystem)
        var content = blob.GetContentText();

        return Task.FromResult<string?>(content);
    }

    /// <inheritdoc />
    public Task<string?> GetFileAtCommitAsync(
        string repositoryPath,
        string relativeFilePath,
        string commitSha)
    {
        if (string.IsNullOrWhiteSpace(repositoryPath))
            throw new ArgumentException("Repository path cannot be null or empty.", nameof(repositoryPath));

        if (string.IsNullOrWhiteSpace(relativeFilePath))
            throw new ArgumentException("Relative file path cannot be null or empty.", nameof(relativeFilePath));

        if (string.IsNullOrWhiteSpace(commitSha))
            throw new ArgumentException("Commit SHA cannot be null or empty.", nameof(commitSha));

        repositoryPath = Path.GetFullPath(repositoryPath);
        relativeFilePath = relativeFilePath.Replace('\\', '/');

        if (!Repository.IsValid(repositoryPath))
            throw new InvalidOperationException($"No valid Git repository found at '{repositoryPath}'.");

        using var repo = new Repository(repositoryPath);

        // Resolve commit safely
        var commit = repo.Lookup<Commit>(commitSha);
        if (commit == null)
            return Task.FromResult<string?>(null);

        // Traverse tree to find file
        var entry = commit[relativeFilePath];

        if (entry == null || entry.TargetType != TreeEntryTargetType.Blob)
            return Task.FromResult<string?>(null);

        var blob = (Blob)entry.Target;

        var content = blob.GetContentText();

        return Task.FromResult<string?>(content);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<GitCommitInfo>> GetFileHistoryAsync(
        string repositoryPath,
        string relativeFilePath,
        int? maxCount = null)
    {
        if (string.IsNullOrWhiteSpace(repositoryPath))
            throw new ArgumentException("Repository path cannot be null or empty.", nameof(repositoryPath));

        if (string.IsNullOrWhiteSpace(relativeFilePath))
            throw new ArgumentException("Relative file path cannot be null or empty.", nameof(relativeFilePath));

        repositoryPath = Path.GetFullPath(repositoryPath);
        relativeFilePath = relativeFilePath.Replace('\\', '/');

        if (!Repository.IsValid(repositoryPath))
            throw new InvalidOperationException($"No valid Git repository found at '{repositoryPath}'.");

        using var repo = new Repository(repositoryPath);

        var filter = new CommitFilter
        {
            IncludeReachableFrom = repo.Head,
            SortBy = CommitSortStrategies.Time
        };

        var results = new List<GitCommitInfo>();

        // IMPORTANT: QueryBy is file-specific history traversal
        var commits = repo.Commits.QueryBy(relativeFilePath, filter);

        foreach (var commit in commits)
        {
            results.Add(new GitCommitInfo
            {
                Sha = commit.Commit.Sha,
                Message = commit.Commit.MessageShort ?? commit.Commit.Message,
                AuthorName = commit.Commit.Author.Name,
                AuthorEmail = commit.Commit.Author.Email,
                Timestamp = commit.Commit.Author.When
            });

            if (maxCount.HasValue && results.Count >= maxCount.Value)
                break;
        }

        return Task.FromResult<IReadOnlyList<GitCommitInfo>>(results);
    }

    /// <inheritdoc />
    public Task<string> GetFileDiffAsync(
        string repositoryPath,
        string relativeFilePath,
        string olderCommitSha,
        string newerCommitSha)
    {
        if (string.IsNullOrWhiteSpace(repositoryPath))
            throw new ArgumentException("Repository path cannot be null or empty.", nameof(repositoryPath));

        if (string.IsNullOrWhiteSpace(relativeFilePath))
            throw new ArgumentException("Relative file path cannot be null or empty.", nameof(relativeFilePath));

        if (string.IsNullOrWhiteSpace(olderCommitSha))
            throw new ArgumentException("Older commit SHA cannot be null or empty.", nameof(olderCommitSha));

        if (string.IsNullOrWhiteSpace(newerCommitSha))
            throw new ArgumentException("Newer commit SHA cannot be null or empty.", nameof(newerCommitSha));

        repositoryPath = Path.GetFullPath(repositoryPath);
        relativeFilePath = relativeFilePath.Replace('\\', '/');

        if (!Repository.IsValid(repositoryPath))
            throw new InvalidOperationException($"No valid Git repository found at '{repositoryPath}'.");

        using var repo = new Repository(repositoryPath);

        var oldCommit = repo.Lookup<Commit>(olderCommitSha);
        var newCommit = repo.Lookup<Commit>(newerCommitSha);

        if (oldCommit == null || newCommit == null)
            return Task.FromResult(string.Empty);

        var oldEntry = oldCommit[relativeFilePath];
        var newEntry = newCommit[relativeFilePath];

        // If file didn't exist in one of the commits
        var oldBlob = oldEntry?.Target as Blob;
        var newBlob = newEntry?.Target as Blob;

        var oldContent = oldBlob?.GetContentText() ?? string.Empty;
        var newContent = newBlob?.GetContentText() ?? string.Empty;

        // Use LibGit2Sharp diff engine
        var patch = repo.Diff.Compare<Patch>(
            oldCommit.Tree,
            newCommit.Tree
        );

        // Filter to only the file we care about
        var filePatch = patch.FirstOrDefault(p => p.Path == relativeFilePath);

        // If Git didn't produce a structured patch, fallback to manual diff text
        return Task.FromResult(filePatch == null ? BuildSimpleDiff(oldContent, newContent) : filePatch.Patch);
    }
    
    private static string BuildSimpleDiff(string oldText, string newText)
    {
        var oldLines = oldText.Split('\n');
        var newLines = newText.Split('\n');

        var sb = new System.Text.StringBuilder();

        sb.AppendLine("--- Old");
        sb.AppendLine("+++ New");

        var max = Math.Max(oldLines.Length, newLines.Length);

        for (var i = 0; i < max; i++)
        {
            var oldLine = i < oldLines.Length ? oldLines[i] : string.Empty;
            var newLine = i < newLines.Length ? newLines[i] : string.Empty;

            if (oldLine == newLine) continue;
            sb.AppendLine($"- {oldLine}");
            sb.AppendLine($"+ {newLine}");
        }

        return sb.ToString();
    }

    /// <inheritdoc />
    public Task MoveFileAsync(
        string repositoryPath,
        string oldRelativePath,
        string newRelativePath)
    {
        repositoryPath = Path.GetFullPath(repositoryPath);

        oldRelativePath = oldRelativePath.Replace('\\', '/');
        newRelativePath = newRelativePath.Replace('\\', '/');

        if (!Repository.IsValid(repositoryPath))
            throw new InvalidOperationException($"No valid Git repository found at '{repositoryPath}'.");

        using var repo = new Repository(repositoryPath);

        var oldFullPath = Path.Combine(repositoryPath, oldRelativePath);
        var newFullPath = Path.Combine(repositoryPath, newRelativePath);

        if (!File.Exists(oldFullPath))
            throw new FileNotFoundException($"File not found at '{oldRelativePath}'.");

        // Ensure destination directory exists
        var newDirectory = Path.GetDirectoryName(newFullPath);
        if (!string.IsNullOrEmpty(newDirectory) && !Directory.Exists(newDirectory))
        {
            Directory.CreateDirectory(newDirectory);
        }

        // Move file
        File.Move(oldFullPath, newFullPath, overwrite: true);

        // Stage BOTH paths
        Commands.Stage(repo, oldRelativePath);
        Commands.Stage(repo, newRelativePath);

        return Task.CompletedTask;
    }
}
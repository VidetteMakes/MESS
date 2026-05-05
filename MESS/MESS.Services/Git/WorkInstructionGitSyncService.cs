using MESS.Services.CRUD.ApplicationUser;
using MESS.Services.DTOs.WorkInstructions.File;
using MESS.Services.Files.WorkInstructions;
using Serilog;

namespace MESS.Services.Git;

/// <inheritdoc />
public class WorkInstructionGitSyncService : IWorkInstructionGitSyncService
{
    private static class RepoPaths
    {
        public const string RootFolder = "mess-git-repo";
        public const string WorkInstructionsFolder = "work-instructions";
    }

    private readonly string _repositoryPath;
    
    private readonly IGitRepositoryService _gitRepository;
    private readonly IWorkInstructionMarkdownService _markdownService;
    private readonly ICurrentUserService _currentUserService;

    /// <summary>
    /// Constructs an instance of the WorkInstructionGitSyncService, pulling in necessary service dependencies.
    /// </summary>
    /// <param name="gitRepository"></param>
    /// <param name="markdownService"></param>
    /// <param name="currentUserService"></param>
    public WorkInstructionGitSyncService(
        IGitRepositoryService gitRepository,
        IWorkInstructionMarkdownService markdownService,
        ICurrentUserService currentUserService)
    {
        _gitRepository = gitRepository;
        _markdownService = markdownService;
        _currentUserService = currentUserService;
        _repositoryPath = Path.Combine(AppContext.BaseDirectory, RepoPaths.RootFolder);
    }
    
    /// <inheritdoc />
    public async Task<string> CommitAsync(
        WorkInstructionFileDTO dto,
        string commitMessage,
        string? originalTitle = null)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (string.IsNullOrWhiteSpace(dto.Title))
            throw new ArgumentException("WorkInstruction title is required.", nameof(dto));

        if (string.IsNullOrWhiteSpace(commitMessage))
            throw new ArgumentException("Commit message is required.", nameof(commitMessage));

        Log.Information("Starting Git commit for WorkInstruction '{Title}'", dto.Title);

        var markdown = _markdownService.Serialize(dto);

        var newPath = ResolveWorkInstructionGitPath(dto.Title);
        var oldPath = string.IsNullOrWhiteSpace(originalTitle)
            ? newPath
            : ResolveWorkInstructionGitPath(originalTitle);

        var isRename = !string.Equals(oldPath, newPath, StringComparison.OrdinalIgnoreCase);

        if (isRename)
        {
            Log.Information("Detected rename: '{OldTitle}' → '{NewTitle}'",
                originalTitle, dto.Title);

            var newFullPath = Path.Combine(_repositoryPath, newPath);

            if (File.Exists(newFullPath))
            {
                Log.Warning("Commit aborted: file already exists at '{Path}'", newFullPath);
                throw new InvalidOperationException("A work instruction with this title already exists.");
            }
        }

        var authorName = _currentUserService.GetUserName();
        var authorEmail = _currentUserService.GetEmail();

        Log.Debug("Git author resolved as {Author} <{Email}>", authorName, authorEmail);

        var fullFolderPath = Path.Combine(_repositoryPath, RepoPaths.WorkInstructionsFolder);
        Directory.CreateDirectory(fullFolderPath);

        if (isRename)
        {
            Log.Information("Moving Git file '{OldPath}' → '{NewPath}'", oldPath, newPath);

            await _gitRepository.MoveFileAsync(
                repositoryPath: _repositoryPath,
                oldRelativePath: oldPath,
                newRelativePath: newPath);
        }

        var commitSha = await _gitRepository.CommitFileAsync(
            repositoryPath: _repositoryPath,
            relativeFilePath: newPath,
            content: markdown,
            commitMessage: commitMessage,
            authorName: authorName,
            authorEmail: authorEmail);

        Log.Information("Git commit successful for '{Title}' with SHA {Sha}",
            dto.Title, commitSha);

        return commitSha;
    }

    /// <inheritdoc />
    public async Task<WorkInstructionFileDTO?> GetLatestFromGitAsync(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        Log.Debug("Fetching latest Git version for WorkInstruction '{Title}'", title);

        var relativePath = ResolveWorkInstructionGitPath(title);

        var markdown = await _gitRepository.GetFileAtHeadAsync(
            repositoryPath: _repositoryPath,
            relativeFilePath: relativePath);

        if (markdown == null)
        {
            Log.Warning("No Git file found for WorkInstruction '{Title}'", title);
            return null;
        }

        Log.Debug("Git file found for '{Title}', parsing markdown", title);

        return _markdownService.Parse(markdown);
    }
    
    /// <inheritdoc />
    public async Task<IReadOnlyList<GitCommitInfo>> GetHistoryAsync(
        string title,
        int? maxCount = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        Log.Information("Retrieving Git history for '{Title}' (Max: {Max})", title, maxCount);

        var relativePath = ResolveWorkInstructionGitPath(title);

        var history = await _gitRepository.GetFileHistoryAsync(
            repositoryPath: _repositoryPath,
            relativeFilePath: relativePath,
            maxCount: maxCount);

        Log.Information("Retrieved {Count} commits for '{Title}'",
            history.Count, title);

        return history;
    }

    /// <inheritdoc />
    public async Task<WorkInstructionFileDTO?> GetVersionFromGitAsync(
        string title,
        string commitSha)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        if (string.IsNullOrWhiteSpace(commitSha))
            throw new ArgumentException("Commit SHA is required.", nameof(commitSha));

        Log.Information("Fetching Git version '{Sha}' for WorkInstruction '{Title}'",
            commitSha, title);

        var relativePath = ResolveWorkInstructionGitPath(title);

        var markdown = await _gitRepository.GetFileAtCommitAsync(
            repositoryPath: _repositoryPath,
            relativeFilePath: relativePath,
            commitSha: commitSha);

        if (markdown == null)
        {
            Log.Warning("No Git version found for '{Title}' at commit {Sha}",
                title, commitSha);

            return null;
        }

        return _markdownService.Parse(markdown);
    }

    private static string ToSafeFileName(string input)
    {
        input = Path.GetInvalidFileNameChars().Aggregate(input, (current, c) => current.Replace(c, '-'));

        return input.Trim();
    }
    
    private static string ResolveWorkInstructionGitPath(string title)
    {
        var safeTitle = ToSafeFileName(title).Replace(" ", "_");

        return Path.Combine(
            RepoPaths.WorkInstructionsFolder,
            $"{safeTitle.ToLowerInvariant()}.md");
    }
}

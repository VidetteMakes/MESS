using MESS.Services.CRUD.ApplicationUser;
using MESS.Services.DTOs.WorkInstructions.File;
using MESS.Services.Files.WorkInstructions;

namespace MESS.Services.Git;

/// <inheritdoc />
public class WorkInstructionGitSyncService : IWorkInstructionGitSyncService
{
    private const string REPOSITORY_FOLDER_NAME = "mess-git-repo";
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
        _repositoryPath = Path.Combine(AppContext.BaseDirectory, REPOSITORY_FOLDER_NAME);
    }
    
    /// <inheritdoc />
    public async Task<string> CommitAsync(
        WorkInstructionFileDTO dto,
        string commitMessage)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (string.IsNullOrWhiteSpace(dto.Title))
            throw new ArgumentException("WorkInstruction title is required.", nameof(dto));

        if (string.IsNullOrWhiteSpace(commitMessage))
            throw new ArgumentException("Commit message is required.", nameof(commitMessage));

        // 1. Serialize DTO → Markdown
        var markdown = _markdownService.Serialize(dto);

        // 2. Determine file path from TITLE ONLY (no product dependency)
        var relativePath = ResolveFilePathFromTitle(dto.Title);

        // 3. Resolve current user for commit attribution
        var authorName = _currentUserService.GetUserName();
        var authorEmail = _currentUserService.GetEmail();

        // 4. Commit to Git
        var commitSha = await _gitRepository.CommitFileAsync(
            repositoryPath: _repositoryPath,
            relativeFilePath: relativePath,
            content: markdown,
            commitMessage: commitMessage,
            authorName: authorName,
            authorEmail: authorEmail);

        return commitSha;
    }

    /// <inheritdoc />
    public async Task<WorkInstructionFileDTO?> GetLatestFromGitAsync(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        // 1. Resolve file path from title (same identity rule as all Git operations)
        var relativePath = ResolveFilePathFromTitle(title);

        // 2. Get latest file content from HEAD
        var markdown = await _gitRepository.GetFileAtHeadAsync(
            repositoryPath: _repositoryPath,
            relativeFilePath: relativePath);

        // 3. If file doesn't exist in Git, return null
        if (markdown == null)
            return null;

        // 4. Convert Markdown → DTO
        var dto = _markdownService.Parse(markdown);

        return dto;
    }
    
    /// <inheritdoc />
    public async Task<IReadOnlyList<GitCommitInfo>> GetHistoryAsync(
        string title,
        int? maxCount = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        // 1. Resolve file path using ONLY title (no product assumptions)
        var relativePath = ResolveFilePathFromTitle(title);

        // 2. Query Git history
        var history = await _gitRepository.GetFileHistoryAsync(
            repositoryPath: _repositoryPath,
            relativeFilePath: relativePath,
            maxCount: maxCount);

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

        // 1. Resolve file path from title (Git identity rule)
        var relativePath = ResolveFilePathFromTitle(title);

        // 2. Load file content at specific commit
        var markdown = await _gitRepository.GetFileAtCommitAsync(
            repositoryPath: _repositoryPath,
            relativeFilePath: relativePath,
            commitSha: commitSha);

        // 3. If file doesn't exist at that commit, return null
        if (markdown == null)
            return null;

        // 4. Convert Markdown → DTO
        var dto = _markdownService.Parse(markdown);

        return dto;
    }
    
    /// <summary>
    /// Resolves file path using only WorkInstruction title.
    /// This assumes title uniquely identifies a file within the repository.
    /// </summary>
    private static string ResolveFilePathFromTitle(string title)
    {
        var safeTitle = ToSafeFileName(title);

        return $"{safeTitle}.md";
    }

    private static string ToSafeFileName(string input)
    {
        input = Path.GetInvalidFileNameChars().Aggregate(input, (current, c) => current.Replace(c, '-'));

        return input.Trim();
    }
}

using CodeRefine.Api.DTOs.GitHub;

namespace CodeRefine.Api.Services.GitHub;

/// <summary>
/// All GitHub access flows through this abstraction. Controllers must never
/// call GitHub directly, and credentials never leave this backend.
/// </summary>
public interface IGitHubService
{
    Task<IReadOnlyList<RepositoryDto>> GetRepositoriesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BranchDto>> GetBranchesAsync(Guid repositoryId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PullRequestDto>> GetPullRequestsAsync(Guid repositoryId, string state = "open", CancellationToken cancellationToken = default);

    Task<PullRequestDto> GetPullRequestAsync(Guid repositoryId, int number, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PullRequestFileDto>> GetPullRequestFilesAsync(Guid repositoryId, int number, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates the improvement branch, commits the approved and verified file
    /// contents, pushes, and opens a pull request. Only called after the
    /// approval gate has passed.
    /// </summary>
    Task<PullRequestDto> CreateImprovementPullRequestAsync(
        Guid repositoryId,
        string branchName,
        string baseBranch,
        IReadOnlyDictionary<string, string> fileContents,
        string commitMessage,
        string title,
        string body,
        CancellationToken cancellationToken = default);
}

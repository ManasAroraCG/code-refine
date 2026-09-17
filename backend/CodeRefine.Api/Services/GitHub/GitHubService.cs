using CodeRefine.Api.Configuration;
using CodeRefine.Api.Data;
using CodeRefine.Api.DTOs.GitHub;
using CodeRefine.Api.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Octokit;

namespace CodeRefine.Api.Services.GitHub;

/// <summary>
/// GitHub App backed implementation. This is the only component permitted to
/// talk to GitHub; installation tokens never leave this class.
/// </summary>
public class GitHubService : IGitHubService
{
    private readonly IGitHubAppTokenProvider _tokenProvider;
    private readonly AppDbContext _dbContext;
    private readonly GitHubSettings _settings;
    private readonly ILogger<GitHubService> _logger;

    public GitHubService(
        IGitHubAppTokenProvider tokenProvider,
        AppDbContext dbContext,
        IOptions<GitHubSettings> settings,
        ILogger<GitHubService> logger)
    {
        _tokenProvider = tokenProvider;
        _dbContext = dbContext;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Lists repositories visible to the installation and mirrors them into the
    /// database so every repository has a stable internal identifier.
    /// </summary>

    public async Task<IReadOnlyList<RepositoryDto>> GetRepositoriesAsync(
    CancellationToken cancellationToken = default)
    {
        var client = await CreateClientAsync(cancellationToken);

        var response = await ExecuteAsync(
            () => client.GitHubApps.Installation.GetAllRepositoriesForCurrent(),
            "list installation repositories");

        return response.Repositories
            .Select(repo => new RepositoryDto
            {
                Id = DeriveRepositoryId(repo.Id.ToString()),
                GitHubRepoId = repo.Id.ToString(),
                Owner = repo.Owner.Login,
                Name = repo.Name,
                FullName = repo.FullName,
                DefaultBranch = repo.DefaultBranch,
                IsPrivate = repo.Private,
                CreatedAt = repo.CreatedAt.UtcDateTime
            })
            .ToList();
    }
    public async Task<IReadOnlyList<BranchDto>> GetBranchesAsync(
     string githubRepoId,
     CancellationToken cancellationToken = default)
    {
        var client = await CreateClientAsync(cancellationToken);

        var response = await ExecuteAsync(
            () => client.GitHubApps.Installation.GetAllRepositoriesForCurrent(),
            "list installation repositories");

        var repository = response.Repositories
            .FirstOrDefault(r => r.Id.ToString() == githubRepoId);

        if (repository == null)
        {
            throw new Exceptions.NotFoundException(
                $"GitHub repository '{githubRepoId}' was not found.");
        }

        var branches = await ExecuteAsync(
            () => client.Repository.Branch.GetAll(
                repository.Owner.Login,
                repository.Name),
            $"list branches for GitHub repository {githubRepoId}");

        return branches.Select(branch => new BranchDto
        {
            Name = branch.Name,
            CommitSha = branch.Commit?.Sha ?? string.Empty,
            IsProtected = branch.Protected,
            IsDefault = string.Equals(
                branch.Name,
                repository.DefaultBranch,
                StringComparison.Ordinal)
        }).ToList();
    }

    public async Task<IReadOnlyList<PullRequestDto>> GetPullRequestsAsync(
     string githubRepoId,
     string state = "open",
     CancellationToken cancellationToken = default)
    {
        var client = await CreateClientAsync(cancellationToken);

        var response = await ExecuteAsync(
            () => client.GitHubApps.Installation.GetAllRepositoriesForCurrent(),
            "list installation repositories");

        var repository = response.Repositories
            .FirstOrDefault(r => r.Id.ToString() == githubRepoId);

        if (repository == null)
        {
            throw new Exceptions.NotFoundException(
                $"GitHub repository '{githubRepoId}' was not found.");
        }

        var request = new PullRequestRequest
        {
            State = ParseState(state)
        };

        var pullRequests = await ExecuteAsync(
            () => client.PullRequest.GetAllForRepository(
                repository.Owner.Login,
                repository.Name,
                request),
            $"list pull requests for GitHub repository {githubRepoId}");

        return pullRequests.Select(MapToDto).ToList();
    }

    public async Task<PullRequestDto> GetPullRequestAsync(
    string githubRepoId,
    int number,
    CancellationToken cancellationToken = default)
    {
        var client = await CreateClientAsync(cancellationToken);

        var response = await ExecuteAsync(
            () => client.GitHubApps.Installation.GetAllRepositoriesForCurrent(),
            "list installation repositories");

        var repository = response.Repositories
            .FirstOrDefault(r => r.Id.ToString() == githubRepoId);

        if (repository == null)
        {
            throw new Exceptions.NotFoundException(
                $"GitHub repository '{githubRepoId}' was not found.");
        }

        var pullRequest = await ExecuteAsync(
            () => client.PullRequest.Get(
                repository.Owner.Login,
                repository.Name,
                number),
            $"get pull request #{number} for GitHub repository {githubRepoId}");

        return MapToDto(pullRequest);
    }

    public async Task<IReadOnlyList<PullRequestFileDto>> GetPullRequestFilesAsync(
    string githubRepoId,
    int number,
    CancellationToken cancellationToken = default)
    {
        var client = await CreateClientAsync(cancellationToken);

        var response = await ExecuteAsync(
            () => client.GitHubApps.Installation.GetAllRepositoriesForCurrent(),
            "list installation repositories");

        var repository = response.Repositories
            .FirstOrDefault(r => r.Id.ToString() == githubRepoId);

        if (repository == null)
        {
            throw new Exceptions.NotFoundException(
                $"GitHub repository '{githubRepoId}' was not found.");
        }

        var files = await ExecuteAsync(
            () => client.PullRequest.Files(
                repository.Owner.Login,
                repository.Name,
                number),
            $"list files for pull request #{number} in GitHub repository {githubRepoId}");

        return files.Select(file => new PullRequestFileDto
        {
            FileName = file.FileName,
            Status = file.Status,
            Additions = file.Additions,
            Deletions = file.Deletions,
            Changes = file.Changes,
            Patch = file.Patch,
            ContentsUrl = file.ContentsUrl
        }).ToList();
    }

    public Task<PullRequestDto> CreateImprovementPullRequestAsync(
        Guid repositoryId,
        string branchName,
        string baseBranch,
        IReadOnlyDictionary<string, string> fileContents,
        string commitMessage,
        string title,
        string body,
        CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Improvement pull request delivery is implemented in Step 6.");

    private async Task<Models.Repository> GetTrackedRepositoryAsync(Guid repositoryId, CancellationToken cancellationToken)
        => await _dbContext.Repositories.FirstOrDefaultAsync(r => r.Id == repositoryId, cancellationToken)
            ?? throw new Exceptions.NotFoundException($"Repository '{repositoryId}' was not found.");

    /// <summary>Derives a stable GUID from a GitHub repository id so callers get a consistent identifier without a database.</summary>
    private static Guid DeriveRepositoryId(string gitHubRepoId)
    {
        var hash = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(gitHubRepoId));
        return new Guid(hash);
    }

    private async Task<GitHubClient> CreateClientAsync(CancellationToken cancellationToken)
    {
        var token = await _tokenProvider.GetInstallationTokenAsync(cancellationToken);

        return new GitHubClient(new ProductHeaderValue(_settings.UserAgent))
        {
            Credentials = new Credentials(token)
        };
    }

    /// <summary>
    /// Runs a GitHub call and converts Octokit failures into safe API errors.
    /// GitHub response bodies are not surfaced, so tokens cannot leak.
    /// </summary>
    private async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string description)
    {
        try
        {
            return await operation();
        }
        catch (Octokit.NotFoundException ex)
        {
            _logger.LogWarning(ex, "GitHub API call failed: {Description} returned not found", description);
            throw new Exceptions.NotFoundException("The requested GitHub resource was not found.");
        }
        catch (AuthorizationException ex)
        {
            _logger.LogError(ex, "GitHub API call failed: {Description} was not authorised", description);
            throw new GitHubApiException("CodeRefine is not authorised to access this GitHub resource.");
        }
        catch (RateLimitExceededException ex)
        {
            _logger.LogError(ex, "GitHub API call failed: {Description} exceeded the rate limit", description);
            throw new GitHubApiException("The GitHub API rate limit has been exceeded. Try again later.");
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "GitHub API call failed: {Description} returned status {StatusCode}",
                description, (int)ex.StatusCode);
            throw new GitHubApiException("The GitHub API request failed.");
        }
    }

    private static ItemStateFilter ParseState(string state) => state?.ToLowerInvariant() switch
    {
        "closed" => ItemStateFilter.Closed,
        "all" => ItemStateFilter.All,
        _ => ItemStateFilter.Open
    };

    private static RepositoryDto MapToDto(Models.Repository repository) => new()
    {
        Id = repository.Id,
        GitHubRepoId = repository.GitHubRepoId,
        Owner = repository.Owner,
        Name = repository.Name,
        FullName = repository.FullName,
        DefaultBranch = repository.DefaultBranch,
        IsPrivate = repository.IsPrivate,
        CreatedAt = repository.CreatedAt
    };

    private static PullRequestDto MapToDto(Octokit.PullRequest pullRequest) => new()
    {
        Number = pullRequest.Number,
        Title = pullRequest.Title,
        Body = pullRequest.Body,
        State = pullRequest.State.StringValue,
        AuthorLogin = pullRequest.User?.Login,
        HeadBranch = pullRequest.Head?.Ref ?? string.Empty,
        BaseBranch = pullRequest.Base?.Ref ?? string.Empty,
        HeadSha = pullRequest.Head?.Sha ?? string.Empty,
        HtmlUrl = pullRequest.HtmlUrl,
        ChangedFiles = pullRequest.ChangedFiles,
        Additions = pullRequest.Additions,
        Deletions = pullRequest.Deletions,
        CreatedAt = pullRequest.CreatedAt.UtcDateTime,
        UpdatedAt = pullRequest.UpdatedAt.UtcDateTime
    };
}

using CodeRefine.Api.Data;
using CodeRefine.Api.DTOs.Analysis;
using CodeRefine.Api.DTOs.GitHub;
using CodeRefine.Api.DTOs.Review;
using CodeRefine.Api.Enums;
using CodeRefine.Api.Exceptions;
using CodeRefine.Api.Models;
using CodeRefine.Api.Services.GitHub;
using Microsoft.EntityFrameworkCore;

namespace CodeRefine.Api.Services.Analysis;

public class AnalysisService : IAnalysisService
{
    private readonly AppDbContext _dbContext;
    private readonly IGitHubService _gitHubService;
    private readonly ILogger<AnalysisService> _logger;

    public AnalysisService(
        AppDbContext dbContext,
        IGitHubService gitHubService,
        ILogger<AnalysisService> logger)
    {
        _dbContext = dbContext;
        _gitHubService = gitHubService;
        _logger = logger;
    }

    public async Task<AnalysisResponse> StartAnalysisAsync(StartAnalysisRequest request, CancellationToken cancellationToken = default)
    {
        if (request.PullRequestNumber is null && string.IsNullOrWhiteSpace(request.Branch))
        {
            throw new ValidationException("Either a pull request number or a branch must be supplied.");
        }

        var repository = await _dbContext.Repositories
            .FirstOrDefaultAsync(r => r.Id == request.RepositoryId, cancellationToken)
            ?? throw new NotFoundException($"Repository '{request.RepositoryId}' was not found.");

        var run = new AnalysisRun
        {
            RepositoryId = repository.Id,
            PullRequestNumber = request.PullRequestNumber,
            SourceBranch = request.Branch,
            TargetBranch = repository.DefaultBranch,
            Status = AnalysisStatus.Queued
        };

        if (request.PullRequestNumber is int prNumber)
        {
            var pullRequest = await _gitHubService.GetPullRequestAsync(repository.Id, prNumber, cancellationToken);
            run.SourceBranch = pullRequest.HeadBranch;
            run.TargetBranch = pullRequest.BaseBranch;
            run.CommitSha = pullRequest.HeadSha;
        }

        _dbContext.AnalysisRuns.Add(run);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Analysis {AnalysisId} started for repository {RepositoryId} (PR {PullRequestNumber}, branch {SourceBranch})",
            run.Id, repository.Id, run.PullRequestNumber, run.SourceBranch);

        return MapToResponse(run, repository);
    }

    public async Task<AnalysisResponse> GetAnalysisAsync(Guid analysisRunId, CancellationToken cancellationToken = default)
    {
        var run = await _dbContext.AnalysisRuns
            .Include(r => r.Repository)
            .Include(r => r.Findings).ThenInclude(f => f.Patches)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == analysisRunId, cancellationToken)
            ?? throw new NotFoundException($"Analysis '{analysisRunId}' was not found.");

        return MapToResponse(run, run.Repository, includeFindings: true);
    }

    public async Task<IReadOnlyList<FindingDto>> GetFindingsAsync(Guid analysisRunId, CancellationToken cancellationToken = default)
    {
        await EnsureAnalysisExistsAsync(analysisRunId, cancellationToken);

        var findings = await _dbContext.Findings
            .Include(f => f.Patches)
            .Where(f => f.AnalysisRunId == analysisRunId)
            .OrderByDescending(f => f.Severity)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return findings.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<PatchDto>> GetPatchesAsync(Guid analysisRunId, CancellationToken cancellationToken = default)
    {
        await EnsureAnalysisExistsAsync(analysisRunId, cancellationToken);

        var patches = await _dbContext.Patches
            .Where(p => p.Finding!.AnalysisRunId == analysisRunId)
            .OrderBy(p => p.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return patches.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<VerificationDto>> GetVerificationAsync(Guid analysisRunId, CancellationToken cancellationToken = default)
    {
        await EnsureAnalysisExistsAsync(analysisRunId, cancellationToken);

        var results = await _dbContext.VerificationResults
            .Where(v => v.AnalysisRunId == analysisRunId)
            .OrderBy(v => v.CheckType)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return results.Select(MapToDto).ToList();
    }

    public Task<AnalysisResponse> ApproveAsync(Guid analysisRunId, ReviewRequest request, CancellationToken cancellationToken = default)
        => RecordDecisionAsync(analysisRunId, request, ReviewDecision.Approved, cancellationToken);

    public Task<AnalysisResponse> RejectAsync(Guid analysisRunId, ReviewRequest request, CancellationToken cancellationToken = default)
        => RecordDecisionAsync(analysisRunId, request, ReviewDecision.Rejected, cancellationToken);

    public Task<PullRequestDto> CreateImprovementPullRequestAsync(Guid analysisRunId, ImprovementPrRequest request, CancellationToken cancellationToken = default)
    {
        // Delivery is implemented in Step 6, once Developer 3 confirms how the
        // verified file contents are exposed to this backend.
        throw new NotImplementedException("Improvement pull request delivery is implemented in Step 6.");
    }

    private async Task<AnalysisResponse> RecordDecisionAsync(
        Guid analysisRunId,
        ReviewRequest request,
        ReviewDecision decision,
        CancellationToken cancellationToken)
    {
        var run = await _dbContext.AnalysisRuns
            .Include(r => r.Repository)
            .FirstOrDefaultAsync(r => r.Id == analysisRunId, cancellationToken)
            ?? throw new NotFoundException($"Analysis '{analysisRunId}' was not found.");

        if (run.Status != AnalysisStatus.ReadyForReview)
        {
            throw new InvalidStateException($"Analysis is '{run.Status}' and cannot be reviewed. Expected 'ReadyForReview'.");
        }

        await ValidateReviewTargetsAsync(analysisRunId, request, cancellationToken);

        var reviews = BuildReviews(analysisRunId, request, decision);
        _dbContext.Reviews.AddRange(reviews);

        run.Status = decision == ReviewDecision.Approved ? AnalysisStatus.Approved : AnalysisStatus.Rejected;

        if (decision == ReviewDecision.Rejected)
        {
            run.CompletedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Human {Decision} recorded for analysis {AnalysisId} ({ReviewCount} review records)",
            decision, analysisRunId, reviews.Count);

        return MapToResponse(run, run.Repository);
    }

    private async Task ValidateReviewTargetsAsync(Guid analysisRunId, ReviewRequest request, CancellationToken cancellationToken)
    {
        if (request.FindingIds.Count > 0)
        {
            var validFindings = await _dbContext.Findings
                .CountAsync(f => f.AnalysisRunId == analysisRunId && request.FindingIds.Contains(f.Id), cancellationToken);

            if (validFindings != request.FindingIds.Count)
            {
                throw new ValidationException("One or more findings do not belong to this analysis.");
            }
        }

        if (request.PatchIds.Count > 0)
        {
            var validPatches = await _dbContext.Patches
                .CountAsync(p => p.Finding!.AnalysisRunId == analysisRunId && request.PatchIds.Contains(p.Id), cancellationToken);

            if (validPatches != request.PatchIds.Count)
            {
                throw new ValidationException("One or more patches do not belong to this analysis.");
            }
        }
    }

    private static List<Review> BuildReviews(Guid analysisRunId, ReviewRequest request, ReviewDecision decision)
    {
        var reviews = new List<Review>();

        foreach (var patchId in request.PatchIds)
        {
            reviews.Add(new Review
            {
                AnalysisRunId = analysisRunId,
                PatchId = patchId,
                Decision = decision,
                Comment = request.Comment
            });
        }

        foreach (var findingId in request.FindingIds)
        {
            reviews.Add(new Review
            {
                AnalysisRunId = analysisRunId,
                FindingId = findingId,
                Decision = decision,
                Comment = request.Comment
            });
        }

        if (reviews.Count == 0)
        {
            reviews.Add(new Review
            {
                AnalysisRunId = analysisRunId,
                Decision = decision,
                Comment = request.Comment
            });
        }

        return reviews;
    }

    private async Task EnsureAnalysisExistsAsync(Guid analysisRunId, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.AnalysisRuns.AnyAsync(r => r.Id == analysisRunId, cancellationToken);

        if (!exists)
        {
            throw new NotFoundException($"Analysis '{analysisRunId}' was not found.");
        }
    }

    private static AnalysisResponse MapToResponse(AnalysisRun run, Models.Repository? repository, bool includeFindings = false) => new()
    {
        Id = run.Id,
        RepositoryId = run.RepositoryId,
        RepositoryFullName = repository?.FullName ?? string.Empty,
        PullRequestNumber = run.PullRequestNumber,
        SourceBranch = run.SourceBranch,
        TargetBranch = run.TargetBranch,
        CommitSha = run.CommitSha,
        Status = run.Status,
        ErrorMessage = run.ErrorMessage,
        QualityScoreBefore = run.QualityScoreBefore,
        QualityScoreAfter = run.QualityScoreAfter,
        ImprovementBranch = run.ImprovementBranch,
        ImprovementPrNumber = run.ImprovementPrNumber,
        ImprovementPrUrl = run.ImprovementPrUrl,
        StartedAt = run.StartedAt,
        CompletedAt = run.CompletedAt,
        FindingCount = run.Findings.Count,
        Findings = includeFindings ? run.Findings.Select(MapToDto).ToList() : new List<FindingDto>()
    };

    private static FindingDto MapToDto(Finding finding) => new()
    {
        Id = finding.Id,
        AgentType = finding.AgentType,
        FilePath = finding.FilePath,
        StartLine = finding.StartLine,
        EndLine = finding.EndLine,
        Category = finding.Category,
        Severity = finding.Severity,
        Title = finding.Title,
        Description = finding.Description,
        Recommendation = finding.Recommendation,
        Confidence = finding.Confidence,
        Status = finding.Status,
        CreatedAt = finding.CreatedAt,
        Patches = finding.Patches.Select(MapToDto).ToList()
    };

    private static PatchDto MapToDto(Patch patch) => new()
    {
        Id = patch.Id,
        FindingId = patch.FindingId,
        FilePath = patch.FilePath,
        OriginalCode = patch.OriginalCode,
        ProposedCode = patch.ProposedCode,
        Diff = patch.Diff,
        Explanation = patch.Explanation,
        Status = patch.Status,
        CreatedAt = patch.CreatedAt
    };

    private static VerificationDto MapToDto(VerificationResult result) => new()
    {
        Id = result.Id,
        AnalysisRunId = result.AnalysisRunId,
        CheckType = result.CheckType,
        Status = result.Status,
        Output = result.Output,
        DurationMs = result.DurationMs,
        CreatedAt = result.CreatedAt
    };
}

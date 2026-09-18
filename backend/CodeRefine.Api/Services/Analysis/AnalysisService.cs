using CodeRefine.Api.DTOs.AiService;
using CodeRefine.Api.DTOs.Analysis;
using CodeRefine.Api.DTOs.GitHub;
using CodeRefine.Api.DTOs.Review;
using CodeRefine.Api.Enums;
using CodeRefine.Api.Exceptions;
using CodeRefine.Api.Models;
using CodeRefine.Api.Services.AI;
using CodeRefine.Api.Services.GitHub;

namespace CodeRefine.Api.Services.Analysis;

public class AnalysisService : IAnalysisService
{
    private readonly IAnalysisRunStore _runStore;
    private readonly IGitHubService _gitHubService;
    private readonly IGitHubAppTokenProvider _gitHubAppTokenProvider;
    private readonly IAiService _aiService;
    private readonly ILogger<AnalysisService> _logger;

    public AnalysisService(
        IAnalysisRunStore runStore,
        IGitHubService gitHubService,
        IGitHubAppTokenProvider gitHubAppTokenProvider,
        IAiService aiService,
        ILogger<AnalysisService> logger)
    {
        _runStore = runStore;
        _gitHubService = gitHubService;
        _gitHubAppTokenProvider = gitHubAppTokenProvider;
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<AnalysisResponse> StartAnalysisAsync(
     StartAnalysisRequest request,
     CancellationToken cancellationToken = default)
    {
        if (request.PullRequestNumber is null &&
            string.IsNullOrWhiteSpace(request.Branch))
        {
            throw new ValidationException(
                "Either a pull request number or a branch must be supplied.");
        }

        // Repositories are resolved live from the GitHub App installation list;
        // nothing about an analysis run touches Postgres.
        var repositories = await _gitHubService.GetRepositoriesAsync(cancellationToken);
        var repositoryDto = repositories.FirstOrDefault(r => r.Id == request.RepositoryId)
            ?? throw new NotFoundException($"Repository '{request.RepositoryId}' was not found.");

        var repository = new Models.Repository
        {
            Id = repositoryDto.Id,
            GitHubRepoId = repositoryDto.GitHubRepoId,
            Owner = repositoryDto.Owner,
            Name = repositoryDto.Name,
            DefaultBranch = repositoryDto.DefaultBranch,
            IsPrivate = repositoryDto.IsPrivate
        };

        var run = new AnalysisRun
        {
            RepositoryId = repository.Id,
            Repository = repository,
            PullRequestNumber = request.PullRequestNumber,
            SourceBranch = request.Branch,
            TargetBranch = repository.DefaultBranch,
            Status = AnalysisStatus.Queued
        };

        if (request.PullRequestNumber is int prNumber)
        {
            var pullRequest = await _gitHubService.GetPullRequestAsync(
                repository.GitHubRepoId,
                prNumber,
                cancellationToken);

            run.SourceBranch = pullRequest.HeadBranch;
            run.TargetBranch = pullRequest.BaseBranch;
            run.CommitSha = pullRequest.HeadSha;
        }

        _runStore.Save(run);

        _logger.LogInformation(
            "Analysis {AnalysisId} started for repository {RepositoryId} (PR {PullRequestNumber}, branch {SourceBranch})",
            run.Id,
            repository.Id,
            run.PullRequestNumber,
            run.SourceBranch);

        await RunWorkflowAsync(run, repository, cancellationToken);

        return MapToResponse(run, repository, includeFindings: true);
    }

    /// <summary>
    /// Synchronously drives the engine: create a workspace, run the full
    /// LangGraph workflow, then persist findings/patches/verification results.
    /// Failures are recorded on the run rather than thrown, so the caller
    /// always gets back the (possibly Failed) analysis.
    /// </summary>
    private async Task RunWorkflowAsync(AnalysisRun run, Models.Repository repository, CancellationToken cancellationToken)
    {
        run.Status = AnalysisStatus.Analyzing;
        _runStore.Save(run);

        try
        {
            var repoUrl = await BuildCloneUrlAsync(repository, cancellationToken);

            var workspace = await _aiService.CreateWorkspaceAsync(new WorkspaceRequest
            {
                RepoUrl = repoUrl,
                Branch = run.PullRequestNumber is null ? run.SourceBranch : null,
                PrNumber = run.PullRequestNumber,
                BaseBranch = run.TargetBranch
            }, cancellationToken);

            var workflowResult = await _aiService.RunWorkflowAsync(new WorkflowRequest
            {
                AnalysisId = run.Id.ToString(),
                RepoPath = workspace.RepoPath,
                ChangedFiles = workspace.ChangedFiles,
                Language = "python",
                RepoUrl = repoUrl,
                PrNumber = run.PullRequestNumber,
                BaseBranch = run.TargetBranch
            }, cancellationToken);

            ApplyWorkflowResult(run, workflowResult);

            // The engine always pushes an improvement branch and opens a PR for
            // human review on GitHub itself once verification finishes, so a
            // populated PR URL means the run has reached its terminal state.
            run.Status = string.IsNullOrWhiteSpace(run.ImprovementPrUrl)
                ? AnalysisStatus.ReadyForReview
                : AnalysisStatus.Completed;

            if (run.Status == AnalysisStatus.Completed)
            {
                run.CompletedAt = DateTime.UtcNow;
            }
        }
        catch (AiServiceException ex)
        {
            _logger.LogError(ex, "Analysis {AnalysisId} failed while calling the analysis engine", run.Id);
            run.Status = AnalysisStatus.Failed;
            run.ErrorMessage = ex.Message;
            run.CompletedAt = DateTime.UtcNow;
        }

        _runStore.Save(run);
    }

    /// <summary>Builds an HTTPS clone URL, embedding a GitHub App installation token for private repos.</summary>
    private async Task<string> BuildCloneUrlAsync(Models.Repository repository, CancellationToken cancellationToken)
    {
        if (!repository.IsPrivate)
        {
            return $"https://github.com/{repository.Owner}/{repository.Name}.git";
        }

        var token = await _gitHubAppTokenProvider.GetInstallationTokenAsync(cancellationToken);
        return $"https://x-access-token:{token}@github.com/{repository.Owner}/{repository.Name}.git";
    }

    /// <summary>Maps the engine's workflow output onto Finding/Patch/VerificationResult rows for this run.</summary>
    private static void ApplyWorkflowResult(AnalysisRun run, WorkflowResponse workflowResult)
    {
        run.QualityScoreBefore = workflowResult.QualityScore;
        run.ImprovementBranch = workflowResult.ImprovementBranch;
        run.ImprovementPrUrl = workflowResult.ImprovementPrUrl;
        run.ImprovementPrNumber = ParsePrNumberFromUrl(workflowResult.ImprovementPrUrl);

        var findingsByFile = new Dictionary<string, List<Finding>>(StringComparer.OrdinalIgnoreCase);

        foreach (var dto in workflowResult.Findings)
        {
            var filePath = dto.FilePath ?? dto.File;

            var finding = new Finding
            {
                Id = Guid.NewGuid(),
                AnalysisRunId = run.Id,
                AgentType = dto.AgentType,
                FilePath = filePath,
                StartLine = dto.StartLine,
                EndLine = dto.EndLine,
                Category = dto.Category,
                Severity = ParseEnum(dto.Severity, FindingSeverity.Low),
                Title = dto.Title,
                Description = dto.Description,
                Recommendation = dto.Recommendation,
                Confidence = dto.Confidence,
                Status = FindingStatus.Open,
                CreatedAt = DateTime.UtcNow
            };

            run.Findings.Add(finding);

            if (!findingsByFile.TryGetValue(filePath, out var list))
            {
                findingsByFile[filePath] = list = new List<Finding>();
            }

            list.Add(finding);
        }

        // The engine generates one patch per file (covering every finding in
        // that file), while the backend schema requires a FindingId per patch.
        // Fan the patch out to every finding detected in the same file.
        var verdict = workflowResult.VerificationResult?.Verdict;
        var patchStatus = verdict switch
        {
            "passed" => PatchStatus.Verified,
            "failed" => PatchStatus.VerificationFailed,
            _ => PatchStatus.Proposed
        };

        foreach (var patchDto in workflowResult.GeneratedPatches)
        {
            // One entry per file — this is exactly what ends up in the improvement PR.
            run.FinalCodeFiles.Add(new FinalCodeFile
            {
                FilePath = patchDto.FilePath,
                Code = patchDto.ProposedCode
            });

            if (!findingsByFile.TryGetValue(patchDto.FilePath, out var findingsForFile))
            {
                continue;
            }

            foreach (var finding in findingsForFile)
            {
                finding.Status = FindingStatus.PatchProposed;

                finding.Patches.Add(new Patch
                {
                    Id = Guid.NewGuid(),
                    FindingId = finding.Id,
                    FilePath = patchDto.FilePath,
                    OriginalCode = patchDto.OriginalCode,
                    ProposedCode = patchDto.ProposedCode,
                    Diff = patchDto.Diff,
                    Explanation = patchDto.Explanation,
                    Status = patchStatus,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        if (workflowResult.VerificationResult is not null)
        {
            foreach (var check in workflowResult.VerificationResult.Checks)
            {
                if (!TryParseCheckType(check.CheckType, out var checkType))
                {
                    continue;
                }

                run.VerificationResults.Add(new VerificationResult
                {
                    Id = Guid.NewGuid(),
                    AnalysisRunId = run.Id,
                    CheckType = checkType,
                    Status = ParseEnum(check.Status, Enums.VerificationStatus.Error),
                    Output = check.Output,
                    DurationMs = check.DurationMs,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
    }

    private static bool TryParseCheckType(string checkType, out VerificationCheckType result)
        => Enum.TryParse(ToPascalCase(checkType), ignoreCase: true, out result);

    /// <summary>Extracts the trailing PR number from a GitHub PR URL, e.g. ".../pull/13".</summary>
    private static int? ParsePrNumberFromUrl(string? prUrl)
    {
        if (string.IsNullOrWhiteSpace(prUrl))
        {
            return null;
        }

        var lastSegment = prUrl.TrimEnd('/').Split('/').LastOrDefault();
        return int.TryParse(lastSegment, out var number) ? number : null;
    }

    private static TEnum ParseEnum<TEnum>(string value, TEnum fallback) where TEnum : struct, Enum
        => Enum.TryParse<TEnum>(ToPascalCase(value), ignoreCase: true, out var parsed) ? parsed : fallback;

    private static string ToPascalCase(string snakeCaseValue)
    {
        if (string.IsNullOrWhiteSpace(snakeCaseValue))
        {
            return string.Empty;
        }

        var parts = snakeCaseValue.Split('_', StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(parts.Select(p => char.ToUpperInvariant(p[0]) + p[1..].ToLowerInvariant()));
    }

    public Task<AnalysisResponse> GetAnalysisAsync(Guid analysisRunId, CancellationToken cancellationToken = default)
    {
        var run = FindRunOrThrow(analysisRunId);
        return Task.FromResult(MapToResponse(run, run.Repository, includeFindings: true));
    }

    public Task<IReadOnlyList<FindingDto>> GetFindingsAsync(Guid analysisRunId, CancellationToken cancellationToken = default)
    {
        var run = FindRunOrThrow(analysisRunId);
        IReadOnlyList<FindingDto> findings = run.Findings
            .OrderByDescending(f => f.Severity)
            .Select(MapToDto)
            .ToList();

        return Task.FromResult(findings);
    }

    public Task<IReadOnlyList<PatchDto>> GetPatchesAsync(Guid analysisRunId, CancellationToken cancellationToken = default)
    {
        var run = FindRunOrThrow(analysisRunId);
        IReadOnlyList<PatchDto> patches = run.Findings
            .SelectMany(f => f.Patches)
            .OrderBy(p => p.CreatedAt)
            .Select(MapToDto)
            .ToList();

        return Task.FromResult(patches);
    }

    public Task<IReadOnlyList<VerificationDto>> GetVerificationAsync(Guid analysisRunId, CancellationToken cancellationToken = default)
    {
        var run = FindRunOrThrow(analysisRunId);
        IReadOnlyList<VerificationDto> results = run.VerificationResults
            .OrderBy(v => v.CheckType)
            .Select(MapToDto)
            .ToList();

        return Task.FromResult(results);
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

    private Task<AnalysisResponse> RecordDecisionAsync(
        Guid analysisRunId,
        ReviewRequest request,
        ReviewDecision decision,
        CancellationToken cancellationToken)
    {
        var run = FindRunOrThrow(analysisRunId);

        if (run.Status != AnalysisStatus.ReadyForReview)
        {
            throw new InvalidStateException($"Analysis is '{run.Status}' and cannot be reviewed. Expected 'ReadyForReview'.");
        }

        ValidateReviewTargets(run, request);

        var reviews = BuildReviews(analysisRunId, request, decision);
        foreach (var review in reviews)
        {
            run.Reviews.Add(review);
        }

        run.Status = decision == ReviewDecision.Approved ? AnalysisStatus.Approved : AnalysisStatus.Rejected;

        if (decision == ReviewDecision.Rejected)
        {
            run.CompletedAt = DateTime.UtcNow;
        }

        _runStore.Save(run);

        _logger.LogInformation("Human {Decision} recorded for analysis {AnalysisId} ({ReviewCount} review records)",
            decision, analysisRunId, reviews.Count);

        return Task.FromResult(MapToResponse(run, run.Repository));
    }

    private static void ValidateReviewTargets(AnalysisRun run, ReviewRequest request)
    {
        if (request.FindingIds.Count > 0)
        {
            var validFindings = run.Findings.Count(f => request.FindingIds.Contains(f.Id));

            if (validFindings != request.FindingIds.Count)
            {
                throw new ValidationException("One or more findings do not belong to this analysis.");
            }
        }

        if (request.PatchIds.Count > 0)
        {
            var validPatches = run.Findings.SelectMany(f => f.Patches).Count(p => request.PatchIds.Contains(p.Id));

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

    private AnalysisRun FindRunOrThrow(Guid analysisRunId) =>
        _runStore.Find(analysisRunId) ?? throw new NotFoundException($"Analysis '{analysisRunId}' was not found.");

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
        Findings = includeFindings ? run.Findings.Select(MapToDto).ToList() : new List<FindingDto>(),
        FinalCode = includeFindings
            ? run.FinalCodeFiles.Select(f => new FinalCodeFileDto { FilePath = f.FilePath, Code = f.Code }).ToList()
            : new List<FinalCodeFileDto>()
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

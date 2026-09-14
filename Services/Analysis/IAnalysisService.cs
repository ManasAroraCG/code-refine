using CodeRefine.Api.DTOs.Analysis;
using CodeRefine.Api.DTOs.GitHub;
using CodeRefine.Api.DTOs.Review;

namespace CodeRefine.Api.Services.Analysis;

public interface IAnalysisService
{
    Task<AnalysisResponse> StartAnalysisAsync(StartAnalysisRequest request, CancellationToken cancellationToken = default);

    Task<AnalysisResponse> GetAnalysisAsync(Guid analysisRunId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FindingDto>> GetFindingsAsync(Guid analysisRunId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PatchDto>> GetPatchesAsync(Guid analysisRunId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VerificationDto>> GetVerificationAsync(Guid analysisRunId, CancellationToken cancellationToken = default);

    Task<AnalysisResponse> ApproveAsync(Guid analysisRunId, ReviewRequest request, CancellationToken cancellationToken = default);

    Task<AnalysisResponse> RejectAsync(Guid analysisRunId, ReviewRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delivers approved and verified patches to GitHub. Enforces the approval
    /// gate before any write operation.
    /// </summary>
    Task<PullRequestDto> CreateImprovementPullRequestAsync(Guid analysisRunId, ImprovementPrRequest request, CancellationToken cancellationToken = default);
}

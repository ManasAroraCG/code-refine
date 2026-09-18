using CodeRefine.Api.Enums;

namespace CodeRefine.Api.DTOs.Analysis;

public class AnalysisResponse
{
    public Guid Id { get; set; }
    public Guid RepositoryId { get; set; }
    public string RepositoryFullName { get; set; } = string.Empty;
    public int? PullRequestNumber { get; set; }
    public string? SourceBranch { get; set; }
    public string? TargetBranch { get; set; }
    public string? CommitSha { get; set; }
    public AnalysisStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public double? QualityScoreBefore { get; set; }
    public double? QualityScoreAfter { get; set; }
    public string? ImprovementBranch { get; set; }
    public int? ImprovementPrNumber { get; set; }
    public string? ImprovementPrUrl { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int FindingCount { get; set; }
    public IReadOnlyList<FindingDto> Findings { get; set; } = new List<FindingDto>();

    /// <summary>Final per-file code going into the improvement PR, for human review.</summary>
    public IReadOnlyList<FinalCodeFileDto> FinalCode { get; set; } = new List<FinalCodeFileDto>();
}

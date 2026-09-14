using CodeRefine.Api.Enums;

namespace CodeRefine.Api.Models;

public class AnalysisRun
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RepositoryId { get; set; }
    public Repository? Repository { get; set; }

    public int? PullRequestNumber { get; set; }
    public string? SourceBranch { get; set; }
    public string? TargetBranch { get; set; }
    public string? CommitSha { get; set; }

    public AnalysisStatus Status { get; set; } = AnalysisStatus.Queued;
    public string? ErrorMessage { get; set; }

    public double? QualityScoreBefore { get; set; }
    public double? QualityScoreAfter { get; set; }

    /// <summary>Branch created by the backend when delivering approved improvements.</summary>
    public string? ImprovementBranch { get; set; }

    /// <summary>Number of the improvement pull request opened on GitHub, when created.</summary>
    public int? ImprovementPrNumber { get; set; }

    public string? ImprovementPrUrl { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public ICollection<Finding> Findings { get; set; } = new List<Finding>();
    public ICollection<VerificationResult> VerificationResults { get; set; } = new List<VerificationResult>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}

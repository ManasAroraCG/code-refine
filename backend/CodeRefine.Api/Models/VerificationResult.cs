using CodeRefine.Api.Enums;

namespace CodeRefine.Api.Models;

/// <summary>
/// One sandbox check executed for an analysis run. A run has many results,
/// one per check type.
/// </summary>
public class VerificationResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AnalysisRunId { get; set; }
    public AnalysisRun? AnalysisRun { get; set; }

    public VerificationCheckType CheckType { get; set; }
    public VerificationStatus Status { get; set; } = VerificationStatus.Pending;

    /// <summary>Sandbox output. Truncated by the sandbox; never contains credentials.</summary>
    public string? Output { get; set; }

    public long DurationMs { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

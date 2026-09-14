using CodeRefine.Api.Enums;

namespace CodeRefine.Api.Models;

public class Review
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid AnalysisRunId { get; set; }
    public AnalysisRun? AnalysisRun { get; set; }

    /// <summary>Set when the decision targets a single finding rather than the whole run.</summary>
    public Guid? FindingId { get; set; }
    public Finding? Finding { get; set; }

    /// <summary>Set when the decision targets a specific proposed patch.</summary>
    public Guid? PatchId { get; set; }
    public Patch? Patch { get; set; }

    public Guid? UserId { get; set; }
    public User? User { get; set; }

    public ReviewDecision Decision { get; set; } = ReviewDecision.Pending;
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

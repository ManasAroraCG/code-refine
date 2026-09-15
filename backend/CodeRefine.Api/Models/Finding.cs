using CodeRefine.Api.Enums;

namespace CodeRefine.Api.Models;

public class Finding
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AnalysisRunId { get; set; }
    public AnalysisRun? AnalysisRun { get; set; }

    /// <summary>Agent that produced the finding, e.g. redundancy, efficiency, dead_code, security.</summary>
    public string AgentType { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;
    public int StartLine { get; set; }
    public int EndLine { get; set; }

    public string? Category { get; set; }
    public FindingSeverity Severity { get; set; } = FindingSeverity.Info;

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Recommendation { get; set; }

    /// <summary>Agent confidence between 0 and 1.</summary>
    public double Confidence { get; set; }

    public FindingStatus Status { get; set; } = FindingStatus.Open;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Patch> Patches { get; set; } = new List<Patch>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}

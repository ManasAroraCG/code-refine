using CodeRefine.Api.Enums;

namespace CodeRefine.Api.DTOs.Analysis;

public class FindingDto
{
    public Guid Id { get; set; }
    public string AgentType { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int StartLine { get; set; }
    public int EndLine { get; set; }
    public string? Category { get; set; }
    public FindingSeverity Severity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Recommendation { get; set; }
    public double Confidence { get; set; }
    public FindingStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public IReadOnlyList<PatchDto> Patches { get; set; } = new List<PatchDto>();
}

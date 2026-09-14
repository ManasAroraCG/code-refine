using CodeRefine.Api.Enums;

namespace CodeRefine.Api.DTOs.Analysis;

public class VerificationDto
{
    public Guid Id { get; set; }
    public Guid AnalysisRunId { get; set; }
    public VerificationCheckType CheckType { get; set; }
    public VerificationStatus Status { get; set; }
    public string? Output { get; set; }
    public long DurationMs { get; set; }
    public DateTime CreatedAt { get; set; }
}

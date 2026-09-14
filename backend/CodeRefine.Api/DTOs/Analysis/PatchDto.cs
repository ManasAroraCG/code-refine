using CodeRefine.Api.Enums;

namespace CodeRefine.Api.DTOs.Analysis;

public class PatchDto
{
    public Guid Id { get; set; }
    public Guid FindingId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string OriginalCode { get; set; } = string.Empty;
    public string ProposedCode { get; set; } = string.Empty;
    public string Diff { get; set; } = string.Empty;
    public string? Explanation { get; set; }
    public PatchStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

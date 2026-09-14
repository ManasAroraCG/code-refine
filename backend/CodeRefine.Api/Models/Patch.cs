using CodeRefine.Api.Enums;

namespace CodeRefine.Api.Models;

public class Patch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FindingId { get; set; }
    public Finding? Finding { get; set; }

    public string FilePath { get; set; } = string.Empty;
    public string OriginalCode { get; set; } = string.Empty;
    public string ProposedCode { get; set; } = string.Empty;

    /// <summary>Unified diff produced by the AI service.</summary>
    public string Diff { get; set; } = string.Empty;

    public string? Explanation { get; set; }
    public PatchStatus Status { get; set; } = PatchStatus.Proposed;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}

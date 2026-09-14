using System.ComponentModel.DataAnnotations;

namespace CodeRefine.Api.DTOs.Review;

public class ReviewRequest
{
    /// <summary>Optional subset of findings the decision applies to. Empty means the whole run.</summary>
    public IReadOnlyList<Guid> FindingIds { get; set; } = new List<Guid>();

    /// <summary>Optional subset of patches the decision applies to. Empty means all verified patches.</summary>
    public IReadOnlyList<Guid> PatchIds { get; set; } = new List<Guid>();

    [MaxLength(2000)]
    public string? Comment { get; set; }
}

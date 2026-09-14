using System.ComponentModel.DataAnnotations;

namespace CodeRefine.Api.DTOs.Review;

public class ImprovementPrRequest
{
    /// <summary>Optional override for the generated pull request title.</summary>
    [MaxLength(300)]
    public string? Title { get; set; }

    /// <summary>Optional override for the generated pull request body.</summary>
    public string? Body { get; set; }

    /// <summary>Base branch for the improvement pull request. Defaults to the analysed branch.</summary>
    public string? BaseBranch { get; set; }
}

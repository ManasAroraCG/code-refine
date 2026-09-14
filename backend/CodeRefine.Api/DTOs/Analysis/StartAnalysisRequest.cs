using System.ComponentModel.DataAnnotations;

namespace CodeRefine.Api.DTOs.Analysis;

public class StartAnalysisRequest
{
    /// <summary>Internal CodeRefine repository identifier.</summary>
    [Required]
    public Guid RepositoryId { get; set; }

    /// <summary>Pull request to analyse. Required when no branch is supplied.</summary>
    public int? PullRequestNumber { get; set; }

    /// <summary>Branch to analyse when running outside a pull request.</summary>
    public string? Branch { get; set; }
}

namespace CodeRefine.Api.DTOs.GitHub;

public class PullRequestDto
{
    public int Number { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Body { get; set; }
    public string State { get; set; } = string.Empty;
    public string? AuthorLogin { get; set; }
    public string HeadBranch { get; set; } = string.Empty;
    public string BaseBranch { get; set; } = string.Empty;
    public string HeadSha { get; set; } = string.Empty;
    public string? HtmlUrl { get; set; }
    public int ChangedFiles { get; set; }
    public int Additions { get; set; }
    public int Deletions { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

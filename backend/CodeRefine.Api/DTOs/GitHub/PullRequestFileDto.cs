namespace CodeRefine.Api.DTOs.GitHub;

public class PullRequestFileDto
{
    public string FileName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Additions { get; set; }
    public int Deletions { get; set; }
    public int Changes { get; set; }
    public string? Patch { get; set; }
    public string? ContentsUrl { get; set; }
}

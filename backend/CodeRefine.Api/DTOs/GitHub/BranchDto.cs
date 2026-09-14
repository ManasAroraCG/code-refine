namespace CodeRefine.Api.DTOs.GitHub;

public class BranchDto
{
    public string Name { get; set; } = string.Empty;
    public string CommitSha { get; set; } = string.Empty;
    public bool IsProtected { get; set; }
    public bool IsDefault { get; set; }
}

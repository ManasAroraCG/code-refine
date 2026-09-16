namespace CodeRefine.Api.DTOs.GitHub;

public class RepositoryDto
{
    public Guid Id { get; set; }

    /// <summary>
    /// Repository ID assigned by GitHub.
    /// </summary>
    public string GitHubRepoId { get; set; } = string.Empty;

    public string Owner { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string DefaultBranch { get; set; } = "main";
    public bool IsPrivate { get; set; }
    public DateTime CreatedAt { get; set; }
}
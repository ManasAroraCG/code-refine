namespace CodeRefine.Api.Models;

public class Repository
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? UserId { get; set; }
    public User? User { get; set; }

    /// <summary>External GitHub repository identifier. Used only when calling the GitHub API.</summary>
    public string GitHubRepoId { get; set; } = string.Empty;

    public string Owner { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DefaultBranch { get; set; } = "main";

    /// <summary>GitHub App installation that grants this backend access to the repository.</summary>
    public long? InstallationId { get; set; }

    public bool IsPrivate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string FullName => $"{Owner}/{Name}";

    public ICollection<AnalysisRun> AnalysisRuns { get; set; } = new List<AnalysisRun>();
}

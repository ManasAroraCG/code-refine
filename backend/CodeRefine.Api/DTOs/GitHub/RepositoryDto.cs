namespace CodeRefine.Api.DTOs.GitHub;

public class RepositoryDto
{
    /// <summary>Internal CodeRefine identifier used by all API routes.</summary>
    public Guid Id { get; set; }

    public string Owner { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string DefaultBranch { get; set; } = "main";
    public bool IsPrivate { get; set; }
    public DateTime CreatedAt { get; set; }
}

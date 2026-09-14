namespace CodeRefine.Api.Models;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string GitHubLogin { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Repository> Repositories { get; set; } = new List<Repository>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();}

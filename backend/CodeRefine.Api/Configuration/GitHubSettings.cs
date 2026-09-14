namespace CodeRefine.Api.Configuration;

/// <summary>
/// GitHub App credentials. These values stay inside the ASP.NET backend and are
/// never forwarded to the frontend, the AI service or the sandbox.
/// </summary>
public class GitHubSettings
{
    public const string SectionName = "GitHub";

    public string ApiBaseUrl { get; set; } = "https://api.github.com";

    public string UserAgent { get; set; } = "CodeRefine";

    /// <summary>GitHub App identifier.</summary>
    public string AppId { get; set; } = string.Empty;

    /// <summary>PEM private key used to sign the App JWT.</summary>
    public string PrivateKey { get; set; } = string.Empty;

    /// <summary>Installation used for the demo repository.</summary>
    public long InstallationId { get; set; }

    /// <summary>Committer name used for improvement commits.</summary>
    public string CommitterName { get; set; } = "CodeRefine Bot";

    public string CommitterEmail { get; set; } = "bot@coderefine.local";
}

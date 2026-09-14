namespace CodeRefine.Api.Services.GitHub;

/// <summary>
/// Supplies short-lived GitHub App installation access tokens. Implementations
/// must keep credentials inside the backend and out of logs and responses.
/// </summary>
public interface IGitHubAppTokenProvider
{
    Task<string> GetInstallationTokenAsync(CancellationToken cancellationToken = default);
}

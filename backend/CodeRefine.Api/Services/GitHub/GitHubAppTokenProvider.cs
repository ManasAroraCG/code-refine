using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CodeRefine.Api.Configuration;
using CodeRefine.Api.Exceptions;
using Microsoft.Extensions.Options;

namespace CodeRefine.Api.Services.GitHub;

/// <summary>
/// Mints GitHub App installation access tokens.
///
/// Flow: sign a short-lived RS256 JWT with the App private key, exchange it for
/// an installation access token, then cache that token until shortly before it
/// expires. Tokens and the private key never leave this backend and are never
/// written to logs.
/// </summary>
public class GitHubAppTokenProvider : IGitHubAppTokenProvider
{
    /// <summary>Refresh this far ahead of expiry to avoid using a token mid-flight.</summary>
    private static readonly TimeSpan ExpiryBuffer = TimeSpan.FromMinutes(5);

    /// <summary>Name of the configured GitHub HTTP client.</summary>
    public const string HttpClientName = "github-app";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly GitHubSettings _settings;
    private readonly ILogger<GitHubAppTokenProvider> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private string? _cachedToken;
    private DateTimeOffset _cachedTokenExpiresAt = DateTimeOffset.MinValue;

    public GitHubAppTokenProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<GitHubSettings> settings,
        ILogger<GitHubAppTokenProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<string> GetInstallationTokenAsync(CancellationToken cancellationToken = default)
    {
        if (IsCachedTokenUsable())
        {
            return _cachedToken!;
        }

        await _lock.WaitAsync(cancellationToken);

        try
        {
            if (IsCachedTokenUsable())
            {
                return _cachedToken!;
            }

            var (token, expiresAt) = await RequestInstallationTokenAsync(cancellationToken);

            _cachedToken = token;
            _cachedTokenExpiresAt = expiresAt;

            _logger.LogInformation(
                "Acquired GitHub App installation token for installation {InstallationId}, expiring at {ExpiresAt:o}",
                _settings.InstallationId, expiresAt);

            return token;
        }
        finally
        {
            _lock.Release();
        }
    }

    private bool IsCachedTokenUsable()
        => _cachedToken is not null && DateTimeOffset.UtcNow < _cachedTokenExpiresAt - ExpiryBuffer;

    private async Task<(string Token, DateTimeOffset ExpiresAt)> RequestInstallationTokenAsync(CancellationToken cancellationToken)
    {
        ValidateSettings();

        var jwt = CreateAppJwt();

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/app/installations/{_settings.InstallationId}/access_tokens");

        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);

        HttpResponseMessage response;

        try
        {
            using var httpClient = _httpClientFactory.CreateClient(HttpClientName);
            response = await httpClient.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "GitHub API call failed while requesting an installation token");
            throw new GitHubApiException("Unable to reach GitHub to authenticate.", ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                // Body may echo credential details, so it is never surfaced or logged.
                _logger.LogError(
                    "GitHub App token exchange failed with status {StatusCode} for installation {InstallationId}",
                    (int)response.StatusCode, _settings.InstallationId);

                throw new GitHubApiException("GitHub rejected the CodeRefine App credentials.");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            var token = document.RootElement.GetProperty("token").GetString()
                ?? throw new GitHubApiException("GitHub returned an empty installation token.");

            var expiresAt = document.RootElement.TryGetProperty("expires_at", out var expiresElement)
                && expiresElement.TryGetDateTimeOffset(out var parsed)
                    ? parsed
                    : DateTimeOffset.UtcNow.AddMinutes(60);

            return (token, expiresAt);
        }
    }

    private void ValidateSettings()
    {
        if (string.IsNullOrWhiteSpace(_settings.AppId)
            || string.IsNullOrWhiteSpace(_settings.PrivateKey)
            || _settings.InstallationId <= 0)
        {
            throw new GitHubApiException("GitHub App credentials are not configured.");
        }
    }

    /// <summary>
    /// Builds the RS256 JWT GitHub requires for App-level authentication. Valid
    /// for nine minutes; GitHub rejects anything over ten.
    /// </summary>
    private string CreateAppJwt()
    {
        var now = DateTimeOffset.UtcNow;

        var header = new { alg = "RS256", typ = "JWT" };
        var payload = new
        {
            iat = now.AddSeconds(-60).ToUnixTimeSeconds(),
            exp = now.AddMinutes(9).ToUnixTimeSeconds(),
            iss = _settings.AppId
        };

        var signingInput = $"{Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(header))}." +
                           $"{Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(payload))}";

        using var rsa = RSA.Create();

        try
        {
            rsa.ImportFromPem(_settings.PrivateKey);
        }
        catch (ArgumentException ex)
        {
            // Never include the key material in the exception.
            throw new GitHubApiException("The configured GitHub App private key is not a valid PEM key.", ex);
        }

        var signature = rsa.SignData(
            Encoding.UTF8.GetBytes(signingInput),
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        return $"{signingInput}.{Base64UrlEncode(signature)}";
    }

    private static string Base64UrlEncode(byte[] value)
        => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}

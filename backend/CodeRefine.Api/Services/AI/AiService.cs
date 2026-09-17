using System.Net.Http.Json;
using System.Text.Json;
using CodeRefine.Api.DTOs.AiService;
using CodeRefine.Api.Exceptions;

namespace CodeRefine.Api.Services.AI;

public class AiService : IAiService
{
    // The engine is a FastAPI/Pydantic service that uses snake_case field names
    // (no camelCase alias generator configured), so requests/responses must be
    // serialized using snake_case rather than the default camelCase.
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<AiService> _logger;

    public AiService(HttpClient httpClient, ILogger<AiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<AnalyzeResponse> AnalyzeAsync(AnalyzeRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("AI analyze request started for analysis {AnalysisId} with {FileCount} changed files",
            request.AnalysisId, request.ChangedFiles.Count);

        try
        {
            using var response = await _httpClient.PostAsJsonAsync("/analyze", request, SerializerOptions, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("AI analyze request failed for analysis {AnalysisId} with status {StatusCode}",
                    request.AnalysisId, (int)response.StatusCode);

                throw new AiServiceException($"Analysis service returned status {(int)response.StatusCode}.");
            }

            var result = await response.Content.ReadFromJsonAsync<AnalyzeResponse>(SerializerOptions, cancellationToken);

            if (result is null)
            {
                throw new AiServiceException("Analysis service returned an empty response.");
            }

            _logger.LogInformation("AI analyze request completed for analysis {AnalysisId} with {FindingCount} findings",
                request.AnalysisId, result.Findings.Count);

            return result;
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "AI analyze request timed out for analysis {AnalysisId}", request.AnalysisId);
            throw new AiServiceException("Analysis service timed out.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "AI analyze request could not reach the analysis service for analysis {AnalysisId}", request.AnalysisId);
            throw new AiServiceException("Analysis service is unavailable.", ex);
        }
    }

    public async Task<WorkspaceResponse> CreateWorkspaceAsync(WorkspaceRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("AI workspace request started for repo {RepoUrl}", request.RepoUrl);

        try
        {
            using var response = await _httpClient.PostAsJsonAsync("/workspace", request, SerializerOptions, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("AI workspace request failed with status {StatusCode}", (int)response.StatusCode);
                throw new AiServiceException($"Analysis service returned status {(int)response.StatusCode} while preparing the workspace.");
            }

            var result = await response.Content.ReadFromJsonAsync<WorkspaceResponse>(SerializerOptions, cancellationToken)
                ?? throw new AiServiceException("Analysis service returned an empty workspace response.");

            _logger.LogInformation("AI workspace ready at {RepoPath} with {FileCount} changed files",
                result.RepoPath, result.ChangedFiles.Count);

            return result;
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "AI workspace request timed out for repo {RepoUrl}", request.RepoUrl);
            throw new AiServiceException("Analysis service timed out while preparing the workspace.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "AI workspace request could not reach the analysis service for repo {RepoUrl}", request.RepoUrl);
            throw new AiServiceException("Analysis service is unavailable.", ex);
        }
    }

    public async Task<WorkflowResponse> RunWorkflowAsync(WorkflowRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("AI workflow request started for analysis {AnalysisId} with {FileCount} changed files",
            request.AnalysisId, request.ChangedFiles.Count);

        try
        {
            using var response = await _httpClient.PostAsJsonAsync("/workflow/run", request, SerializerOptions, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("AI workflow request failed for analysis {AnalysisId} with status {StatusCode}",
                    request.AnalysisId, (int)response.StatusCode);
                throw new AiServiceException($"Analysis service returned status {(int)response.StatusCode} while running the workflow.");
            }

            var result = await response.Content.ReadFromJsonAsync<WorkflowResponse>(SerializerOptions, cancellationToken)
                ?? throw new AiServiceException("Analysis service returned an empty workflow response.");

            _logger.LogInformation("AI workflow request completed for analysis {AnalysisId} with {FindingCount} findings and {PatchCount} patches",
                request.AnalysisId, result.Findings.Count, result.GeneratedPatches.Count);

            return result;
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "AI workflow request timed out for analysis {AnalysisId}", request.AnalysisId);
            throw new AiServiceException("Analysis service timed out while running the workflow.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "AI workflow request could not reach the analysis service for analysis {AnalysisId}", request.AnalysisId);
            throw new AiServiceException("Analysis service is unavailable.", ex);
        }
    }
}

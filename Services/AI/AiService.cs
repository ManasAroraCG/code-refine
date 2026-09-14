using System.Net.Http.Json;
using System.Text.Json;
using CodeRefine.Api.DTOs.AiService;
using CodeRefine.Api.Exceptions;

namespace CodeRefine.Api.Services.AI;

public class AiService : IAiService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

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
}

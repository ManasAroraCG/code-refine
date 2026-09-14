using CodeRefine.Api.DTOs.AiService;

namespace CodeRefine.Api.Services.AI;

/// <summary>
/// Thin HTTP client for the Python FastAPI/LangGraph service. This backend
/// orchestrates the workflow; it does not implement AI logic.
/// </summary>
public interface IAiService
{
    Task<AnalyzeResponse> AnalyzeAsync(AnalyzeRequest request, CancellationToken cancellationToken = default);
}

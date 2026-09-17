using CodeRefine.Api.DTOs.AiService;

namespace CodeRefine.Api.Services.AI;

/// <summary>
/// Thin HTTP client for the Python FastAPI/LangGraph service. This backend
/// orchestrates the workflow; it does not implement AI logic.
/// </summary>
public interface IAiService
{
    Task<AnalyzeResponse> AnalyzeAsync(AnalyzeRequest request, CancellationToken cancellationToken = default);

    /// <summary>Clones/checks out the repo inside the engine and returns the changed files to analyze.</summary>
    Task<WorkspaceResponse> CreateWorkspaceAsync(WorkspaceRequest request, CancellationToken cancellationToken = default);

    /// <summary>Runs the full LangGraph workflow (findings, patches, verification) against a prepared workspace.</summary>
    Task<WorkflowResponse> RunWorkflowAsync(WorkflowRequest request, CancellationToken cancellationToken = default);
}

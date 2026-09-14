namespace CodeRefine.Api.DTOs.AiService;

/// <summary>
/// Request sent to the FastAPI service (POST /analyze). Shared contract with
/// Developer 2; keep in sync and do not leak GitHub credentials into it.
/// </summary>
public class AnalyzeRequest
{
    public string AnalysisId { get; set; } = string.Empty;
    public string RepositoryPath { get; set; } = string.Empty;
    public IReadOnlyList<string> ChangedFiles { get; set; } = new List<string>();
    public string Language { get; set; } = string.Empty;
    public IReadOnlyList<string> Agents { get; set; } = new List<string>();
}

/// <summary>Finding returned by an AI agent.</summary>
public class AgentFindingDto
{
    public string Agent { get; set; } = string.Empty;
    public string File { get; set; } = string.Empty;
    public int StartLine { get; set; }
    public int EndLine { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Recommendation { get; set; }
    public double Confidence { get; set; }
    public bool FixAvailable { get; set; }
}

public class AnalyzeResponse
{
    public string AnalysisId { get; set; } = string.Empty;
    public IReadOnlyList<AgentFindingDto> Findings { get; set; } = new List<AgentFindingDto>();
    public double? QualityScore { get; set; }
}

namespace CodeRefine.Api.DTOs.AiService;

/// <summary>Request sent to the engine (POST /workspace) to clone/checkout a repo and list changed files.</summary>
public class WorkspaceRequest
{
    public string RepoUrl { get; set; } = string.Empty;
    public string? Branch { get; set; }
    public int? PrNumber { get; set; }
    public string? BaseBranch { get; set; }
}

public class WorkspaceResponse
{
    public string WorkspaceId { get; set; } = string.Empty;
    public string RepoPath { get; set; } = string.Empty;
    public IReadOnlyList<string> ChangedFiles { get; set; } = new List<string>();
}

/// <summary>Request sent to the engine (POST /workflow/run) once a workspace has been created.</summary>
public class WorkflowRequest
{
    public string AnalysisId { get; set; } = string.Empty;
    public string RepoPath { get; set; } = string.Empty;
    public IReadOnlyList<string> ChangedFiles { get; set; } = new List<string>();
    public string Language { get; set; } = "python";
    public IReadOnlyList<string> Agents { get; set; } = new List<string>();
}

/// <summary>Rich finding shape returned inside WorkflowResponse (distinct from AgentFindingDto used by /analyze).</summary>
public class WorkflowFindingDto
{
    public string Id { get; set; } = string.Empty;
    public string AgentType { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string File { get; set; } = string.Empty;
    public string? FilePath { get; set; }
    public int StartLine { get; set; }
    public int EndLine { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public bool FixAvailable { get; set; }
}

/// <summary>One patch per file (may resolve multiple findings in that file).</summary>
public class GeneratedPatchDto
{
    public string FilePath { get; set; } = string.Empty;
    public string OriginalCode { get; set; } = string.Empty;
    public string ProposedCode { get; set; } = string.Empty;
    public string Diff { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public bool AppliedSuccessfully { get; set; }
}

public class VerificationCheckDto
{
    public string CheckType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Output { get; set; } = string.Empty;
    public long DurationMs { get; set; }
}

public class WorkflowVerificationResultDto
{
    public string Verdict { get; set; } = "passed";
    public IReadOnlyList<VerificationCheckDto> Checks { get; set; } = new List<VerificationCheckDto>();
    public string Summary { get; set; } = string.Empty;
}

public class WorkflowResponse
{
    public string AnalysisId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public double QualityScore { get; set; }
    public IReadOnlyList<WorkflowFindingDto> Findings { get; set; } = new List<WorkflowFindingDto>();
    public IReadOnlyList<GeneratedPatchDto> GeneratedPatches { get; set; } = new List<GeneratedPatchDto>();
    public string CombinedDiff { get; set; } = string.Empty;
    public WorkflowVerificationResultDto? VerificationResult { get; set; }
    public int RetryCount { get; set; }
}

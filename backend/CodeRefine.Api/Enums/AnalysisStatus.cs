namespace CodeRefine.Api.Enums;

public enum AnalysisStatus
{
    Queued = 0,
    Analyzing = 1,
    GeneratingFixes = 2,
    Verifying = 3,
    ReadyForReview = 4,
    Approved = 5,
    Rejected = 6,
    CreatingPullRequest = 7,
    Completed = 8,
    Failed = 9
}

using CodeRefine.Api.Models;

namespace CodeRefine.Api.Services.Analysis;

/// <summary>Process-lifetime store for analysis runs. Replaces Postgres persistence for the analysis flow.</summary>
public interface IAnalysisRunStore
{
    void Save(AnalysisRun run);

    AnalysisRun? Find(Guid id);
}

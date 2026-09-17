using System.Collections.Concurrent;
using CodeRefine.Api.Models;

namespace CodeRefine.Api.Services.Analysis;

/// <summary>In-memory analysis run store. Data is lost on restart; register as a singleton.</summary>
public class InMemoryAnalysisRunStore : IAnalysisRunStore
{
    private readonly ConcurrentDictionary<Guid, AnalysisRun> _runs = new();

    public void Save(AnalysisRun run) => _runs[run.Id] = run;

    public AnalysisRun? Find(Guid id) => _runs.TryGetValue(id, out var run) ? run : null;
}

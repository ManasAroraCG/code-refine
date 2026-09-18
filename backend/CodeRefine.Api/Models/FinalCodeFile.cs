namespace CodeRefine.Api.Models;

/// <summary>The final per-file code that will be committed to the improvement PR.</summary>
public class FinalCodeFile
{
    public string FilePath { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

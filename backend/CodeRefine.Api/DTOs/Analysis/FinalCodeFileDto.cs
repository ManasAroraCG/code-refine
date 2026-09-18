namespace CodeRefine.Api.DTOs.Analysis;

/// <summary>The final per-file code that will be committed to the improvement PR.</summary>
public class FinalCodeFileDto
{
    public string FilePath { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

namespace CodeRefine.Api.Configuration;

/// <summary>
/// Connection settings for the Python FastAPI/LangGraph service. Model and
/// provider configuration belongs to the AI service, not to this backend.
/// </summary>
public class AiServiceSettings
{
    public const string SectionName = "AiService";

    public string BaseUrl { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 300;
}

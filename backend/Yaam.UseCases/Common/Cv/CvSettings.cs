namespace Yaam.UseCases.Common.Cv;

public class CvSettings
{
    public string OllamaBaseUrl { get; init; } = "http://localhost:11434";
    public string OllamaModel { get; init; } = "llama3.1";
    public int TimeoutSeconds { get; init; } = 300;
}

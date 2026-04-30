namespace MusicLibraryManager.Models;

public record OpenAiConfig
{
    public string? Model { get; init; } = "gpt-4o-mini";
    public string? Endpoint { get; init; } = "https://api.openai.com/v1/chat/completions";
    public string? ApiKey { get; init; }
}

namespace MusicLibraryManager.Models;

public record SoundCloudConfig
{
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
    public int CallbackPort { get; init; } = 5483;
}

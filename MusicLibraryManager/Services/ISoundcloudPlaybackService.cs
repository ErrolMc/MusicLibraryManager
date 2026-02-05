namespace MusicLibraryManager.Services;

public interface ISoundCloudPlaybackService : IPlaybackService
{
    /// <summary>
    /// Fetches the stream URL for a SoundCloud track and starts streaming it.
    /// </summary>
    Task LoadAndPlayAsync(long trackId);
}

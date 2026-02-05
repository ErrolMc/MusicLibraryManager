namespace MusicLibraryManager.Services;

public interface IPlaybackService : IDisposable
{
    bool IsPlaying { get; }
    bool IsLoaded { get; }
    TimeSpan CurrentPosition { get; }
    TimeSpan TotalDuration { get; }
    float Volume { get; set; }

    /// <summary>
    /// Loads audio from a file path or URL and starts playback.
    /// Supports streaming from HTTP URLs via MediaFoundation.
    /// </summary>
    Task LoadAsync(string source);
    void Play();
    void Pause();
    void Stop();
    void Seek(TimeSpan position);

    event EventHandler? PlaybackStateChanged;
    event EventHandler? PlaybackEnded;
}

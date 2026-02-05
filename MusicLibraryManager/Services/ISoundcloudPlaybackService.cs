namespace MusicLibraryManager.Services;

public interface ISoundCloudPlaybackService : IDisposable
{
    /// <summary>
    /// Gets whether audio is currently playing.
    /// </summary>
    bool IsPlaying { get; }

    /// <summary>
    /// Gets whether a stream is currently loaded and ready.
    /// </summary>
    bool IsLoaded { get; }

    /// <summary>
    /// Gets the current playback position.
    /// </summary>
    TimeSpan CurrentPosition { get; }

    /// <summary>
    /// Gets the total duration of the loaded stream.
    /// </summary>
    TimeSpan TotalDuration { get; }

    /// <summary>
    /// Gets or sets the volume (0.0 to 1.0).
    /// </summary>
    float Volume { get; set; }

    /// <summary>
    /// Loads and starts streaming a track by its ID.
    /// </summary>
    Task LoadAndPlayAsync(long trackId);

    /// <summary>
    /// Resumes playback.
    /// </summary>
    void Play();

    /// <summary>
    /// Pauses playback.
    /// </summary>
    void Pause();

    /// <summary>
    /// Stops playback and releases the stream.
    /// </summary>
    void Stop();

    /// <summary>
    /// Seeks to a specific position.
    /// </summary>
    void Seek(TimeSpan position);

    /// <summary>
    /// Raised when playback state changes.
    /// </summary>
    event EventHandler? PlaybackStateChanged;

    /// <summary>
    /// Raised when the stream finishes playing.
    /// </summary>
    event EventHandler? PlaybackEnded;
}

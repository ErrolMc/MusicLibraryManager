using NAudio.Wave;

namespace MusicLibraryManager.Services.Concrete;

public class PlaybackService : IPlaybackService
{
    private WaveOutEvent? _waveOut;
    private WaveStream? _mediaReader;

    public bool IsPlaying => _waveOut?.PlaybackState == PlaybackState.Playing;
    public bool IsLoaded => _mediaReader != null;

    public TimeSpan CurrentPosition => _mediaReader?.CurrentTime ?? TimeSpan.Zero;
    public TimeSpan TotalDuration => _mediaReader?.TotalTime ?? TimeSpan.Zero;

    public float Volume
    {
        get => _waveOut?.Volume ?? 1f;
        set
        {
            if (_waveOut != null)
                _waveOut.Volume = Math.Clamp(value, 0f, 1f);
        }
    }

    public event EventHandler? PlaybackStateChanged;
    public event EventHandler? PlaybackEnded;

    public async Task LoadAsync(string source)
    {
        Stop();

        // MediaFoundationReader handles both file paths and HTTP URLs,
        // streaming from the network when given a URL.
        // Constructor may block while buffering, so run on background thread.
        _mediaReader = await Task.Run(() => new MediaFoundationReader(source));

        _waveOut = new WaveOutEvent();
        _waveOut.Init(_mediaReader);
        _waveOut.PlaybackStopped += OnPlaybackStopped;
        _waveOut.Play();

        PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Play()
    {
        if (_waveOut?.PlaybackState == PlaybackState.Paused)
        {
            _waveOut.Play();
            PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Pause()
    {
        if (_waveOut?.PlaybackState == PlaybackState.Playing)
        {
            _waveOut.Pause();
            PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Stop()
    {
        if (_waveOut != null)
        {
            _waveOut.PlaybackStopped -= OnPlaybackStopped;
            _waveOut.Stop();
            _waveOut.Dispose();
            _waveOut = null;
        }

        if (_mediaReader != null)
        {
            _mediaReader.Dispose();
            _mediaReader = null;
        }

        PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Seek(TimeSpan position)
    {
        if (_mediaReader != null)
        {
            _mediaReader.CurrentTime = position;
        }
    }

    public void Dispose()
    {
        Stop();
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        PlaybackEnded?.Invoke(this, EventArgs.Empty);
        PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
    }
}

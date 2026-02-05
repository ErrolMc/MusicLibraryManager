using Microsoft.UI.Dispatching;
using MusicLibraryManager.Services;

namespace MusicLibraryManager.ViewModels;

public partial class PlaybackViewModel : ObservableObject
{
    private readonly IPlaybackService _playbackService;
    private DispatcherQueue? _dispatcherQueue;
    private DispatcherQueueTimer? _positionTimer;
    private bool _isSeeking;

    [ObservableProperty]
    private bool isPlaying;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool isLoaded;

    [ObservableProperty]
    private double playbackPosition;

    [ObservableProperty]
    private double playbackDuration;

    [ObservableProperty]
    private string? positionText;

    [ObservableProperty]
    private string? durationText;

    [ObservableProperty]
    private double volume = 100;

    /// <summary>
    /// Optional callback invoked when PlayPause is triggered but no audio is loaded.
    /// The parent ViewModel sets this to provide the audio source (e.g. SoundCloud stream URL or file path).
    /// The callback should call <see cref="LoadAsync"/> with the appropriate source.
    /// </summary>
    public Func<Task>? LoadRequestedAsync { get; set; }

    public PlaybackViewModel(IPlaybackService playbackService)
    {
        _playbackService = playbackService;

        _playbackService.PlaybackStateChanged += OnPlaybackStateChanged;
        _playbackService.PlaybackEnded += OnPlaybackEnded;

        _playbackService.Volume = 1.0f;
    }

    private void EnsureTimer()
    {
        if (_positionTimer != null) return;

        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _positionTimer = _dispatcherQueue.CreateTimer();
        _positionTimer.Interval = TimeSpan.FromMilliseconds(250);
        _positionTimer.Tick += OnPositionTimerTick;
    }

    public async Task LoadAsync(string source)
    {
        await _playbackService.LoadAsync(source);
        SyncAfterLoad();
    }

    /// <summary>
    /// Syncs UI state after the underlying service has been loaded externally
    /// (e.g. via ISoundCloudPlaybackService.LoadAndPlayAsync).
    /// </summary>
    public void SyncAfterLoad()
    {
        IsLoaded = _playbackService.IsLoaded;

        if (IsLoaded)
        {
            PlaybackDuration = _playbackService.TotalDuration.TotalSeconds;
            DurationText = FormatTime(_playbackService.TotalDuration);
            StartPositionTimer();
        }
    }

    [RelayCommand]
    private async Task PlayPauseAsync()
    {
        if (IsPlaying)
        {
            _playbackService.Pause();
            return;
        }

        if (_playbackService.IsLoaded)
        {
            _playbackService.Play();
            return;
        }

        if (LoadRequestedAsync != null)
        {
            IsLoading = true;
            try
            {
                await LoadRequestedAsync();
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    [RelayCommand]
    private void StopPlayback()
    {
        _playbackService.Stop();
        StopPositionTimer();
        IsLoaded = false;
        PlaybackPosition = 0;
        PositionText = "0:00";
    }

    partial void OnPlaybackPositionChanged(double value)
    {
        if (_isSeeking && _playbackService.IsLoaded)
        {
            _playbackService.Seek(TimeSpan.FromSeconds(value));
            PositionText = FormatTime(TimeSpan.FromSeconds(value));
        }
    }

    partial void OnVolumeChanged(double value)
    {
        _playbackService.Volume = (float)(value / 100.0);
    }

    public void BeginSeek() => _isSeeking = true;

    public void EndSeek()
    {
        if (_playbackService.IsLoaded)
        {
            _playbackService.Seek(TimeSpan.FromSeconds(PlaybackPosition));
        }
        _isSeeking = false;
    }

    public void Reset()
    {
        _playbackService.Stop();
        StopPositionTimer();

        IsPlaying = false;
        IsLoaded = false;
        IsLoading = false;
        PlaybackPosition = 0;
        PlaybackDuration = 0;
        PositionText = "0:00";
        DurationText = "0:00";
    }

    private void OnPlaybackStateChanged(object? sender, EventArgs e)
    {
        _dispatcherQueue?.TryEnqueue(() =>
        {
            IsPlaying = _playbackService.IsPlaying;

            if (IsPlaying)
                StartPositionTimer();
            else
                StopPositionTimer();
        });
    }

    private void OnPlaybackEnded(object? sender, EventArgs e)
    {
        _dispatcherQueue?.TryEnqueue(() =>
        {
            IsPlaying = false;
            IsLoaded = false;
            StopPositionTimer();
            PlaybackPosition = 0;
            PositionText = "0:00";
        });
    }

    private void OnPositionTimerTick(DispatcherQueueTimer sender, object args)
    {
        if (_isSeeking || !_playbackService.IsLoaded) return;

        PlaybackPosition = _playbackService.CurrentPosition.TotalSeconds;
        PositionText = FormatTime(_playbackService.CurrentPosition);

        if (PlaybackDuration <= 0 && _playbackService.TotalDuration.TotalSeconds > 0)
        {
            PlaybackDuration = _playbackService.TotalDuration.TotalSeconds;
            DurationText = FormatTime(_playbackService.TotalDuration);
        }
    }

    private void StartPositionTimer()
    {
        EnsureTimer();
        _positionTimer!.Start();
    }

    private void StopPositionTimer() => _positionTimer?.Stop();

    private static string FormatTime(TimeSpan time)
    {
        return time.TotalHours >= 1
            ? time.ToString(@"h\:mm\:ss")
            : time.ToString(@"m\:ss");
    }
}

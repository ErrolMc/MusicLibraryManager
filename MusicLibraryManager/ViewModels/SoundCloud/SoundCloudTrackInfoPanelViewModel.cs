using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Imaging;
using MusicLibraryManager.Services;

namespace MusicLibraryManager.ViewModels.SoundCloud;

public partial class SoundCloudTrackInfoPanelViewModel : ObservableObject
{
    private readonly IImageService _imageService;
    private readonly ISoundCloudPlaybackService _playbackService;
    private readonly DispatcherQueueTimer _positionTimer;
    private byte[]? _albumCoverData;
    private bool _isSeeking;

    [ObservableProperty]
    private string? fileName;

    [ObservableProperty]
    private string? title;

    [ObservableProperty]
    private string? artist;

    [ObservableProperty]
    private string? album;

    [ObservableProperty]
    private string? year;

    [ObservableProperty]
    private string? genre;

    [ObservableProperty]
    private string? comment;

    [ObservableProperty]
    private string? composer;

    [ObservableProperty]
    private string? albumArtist;

    [ObservableProperty]
    private string? track;

    [ObservableProperty]
    private string? duration;

    [ObservableProperty]
    private BitmapImage? albumCover;

    [ObservableProperty]
    private bool hasTrackLoaded;

    // Playback properties
    [ObservableProperty]
    private bool isPlaying;

    [ObservableProperty]
    private bool isLoadingStream;

    [ObservableProperty]
    private bool isStreamLoaded;

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

    private long _currentTrackId;

    public SoundCloudTrackInfoPanelViewModel(IImageService imageService, ISoundCloudPlaybackService playbackService)
    {
        _imageService = imageService;
        _playbackService = playbackService;

        _playbackService.PlaybackStateChanged += OnPlaybackStateChanged;
        _playbackService.PlaybackEnded += OnPlaybackEnded;

        _positionTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        _positionTimer.Interval = TimeSpan.FromMilliseconds(250);
        _positionTimer.Tick += OnPositionTimerTick;

        // Set initial volume on service
        _playbackService.Volume = 1.0f;
    }

    public void SetTrackInfo(
        string? fileName,
        string? title,
        string? artist,
        string? album,
        string? year,
        string? genre,
        string? comment,
        string? composer,
        string? albumArtist,
        string? track,
        string? duration,
        BitmapImage? albumCover,
        byte[]? albumCoverData = null,
        long trackId = 0)
    {
        // Stop any current playback when switching tracks
        _playbackService.Stop();
        StopPositionTimer();

        FileName = fileName;
        Title = title;
        Artist = artist;
        Album = album;
        Year = year;
        Genre = genre;
        Comment = comment;
        Composer = composer;
        AlbumArtist = albumArtist;
        Track = track;
        Duration = duration;
        AlbumCover = albumCover;
        _albumCoverData = albumCoverData;
        _currentTrackId = trackId;
        HasTrackLoaded = true;

        // Reset playback UI
        IsPlaying = false;
        IsStreamLoaded = false;
        IsLoadingStream = false;
        PlaybackPosition = 0;
        PlaybackDuration = 0;
        PositionText = "0:00";
        DurationText = "0:00";
    }

    public void ClearTrackInfo()
    {
        _playbackService.Stop();
        StopPositionTimer();

        FileName = null;
        Title = null;
        Artist = null;
        Album = null;
        Year = null;
        Genre = null;
        Comment = null;
        Composer = null;
        AlbumArtist = null;
        Track = null;
        Duration = null;
        AlbumCover = null;
        _albumCoverData = null;
        _currentTrackId = 0;
        HasTrackLoaded = false;

        IsPlaying = false;
        IsStreamLoaded = false;
        IsLoadingStream = false;
        PlaybackPosition = 0;
        PlaybackDuration = 0;
        PositionText = "0:00";
        DurationText = "0:00";
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

        // First time playing - load stream
        if (_currentTrackId <= 0) return;

        IsLoadingStream = true;
        try
        {
            await _playbackService.LoadAndPlayAsync(_currentTrackId);
            IsStreamLoaded = _playbackService.IsLoaded;

            if (IsStreamLoaded)
            {
                PlaybackDuration = _playbackService.TotalDuration.TotalSeconds;
                DurationText = FormatTime(_playbackService.TotalDuration);
                StartPositionTimer();
            }
        }
        finally
        {
            IsLoadingStream = false;
        }
    }

    [RelayCommand]
    private void StopPlayback()
    {
        _playbackService.Stop();
        StopPositionTimer();
        IsStreamLoaded = false;
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

    public void BeginSeek() => _isSeeking = true;

    public void EndSeek()
    {
        if (_playbackService.IsLoaded)
        {
            _playbackService.Seek(TimeSpan.FromSeconds(PlaybackPosition));
        }
        _isSeeking = false;
    }

    partial void OnVolumeChanged(double value)
    {
        _playbackService.Volume = (float)(value / 100.0);
    }

    [RelayCommand]
    private void CopyAlbumCover()
    {
        if (_albumCoverData == null) return;
        _imageService.CopyToClipboard(_albumCoverData);
    }

    [RelayCommand]
    private void CopyField(string fieldName)
    {
        var value = fieldName switch
        {
            "Title" => Title,
            "Artist" => Artist,
            "Album" => Album,
            "Duration" => Duration,
            "Genre" => Genre,
            "Year" => Year,
            "Description" => Comment,
            _ => null
        };

        if (string.IsNullOrEmpty(value)) return;

        var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
        dataPackage.SetText(value);
        Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
    }

    private void OnPlaybackStateChanged(object? sender, EventArgs e)
    {
        DispatcherQueue.GetForCurrentThread()?.TryEnqueue(() =>
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
        DispatcherQueue.GetForCurrentThread()?.TryEnqueue(() =>
        {
            IsPlaying = false;
            IsStreamLoaded = false;
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

        // Update duration in case it wasn't available initially (streaming)
        if (PlaybackDuration <= 0 && _playbackService.TotalDuration.TotalSeconds > 0)
        {
            PlaybackDuration = _playbackService.TotalDuration.TotalSeconds;
            DurationText = FormatTime(_playbackService.TotalDuration);
        }
    }

    private void StartPositionTimer() => _positionTimer.Start();
    private void StopPositionTimer() => _positionTimer.Stop();

    private static string FormatTime(TimeSpan time)
    {
        return time.TotalHours >= 1
            ? time.ToString(@"h\:mm\:ss")
            : time.ToString(@"m\:ss");
    }
}

using Microsoft.UI.Xaml.Media.Imaging;
using MusicLibraryManager.Services;

namespace MusicLibraryManager.ViewModels.SoundCloud;

public partial class SoundCloudTrackInfoPanelViewModel : ObservableObject
{
    private readonly IImageService _imageService;
    private readonly ISoundCloudPlaybackService _playbackService;
    protected byte[]? AlbumCoverData;
    protected long CurrentTrackId;

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

    [ObservableProperty]
    private bool isSyncToPlaceholderEnabled;

    [ObservableProperty]
    private bool isPickPlaceholderMode;

    [ObservableProperty]
    private string placeholderOverlayMessage = "click on placeholder or right click to cancel";

    public PlaybackViewModel Playback { get; }

    public SoundCloudTrackInfoPanelViewModel(
        IImageService imageService,
        ISoundCloudPlaybackService playbackService)
    {
        _imageService = imageService;
        _playbackService = playbackService;
        Playback = new PlaybackViewModel(playbackService, "SoundCloud", volumeMultiplier: 0.1);
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
        Playback.Reset();

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
        AlbumCoverData = albumCoverData;
        CurrentTrackId = trackId;
        HasTrackLoaded = true;

        // Set up lazy loading: when play is pressed, fetch the stream URL and stream it
        Playback.LoadRequestedAsync = async () =>
        {
            if (CurrentTrackId <= 0) return;
            await _playbackService.LoadAndPlayAsync(CurrentTrackId);
            Playback.SyncAfterLoad();
        };
    }

    public void ClearTrackInfo()
    {
        Playback.Reset();
        Playback.LoadRequestedAsync = null;

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
        AlbumCoverData = null;
        CurrentTrackId = 0;
        HasTrackLoaded = false;
        IsPickPlaceholderMode = false;
    }

    public void SetSyncPanelOpen(bool isOpen)
    {
        IsSyncToPlaceholderEnabled = isOpen;
        if (!isOpen)
        {
            IsPickPlaceholderMode = false;
        }
    }

    [RelayCommand]
    protected virtual void SyncToPlaceholder()
    {
    }

    [RelayCommand]
    protected virtual void CancelPlaceholderPickMode()
    {
        IsPickPlaceholderMode = false;
    }

    [RelayCommand]
    private void CopyAlbumCover()
    {
        if (AlbumCoverData == null) return;
        _imageService.CopyToClipboard(AlbumCoverData);
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
}

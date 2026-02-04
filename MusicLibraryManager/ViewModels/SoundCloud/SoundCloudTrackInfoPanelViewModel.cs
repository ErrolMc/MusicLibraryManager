using Microsoft.UI.Xaml.Media.Imaging;

namespace MusicLibraryManager.ViewModels.SoundCloud;

public partial class SoundCloudTrackInfoPanelViewModel : ObservableObject
{
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

    public SoundCloudTrackInfoPanelViewModel()
    {
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
        BitmapImage? albumCover)
    {
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
        HasTrackLoaded = true;
    }

    public void ClearTrackInfo()
    {
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
        HasTrackLoaded = false;
    }
}

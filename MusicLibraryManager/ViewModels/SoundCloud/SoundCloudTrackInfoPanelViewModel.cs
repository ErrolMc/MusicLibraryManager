using Microsoft.UI.Xaml.Media.Imaging;
using MusicLibraryManager.Services;

namespace MusicLibraryManager.ViewModels.SoundCloud;

public partial class SoundCloudTrackInfoPanelViewModel : ObservableObject
{
    private readonly IImageService _imageService;
    private byte[]? _albumCoverData;
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

    public SoundCloudTrackInfoPanelViewModel(IImageService imageService)
    {
        _imageService = imageService;
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
        byte[]? albumCoverData = null)
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
        _albumCoverData = albumCoverData;
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
        _albumCoverData = null;
        HasTrackLoaded = false;
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
}

using Microsoft.UI.Xaml.Media.Imaging;

namespace MusicLibraryManager.ViewModels;

public partial class TrackInfoPanelViewModel : ObservableObject
{
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
    private BitmapImage? albumCover;

    public void SetInfoFromSong(Track track)
    {
        Title = track.Title;
        Artist = track.Artist;
        Album = track.Album;
        Year = (track.Year ?? 0).ToString();
        Genre = track.Genre;
        Comment = "";
        Composer = "";
        AlbumArtist = "";
        Track = (track.TrackNumber ?? 0).ToString();
        AlbumCover = GetAlbumCover(track);
    }

    private static BitmapImage? GetAlbumCover(Track track)
    {
        var pictures = track.Tag.Pictures;
        if (pictures == null || pictures.Length == 0)
            return null;

        var picture = pictures[0];
        var imageData = picture.Data.Data;

        var bitmap = new BitmapImage();
        using var stream = new MemoryStream(imageData);
        bitmap.SetSource(stream.AsRandomAccessStream());
        return bitmap;
    }
}

namespace MusicLibraryManager.Models;

public class Track
{
    private TagLib.File _file;

    public string Title
    {
        get => Tag.Title ?? string.Empty;
    }

    public string Artist
    {
        get => Tag.FirstPerformer ?? string.Empty;
    }

    public string Album
    {
        get => Tag.Album ?? string.Empty;
    }

    public TimeSpan Duration
    {
        get => _file.Properties.Duration;
    }

    public string Genre
    {
        get => Tag.FirstGenre ?? string.Empty;
    }

    public int? Year
    {
        get => Tag.Year > 0 ? (int?)Tag.Year : null;
    }

    public int? TrackNumber
    {
        get => Tag.Track > 0 ? (int?)Tag.Track : null;
    }

    public TagLib.Tag Tag => _file.Tag;
    public string FilePath { get; set; }

    public Track(TagLib.File file, string filePath)
    {
        _file = file;
        FilePath = filePath;
    }
}

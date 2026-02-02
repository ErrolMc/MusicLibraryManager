namespace MusicLibraryManager.ViewModels;

public partial class TrackListItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string songName = string.Empty;

    [ObservableProperty]
    private string artist = string.Empty;

    [ObservableProperty]
    private string albumTitle = string.Empty;

    [ObservableProperty]
    private int year;

    public TrackListItemViewModel()
    {
    }

    public TrackListItemViewModel(string songName, string artist, string albumTitle, int year)
    {
        SongName = songName;
        Artist = artist;
        AlbumTitle = albumTitle;
        Year = year;
    }
}

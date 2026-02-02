using System.Collections.ObjectModel;

namespace MusicLibraryManager.ViewModels;

public partial class TrackListPanelViewModel : ObservableObject
{
    [ObservableProperty]
    private string title = "Track List";

    [ObservableProperty]
    private ObservableCollection<TrackListItemViewModel> tracks = new();

    public TrackListPanelViewModel()
    {
        // Add some sample tracks for demonstration
        Tracks.Add(new TrackListItemViewModel("Bohemian Rhapsody", "Queen", "A Night at the Opera", 1975));
        Tracks.Add(new TrackListItemViewModel("Hotel California", "Eagles", "Hotel California", 1977));
        Tracks.Add(new TrackListItemViewModel("Stairway to Heaven", "Led Zeppelin", "Led Zeppelin IV", 1971));
        Tracks.Add(new TrackListItemViewModel("Imagine", "John Lennon", "Imagine", 1971));
        Tracks.Add(new TrackListItemViewModel("Smells Like Teen Spirit", "Nirvana", "Nevermind", 1991));
        Tracks.Add(new TrackListItemViewModel("Billie Jean", "Michael Jackson", "Thriller", 1982));
        Tracks.Add(new TrackListItemViewModel("Like a Rolling Stone", "Bob Dylan", "Highway 61 Revisited", 1965));
        Tracks.Add(new TrackListItemViewModel("Hey Jude", "The Beatles", "Hey Jude", 1968));
    }
}

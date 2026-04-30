namespace MusicLibraryManager.Models;

public sealed partial record SoundCloudPlaylist(
    long Id,
    string Title,
    int TrackCount
);

using System.Collections.ObjectModel;
using MusicLibraryManager.Models;
using MusicLibraryManager.Services;

namespace MusicLibraryManager.ViewModels;

public partial class TrackListPanelViewModel : ObservableObject
{
    [ObservableProperty]
    private string title = "Track List";

    [ObservableProperty]
    private ObservableCollection<TrackListItemViewModel> tracks = new();

    private readonly IMusicLibraryService _musicLibraryService;
    private readonly TrackInfoPanelViewModel _trackInfoPanelViewModel;

    public TrackListPanelViewModel(IMusicLibraryService musicLibraryService, TrackInfoPanelViewModel trackInfoPanelViewModel)
    {
        _musicLibraryService = musicLibraryService;
        _trackInfoPanelViewModel = trackInfoPanelViewModel;
    }

    public async Task LoadSongsFromFolderAsync(string folderPath)
    {
        IReadOnlyList<Track> tracks = await _musicLibraryService.GetSongsFromFolderAsync(folderPath);

        Tracks.Clear();
        foreach (Track track in tracks)
        {
            Tracks.Add(new TrackListItemViewModel(track, OnSelectTrack));
        }
    }

    private void OnSelectTrack(TrackListItemViewModel trackListItem)
    {
        Track? track = trackListItem.Track;
        if (track == null)
            return;
        _trackInfoPanelViewModel.SetInfoFromSong(track);
    }
}

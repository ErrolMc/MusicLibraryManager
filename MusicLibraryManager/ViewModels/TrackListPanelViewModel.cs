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
    private readonly IOverlayService _overlayService;

    public TrackListPanelViewModel(IMusicLibraryService musicLibraryService, TrackInfoPanelViewModel trackInfoPanelViewModel, IOverlayService overlayService)
    {
        _musicLibraryService = musicLibraryService;
        _trackInfoPanelViewModel = trackInfoPanelViewModel;
        _overlayService = overlayService;
    }

    public async Task LoadSongsFromFolderAsync(string folderPath)
    {
        _overlayService.Show("Loading tracks...");
        IReadOnlyList<Track> tracks = await _musicLibraryService.GetSongsFromFolderAsync(folderPath);
        _overlayService.Hide();

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

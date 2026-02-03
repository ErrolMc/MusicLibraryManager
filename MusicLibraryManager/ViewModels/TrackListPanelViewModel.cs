using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using MusicLibraryManager.Models;
using MusicLibraryManager.Services;

namespace MusicLibraryManager.ViewModels;

public partial class TrackListPanelViewModel : ObservableObject
{
    [ObservableProperty]
    private string title = "Track List";

    [ObservableProperty]
    private ObservableCollection<TrackListItemViewModel> tracks = new();

    [ObservableProperty]
    private string? folderPath;

    [ObservableProperty]
    private GridLength artistColumnWidth = new(200);

    [ObservableProperty]
    private GridLength yearColumnWidth = new(60);

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

        FolderPath = folderPath;
        Tracks.Clear();
        foreach (Track track in tracks)
        {
            Tracks.Add(new TrackListItemViewModel(track, OnSelectTrack, this));
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

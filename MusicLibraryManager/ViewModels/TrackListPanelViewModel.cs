using System.Collections.ObjectModel;

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
    private GridLength fileNameColumnWidth = new(150);

    [ObservableProperty]
    private GridLength artistColumnWidth = new(150);

    [ObservableProperty]
    private GridLength yearColumnWidth = new(60);

    private readonly IMusicLibraryService _musicLibraryService;
    private readonly TrackInfoPanelViewModel _trackInfoPanel;
    private readonly IOverlayService _overlayService;
    private TrackListItemViewModel? _selectedTrackItem;

    public TrackListPanelViewModel(IMusicLibraryService musicLibraryService, TrackInfoPanelViewModel trackInfoPanelViewModel, IOverlayService overlayService)
    {
        _musicLibraryService = musicLibraryService;
        _trackInfoPanel = trackInfoPanelViewModel;
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

    private async void OnSelectTrack(TrackListItemViewModel trackListItem)
    {
        Track? track = trackListItem.Track;
        if (track == null)
            return;

        // Check for unsaved changes
        if (_trackInfoPanel.HasChanges)
        {
            bool result = await _overlayService.ShowConfirmationAsync(
                "Unsaved Changes",
                "You have unsaved changes. Do you want to save them before switching tracks?",
                "Save",
                "Discard"
            );

            if (result)
            {
                await _trackInfoPanel.SaveChangesAsync();
            }
            else
            {
                _trackInfoPanel.CancelChanges();
            }
        }

        _trackInfoPanel.SetInfoFromSong(track, trackListItem.PopulateInfo);
        _selectedTrackItem = trackListItem;
    }
}

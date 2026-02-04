using System.Collections.ObjectModel;

namespace MusicLibraryManager.ViewModels.SoundCloud;

public partial class SoundCloudSearchListViewModel : ObservableObject
{
    [ObservableProperty]
    private string title = "SoundCloud Search";

    [ObservableProperty]
    private string? searchQuery;

    [ObservableProperty]
    private ObservableCollection<SoundCloudSearchItemViewModel> searchResults = [];

    [ObservableProperty]
    private bool isSearching;

    [ObservableProperty]
    private bool hasResults;

    private readonly SoundCloudTrackInfoPanelViewModel _trackInfoPanel;
    private SoundCloudSearchItemViewModel? _selectedItem;

    public SoundCloudSearchListViewModel(SoundCloudTrackInfoPanelViewModel trackInfoPanelViewModel)
    {
        _trackInfoPanel = trackInfoPanelViewModel;
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
            return;

        IsSearching = true;
        SearchResults.Clear();

        try
        {
            // TODO: Implement actual SoundCloud search
            await Task.Delay(100); // Placeholder for actual API call
        }
        finally
        {
            IsSearching = false;
            HasResults = SearchResults.Count > 0;
        }
    }

    public void OnSelectItem(SoundCloudSearchItemViewModel item)
    {
        _selectedItem = item;

        _trackInfoPanel.SetTrackInfo(
            fileName: null,
            title: item.Title,
            artist: item.Artist,
            album: item.Album,
            year: item.Year,
            genre: item.Genre,
            comment: null,
            composer: null,
            albumArtist: null,
            track: null,
            duration: item.Duration,
            albumCover: item.AlbumCover
        );
    }
}

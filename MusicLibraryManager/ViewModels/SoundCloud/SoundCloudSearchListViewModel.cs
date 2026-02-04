using System.Collections.ObjectModel;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml.Media.Imaging;
using MusicLibraryManager.Services;

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
    private readonly ISoundCloudService _soundCloudService;
    private SoundCloudSearchItemViewModel? _selectedItem;

    public SoundCloudSearchListViewModel(
        SoundCloudTrackInfoPanelViewModel trackInfoPanelViewModel,
        ISoundCloudService soundCloudService)
    {
        _trackInfoPanel = trackInfoPanelViewModel;
        _soundCloudService = soundCloudService;
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
            var tracks = await _soundCloudService.SearchTracksAsync(SearchQuery, limit: 50);

            foreach (var track in tracks)
            {
                var item = new SoundCloudSearchItemViewModel(OnSelectItem)
                {
                    Title = track.Title,
                    Artist = track.Artist,
                    Genre = track.Genre,
                    Duration = track.FormattedDuration,
                    Year = track.Year,
                    TrackUrl = track.PermalinkUrl,
                    Album = null // SoundCloud doesn't have albums
                };

                // Load artwork asynchronously
                if (!string.IsNullOrEmpty(track.ArtworkUrl))
                {
                    _ = LoadArtworkAsync(item, track.ArtworkUrl);
                }

                SearchResults.Add(item);
            }
        }
        finally
        {
            IsSearching = false;
            HasResults = SearchResults.Count > 0;
        }
    }

    private static async Task LoadArtworkAsync(SoundCloudSearchItemViewModel item, string artworkUrl)
    {
        try
        {
            using var httpClient = new HttpClient();
            var imageBytes = await httpClient.GetByteArrayAsync(artworkUrl);
            item.AlbumCoverData = imageBytes;

            var bitmap = new BitmapImage();
            using var stream = new MemoryStream(imageBytes);
            bitmap.SetSource(stream.AsRandomAccessStream());
            item.AlbumCover = bitmap;
        }
        catch
        {
            // Ignore artwork loading failures
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
            albumCover: item.AlbumCover,
            albumCoverData: item.AlbumCoverData
        );
    }
}

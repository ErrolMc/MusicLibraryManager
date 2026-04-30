using System.Collections.ObjectModel;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml.Media.Imaging;
using MusicLibraryManager.Models;
using MusicLibraryManager.Services;

namespace MusicLibraryManager.ViewModels.SoundCloud;

public partial class SoundCloudSearchListViewModel : ObservableObject
{
    [ObservableProperty]
    private long? selectedPlaylistId;

    [ObservableProperty]
    private string? selectedPlaylistTitle = "Select SoundCloud Playlist";

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
    private readonly ITrackMatchingService _trackMatchingService;
    private SoundCloudSearchItemViewModel? _selectedItem;

    public SoundCloudSearchListViewModel(
        SoundCloudTrackInfoPanelViewModel trackInfoPanelViewModel,
        ISoundCloudService soundCloudService,
        ITrackMatchingService trackMatchingService)
    {
        _trackInfoPanel = trackInfoPanelViewModel;
        _soundCloudService = soundCloudService;
        _trackMatchingService = trackMatchingService;
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
                SearchResults.Add(CreateItem(track));
            }
        }
        finally
        {
            IsSearching = false;
            HasResults = SearchResults.Count > 0;
        }
    }

    public async Task AlignWithLocalTracksAsync(
        IReadOnlyList<TrackListItemViewModel> localTracks,
        IReadOnlyList<SoundCloudTrack> soundCloudTracks)
    {
        IsSearching = true;
        SearchResults.Clear();

        try
        {
            if (soundCloudTracks.Count == 0)
            {
                HasResults = false;
                return;
            }

            var candidateItems = soundCloudTracks.Select(CreateItem).ToList();
            var aligned = await _trackMatchingService.AlignAllTracksAsync(localTracks, candidateItems);
            foreach (var item in aligned)
            {
                SearchResults.Add(item);
            }
        }
        finally
        {
            IsSearching = false;
            HasResults = SearchResults.Count > 0;
        }
    }

    public async Task LoadPlaylistTracksAsync(IReadOnlyList<SoundCloudTrack> tracks)
    {
        IsSearching = true;
        SearchResults.Clear();

        try
        {
            foreach (var track in tracks)
            {
                SearchResults.Add(CreateItem(track));
            }
        }
        finally
        {
            IsSearching = false;
            HasResults = SearchResults.Count > 0;
        }
    }

    private SoundCloudSearchItemViewModel CreateItem(SoundCloudTrack track)
    {
        var item = new SoundCloudSearchItemViewModel(OnSelectItem)
        {
            Title = track.Title,
            Artist = track.Artist,
            Genre = track.Genre,
            Duration = track.FormattedDuration,
            Year = track.Year,
            TrackUrl = track.PermalinkUrl,
            TrackId = track.Id,
            Album = null,
            IsGap = false
        };

        if (!string.IsNullOrEmpty(track.ArtworkUrl))
        {
            _ = LoadArtworkAsync(item, track.ArtworkUrl);
        }

        return item;
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
            albumCoverData: item.AlbumCoverData,
            trackId: item.TrackId
        );
    }

    public void Clear()
    {
        SearchQuery = null;
        SearchResults.Clear();
        HasResults = false;
        _selectedItem = null;
        _trackInfoPanel.ClearTrackInfo();
    }
}

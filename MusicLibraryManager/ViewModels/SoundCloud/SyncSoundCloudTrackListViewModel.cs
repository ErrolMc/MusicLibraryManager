using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Media.Imaging;
using MusicLibraryManager.Models;
using MusicLibraryManager.Services;

namespace MusicLibraryManager.ViewModels.SoundCloud;

public partial class SyncSoundCloudTrackListViewModel : ObservableObject
{
    [ObservableProperty]
    private long? selectedPlaylistId;

    [ObservableProperty]
    private string? selectedPlaylistTitle = "Select SoundCloud Playlist";

    [ObservableProperty]
    private ObservableCollection<SoundCloudSearchItemViewModel> searchResults = [];

    [ObservableProperty]
    private bool isSearching;

    [ObservableProperty]
    private bool hasResults;

    [ObservableProperty]
    private bool isUpdatingPlaylist;

    private string _playlistUpdateMessage = "Updating SoundCloud playlist...";

    public string PlaylistUpdateMessage
    {
        get => _playlistUpdateMessage;
        set => SetProperty(ref _playlistUpdateMessage, value);
    }

    [ObservableProperty]
    private bool isInsertPaletteDragging;

    [ObservableProperty]
    private bool isNewInsertDragging;

    [ObservableProperty]
    private int? dropTargetIndex;

    private readonly SyncSoundCloudTrackInfoPanelViewModel _trackInfoPanel;
    private readonly ITrackMatchingService _trackMatchingService;
    private readonly ISoundCloudService _soundCloudService;
    private readonly ILogger<SyncSoundCloudTrackListViewModel> _logger;
    private SoundCloudSearchItemViewModel? _selectedItem;
    private SoundCloudSearchItemViewModel? _draggedItem;
    private long _nextManualPlaceholderTrackId = -1;
    private CancellationTokenSource? _persistPlaylistCts;

    public SyncSoundCloudTrackListViewModel(
        SyncSoundCloudTrackInfoPanelViewModel trackInfoPanelViewModel,
        ITrackMatchingService trackMatchingService,
        ISoundCloudService soundCloudService,
        ILogger<SyncSoundCloudTrackListViewModel> logger)
    {
        _trackInfoPanel = trackInfoPanelViewModel;
        _trackMatchingService = trackMatchingService;
        _soundCloudService = soundCloudService;
        _logger = logger;
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

        await Task.CompletedTask;
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
        SearchResults.Clear();
        HasResults = false;
        _selectedItem = null;
        _trackInfoPanel.ClearTrackInfo();
    }

    [RelayCommand]
    public async Task RemoveTrackFromPlaylistAsync(SoundCloudSearchItemViewModel? item)
    {
        if (item is null || item.IsGap || item.TrackId <= 0 || SelectedPlaylistId is null)
        {
            return;
        }

        IsUpdatingPlaylist = true;
        PlaylistUpdateMessage = "Removing track from SoundCloud playlist...";
        try
        {
            var removed = await _soundCloudService.RemoveTrackFromPlaylistAsync(SelectedPlaylistId.Value, item.TrackId);
            if (!removed)
            {
                return;
            }

            var tracks = await _soundCloudService.GetPlaylistTracksAsync(SelectedPlaylistId.Value);
            await LoadPlaylistTracksAsync(tracks);
        }
        finally
        {
            IsUpdatingPlaylist = false;
            PlaylistUpdateMessage = "Updating SoundCloud playlist...";
        }
    }

    public void RemovePlaceholder(SoundCloudSearchItemViewModel item)
    {
        if (!item.IsManualPlaceholder)
        {
            return;
        }

        SearchResults.Remove(item);
        ReindexDisplayRows();
    }

    public void BeginInsertPaletteDrag()
    {
        if (IsInsertPaletteDragging || IsNewInsertDragging)
        {
            return;
        }

        // Insert a semi-transparent preview placeholder that will move as the user drags
        var preview = CreateManualPlaceholder();
        preview.ItemOpacity = 0.45;
        SearchResults.Add(preview);
        ReindexDisplayRows();
        _draggedItem = preview;

        IsNewInsertDragging = true;
        IsInsertPaletteDragging = false;
        DropTargetIndex = SearchResults.Count - 1;
    }

    public void BeginExistingPlaceholderDrag(SoundCloudSearchItemViewModel item)
    {
        if (!item.IsManualPlaceholder)
        {
            return;
        }

        _draggedItem = item;
        _draggedItem.ItemOpacity = 0.45;
        IsInsertPaletteDragging = true;
        DropTargetIndex = SearchResults.IndexOf(item);
    }

    public void EndInsertPaletteDrag()
    {
        if (_draggedItem is not null)
        {
            _draggedItem.ItemOpacity = 1.0;
        }

        // If this was a fresh drag that was cancelled (not committed), remove the preview row
        if (IsNewInsertDragging && _draggedItem is not null && _draggedItem.IsManualPlaceholder)
        {
            SearchResults.Remove(_draggedItem);
            ReindexDisplayRows();
        }

        IsInsertPaletteDragging = false;
        IsNewInsertDragging = false;
        DropTargetIndex = null;
        _draggedItem = null;
    }

    public bool IsReorderCandidate(SoundCloudSearchItemViewModel item)
    {
        return item is not null && !item.IsGap && !item.IsManualPlaceholder;
    }

    public void BeginTrackReorderDrag(SoundCloudSearchItemViewModel item)
    {
        if (!IsReorderCandidate(item))
        {
            return;
        }

        _draggedItem = item;
        _draggedItem.ItemOpacity = 0.45;
        IsInsertPaletteDragging = true;
        IsNewInsertDragging = false;
        DropTargetIndex = SearchResults.IndexOf(item);
    }

    public async Task<bool> CompleteTrackReorderDragAsync()
    {
        var hadTrackDrag = _draggedItem is not null && !_draggedItem.IsManualPlaceholder;
        EndInsertPaletteDrag();

        if (!hadTrackDrag)
        {
            return true;
        }

        return await PersistCurrentPlaylistOrderAsync();
    }

    public void SetDropTargetIndex(int? index)
    {
        if (_draggedItem is null || index is not int targetIndex)
        {
            DropTargetIndex = index;
            return;
        }

        // Both fresh inserts and existing placeholder re-drags use the same live-move logic
        MoveDraggedItem(targetIndex);
    }

    public void CommitNewInsert()
    {
        if (!IsNewInsertDragging || _draggedItem is null)
        {
            return;
        }

        // The preview row is already in the right place — just make it fully opaque/permanent
        _draggedItem.ItemOpacity = 1.0;

        // Prevent EndInsertPaletteDrag from removing it
        var committed = _draggedItem;
        _draggedItem = null;

        IsNewInsertDragging = false;
        DropTargetIndex = null;
    }

    public bool TryReplacePlaceholderWithTrack(long placeholderTrackId, SoundCloudSearchItemViewModel sourceTrack)
    {
        if (sourceTrack is null || sourceTrack.TrackId <= 0)
        {
            return false;
        }

        var targetIndex = -1;
        for (var i = 0; i < SearchResults.Count; i++)
        {
            var candidate = SearchResults[i];
            if (candidate.IsManualPlaceholder && candidate.TrackId == placeholderTrackId)
            {
                targetIndex = i;
                break;
            }
        }

        if (targetIndex < 0)
        {
            return false;
        }

        var replacement = new SoundCloudSearchItemViewModel(OnSelectItem)
        {
            Title = sourceTrack.Title,
            Artist = sourceTrack.Artist,
            Album = sourceTrack.Album,
            Year = sourceTrack.Year,
            Genre = sourceTrack.Genre,
            Duration = sourceTrack.Duration,
            TrackUrl = sourceTrack.TrackUrl,
            TrackId = sourceTrack.TrackId,
            AlbumCover = sourceTrack.AlbumCover,
            AlbumCoverData = sourceTrack.AlbumCoverData,
            IsGap = false,
            IsManualPlaceholder = false,
            IsUnmatched = sourceTrack.IsUnmatched,
            ItemOpacity = 1.0
        };

        SearchResults[targetIndex] = replacement;
        ReindexDisplayRows();
        return true;
    }

    public IReadOnlyList<long> GetPersistedPlaylistTrackIds()
    {
        return SearchResults
            .Where(item => !item.IsManualPlaceholder && !item.IsGap && item.TrackId > 0)
            .Select(item => item.TrackId)
            .ToList();
    }

    public async Task<bool> PersistCurrentPlaylistOrderAsync()
    {
        if (SelectedPlaylistId is not long playlistId || playlistId <= 0)
        {
            return false;
        }

        var previousCts = _persistPlaylistCts;
        var nextCts = new CancellationTokenSource();
        _persistPlaylistCts = nextCts;
        previousCts?.Cancel();
        previousCts?.Dispose();
        var token = nextCts.Token;

        var persistedTrackIds = GetPersistedPlaylistTrackIds();
        try
        {
            IsUpdatingPlaylist = true;
            PlaylistUpdateMessage = "Updating SoundCloud playlist order...";
            var persisted = await _soundCloudService.ReplacePlaylistTracksAsync(playlistId, persistedTrackIds, token);
            if (!persisted)
            {
                return false;
            }

            _logger.LogInformation("[Sync] Persisted reordered SoundCloud playlist. PlaylistId={PlaylistId}, TrackCount={Count}", playlistId, persistedTrackIds.Count);
            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("[Sync] Playlist reorder persist canceled. PlaylistId={PlaylistId}", playlistId);
            return false;
        }
        finally
        {
            if (!ReferenceEquals(_persistPlaylistCts, nextCts))
            {
                // newer request owns the active CTS
            }
            else
            {
                nextCts.Dispose();
                _persistPlaylistCts = null;
                IsUpdatingPlaylist = false;
                PlaylistUpdateMessage = "Updating SoundCloud playlist...";
            }
        }
    }

    private void MoveDraggedItem(int targetIndex)
    {
        if (_draggedItem is null)
        {
            return;
        }

        var boundedTarget = Math.Max(0, Math.Min(targetIndex, SearchResults.Count));
        var currentIndex = SearchResults.IndexOf(_draggedItem);
        if (currentIndex < 0)
        {
            return;
        }

        if (boundedTarget > currentIndex)
        {
            boundedTarget--;
        }

        boundedTarget = Math.Max(0, Math.Min(boundedTarget, SearchResults.Count - 1));
        if (currentIndex == boundedTarget)
        {
            DropTargetIndex = boundedTarget;
            return;
        }

        SearchResults.Move(currentIndex, boundedTarget);
        DropTargetIndex = boundedTarget;
        ReindexDisplayRows();
    }

    private SoundCloudSearchItemViewModel CreateManualPlaceholder()
    {
        return new SoundCloudSearchItemViewModel(OnSelectItem)
        {
            Title = "Manual item",
            Artist = "Pending",
            Duration = string.Empty,
            IsManualPlaceholder = true,
            IsGap = false,
            IsUnmatched = true,
            TrackId = _nextManualPlaceholderTrackId--
        };
    }

    private void ReindexDisplayRows()
    {
        var displayIndex = 1;
        foreach (var item in SearchResults)
        {
            item.DisplayIndex = item.IsGap ? 0 : displayIndex;
            if (!item.IsGap)
            {
                displayIndex++;
            }
        }
    }
}

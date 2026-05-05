using MusicLibraryManager.ViewModels.SoundCloud;
using MusicLibraryManager.Models;
using MusicLibraryManager.Services;
using Microsoft.Extensions.Logging;
using System.Collections.Specialized;

namespace MusicLibraryManager.ViewModels;

public partial class SyncPlaylistPanelViewModel : ObservableObject
{
    public const string LocalTracksTitle = "Local Tracks";

    public MenuBarViewModel MenuBarViewModel { get; }
    public TrackListPanelViewModel TrackListPanelViewModel { get; }
    public TrackInfoPanelViewModel TrackInfoPanelViewModel { get; }
    public SyncSoundCloudTrackListViewModel SyncSoundCloudTrackListViewModel { get; }
    public SyncSoundCloudTrackInfoPanelViewModel SoundCloudTrackInfoPanelViewModel { get; }

    private readonly ISoundCloudService _soundCloudService;
    private readonly ISoundCloudAuthService _soundCloudAuthService;
    private readonly ITrackMatchingService _trackMatchingService;
    private readonly ILogger<SyncPlaylistPanelViewModel> _logger;

    [ObservableProperty]
    private long? selectedPlaylistId;

    [ObservableProperty]
    private bool isSyncingToSoundCloud;

    public bool IsSyncButtonEnabled => !IsSyncingToSoundCloud && SelectedPlaylistId is not null;

    public bool IsBusyOverlayVisible =>
        IsSyncingToSoundCloud || SyncSoundCloudTrackListViewModel.IsSearching || SyncSoundCloudTrackListViewModel.IsUpdatingPlaylist;

    public string BusyOverlayMessage => IsSyncingToSoundCloud
        ? "Syncing local playlist to SoundCloud..."
        : SyncSoundCloudTrackListViewModel.IsUpdatingPlaylist
            ? "Removing track from SoundCloud playlist..."
            : "Matching tracks with AI model...";

    partial void OnSelectedPlaylistIdChanged(long? value)
    {
        OnPropertyChanged(nameof(IsSyncButtonEnabled));
    }

    partial void OnIsSyncingToSoundCloudChanged(bool value)
    {
        OnPropertyChanged(nameof(IsSyncButtonEnabled));
        OnPropertyChanged(nameof(IsBusyOverlayVisible));
        OnPropertyChanged(nameof(BusyOverlayMessage));
    }

    [ObservableProperty]
    private string? selectedPlaylistTitle = "Select SoundCloud Playlist";

    public bool IsSelectSoundCloudPlaylistEnabled => TrackListPanelViewModel.Tracks.Count > 0;
    public bool IsAiMatchEnabled => SelectedPlaylistId is not null && TrackListPanelViewModel.Tracks.Count > 0 && !SyncSoundCloudTrackListViewModel.IsSearching;
    public bool IsShowPlaylistOrderEnabled => SelectedPlaylistId is not null && !SyncSoundCloudTrackListViewModel.IsSearching;

    public string SoundCloudPlaylistTitle =>
        string.IsNullOrWhiteSpace(SelectedPlaylistTitle) || SelectedPlaylistTitle == "Select SoundCloud Playlist"
            ? "SoundCloud Playlist"
            : SelectedPlaylistTitle;

    partial void OnSelectedPlaylistTitleChanged(string? value)
    {
        OnPropertyChanged(nameof(SoundCloudPlaylistTitle));
    }

    public SyncPlaylistPanelViewModel(
        MenuBarViewModel menuBarViewModel,
        TrackListPanelViewModel trackListPanelViewModel,
        TrackInfoPanelViewModel trackInfoPanelViewModel,
        SyncSoundCloudTrackListViewModel syncSoundCloudTrackListViewModel,
        SyncSoundCloudTrackInfoPanelViewModel soundCloudTrackInfoPanelViewModel,
        ISoundCloudService soundCloudService,
        ISoundCloudAuthService soundCloudAuthService,
        ITrackMatchingService trackMatchingService,
        ILogger<SyncPlaylistPanelViewModel> logger)
    {
        MenuBarViewModel = menuBarViewModel;
        TrackListPanelViewModel = trackListPanelViewModel;
        TrackInfoPanelViewModel = trackInfoPanelViewModel;
        SyncSoundCloudTrackListViewModel = syncSoundCloudTrackListViewModel;
        SoundCloudTrackInfoPanelViewModel = soundCloudTrackInfoPanelViewModel;
        _soundCloudService = soundCloudService;
        _soundCloudAuthService = soundCloudAuthService;
        _trackMatchingService = trackMatchingService;
        _logger = logger;

        SelectedPlaylistId = SyncSoundCloudTrackListViewModel.SelectedPlaylistId;
        if (!string.IsNullOrWhiteSpace(SyncSoundCloudTrackListViewModel.SelectedPlaylistTitle))
        {
            SelectedPlaylistTitle = SyncSoundCloudTrackListViewModel.SelectedPlaylistTitle;
        }

        SyncSoundCloudTrackListViewModel.PropertyChanged += SoundCloudSearchListViewModel_PropertyChanged;
        TrackListPanelViewModel.Tracks.CollectionChanged += Tracks_CollectionChanged;
    }

    private void SoundCloudSearchListViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SyncSoundCloudTrackListViewModel.SelectedPlaylistId))
        {
            SelectedPlaylistId = SyncSoundCloudTrackListViewModel.SelectedPlaylistId;
        }

        if (e.PropertyName == nameof(SyncSoundCloudTrackListViewModel.SelectedPlaylistTitle))
        {
            SelectedPlaylistTitle = SyncSoundCloudTrackListViewModel.SelectedPlaylistTitle;
        }

        if (e.PropertyName == nameof(SyncSoundCloudTrackListViewModel.IsSearching))
        {
            OnPropertyChanged(nameof(IsBusyOverlayVisible));
            OnPropertyChanged(nameof(BusyOverlayMessage));
            OnPropertyChanged(nameof(IsAiMatchEnabled));
            OnPropertyChanged(nameof(IsShowPlaylistOrderEnabled));
        }

        if (e.PropertyName == nameof(SyncSoundCloudTrackListViewModel.IsUpdatingPlaylist))
        {
            OnPropertyChanged(nameof(IsBusyOverlayVisible));
            OnPropertyChanged(nameof(BusyOverlayMessage));
            OnPropertyChanged(nameof(IsAiMatchEnabled));
            OnPropertyChanged(nameof(IsShowPlaylistOrderEnabled));
        }
    }

    private void Tracks_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(IsSelectSoundCloudPlaylistEnabled));
        OnPropertyChanged(nameof(IsAiMatchEnabled));
    }

    [RelayCommand]
    private async Task ShowPlaylistOrderAsync()
    {
        var playlistId = SelectedPlaylistId ?? SyncSoundCloudTrackListViewModel.SelectedPlaylistId;
        if (playlistId is null)
        {
            _logger.LogWarning("[Sync] Show playlist order aborted: no selected playlist id.");
            return;
        }

        var tracks = await _soundCloudService.GetPlaylistTracksAsync(playlistId.Value);
        _logger.LogInformation("[Sync] Loaded default SoundCloud playlist order. PlaylistId={PlaylistId}, TrackCount={Count}", playlistId.Value, tracks.Count);
        await SyncSoundCloudTrackListViewModel.LoadPlaylistTracksAsync(tracks);
    }

    [RelayCommand]
    private async Task RunAiMatchingAsync()
    {
        var playlistId = SelectedPlaylistId ?? SyncSoundCloudTrackListViewModel.SelectedPlaylistId;
        if (playlistId is null)
        {
            _logger.LogWarning("[Sync] AI matching aborted: no selected playlist id.");
            return;
        }

        if (TrackListPanelViewModel.Tracks.Count == 0)
        {
            _logger.LogWarning("[Sync] AI matching aborted: no local tracks loaded.");
            return;
        }

        try
        {
            var tracks = await _soundCloudService.GetPlaylistTracksAsync(playlistId.Value);
            _logger.LogInformation("[Sync] Running AI alignment. PlaylistId={PlaylistId}, PlaylistTrackCount={Count}", playlistId.Value, tracks.Count);
            await SyncSoundCloudTrackListViewModel.AlignWithLocalTracksAsync(TrackListPanelViewModel.Tracks, tracks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Sync] AI matching failed.");
            throw;
        }
    }

    [RelayCommand]
    private async Task SyncLocalPlaylistToSoundCloudAsync()
    {
        _logger.LogInformation("[Sync] SyncLocalPlaylistToSoundCloud started.");

        var playlistId = SelectedPlaylistId ?? SyncSoundCloudTrackListViewModel.SelectedPlaylistId;
        if (playlistId is null)
        {
            _logger.LogWarning("[Sync] Aborted: no selected playlist id.");
            return;
        }

        if (TrackListPanelViewModel.Tracks.Count == 0)
        {
            _logger.LogWarning("[Sync] Aborted: no local tracks loaded.");
            return;
        }

        IsSyncingToSoundCloud = true;
        try
        {
            if (!_soundCloudAuthService.IsAuthenticated)
            {
                var authenticated = await _soundCloudAuthService.StartOAuthFlowAsync();
                if (!authenticated)
                {
                    return;
                }
            }

            var selectedPlaylistIdValue = playlistId.Value;
            var playlistTracks = await _soundCloudService.GetPlaylistTracksAsync(selectedPlaylistIdValue);
            _logger.LogInformation("[Sync] Loaded playlist tracks. PlaylistId={PlaylistId}, ExistingTracks={Count}", selectedPlaylistIdValue, playlistTracks.Count);

            var existingIds = playlistTracks.Select(t => t.Id).ToHashSet();
            var existingCandidates = playlistTracks.Select(track => new SoundCloudSearchItemViewModel
            {
                Title = track.Title,
                Artist = track.Artist,
                Album = null,
                Duration = track.FormattedDuration,
                Genre = track.Genre,
                Year = track.Year,
                TrackUrl = track.PermalinkUrl,
                TrackId = track.Id,
                IsGap = false
            }).ToList();

            var toAppend = new HashSet<long>();
            var searchCache = new Dictionary<string, IReadOnlyList<SoundCloudTrack>>(StringComparer.OrdinalIgnoreCase);
            var skippedAlreadyMatched = 0;
            var searchedTracks = 0;

            foreach (var localTrack in TrackListPanelViewModel.Tracks)
            {
                var existingMatch = _trackMatchingService.FindBestMatch(localTrack, existingCandidates);
                if (existingMatch is not null && existingIds.Contains(existingMatch.TrackId))
                {
                    skippedAlreadyMatched++;
                    continue;
                }

                var query = string.Join(" ", new[] { localTrack.SongName, localTrack.Artist }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();
                if (string.IsNullOrWhiteSpace(query))
                {
                    query = localTrack.FileNameWithoutExtension;
                }

                if (string.IsNullOrWhiteSpace(query))
                {
                    continue;
                }

                if (!searchCache.TryGetValue(query, out var searchResults))
                {
                    searchResults = await _soundCloudService.SearchTracksAsync(query, limit: 12);
                    searchCache[query] = searchResults;
                }

                searchedTracks++;

                if (searchResults.Count == 0)
                {
                    continue;
                }

                var candidates = searchResults.Select(track => new SoundCloudSearchItemViewModel
                {
                    Title = track.Title,
                    Artist = track.Artist,
                    Album = null,
                    Duration = track.FormattedDuration,
                    Genre = track.Genre,
                    Year = track.Year,
                    TrackUrl = track.PermalinkUrl,
                    TrackId = track.Id,
                    IsGap = false
                }).ToList();

                var bestMatch = _trackMatchingService.FindBestMatch(localTrack, candidates);
                if (bestMatch is null || bestMatch.TrackId <= 0 || existingIds.Contains(bestMatch.TrackId))
                {
                    continue;
                }

                toAppend.Add(bestMatch.TrackId);
            }

            _logger.LogInformation("[Sync] Matching summary. LocalTracks={Local}, Searched={Searched}, SkippedAlreadyMatched={Skipped}, ToAppend={ToAppend}",
                TrackListPanelViewModel.Tracks.Count,
                searchedTracks,
                skippedAlreadyMatched,
                toAppend.Count);

            if (toAppend.Count > 0)
            {
                var updated = await _soundCloudService.AppendTracksToPlaylistAsync(selectedPlaylistIdValue, toAppend.ToList());
                if (!updated)
                {
                    _logger.LogWarning("[Sync] Playlist append failed. PlaylistId={PlaylistId}", selectedPlaylistIdValue);
                    return;
                }

                _logger.LogInformation("[Sync] Playlist append succeeded. PlaylistId={PlaylistId}, AddedTracks={Count}", selectedPlaylistIdValue, toAppend.Count);
            }
            else
            {
                _logger.LogInformation("[Sync] No tracks needed to be appended.");
            }

            var updatedPlaylistTracks = await _soundCloudService.GetPlaylistTracksAsync(selectedPlaylistIdValue);
            await SyncSoundCloudTrackListViewModel.AlignWithLocalTracksAsync(TrackListPanelViewModel.Tracks, updatedPlaylistTracks);
            _logger.LogInformation("[Sync] SyncLocalPlaylistToSoundCloud completed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Sync] SyncLocalPlaylistToSoundCloud failed.");
            throw;
        }
        finally
        {
            IsSyncingToSoundCloud = false;
        }
    }
}

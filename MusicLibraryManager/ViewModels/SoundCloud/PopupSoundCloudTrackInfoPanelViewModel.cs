using Microsoft.Extensions.Logging;
using MusicLibraryManager.Services;

namespace MusicLibraryManager.ViewModels.SoundCloud;

public sealed class PopupSoundCloudTrackInfoPanelViewModel : SoundCloudTrackInfoPanelViewModel
{
    private readonly SyncSoundCloudTrackListViewModel _syncSoundCloudTrackListViewModel;
    private readonly ILogger<PopupSoundCloudTrackInfoPanelViewModel> _logger;

    public PopupSoundCloudTrackInfoPanelViewModel(
        IImageService imageService,
        ISoundCloudPlaybackService playbackService,
        SyncSoundCloudTrackListViewModel syncSoundCloudTrackListViewModel,
        ILogger<PopupSoundCloudTrackInfoPanelViewModel> logger)
        : base(imageService, playbackService)
    {
        _syncSoundCloudTrackListViewModel = syncSoundCloudTrackListViewModel;
        _logger = logger;
    }

    public async Task<bool> TryApplyToPlaceholderAsync(long placeholderTrackId)
    {
        if (!IsPickPlaceholderMode || CurrentTrackId <= 0)
        {
            return false;
        }

        if (_syncSoundCloudTrackListViewModel.SelectedPlaylistId is not long playlistId || playlistId <= 0)
        {
            return false;
        }

        var source = new SoundCloudSearchItemViewModel
        {
            Title = Title,
            Artist = Artist,
            Album = Album,
            Year = Year,
            Genre = Genre,
            Duration = Duration,
            TrackId = CurrentTrackId,
            AlbumCover = AlbumCover,
            AlbumCoverData = AlbumCoverData,
            IsGap = false,
            IsManualPlaceholder = false
        };

        var appliedInUi = _syncSoundCloudTrackListViewModel.TryReplacePlaceholderWithTrack(placeholderTrackId, source);
        if (!appliedInUi)
        {
            return false;
        }

        var persisted = await _syncSoundCloudTrackListViewModel.PersistCurrentPlaylistOrderAsync();
        if (persisted)
        {
            IsPickPlaceholderMode = false;
            _logger.LogInformation("[PopupSync] Replaced placeholder {PlaceholderTrackId} from popup selected track {TrackId}.", placeholderTrackId, CurrentTrackId);
            return true;
        }

        _logger.LogWarning("[PopupSync] Failed to persist playlist update after replacing placeholder {PlaceholderTrackId}. PlaylistId={PlaylistId}", placeholderTrackId, playlistId);
        return false;
    }

    protected override void SyncToPlaceholder()
    {
        if (!IsSyncToPlaceholderEnabled || !HasTrackLoaded || CurrentTrackId <= 0)
        {
            return;
        }

        IsPickPlaceholderMode = true;
    }
}

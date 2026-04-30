using MusicLibraryManager.ViewModels;
using MusicLibraryManager.ViewModels.SoundCloud;

namespace MusicLibraryManager.Services;

public interface ITrackMatchingService
{
    SoundCloudSearchItemViewModel? FindBestMatch(
        TrackListItemViewModel local,
        IReadOnlyList<SoundCloudSearchItemViewModel> candidates);

    Task<IReadOnlyList<SoundCloudSearchItemViewModel>> AlignAllTracksAsync(
        IReadOnlyList<TrackListItemViewModel> localTracks,
        IReadOnlyList<SoundCloudSearchItemViewModel> soundCloudTracks);
}

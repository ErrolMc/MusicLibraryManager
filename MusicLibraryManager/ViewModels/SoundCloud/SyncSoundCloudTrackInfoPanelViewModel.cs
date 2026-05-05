using MusicLibraryManager.Services;

namespace MusicLibraryManager.ViewModels.SoundCloud;

public sealed class SyncSoundCloudTrackInfoPanelViewModel : SoundCloudTrackInfoPanelViewModel
{
    public SyncSoundCloudTrackInfoPanelViewModel(
        IImageService imageService,
        ISoundCloudPlaybackService playbackService)
        : base(imageService, playbackService)
    {
    }
}

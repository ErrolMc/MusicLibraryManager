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

    protected override void SyncToPlaceholder()
    {
        // Only popup panel supports syncing selected track to placeholders.
    }
}

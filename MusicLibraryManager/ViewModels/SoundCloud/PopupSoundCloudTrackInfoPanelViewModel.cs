using MusicLibraryManager.Services;

namespace MusicLibraryManager.ViewModels.SoundCloud;

public sealed class PopupSoundCloudTrackInfoPanelViewModel : SoundCloudTrackInfoPanelViewModel
{
    public PopupSoundCloudTrackInfoPanelViewModel(
        IImageService imageService,
        ISoundCloudPlaybackService playbackService)
        : base(imageService, playbackService)
    {
    }
}

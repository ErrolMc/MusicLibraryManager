namespace MusicLibraryManager.ViewModels.SoundCloud;

public partial class SoundCloudSplitPanelViewModel : ObservableObject
{
    public SoundCloudSearchListViewModel SoundCloudSearchListViewModel { get; }
    public SoundCloudTrackInfoPanelViewModel SoundCloudTrackInfoPanelViewModel { get; }

    public SoundCloudSplitPanelViewModel(
        SoundCloudSearchListViewModel soundCloudSearchListViewModel,
        SoundCloudTrackInfoPanelViewModel soundCloudTrackInfoPanelViewModel)
    {
        SoundCloudSearchListViewModel = soundCloudSearchListViewModel;
        SoundCloudTrackInfoPanelViewModel = soundCloudTrackInfoPanelViewModel;
    }
}

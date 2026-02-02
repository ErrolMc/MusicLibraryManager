namespace MusicLibraryManager.ViewModels;

public partial class SplitPanelViewModel : ObservableObject
{
    public MenuBarViewModel MenuBarViewModel { get; }
    public TrackListPanelViewModel TrackListPanelViewModel { get; }
    public TrackInfoPanelViewModel TrackInfoPanelViewModel { get; }

    public SplitPanelViewModel(MenuBarViewModel menuBarViewModel, TrackListPanelViewModel trackListPanelViewModel, TrackInfoPanelViewModel trackInfoPanelViewModel)
    {
        MenuBarViewModel = menuBarViewModel;
        TrackListPanelViewModel = trackListPanelViewModel;
        TrackInfoPanelViewModel = trackInfoPanelViewModel;
    }
}

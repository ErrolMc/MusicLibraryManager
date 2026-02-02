namespace MusicLibraryManager.Presentation;

public sealed partial class TrackInfoPanel : UserControl
{
    public TrackInfoPanel()
    {
        this.InitializeComponent();
        this.DataContext = new TrackInfoPanelViewModel();
    }
}

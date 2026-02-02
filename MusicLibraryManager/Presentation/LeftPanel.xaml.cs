namespace MusicLibraryManager.Presentation;

public sealed partial class LeftPanel : UserControl
{
    public LeftPanel()
    {
        this.InitializeComponent();
        this.DataContext = new LeftPanelViewModel();
    }
}

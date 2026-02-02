namespace MusicLibraryManager.Presentation;

public sealed partial class RightPanel : UserControl
{
    public RightPanel()
    {
        this.InitializeComponent();
        this.DataContext = new RightPanelViewModel();
    }
}

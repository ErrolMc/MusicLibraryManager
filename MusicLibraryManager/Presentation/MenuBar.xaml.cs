namespace MusicLibraryManager.Presentation;

public sealed partial class MenuBar : UserControl
{
    public MenuBar()
    {
        this.InitializeComponent();
        this.DataContext = new MenuBarViewModel();
    }
}

namespace MusicLibraryManager.ViewModels;

public partial class HomePanelViewModel : ObservableObject
{
    public MenuBarViewModel MenuBarViewModel { get; }

    public HomePanelViewModel(MenuBarViewModel menuBarViewModel)
    {
        MenuBarViewModel = menuBarViewModel;
    }
}

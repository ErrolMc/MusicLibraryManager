namespace MusicLibraryManager.ViewModels;

public partial class RightPanelViewModel : ObservableObject
{
    [ObservableProperty]
    private string title = "Right Panel";

    [ObservableProperty]
    private string content = "Right panel content goes here";
}

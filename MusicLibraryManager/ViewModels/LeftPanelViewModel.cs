namespace MusicLibraryManager.ViewModels;

public partial class LeftPanelViewModel : ObservableObject
{
    [ObservableProperty]
    private string title = "Left Panel";

    [ObservableProperty]
    private string content = "Left panel content goes here";
}

namespace MusicLibraryManager.ViewModels;

public partial class MenuBarViewModel : ObservableObject
{
    [ObservableProperty]
    private string title = "Split Panel View";

    public ICommand NewCommand { get; }
    public ICommand OpenCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand SettingsCommand { get; }

    public MenuBarViewModel()
    {
        NewCommand = new RelayCommand(OnNew);
        OpenCommand = new RelayCommand(OnOpen);
        SaveCommand = new RelayCommand(OnSave);
        SettingsCommand = new RelayCommand(OnSettings);
    }

    private void OnNew()
    {
        // TODO: Implement new action
    }

    private void OnOpen()
    {
        // TODO: Implement open action
    }

    private void OnSave()
    {
        // TODO: Implement save action
    }

    private void OnSettings()
    {
        // TODO: Implement settings action
    }
}

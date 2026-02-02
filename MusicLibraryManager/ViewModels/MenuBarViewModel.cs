using System.Diagnostics;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace MusicLibraryManager.ViewModels;

public partial class MenuBarViewModel : ObservableObject
{
    private readonly TrackListPanelViewModel _trackListPanelViewModel;

    [ObservableProperty]
    private string title = "Split Panel View";

    public ICommand NewCommand { get; }
    public ICommand OpenCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand SettingsCommand { get; }

    public MenuBarViewModel(TrackListPanelViewModel trackListPanelViewModel)
    {
        _trackListPanelViewModel = trackListPanelViewModel;

        NewCommand = new RelayCommand(OnNew);
        OpenCommand = new AsyncRelayCommand(OnOpenAsync);
        SaveCommand = new RelayCommand(OnSave);
        SettingsCommand = new RelayCommand(OnSettings);
    }

    private void OnNew()
    {
        // TODO: Implement new action
    }

    private async Task OnOpenAsync()
    {
        var folderPicker = new FolderPicker();
        folderPicker.SuggestedStartLocation = PickerLocationId.MusicLibrary;
        folderPicker.FileTypeFilter.Add("*");

        // Get the window handle for the picker
        var mainWindow = App.Instance.MainWindow;
        if (mainWindow is not null)
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(mainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);
        }

        StorageFolder? pickedFolder = await folderPicker.PickSingleFolderAsync();
        if (pickedFolder != null)
        {
            Debug.WriteLine($"[MenuBar] Folder picked: {pickedFolder.Path}");
            await _trackListPanelViewModel.LoadSongsFromFolderAsync(pickedFolder.Path);
        }
        else
        {
            Debug.WriteLine("[MenuBar] Folder picker cancelled");
        }
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

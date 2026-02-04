using System.Diagnostics;
using Windows.Storage;
using Windows.Storage.Pickers;
using MusicLibraryManager.Presentation.SoundCloud;

namespace MusicLibraryManager.ViewModels;

public partial class MenuBarViewModel : ObservableObject
{
    private readonly TrackListPanelViewModel _trackListPanelViewModel;
    private SoundCloudWindow? _soundCloudWindow;

    [ObservableProperty]
    private string title = "Music Library Manager";

    public ICommand OpenCommand { get; }
    public ICommand SoundCloudCommand { get; }
    public ICommand SettingsCommand { get; }

    public MenuBarViewModel(TrackListPanelViewModel trackListPanelViewModel)
    {
        _trackListPanelViewModel = trackListPanelViewModel;

        OpenCommand = new AsyncRelayCommand(OnOpenAsync);
        SoundCloudCommand = new RelayCommand(OnSoundCloud);
        SettingsCommand = new RelayCommand(OnSettings);
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

    private void OnSoundCloud()
    {
        // Only allow one SoundCloud window at a time
        if (_soundCloudWindow is not null)
        {
            // Try to activate the existing window
            _soundCloudWindow.Activate();
            return;
        }

        _soundCloudWindow = new SoundCloudWindow();
        _soundCloudWindow.Closed += (_, _) => _soundCloudWindow = null;
        _soundCloudWindow.Activate();
    }

    private void OnSettings()
    {
        // TODO: Implement settings action
    }
}

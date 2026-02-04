using Microsoft.UI.Xaml.Media.Imaging;

namespace MusicLibraryManager.ViewModels.SoundCloud;

public partial class SoundCloudSearchItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string? title;

    [ObservableProperty]
    private string? artist;

    [ObservableProperty]
    private string? album;

    [ObservableProperty]
    private string? year;

    [ObservableProperty]
    private string? genre;

    [ObservableProperty]
    private string? duration;

    [ObservableProperty]
    private string? trackUrl;

    [ObservableProperty]
    private BitmapImage? albumCover;

    [ObservableProperty]
    private byte[]? albumCoverData;

    [ObservableProperty]
    private bool isSelected;

    private readonly Action<SoundCloudSearchItemViewModel>? _onSelectCallback;

    public SoundCloudSearchItemViewModel(Action<SoundCloudSearchItemViewModel>? onSelectCallback = null)
    {
        _onSelectCallback = onSelectCallback;
    }

    [RelayCommand]
    private void Select()
    {
        _onSelectCallback?.Invoke(this);
    }
}

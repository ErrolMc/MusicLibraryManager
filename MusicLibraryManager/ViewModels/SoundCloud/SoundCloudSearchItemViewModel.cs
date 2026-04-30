using Microsoft.UI.Xaml.Media.Imaging;

namespace MusicLibraryManager.ViewModels.SoundCloud;

public partial class SoundCloudSearchItemViewModel : ObservableObject
{
    [ObservableProperty]
    private int displayIndex;

    private bool _isGap;

    public bool IsGap
    {
        get => _isGap;
        set
        {
            if (SetProperty(ref _isGap, value))
            {
                OnPropertyChanged(nameof(IsContentRow));
            }
        }
    }

    private bool _isManualPlaceholder;

    public bool IsManualPlaceholder
    {
        get => _isManualPlaceholder;
        set
        {
            if (SetProperty(ref _isManualPlaceholder, value))
            {
                OnPropertyChanged(nameof(IsContentRow));
            }
        }
    }

    public bool IsContentRow => !IsGap && !IsManualPlaceholder;

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
    private long trackId;

    [ObservableProperty]
    private BitmapImage? albumCover;

    [ObservableProperty]
    private byte[]? albumCoverData;

    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    private bool isUnmatched;

    [ObservableProperty]
    private double itemOpacity = 1.0;

    [ObservableProperty]
    private bool isInsertTarget;

    [ObservableProperty]
    private bool isInsertTargetBottom;

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

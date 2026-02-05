using MusicLibraryManager.ViewModels.SoundCloud;

namespace MusicLibraryManager.Presentation.SoundCloud;

public sealed partial class SoundCloudTrackInfoPanel : UserControl
{
    public SoundCloudTrackInfoPanel()
    {
        this.InitializeComponent();
    }

    private SoundCloudTrackInfoPanelViewModel? ViewModel => DataContext as SoundCloudTrackInfoPanelViewModel;

    private void SeekSlider_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        ViewModel?.BeginSeek();
    }

    private void SeekSlider_PointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        ViewModel?.EndSeek();
    }

    private void SeekSlider_PointerCaptureLost(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        ViewModel?.EndSeek();
    }
}

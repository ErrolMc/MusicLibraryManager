using MusicLibraryManager.ViewModels;

namespace MusicLibraryManager.Presentation;

public sealed partial class PlaybackView : UserControl
{
    public PlaybackView()
    {
        this.InitializeComponent();
    }

    private PlaybackViewModel? ViewModel => DataContext as PlaybackViewModel;

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

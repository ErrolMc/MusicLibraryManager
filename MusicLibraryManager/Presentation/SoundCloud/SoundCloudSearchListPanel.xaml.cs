using Microsoft.UI.Xaml.Input;
using MusicLibraryManager.ViewModels.SoundCloud;

namespace MusicLibraryManager.Presentation.SoundCloud;

public sealed partial class SoundCloudSearchListPanel : UserControl
{
    public SoundCloudSearchListPanel()
    {
        this.InitializeComponent();
    }

    private SoundCloudSearchListViewModel? ViewModel => DataContext as SoundCloudSearchListViewModel;

    private void SearchItem_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: SoundCloudSearchItemViewModel item })
        {
            ViewModel?.OnSelectItem(item);
        }
    }
}

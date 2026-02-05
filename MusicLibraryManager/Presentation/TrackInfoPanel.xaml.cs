using Microsoft.UI.Xaml.Input;
using MusicLibraryManager.ViewModels;

namespace MusicLibraryManager.Presentation;

public sealed partial class TrackInfoPanel : UserControl
{
    public TrackInfoPanel()
    {
        this.InitializeComponent();
    }

    private TrackInfoPanelViewModel? ViewModel => DataContext as TrackInfoPanelViewModel;

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel?.CancelChanges();
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel != null)
        {
            await ViewModel.SaveChangesAsync();
        }
    }

    private void AlbumCover_Tapped(object sender, TappedRoutedEventArgs e)
    {
        ViewModel?.ChangeAlbumCoverCommand.Execute(null);
    }

    private void AlbumCoverContextMenu_Opening(object sender, object e)
    {
        ViewModel?.RefreshClipboardState();
    }
}

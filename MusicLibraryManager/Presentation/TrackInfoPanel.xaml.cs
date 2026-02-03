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
}

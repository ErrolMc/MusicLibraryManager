using Microsoft.UI.Xaml.Input;
using MusicLibraryManager.ViewModels;

namespace MusicLibraryManager.Presentation;

public sealed partial class TrackInfoPanel : UserControl
{
    public static readonly DependencyProperty IsReadOnlyProperty =
        DependencyProperty.Register(
            nameof(IsReadOnly),
            typeof(bool),
            typeof(TrackInfoPanel),
            new PropertyMetadata(false));

    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    public TrackInfoPanel()
    {
        this.InitializeComponent();
    }

    private TrackInfoPanelViewModel? ViewModel => DataContext as TrackInfoPanelViewModel;

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        if (IsReadOnly)
        {
            return;
        }

        ViewModel?.CancelChanges();
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (IsReadOnly)
        {
            return;
        }

        if (ViewModel != null)
        {
            await ViewModel.SaveChangesAsync();
        }
    }

    private void AlbumCover_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (IsReadOnly)
        {
            return;
        }

        ViewModel?.ChangeAlbumCoverCommand.Execute(null);
    }

    private void AlbumCoverContextMenu_Opening(object sender, object e)
    {
        if (IsReadOnly)
        {
            return;
        }

        ViewModel?.RefreshClipboardState();
    }
}

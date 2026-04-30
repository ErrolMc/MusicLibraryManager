using Microsoft.Extensions.DependencyInjection;
using MusicLibraryManager.Models;
using MusicLibraryManager.ViewModels.SoundCloud;

namespace MusicLibraryManager.Presentation.SoundCloud;

public sealed partial class SoundCloudPlaylistWindow : Window
{
    private readonly SoundCloudPlaylistWindowViewModel? _viewModel;

    public SoundCloudPlaylistWindow()
    {
        this.InitializeComponent();

        this.AppWindow.Resize(new Windows.Graphics.SizeInt32 { Width = 520, Height = 640 });

        _viewModel = App.Instance.Services?.Services.GetRequiredService<SoundCloudPlaylistWindowViewModel>();
        if (_viewModel is not null)
        {
            if (this.Content is FrameworkElement rootElement)
            {
                rootElement.DataContext = _viewModel;
            }

            _viewModel.CloseRequested += OnCloseRequested;
            this.Closed += OnWindowClosed;
            _ = _viewModel.InitializeAsync();
        }
    }

    private void OnCloseRequested(object? sender, EventArgs e)
    {
        Close();
    }

    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        if (_viewModel is not null)
        {
            _viewModel.CloseRequested -= OnCloseRequested;
        }
    }

    private void PlaylistsListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        if (e.ClickedItem is SoundCloudPlaylist playlist)
        {
            _viewModel.SelectPlaylistCommand.Execute(playlist);
        }
    }
}

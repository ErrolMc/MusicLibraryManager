using Microsoft.Extensions.DependencyInjection;
using MusicLibraryManager.ViewModels.SoundCloud;

namespace MusicLibraryManager.Presentation.SoundCloud;

public sealed partial class SoundCloudWindow : Window
{
    private readonly SoundCloudWindowViewModel? _viewModel;

    public SoundCloudWindow()
    {
        this.InitializeComponent();

        // Set default window size
        this.AppWindow.Resize(new Windows.Graphics.SizeInt32 { Width = 1100, Height = 700 });

        // Resolve the ViewModel from DI and set as DataContext for the window content
        _viewModel = App.Instance.Services?.Services.GetRequiredService<SoundCloudWindowViewModel>();

        if (_viewModel != null)
        {
            // Set DataContext on the root Grid
            if (this.Content is FrameworkElement rootElement)
            {
                rootElement.DataContext = _viewModel;
            }

            // Subscribe to close request (e.g., when signed out)
            _viewModel.CloseRequested += OnCloseRequested;

            // Clear data when window closes to free memory
            this.Closed += OnWindowClosed;

            // Initialize async (load stored tokens)
            _ = _viewModel.InitializeAsync();
        }
    }

    private void OnCloseRequested(object? sender, EventArgs e)
    {
        Close();
    }

    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        _viewModel?.OnClosed();
    }
}

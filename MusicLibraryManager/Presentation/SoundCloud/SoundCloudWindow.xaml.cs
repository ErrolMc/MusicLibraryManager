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

            // Initialize async (load stored tokens)
            _ = _viewModel.InitializeAsync();
        }
    }
}

using Microsoft.Extensions.DependencyInjection;
using MusicLibraryManager.ViewModels.SoundCloud;

namespace MusicLibraryManager.Presentation.SoundCloud;

public sealed partial class SoundCloudWindow : Window
{
    public SoundCloudWindow()
    {
        this.InitializeComponent();

        // Set default window size
        this.AppWindow.Resize(new Windows.Graphics.SizeInt32 { Width = 1100, Height = 700 });

        // Resolve the ViewModel from DI and set the DataContext
        var viewModel = App.Instance.Services?.Services.GetRequiredService<SoundCloudSplitPanelViewModel>();
        SplitPanelPage.DataContext = viewModel;
    }
}

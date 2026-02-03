using Microsoft.UI.Dispatching;
using MusicLibraryManager.Services.Concrete;

namespace MusicLibraryManager.Presentation;

public sealed partial class Shell : UserControl, IContentControlProvider
{
    public Shell()
    {
        this.InitializeComponent();
        this.Loaded += Shell_Loaded;
    }

    private void Shell_Loaded(object sender, RoutedEventArgs e)
    {
        // Delay initialization until after DI has created the OverlayService
        InitializeOverlayService();
    }

    private void InitializeOverlayService()
    {
        if (OverlayService.Instance is not null)
        {
            OverlayService.Instance.Initialize(LoadingOverlay, LoadingMessage);
        }
        else
        {
            // Service not yet created, try again
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, InitializeOverlayService);
        }
    }

    public ContentControl ContentControl => Splash;
}

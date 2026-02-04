using MusicLibraryManager.ViewModels.SoundCloud;
using MusicLibraryManager.Presentation.SoundCloud;
using Uno.Resizetizer;

namespace MusicLibraryManager;

public partial class App : Application
{
    /// <summary>
    /// Gets the current App instance.
    /// </summary>
    public static new App Instance => (App)Application.Current;

    /// <summary>
    /// Gets the main window of the application.
    /// </summary>
    public new Window? MainWindow { get; private set; }

    /// <summary>
    /// Initializes the singleton application object. This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();
    }

    protected IHost? Host { get; private set; }

    /// <summary>
    /// Gets the host for accessing services.
    /// </summary>
    public IHost? Services => Host;

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var builder = this.CreateBuilder(args)
            // Add navigation support for toolkit controls such as TabBar and NavigationView
            .UseToolkitNavigation()
            .Configure(host => host
#if DEBUG
                // Switch to Development environment when running in DEBUG
                .UseEnvironment(Environments.Development)
#endif
                .UseLogging(configure: (context, logBuilder) =>
                {
                    // Configure log levels for different categories of logging
                    logBuilder
                        .SetMinimumLevel(
                            context.HostingEnvironment.IsDevelopment() ?
                                LogLevel.Information :
                                LogLevel.Warning)

                        // Default filters for core Uno Platform namespaces
                        .CoreLogLevel(LogLevel.Warning);

                    // Uno Platform namespace filter groups
                    // Uncomment individual methods to see more detailed logging
                    //// Generic Xaml events
                    //logBuilder.XamlLogLevel(LogLevel.Debug);
                    //// Layout specific messages
                    //logBuilder.XamlLayoutLogLevel(LogLevel.Debug);
                    //// Storage messages
                    //logBuilder.StorageLogLevel(LogLevel.Debug);
                    //// Binding related messages
                    //logBuilder.XamlBindingLogLevel(LogLevel.Debug);
                    //// Binder memory references tracking
                    //logBuilder.BinderMemoryReferenceLogLevel(LogLevel.Debug);
                    //// DevServer and HotReload related
                    //logBuilder.HotReloadCoreLogLevel(LogLevel.Information);
                    //// Debug JS interop
                    //logBuilder.WebAssemblyLogLevel(LogLevel.Debug);

                }, enableUnoLogging: true)
                .UseConfiguration(configure: configBuilder =>
                    configBuilder
                        .EmbeddedSource<App>()
                        .Section<AppConfig>()
                )
                .UseHttp((context, services) =>
                {
#if DEBUG
                // DelegatingHandler will be automatically injected
                services.AddTransient<DelegatingHandler, DebugHttpHandler>();
#endif

                })
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<IMusicLibraryService, MusicLibraryService>();
                    services.AddSingleton<IOverlayService, OverlayService>();
                    services.AddSingleton<IImageService, ImageService>();
                    services.AddSingleton<TrackListPanelViewModel>();
                    services.AddSingleton<MenuBarViewModel>();
                    services.AddSingleton<TrackInfoPanelViewModel>();
                    services.AddSingleton<SoundCloudTrackInfoPanelViewModel>();
                    services.AddSingleton<SoundCloudSearchListViewModel>();
                    services.AddSingleton<SoundCloudSplitPanelViewModel>();
                })
                .UseNavigation(RegisterRoutes)
            );
        MainWindow = builder.Window;

#if DEBUG
        MainWindow.UseStudio();
#endif
        MainWindow.SetWindowIcon();

        MainWindow.AppWindow.Resize(new Windows.Graphics.SizeInt32() { Height = 900, Width = 1300});

        Host = await builder.NavigateAsync<Shell>();
    }

    private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
    {
        views.Register(
            new ViewMap(ViewModel: typeof(ShellViewModel)),
            new ViewMap<SplitPanelPage, SplitPanelViewModel>(),
            new ViewMap<SoundCloudSplitPanelPage, SoundCloudSplitPanelViewModel>()
        );

        routes.Register(
            new RouteMap("", View: views.FindByViewModel<ShellViewModel>(),
                Nested:
                [
                    new ("SplitPanel", View: views.FindByViewModel<SplitPanelViewModel>(), IsDefault:true),
                    new ("SoundCloud", View: views.FindByViewModel<SoundCloudSplitPanelViewModel>()),
                ]
            )
        );
    }
}

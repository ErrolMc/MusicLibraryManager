namespace MusicLibraryManager.Presentation;

public sealed partial class MenuBar : UserControl
{
    public static readonly DependencyProperty ShowOpenButtonProperty =
        DependencyProperty.Register(
            nameof(ShowOpenButton),
            typeof(bool),
            typeof(MenuBar),
            new PropertyMetadata(false));

    public static readonly DependencyProperty ShowSyncPlaylistButtonProperty =
        DependencyProperty.Register(
            nameof(ShowSyncPlaylistButton),
            typeof(bool),
            typeof(MenuBar),
            new PropertyMetadata(true));

    public static readonly DependencyProperty ShowMainPanelButtonProperty =
        DependencyProperty.Register(
            nameof(ShowMainPanelButton),
            typeof(bool),
            typeof(MenuBar),
            new PropertyMetadata(true));

    public bool ShowOpenButton
    {
        get => (bool)GetValue(ShowOpenButtonProperty);
        set => SetValue(ShowOpenButtonProperty, value);
    }

    public bool ShowSyncPlaylistButton
    {
        get => (bool)GetValue(ShowSyncPlaylistButtonProperty);
        set => SetValue(ShowSyncPlaylistButtonProperty, value);
    }

    public bool ShowMainPanelButton
    {
        get => (bool)GetValue(ShowMainPanelButtonProperty);
        set => SetValue(ShowMainPanelButtonProperty, value);
    }

    public MenuBar()
    {
        this.InitializeComponent();
    }
}

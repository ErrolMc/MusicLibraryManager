namespace MusicLibraryManager.Services.Concrete;

public class OverlayService : IOverlayService
{
    public static OverlayService? Instance { get; private set; }

    private Grid? _overlayGrid;
    private TextBlock? _messageTextBlock;

    public bool IsVisible { get; private set; }

    public OverlayService()
    {
        Instance = this;
    }

    public void Initialize(Grid overlayGrid, TextBlock messageTextBlock)
    {
        _overlayGrid = overlayGrid;
        _messageTextBlock = messageTextBlock;
        _overlayGrid.Visibility = Visibility.Collapsed;
    }

    public void Show(string? message = null)
    {
        if (_overlayGrid is null) return;

        if (_messageTextBlock is not null)
        {
            _messageTextBlock.Text = message ?? string.Empty;
            _messageTextBlock.Visibility = string.IsNullOrEmpty(message) 
                ? Visibility.Collapsed 
                : Visibility.Visible;
        }

        _overlayGrid.Visibility = Visibility.Visible;
        IsVisible = true;
    }

    public void Hide()
    {
        if (_overlayGrid is null) return;

        _overlayGrid.Visibility = Visibility.Collapsed;
        IsVisible = false;
    }
}

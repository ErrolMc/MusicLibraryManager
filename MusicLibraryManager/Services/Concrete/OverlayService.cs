namespace MusicLibraryManager.Services.Concrete;

public class OverlayService : IOverlayService
{
    public static OverlayService? Instance { get; private set; }

    private Grid? _overlayGrid;
    private TextBlock? _messageTextBlock;
    private Grid? _popupGrid;
    private TextBlock? _popupTitleTextBlock;
    private TextBlock? _popupMessageTextBlock;
    private Button? _popupConfirmButton;
    private Button? _popupCancelButton;

    private TaskCompletionSource<bool>? _popupResult;

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

    public void InitializePopup(Grid popupGrid, TextBlock titleTextBlock, TextBlock messageTextBlock, Button confirmButton, Button cancelButton)
    {
        _popupGrid = popupGrid;
        _popupTitleTextBlock = titleTextBlock;
        _popupMessageTextBlock = messageTextBlock;
        _popupConfirmButton = confirmButton;
        _popupCancelButton = cancelButton;

        _popupGrid.Visibility = Visibility.Collapsed;

        _popupConfirmButton.Click += (s, e) => CompletePopup(true);
        _popupCancelButton.Click += (s, e) => CompletePopup(false);
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

    public Task<bool> ShowConfirmationAsync(string title, string message, string confirmText = "Confirm", string cancelText = "Cancel")
    {
        if (_popupGrid is null || _popupTitleTextBlock is null || _popupMessageTextBlock is null || 
            _popupConfirmButton is null || _popupCancelButton is null)
        {
            return Task.FromResult(false);
        }

        _popupTitleTextBlock.Text = title;
        _popupMessageTextBlock.Text = message;
        _popupConfirmButton.Content = confirmText;
        _popupCancelButton.Content = cancelText;

        _popupGrid.Visibility = Visibility.Visible;

        _popupResult = new TaskCompletionSource<bool>();
        return _popupResult.Task;
    }

    private void CompletePopup(bool result)
    {
        if (_popupGrid is not null)
        {
            _popupGrid.Visibility = Visibility.Collapsed;
        }

        _popupResult?.TrySetResult(result);
        _popupResult = null;
    }
}

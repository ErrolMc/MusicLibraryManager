namespace MusicLibraryManager.Services;

public interface IOverlayService
{
    bool IsVisible { get; }
    void Show(string? message = null);
    void Hide();
    Task<bool> ShowConfirmationAsync(string title, string message, string confirmText = "Confirm", string cancelText = "Cancel");
}

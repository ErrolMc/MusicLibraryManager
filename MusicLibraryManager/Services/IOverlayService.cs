namespace MusicLibraryManager.Services;

public interface IOverlayService
{
    bool IsVisible { get; }
    void Show(string? message = null);
    void Hide();
}

namespace MusicLibraryManager.Services;

public interface IImageService
{
    void CopyToClipboard(byte[] imageData);
    bool HasImageInClipboard();
    (byte[]? Data, string? MimeType) GetImageFromClipboard();
    byte[]? GetAlbumCoverData(Track track);
}

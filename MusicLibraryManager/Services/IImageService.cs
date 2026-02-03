namespace MusicLibraryManager.Services;

public interface IImageService
{
    void CopyToClipboard(byte[] imageData);
    byte[]? GetAlbumCoverData(Track track);
}

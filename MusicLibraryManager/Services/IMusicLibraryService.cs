using MusicLibraryManager.Models;

namespace MusicLibraryManager.Services;

public interface IMusicLibraryService
{
    Task<IReadOnlyList<Track>> GetSongsFromFolderAsync(string folderPath, bool includeSubfolders = true);
    Task<bool> UpdateTrackAsync(Track track, TrackUpdateInfo updateInfo);
}

public record TrackUpdateInfo(
    string? FileName,
    string? Title,
    string? Artist,
    string? Album,
    string? AlbumArtist,
    string? Composer,
    string? Genre,
    string? Comment,
    uint? Year,
    uint? TrackNumber,
    byte[]? AlbumCoverData = null,
    string? AlbumCoverMimeType = null
);

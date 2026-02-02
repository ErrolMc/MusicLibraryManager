using MusicLibraryManager.Models;

namespace MusicLibraryManager.Services;

public interface IMusicLibraryService
{
    Task<IReadOnlyList<Track>> GetSongsFromFolderAsync(string folderPath, bool includeSubfolders = true);
}

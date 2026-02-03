using System.Collections;
using MusicLibraryManager.Models;

namespace MusicLibraryManager.Services.Concrete;

public class MusicLibraryService : IMusicLibraryService
{
    private static readonly HashSet<string> SupportedExtensions =
    [
        ".mp3", ".flac", ".wav", ".m4a", ".aac", ".ogg", ".wma"
    ];

    public async Task<IReadOnlyList<Track>> GetSongsFromFolderAsync(string folderPath, bool includeSubfolders = false)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            throw new ArgumentException("Folder path cannot be null or empty.", nameof(folderPath));

        if (!Directory.Exists(folderPath))
            throw new DirectoryNotFoundException($"The folder '{folderPath}' does not exist.");

        var searchOption = includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        List<Track> songs = new List<Track>();

        await Task.Run(() =>
        {
            IEnumerable<string> files = Directory.EnumerateFiles(folderPath, "*.*", searchOption)
                .Where(file => SupportedExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()));
            foreach (string songPath in files)
            {
                Track? song = CreateSongFromFile(songPath);
                if (song != null)
                    songs.Add(song);
            }
        });

        return songs;
    }

    private static Track? CreateSongFromFile(string filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);

        try
        {
            using var tagFile = TagLib.File.Create(filePath);

            return new Track(tagFile, filePath);
        }
        catch
        {
            return null;
        }
    }
}

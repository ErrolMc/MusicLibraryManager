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

        List<Track> tracks = new List<Track>();

        await Task.Run(() =>
        {
            IEnumerable<string> files = Directory.EnumerateFiles(folderPath, "*.*", searchOption)
                .Where(file => SupportedExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()));
            foreach (string songPath in files)
            {
                Track? track = CreateTrackFromFile(songPath);
                if (track != null)
                    tracks.Add(track);
            }
        });

        return tracks;
    }

    public async Task<bool> UpdateTrackAsync(Track track, TrackUpdateInfo updateInfo)
    {
        return await Task.Run(() =>
        {
            try
            {
                TagLib.File tagFile = track.GetTagFile();

                if (updateInfo.Title is not null)
                    tagFile.Tag.Title = updateInfo.Title;
                
                if (updateInfo.Artist is not null)
                    tagFile.Tag.Performers = string.IsNullOrEmpty(updateInfo.Artist) 
                        ? [] 
                        : [updateInfo.Artist];
                
                if (updateInfo.Album is not null)
                    tagFile.Tag.Album = updateInfo.Album;
                
                if (updateInfo.AlbumArtist is not null)
                    tagFile.Tag.AlbumArtists = string.IsNullOrEmpty(updateInfo.AlbumArtist) 
                        ? [] 
                        : [updateInfo.AlbumArtist];
                
                if (updateInfo.Composer is not null)
                    tagFile.Tag.Composers = string.IsNullOrEmpty(updateInfo.Composer) 
                        ? [] 
                        : [updateInfo.Composer];
                
                if (updateInfo.Genre is not null)
                    tagFile.Tag.Genres = string.IsNullOrEmpty(updateInfo.Genre) 
                        ? [] 
                        : [updateInfo.Genre];
                
                if (updateInfo.Comment is not null)
                    tagFile.Tag.Comment = updateInfo.Comment;
                
                if (updateInfo.Year.HasValue)
                    tagFile.Tag.Year = updateInfo.Year.Value;
                
                if (updateInfo.TrackNumber.HasValue)
                    tagFile.Tag.Track = updateInfo.TrackNumber.Value;

                if (updateInfo.AlbumCoverData is not null)
                {
                    var picture = new TagLib.Picture
                    {
                        Type = TagLib.PictureType.FrontCover,
                        MimeType = updateInfo.AlbumCoverMimeType ?? "image/jpeg",
                        Data = new TagLib.ByteVector(updateInfo.AlbumCoverData)
                    };
                    tagFile.Tag.Pictures = [picture];
                }
                else if (tagFile.Tag.Pictures.Length > 0)
                {
                    tagFile.Tag.Pictures = [];
                }

                tagFile.Save();

                // Handle file rename if needed
                if (updateInfo.FileName is not null)
                {
                    var directory = Path.GetDirectoryName(track.FilePath);
                    var extension = Path.GetExtension(track.FilePath);
                    var newFilePath = Path.Combine(directory!, updateInfo.FileName + extension);
                    
                    if (newFilePath != track.FilePath && !File.Exists(newFilePath))
                    {
                        File.Move(track.FilePath, newFilePath);
                        track.FilePath = newFilePath;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        });
    }

    private static Track? CreateTrackFromFile(string filePath)
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

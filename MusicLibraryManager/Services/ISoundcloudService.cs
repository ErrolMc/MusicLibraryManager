using MusicLibraryManager.Models;

namespace MusicLibraryManager.Services;

public interface ISoundCloudService
{
    /// <summary>
    /// Searches for tracks on SoundCloud based on a keyword.
    /// </summary>
    /// <param name="query">The search query/keyword.</param>
    /// <param name="limit">Maximum number of results to return (default: 20).</param>
    /// <returns>A list of matching SoundCloud tracks.</returns>
    Task<IReadOnlyList<SoundCloudTrack>> SearchTracksAsync(string query, int limit = 20);
}

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

    /// <summary>
    /// Gets playlists for the authenticated SoundCloud user.
    /// </summary>
    /// <returns>A list of user playlists.</returns>
    Task<IReadOnlyList<SoundCloudPlaylist>> GetMyPlaylistsAsync();

    /// <summary>
    /// Gets tracks for a specific SoundCloud playlist.
    /// </summary>
    /// <param name="playlistId">Playlist id.</param>
    /// <returns>Tracks in the playlist.</returns>
    Task<IReadOnlyList<SoundCloudTrack>> GetPlaylistTracksAsync(long playlistId);

    /// <summary>
    /// Appends tracks to an existing SoundCloud playlist.
    /// </summary>
    /// <param name="playlistId">Playlist id.</param>
    /// <param name="trackIds">Track ids to append.</param>
    /// <returns>True when update succeeds.</returns>
    Task<bool> AppendTracksToPlaylistAsync(long playlistId, IReadOnlyList<long> trackIds);

    /// <summary>
    /// Removes a track from an existing SoundCloud playlist.
    /// </summary>
    /// <param name="playlistId">Playlist id.</param>
    /// <param name="trackId">Track id to remove.</param>
    /// <returns>True when update succeeds.</returns>
    Task<bool> RemoveTrackFromPlaylistAsync(long playlistId, long trackId);

    /// <summary>
    /// Replaces the full playlist order/content with the provided track ids.
    /// </summary>
    /// <param name="playlistId">Playlist id.</param>
    /// <param name="trackIds">Final playlist track ids in target order.</param>
    /// <returns>True when update succeeds.</returns>
    Task<bool> ReplacePlaylistTracksAsync(long playlistId, IReadOnlyList<long> trackIds);
}

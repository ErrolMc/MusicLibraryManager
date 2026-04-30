using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MusicLibraryManager.Models;
using System.Diagnostics;

namespace MusicLibraryManager.Services.Concrete;

public class SoundCloudService : ISoundCloudService
{
    private const string BaseUrl = "https://api.soundcloud.com";

    private readonly HttpClient _httpClient;
    private readonly ISoundCloudAuthService _authService;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<SoundCloudService> _logger;

    public bool IsInitialized => _authService.IsAuthenticated;

    public SoundCloudService(HttpClient httpClient, ISoundCloudAuthService authService, ILogger<SoundCloudService> logger)
    {
        _httpClient = httpClient;
        _authService = authService;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<IReadOnlyList<SoundCloudPlaylist>> GetMyPlaylistsAsync()
    {
        string? accessToken = await _authService.GetAccessTokenAsync();
        if (accessToken is null)
            return [];

        var urls = new[]
        {
            $"{BaseUrl}/me/playlists?limit=200&linked_partitioning=true",
            $"{BaseUrl}/me/playlists?limit=200"
        };

        foreach (var url in urls)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("OAuth", accessToken);
                request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var rawJson = await response.Content.ReadAsStringAsync();
                var playlists = ParsePlaylists(rawJson);
                if (playlists.Count > 0)
                {
                    return playlists;
                }
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine($"[SoundCloudService] Failed to load playlists from '{url}': {ex.Message}");
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"[SoundCloudService] Failed to parse playlists from '{url}': {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SoundCloudService] Unexpected playlist error for '{url}': {ex.Message}");
            }
        }

        return [];
    }

    private static IReadOnlyList<SoundCloudPlaylist> ParsePlaylists(string rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            return [];
        }

        using var document = JsonDocument.Parse(rawJson);
        var root = document.RootElement;

        if (root.ValueKind == JsonValueKind.Array)
        {
            return ParsePlaylistArray(root);
        }

        if (root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty("collection", out var collection) &&
            collection.ValueKind == JsonValueKind.Array)
        {
            return ParsePlaylistArray(collection);
        }

        return [];
    }

    private static IReadOnlyList<SoundCloudPlaylist> ParsePlaylistArray(JsonElement playlistsElement)
    {
        var result = new List<SoundCloudPlaylist>();

        foreach (var playlistElement in playlistsElement.EnumerateArray())
        {
            if (!playlistElement.TryGetProperty("id", out var idElement) ||
                idElement.ValueKind != JsonValueKind.Number ||
                !idElement.TryGetInt64(out var id))
            {
                continue;
            }

            var title = playlistElement.TryGetProperty("title", out var titleElement)
                ? titleElement.GetString()
                : null;

            var trackCount = 0;
            if (playlistElement.TryGetProperty("track_count", out var trackCountElement) &&
                trackCountElement.ValueKind == JsonValueKind.Number)
            {
                trackCount = trackCountElement.GetInt32();
            }
            else if (playlistElement.TryGetProperty("tracks", out var tracksElement) &&
                     tracksElement.ValueKind == JsonValueKind.Array)
            {
                trackCount = tracksElement.GetArrayLength();
            }

            result.Add(new SoundCloudPlaylist(
                Id: id,
                Title: string.IsNullOrWhiteSpace(title) ? "Untitled Playlist" : title,
                TrackCount: trackCount));
        }

        return result;
    }

    public async Task<IReadOnlyList<SoundCloudTrack>> GetPlaylistTracksAsync(long playlistId)
    {
        string? accessToken = await _authService.GetAccessTokenAsync();
        if (accessToken is null)
            return [];

        var urls = new[]
        {
            $"{BaseUrl}/playlists/{playlistId}?representation=full",
            $"{BaseUrl}/playlists/{playlistId}"
        };

        foreach (var url in urls)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("OAuth", accessToken);
                request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var rawJson = await response.Content.ReadAsStringAsync();
                var tracks = ParsePlaylistTracks(rawJson);
                if (tracks.Count > 0)
                {
                    return tracks;
                }
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine($"[SoundCloudService] Failed to load tracks from playlist '{playlistId}' using '{url}': {ex.Message}");
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"[SoundCloudService] Failed to parse tracks from playlist '{playlistId}': {ex.Message}");
            }
        }

        return [];
    }

    public async Task<bool> AppendTracksToPlaylistAsync(long playlistId, IReadOnlyList<long> trackIds)
    {
        if (trackIds.Count == 0)
        {
            return true;
        }

        return await UpdatePlaylistTracksAsync(playlistId, trackIds, mergeWithExisting: true);
    }

    public async Task<bool> RemoveTrackFromPlaylistAsync(long playlistId, long trackId)
    {
        try
        {
            var existingTracks = await GetPlaylistTracksAsync(playlistId);
            var remainingTrackIds = existingTracks
                .Select(t => t.Id)
                .Where(id => id != trackId)
                .Distinct()
                .ToList();

            if (remainingTrackIds.Count == existingTracks.Count)
            {
                _logger.LogInformation("[SoundCloudService] RemoveTrackFromPlaylistAsync skipped because track was not in playlist. PlaylistId={PlaylistId}, TrackId={TrackId}", playlistId, trackId);
                return true;
            }

            return await UpdatePlaylistTracksAsync(playlistId, remainingTrackIds, mergeWithExisting: false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[SoundCloudService] RemoveTrackFromPlaylistAsync failed. PlaylistId={PlaylistId}, TrackId={TrackId}", playlistId, trackId);
            return false;
        }
    }

    private async Task<bool> UpdatePlaylistTracksAsync(long playlistId, IReadOnlyList<long> trackIds, bool mergeWithExisting)
    {

        string? accessToken = await _authService.GetAccessTokenAsync();
        if (accessToken is null)
        {
            return false;
        }

        try
        {
            _logger.LogInformation("[SoundCloudService] AppendTracksToPlaylistAsync called. PlaylistId={PlaylistId}, IncomingTrackCount={Count}", playlistId, trackIds.Count);

            var playlistRequest = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/playlists/{playlistId}?representation=full");
            playlistRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("OAuth", accessToken);
            playlistRequest.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            var playlistResponse = await _httpClient.SendAsync(playlistRequest);
            if (!playlistResponse.IsSuccessStatusCode)
            {
                var details = await playlistResponse.Content.ReadAsStringAsync();
                _logger.LogWarning("[SoundCloudService] Failed to fetch playlist before append. PlaylistId={PlaylistId}, Status={Status}, Body={Body}", playlistId, (int)playlistResponse.StatusCode, details);
                return false;
            }

            var playlistJson = await playlistResponse.Content.ReadAsStringAsync();
            if (!TryParsePlaylistUpdateInfo(playlistJson, out var playlistTitle, out var existingTrackIds))
            {
                _logger.LogWarning("[SoundCloudService] Could not parse playlist metadata for update. PlaylistId={PlaylistId}", playlistId);
                return false;
            }

            var finalTrackIds = mergeWithExisting
                ? existingTrackIds.Concat(trackIds).Distinct().ToList()
                : trackIds.Distinct().ToList();

            var serializedTrackIds = finalTrackIds
                .Select(id => new { id })
                .ToList();

            var body = JsonSerializer.Serialize(new
            {
                playlist = new
                {
                    title = playlistTitle,
                    tracks = serializedTrackIds
                }
            });

            var request = new HttpRequestMessage(HttpMethod.Put, $"{BaseUrl}/playlists/{playlistId}")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };

            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("OAuth", accessToken);
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var details = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("[SoundCloudService] Playlist update (json) failed. PlaylistId={PlaylistId}, Status={Status}, Body={Body}", playlistId, (int)response.StatusCode, details);

                if ((int)response.StatusCode == 422)
                {
                    var formPairs = new List<KeyValuePair<string, string>>
                    {
                        new("playlist[title]", playlistTitle)
                    };

                    foreach (var trackId in serializedTrackIds.Select(t => t.id))
                    {
                        formPairs.Add(new KeyValuePair<string, string>("playlist[tracks][][id]", trackId.ToString()));
                    }

                    var formRequest = new HttpRequestMessage(HttpMethod.Put, $"{BaseUrl}/playlists/{playlistId}")
                    {
                        Content = new FormUrlEncodedContent(formPairs)
                    };

                    formRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("OAuth", accessToken);
                    formRequest.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                    var formResponse = await _httpClient.SendAsync(formRequest);
                    if (!formResponse.IsSuccessStatusCode)
                    {
                        var formDetails = await formResponse.Content.ReadAsStringAsync();
                        _logger.LogWarning("[SoundCloudService] Playlist update (form) failed. PlaylistId={PlaylistId}, Status={Status}, Body={Body}", playlistId, (int)formResponse.StatusCode, formDetails);
                        return false;
                    }

                    _logger.LogInformation("[SoundCloudService] Playlist update succeeded with form payload fallback. PlaylistId={PlaylistId}", playlistId);
                }
                else
                {
                    return false;
                }
            }

            _logger.LogInformation("[SoundCloudService] Playlist update completed. PlaylistId={PlaylistId}, FinalTrackCount={Count}", playlistId, serializedTrackIds.Count);
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SoundCloudService] Failed to append tracks to playlist '{playlistId}': {ex.Message}");
            return false;
        }
    }

    private static bool TryParsePlaylistUpdateInfo(string rawJson, out string title, out List<long> trackIds)
    {
        title = string.Empty;
        trackIds = [];

        if (string.IsNullOrWhiteSpace(rawJson))
        {
            return false;
        }

        using var document = JsonDocument.Parse(rawJson);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        title = root.TryGetProperty("title", out var titleElement)
            ? titleElement.GetString() ?? string.Empty
            : string.Empty;

        if (root.TryGetProperty("tracks", out var tracksElement) &&
            tracksElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var trackElement in tracksElement.EnumerateArray())
            {
                if (trackElement.TryGetProperty("id", out var idElement) &&
                    idElement.ValueKind == JsonValueKind.Number &&
                    idElement.TryGetInt64(out var id))
                {
                    trackIds.Add(id);
                }
            }
        }

        return !string.IsNullOrWhiteSpace(title);
    }

    private static IReadOnlyList<SoundCloudTrack> ParsePlaylistTracks(string rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            return [];
        }

        using var document = JsonDocument.Parse(rawJson);
        var root = document.RootElement;

        if (root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty("tracks", out var tracksElement) &&
            tracksElement.ValueKind == JsonValueKind.Array)
        {
            var tracks = new List<SoundCloudTrack>();
            foreach (var trackElement in tracksElement.EnumerateArray())
            {
                if (!TryParseTrack(trackElement, out var track))
                {
                    continue;
                }

                tracks.Add(track);
            }

            return tracks;
        }

        return [];
    }

    private static bool TryParseTrack(JsonElement trackElement, out SoundCloudTrack track)
    {
        track = default!;

        if (!trackElement.TryGetProperty("id", out var idElement) ||
            idElement.ValueKind != JsonValueKind.Number ||
            !idElement.TryGetInt64(out var id))
        {
            return false;
        }

        var title = trackElement.TryGetProperty("title", out var titleElement)
            ? titleElement.GetString()
            : null;

        var artist = trackElement.TryGetProperty("user", out var userElement) &&
                     userElement.ValueKind == JsonValueKind.Object &&
                     userElement.TryGetProperty("username", out var usernameElement)
            ? usernameElement.GetString()
            : null;

        var artworkUrl = trackElement.TryGetProperty("artwork_url", out var artworkElement)
            ? artworkElement.GetString()
            : null;

        var genre = trackElement.TryGetProperty("genre", out var genreElement)
            ? genreElement.GetString()
            : null;

        var description = trackElement.TryGetProperty("description", out var descriptionElement)
            ? descriptionElement.GetString()
            : null;

        var duration = trackElement.TryGetProperty("duration", out var durationElement) && durationElement.ValueKind == JsonValueKind.Number
            ? durationElement.GetInt32()
            : 0;

        var streamUrl = trackElement.TryGetProperty("stream_url", out var streamElement)
            ? streamElement.GetString()
            : null;

        var permalinkUrl = trackElement.TryGetProperty("permalink_url", out var permalinkElement)
            ? permalinkElement.GetString()
            : null;

        DateTime.TryParse(trackElement.TryGetProperty("created_at", out var createdElement) ? createdElement.GetString() : null, out var createdAt);

        track = new SoundCloudTrack(
            Id: id,
            Title: string.IsNullOrWhiteSpace(title) ? "Unknown" : title,
            Artist: artist,
            ArtworkUrl: GetHighResArtworkUrl(artworkUrl),
            Genre: genre,
            Description: description,
            DurationMs: duration,
            StreamUrl: streamUrl,
            PermalinkUrl: permalinkUrl ?? string.Empty,
            CreatedAt: createdAt);

        return true;
    }

    public async Task<IReadOnlyList<SoundCloudTrack>> SearchTracksAsync(string query, int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        _logger.LogInformation("[SoundCloudService] SearchTracksAsync called. Query='{Query}', Limit={Limit}", query, limit);

        string? accessToken = await _authService.GetAccessTokenAsync();
        if (accessToken is null)
            return [];

        var encodedQuery = Uri.EscapeDataString(query);
        var url = $"{BaseUrl}/tracks?q={encodedQuery}&limit={limit}&access=playable&linked_partitioning=true";

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("OAuth", accessToken);
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var searchResponse = await response.Content.ReadFromJsonAsync<SoundCloudSearchResponse>(_jsonOptions);

            if (searchResponse?.Collection is null)
            {
                _logger.LogInformation("[SoundCloudService] SearchTracksAsync returned 0 results. Query='{Query}'", query);
                return [];
            }

            var tracks = searchResponse.Collection
                .Select(MapToSoundCloudTrack)
                .ToList();

            _logger.LogInformation("[SoundCloudService] SearchTracksAsync completed. Query='{Query}', Results={Count}", query, tracks.Count);
            return tracks;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "[SoundCloudService] SearchTracksAsync HTTP failure. Query='{Query}'", query);
            return [];
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "[SoundCloudService] SearchTracksAsync JSON parse failure. Query='{Query}'", query);
            return [];
        }
    }

    private static SoundCloudTrack MapToSoundCloudTrack(SoundCloudApiTrack apiTrack)
    {
        DateTime.TryParse(apiTrack.CreatedAt, out var createdAt);

        return new SoundCloudTrack(
            Id: apiTrack.Id,
            Title: apiTrack.Title ?? "Unknown",
            Artist: apiTrack.User?.Username,
            ArtworkUrl: GetHighResArtworkUrl(apiTrack.ArtworkUrl),
            Genre: apiTrack.Genre,
            Description: apiTrack.Description,
            DurationMs: apiTrack.Duration,
            StreamUrl: apiTrack.StreamUrl,
            PermalinkUrl: apiTrack.PermalinkUrl ?? "",
            CreatedAt: createdAt
        );
    }

    private static string? GetHighResArtworkUrl(string? artworkUrl)
    {
        return artworkUrl?.Replace("-large", "-t500x500");
    }

    private sealed class SoundCloudApiTrack
    {
        public long Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Genre { get; set; }
        public int Duration { get; set; }
        public string? ArtworkUrl { get; set; }
        public string? StreamUrl { get; set; }
        public string? PermalinkUrl { get; set; }
        public string? CreatedAt { get; set; }
        public SoundCloudApiUser? User { get; set; }
    }

    private sealed class SoundCloudApiUser
    {
        public long Id { get; set; }
        public string? Username { get; set; }
        public string? AvatarUrl { get; set; }
    }

    private sealed class SoundCloudSearchResponse
    {
        public List<SoundCloudApiTrack>? Collection { get; set; }
        public string? NextHref { get; set; }
    }

}

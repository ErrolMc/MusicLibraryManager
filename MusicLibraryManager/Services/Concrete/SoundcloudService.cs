using System.Net.Http.Json;
using System.Text.Json;
using MusicLibraryManager.Models;

namespace MusicLibraryManager.Services.Concrete;

public class SoundCloudService : ISoundCloudService
{
    private const string BaseUrl = "https://api.soundcloud.com";

    private readonly HttpClient _httpClient;
    private readonly ISoundCloudAuthService _authService;
    private readonly JsonSerializerOptions _jsonOptions;

    public bool IsInitialized => _authService.IsAuthenticated;

    public SoundCloudService(HttpClient httpClient, ISoundCloudAuthService authService)
    {
        _httpClient = httpClient;
        _authService = authService;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<IReadOnlyList<SoundCloudTrack>> SearchTracksAsync(string query, int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

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
                return [];

            return searchResponse.Collection
                .Select(MapToSoundCloudTrack)
                .ToList();
        }
        catch (HttpRequestException)
        {
            return [];
        }
        catch (JsonException)
        {
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

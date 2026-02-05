using System.Net.Http.Json;
using System.Text.Json.Serialization;
using NAudio.Wave;

namespace MusicLibraryManager.Services.Concrete;

public class SoundCloudPlaybackService : ISoundCloudPlaybackService
{
    private const string BaseUrl = "https://api.soundcloud.com";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISoundCloudAuthService _authService;

    private WaveOutEvent? _waveOut;
    private WaveStream? _mediaReader;
    private MemoryStream? _audioStream;
    private long _currentTrackId;

    public bool IsPlaying => _waveOut?.PlaybackState == PlaybackState.Playing;
    public bool IsLoaded => _mediaReader != null;

    public TimeSpan CurrentPosition => _mediaReader?.CurrentTime ?? TimeSpan.Zero;
    public TimeSpan TotalDuration => _mediaReader?.TotalTime ?? TimeSpan.Zero;

    public float Volume
    {
        get => _waveOut?.Volume ?? 1f;
        set
        {
            if (_waveOut != null)
                _waveOut.Volume = Math.Clamp(value, 0f, 1f);
        }
    }

    public event EventHandler? PlaybackStateChanged;
    public event EventHandler? PlaybackEnded;

    public SoundCloudPlaybackService(IHttpClientFactory httpClientFactory, ISoundCloudAuthService authService)
    {
        _httpClientFactory = httpClientFactory;
        _authService = authService;
    }

    public async Task LoadAndPlayAsync(long trackId)
    {
        Stop();

        _currentTrackId = trackId;

        var streamUrl = await GetStreamUrlAsync(trackId);
        if (string.IsNullOrEmpty(streamUrl))
            return;

        // Download via HttpClient to avoid MediaFoundation URL access restrictions.
        var accessToken = await _authService.GetAccessTokenAsync();
        using var httpClient = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, streamUrl);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("OAuth", accessToken);
        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var audioData = await response.Content.ReadAsByteArrayAsync();

        _audioStream = new MemoryStream(audioData);
        _mediaReader = new StreamMediaFoundationReader(_audioStream);

        _waveOut = new WaveOutEvent();
        _waveOut.Init(_mediaReader);
        _waveOut.PlaybackStopped += OnPlaybackStopped;
        _waveOut.Play();

        PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task LoadAsync(string source)
    {
        Stop();

        // For local file paths, MediaFoundationReader works directly.
        _mediaReader = await Task.Run(() => new MediaFoundationReader(source));

        _waveOut = new WaveOutEvent();
        _waveOut.Init(_mediaReader);
        _waveOut.PlaybackStopped += OnPlaybackStopped;
        _waveOut.Play();

        PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Play()
    {
        if (_waveOut?.PlaybackState == PlaybackState.Paused)
        {
            _waveOut.Play();
            PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Pause()
    {
        if (_waveOut?.PlaybackState == PlaybackState.Playing)
        {
            _waveOut.Pause();
            PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Stop()
    {
        if (_waveOut != null)
        {
            _waveOut.PlaybackStopped -= OnPlaybackStopped;
            _waveOut.Stop();
            _waveOut.Dispose();
            _waveOut = null;
        }

        if (_mediaReader != null)
        {
            _mediaReader.Dispose();
            _mediaReader = null;
        }

        if (_audioStream != null)
        {
            _audioStream.Dispose();
            _audioStream = null;
        }

        _currentTrackId = 0;
        PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Seek(TimeSpan position)
    {
        if (_mediaReader != null)
        {
            _mediaReader.CurrentTime = position;
        }
    }

    public void Dispose()
    {
        Stop();
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        PlaybackEnded?.Invoke(this, EventArgs.Empty);
        PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private async Task<string?> GetStreamUrlAsync(long trackId)
    {
        var accessToken = await _authService.GetAccessTokenAsync();
        if (accessToken is null)
            return null;

        var url = $"{BaseUrl}/tracks/{trackId}/streams";

        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("OAuth", accessToken);
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var streams = await response.Content.ReadFromJsonAsync<StreamsResponse>();
            // Prefer progressive MP3 stream
            return streams?.HttpMp3128Url
                ?? streams?.HlsMp3128Url
                ?? streams?.HlsOpus64Url;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    private sealed class StreamsResponse
    {
        [JsonPropertyName("http_mp3_128_url")]
        public string? HttpMp3128Url { get; set; }

        [JsonPropertyName("hls_mp3_128_url")]
        public string? HlsMp3128Url { get; set; }

        [JsonPropertyName("hls_opus_64_url")]
        public string? HlsOpus64Url { get; set; }
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
using MusicLibraryManager.Models;
using Windows.Storage;
using Windows.System;

namespace MusicLibraryManager.Services.Concrete;

public class SoundCloudAuthService : ISoundCloudAuthService
{
    private const string AuthBaseUrl = "https://secure.soundcloud.com";
    private const string TokenEndpoint = "https://secure.soundcloud.com/oauth/token";
    private const string TokenStorageKey = "SoundCloudTokens";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SoundCloudConfig _config;
    private readonly JsonSerializerOptions _jsonOptions;
    private SoundCloudTokenInfo? _currentToken;

    private string RedirectUri => $"http://localhost:{_config.CallbackPort}/callback";

    public bool IsAuthenticated => _currentToken != null && !_currentToken.IsExpired;

    public event EventHandler<bool>? AuthenticationStateChanged;

    public SoundCloudAuthService(IHttpClientFactory httpClientFactory, IOptions<SoundCloudConfig> config)
    {
        _httpClientFactory = httpClientFactory;
        _config = config.Value;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        };
    }

    private HttpClient CreateHttpClient() => _httpClientFactory.CreateClient();

    public async Task<bool> StartOAuthFlowAsync(CancellationToken cancellationToken = default)
    {
        var codeVerifier = GenerateCodeVerifier();
        var codeChallenge = GenerateCodeChallenge(codeVerifier);
        var state = GenerateRandomString(32);

        var authUrl = $"{AuthBaseUrl}/authorize" +
            $"?client_id={_config.ClientId}" +
            $"&redirect_uri={Uri.EscapeDataString(RedirectUri)}" +
            $"&response_type=code" +
            $"&code_challenge={codeChallenge}" +
            $"&code_challenge_method=S256" +
            $"&state={state}";

        // Start HTTP listener to receive callback
        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{_config.CallbackPort}/");

        try
        {
            listener.Start();
        }
        catch (HttpListenerException)
        {
            // Port might be in use
            return false;
        }

        try
        {
            // Open browser for user to sign in
            await Launcher.LaunchUriAsync(new Uri(authUrl));

            // Wait for callback
            var contextTask = listener.GetContextAsync();
            var completedTask = await Task.WhenAny(contextTask, Task.Delay(Timeout.Infinite, cancellationToken));

            if (completedTask != contextTask)
            {
                // Cancelled
                return false;
            }

            var context = await contextTask;
            var request = context.Request;
            var response = context.Response;

            // Parse the callback URL for code and state
            var query = request.Url?.Query;
            if (string.IsNullOrEmpty(query))
            {
                await SendResponseAsync(response, "Error: No authorization code received.", false);
                return false;
            }

            var queryParams = HttpUtility.ParseQueryString(query);
            var code = queryParams["code"];
            var returnedState = queryParams["state"];
            var error = queryParams["error"];

            if (!string.IsNullOrEmpty(error))
            {
                await SendResponseAsync(response, $"Error: {error}", false);
                return false;
            }

            if (string.IsNullOrEmpty(code))
            {
                await SendResponseAsync(response, "Error: No authorization code received.", false);
                return false;
            }

            if (returnedState != state)
            {
                await SendResponseAsync(response, "Error: State mismatch - possible CSRF attack.", false);
                return false;
            }

            // Exchange code for tokens
            var success = await ExchangeCodeForTokensAsync(code, codeVerifier);

            if (success)
            {
                await SendResponseAsync(response, "Success! You can close this window and return to the app.", true);
            }
            else
            {
                await SendResponseAsync(response, "Error: Failed to exchange code for tokens.", false);
            }

            return success;
        }
        finally
        {
            listener.Stop();
        }
    }

    private static async Task SendResponseAsync(HttpListenerResponse response, string message, bool success)
    {
        var html =  $@" <!DOCTYPE html>
                        <html>
                        <head>
                            <title>SoundCloud Authorization</title>
                            <style>
                                body {{
                                    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
                                    display: flex;
                                    justify-content: center;
                                    align-items: center;
                                    height: 100vh;
                                    margin: 0;
                                    background: {(success ? "#1a1a2e" : "#2e1a1a")};
                                    color: white;
                                }}
                                .container {{
                                    text-align: center;
                                    padding: 40px;
                                    border-radius: 12px;
                                    background: {(success ? "#16213e" : "#3e1616")};
                                }}
                                h1 {{ color: {(success ? "#ff5500" : "#ff4444")}; }}
                            </style>
                        </head>
                        <body>
                            <div class='container'>
                                <h1>{(success ? "✓" : "✗")}</h1>
                                <p>{message}</p>
                            </div>
                        </body>
                        </html>";

        var buffer = Encoding.UTF8.GetBytes(html);
        response.ContentType = "text/html";
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer);
        response.Close();
    }

    private async Task<bool> ExchangeCodeForTokensAsync(string code, string codeVerifier)
    {
        try
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["client_id"] = _config.ClientId,
                ["client_secret"] = _config.ClientSecret,
                ["redirect_uri"] = RedirectUri,
                ["code_verifier"] = codeVerifier,
                ["code"] = code
            });

            var request = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint)
            {
                Content = content
            };
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            var response = await CreateHttpClient().SendAsync(request);

            if (!response.IsSuccessStatusCode)
                return false;

            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(_jsonOptions);

            if (tokenResponse is null)
                return false;

            _currentToken = new SoundCloudTokenInfo(
                AccessToken: tokenResponse.AccessToken,
                RefreshToken: tokenResponse.RefreshToken,
                ExpiresAt: DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn)
            );

            await SaveTokensAsync();
            AuthenticationStateChanged?.Invoke(this, true);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        if (_currentToken is null)
            return null;

        if (_currentToken.NeedsRefresh)
        {
            await RefreshTokenAsync();
        }

        return _currentToken?.AccessToken;
    }

    public async Task<bool> TryLoadStoredTokensAsync()
    {
        try
        {
            var localSettings = ApplicationData.Current.LocalSettings;

            if (localSettings.Values.TryGetValue(TokenStorageKey, out var storedJson) &&
                storedJson is string json)
            {
                var stored = JsonSerializer.Deserialize<StoredTokens>(json, _jsonOptions);

                if (stored is not null)
                {
                    _currentToken = new SoundCloudTokenInfo(
                        AccessToken: stored.AccessToken,
                        RefreshToken: stored.RefreshToken,
                        ExpiresAt: stored.ExpiresAt
                    );

                    // If token is expired but we have a refresh token, try to refresh
                    if (_currentToken.IsExpired && !string.IsNullOrEmpty(_currentToken.RefreshToken))
                    {
                        var refreshed = await RefreshTokenAsync();
                        if (!refreshed)
                        {
                            _currentToken = null;
                            return false;
                        }
                    }

                    AuthenticationStateChanged?.Invoke(this, IsAuthenticated);
                    return IsAuthenticated;
                }
            }
        }
        catch
        {
            // Ignore storage errors
        }

        return false;
    }

    public async Task SignOutAsync()
    {
        try
        {
            if (_currentToken != null)
            {
                // Call SoundCloud sign-out endpoint
                var content = JsonContent.Create(new { access_token = _currentToken.AccessToken });
                await CreateHttpClient().PostAsync($"{AuthBaseUrl}/sign-out", content);
            }
        }
        catch
        {
            // Ignore sign-out errors
        }
        finally
        {
            _currentToken = null;

            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values.Remove(TokenStorageKey);

            AuthenticationStateChanged?.Invoke(this, false);
        }
    }

    private async Task<bool> RefreshTokenAsync()
    {
        if (_currentToken?.RefreshToken is null)
            return false;

        try
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = _config.ClientId,
                ["client_secret"] = _config.ClientSecret,
                ["refresh_token"] = _currentToken.RefreshToken
            });

            var request = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint)
            {
                Content = content
            };
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            var response = await CreateHttpClient().SendAsync(request);

            if (!response.IsSuccessStatusCode)
                return false;

            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(_jsonOptions);

            if (tokenResponse is null)
                return false;

            _currentToken = new SoundCloudTokenInfo(
                AccessToken: tokenResponse.AccessToken,
                RefreshToken: tokenResponse.RefreshToken,
                ExpiresAt: DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn)
            );

            await SaveTokensAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task SaveTokensAsync()
    {
        if (_currentToken is null)
            return;

        try
        {
            var stored = new StoredTokens
            {
                AccessToken = _currentToken.AccessToken,
                RefreshToken = _currentToken.RefreshToken,
                ExpiresAt = _currentToken.ExpiresAt
            };

            var json = JsonSerializer.Serialize(stored, _jsonOptions);

            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values[TokenStorageKey] = json;
        }
        catch
        {
            // Ignore storage errors
        }

        await Task.CompletedTask;
    }

    private static string GenerateCodeVerifier()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Base64UrlEncode(bytes);
    }

    private static string GenerateCodeChallenge(string codeVerifier)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
        return Base64UrlEncode(bytes);
    }

    private static string GenerateRandomString(int length)
    {
        var bytes = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Base64UrlEncode(bytes)[..length];
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private sealed class TokenResponse
    {
        public string AccessToken { get; set; } = "";
        public string RefreshToken { get; set; } = "";
        public int ExpiresIn { get; set; }
        public string Scope { get; set; } = "";
    }

    private sealed class StoredTokens
    {
        public string AccessToken { get; set; } = "";
        public string RefreshToken { get; set; } = "";
        public DateTime ExpiresAt { get; set; }
    }
}

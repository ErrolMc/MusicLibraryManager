namespace MusicLibraryManager.Services;

public interface ISoundCloudAuthService
{
    /// <summary>
    /// Gets whether the user is currently authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Gets the current access token, refreshing if necessary.
    /// </summary>
    Task<string?> GetAccessTokenAsync();

    /// <summary>
    /// Starts the OAuth flow: opens browser, waits for callback via local HTTP server, exchanges code for tokens.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>True if authentication succeeded</returns>
    Task<bool> StartOAuthFlowAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads stored tokens from local storage.
    /// </summary>
    Task<bool> TryLoadStoredTokensAsync();

    /// <summary>
    /// Signs out and clears stored tokens.
    /// </summary>
    Task SignOutAsync();

    /// <summary>
    /// Event raised when authentication state changes.
    /// </summary>
    event EventHandler<bool>? AuthenticationStateChanged;
}

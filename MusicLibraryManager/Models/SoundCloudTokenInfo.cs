namespace MusicLibraryManager.Models;

public partial record SoundCloudTokenInfo(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt
)
{
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool NeedsRefresh => DateTime.UtcNow >= ExpiresAt.AddMinutes(-5); // Refresh 5 min before expiry
}

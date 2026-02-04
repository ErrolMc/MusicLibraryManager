namespace MusicLibraryManager.Models;

public partial record SoundCloudTrack(
    long Id,
    string Title,
    string? Artist,
    string? ArtworkUrl,
    string? Genre,
    string? Description,
    int DurationMs,
    string? StreamUrl,
    string PermalinkUrl,
    DateTime CreatedAt
)
{
    public string FormattedDuration
    {
        get
        {
            var timeSpan = TimeSpan.FromMilliseconds(DurationMs);
            return timeSpan.Hours > 0
                ? $"{timeSpan.Hours}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}"
                : $"{timeSpan.Minutes}:{timeSpan.Seconds:D2}";
        }
    }

    public string? Year => CreatedAt.Year.ToString();
}

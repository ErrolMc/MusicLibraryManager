using System.Globalization;
using MusicLibraryManager.ViewModels;
using MusicLibraryManager.ViewModels.SoundCloud;

namespace MusicLibraryManager.Services.Concrete;

public class TrackMatchingService : ITrackMatchingService
{
    public SoundCloudSearchItemViewModel? FindBestMatch(
        TrackListItemViewModel local,
        IReadOnlyList<SoundCloudSearchItemViewModel> candidates)
    {
        SoundCloudSearchItemViewModel? best = null;
        var bestScore = 0;

        foreach (var candidate in candidates)
        {
            var score = CalculateMatchScore(local, candidate);
            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return bestScore >= 30 ? best : null;
    }

    public Task<IReadOnlyList<SoundCloudSearchItemViewModel>> AlignAllTracksAsync(
        IReadOnlyList<TrackListItemViewModel> localTracks,
        IReadOnlyList<SoundCloudSearchItemViewModel> soundCloudTracks)
    {
        var aligned = new List<SoundCloudSearchItemViewModel>();
        var remaining = soundCloudTracks.ToList();

        foreach (var local in localTracks)
        {
            var match = FindBestMatch(local, remaining);
            if (match is not null)
            {
                match.DisplayIndex = local.DisplayIndex;
                match.IsUnmatched = false;
                aligned.Add(match);
                remaining.Remove(match);
            }
            else
            {
                aligned.Add(new SoundCloudSearchItemViewModel
                {
                    DisplayIndex = local.DisplayIndex,
                    IsGap = true,
                    Title = string.Empty,
                    Artist = string.Empty,
                    Duration = string.Empty,
                    TrackId = 0
                });
            }
        }

        foreach (var unmatched in remaining)
        {
            unmatched.DisplayIndex = aligned.Count + 1;
            unmatched.IsUnmatched = true;
            aligned.Add(unmatched);
        }

        return Task.FromResult<IReadOnlyList<SoundCloudSearchItemViewModel>>(aligned);
    }

    private static int CalculateMatchScore(TrackListItemViewModel local, SoundCloudSearchItemViewModel candidate)
    {
        var score = 0;

        var localFile = Normalize(local.FileNameWithoutExtension);
        var localTitle = Normalize(local.SongName);
        var localArtist = Normalize(local.Artist);
        var localAlbum = Normalize(local.AlbumTitle);

        var remoteTitle = Normalize(candidate.Title);
        var remoteArtist = Normalize(candidate.Artist);

        if (!string.IsNullOrWhiteSpace(localTitle) && localTitle == remoteTitle)
        {
            score += 40;
        }
        else if (!string.IsNullOrWhiteSpace(localTitle) && ContainsEither(localTitle, remoteTitle))
        {
            score += 20;
        }

        if (!string.IsNullOrWhiteSpace(localArtist) && localArtist == remoteArtist)
        {
            score += 40;
        }
        else if (!string.IsNullOrWhiteSpace(localArtist) && ContainsEither(localArtist, remoteArtist))
        {
            score += 20;
        }

        if (!string.IsNullOrWhiteSpace(localFile) && ContainsEither(localFile, remoteTitle))
        {
            score += 20;
        }

        if (!string.IsNullOrWhiteSpace(localAlbum) && ContainsEither(localAlbum, remoteTitle))
        {
            score += 10;
        }

        return score;
    }

    private static bool ContainsEither(string left, string right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        return left.Contains(right, StringComparison.OrdinalIgnoreCase) ||
               right.Contains(left, StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var chars = value
            .ToLower(CultureInfo.InvariantCulture)
            .Where(char.IsLetterOrDigit)
            .ToArray();

        return new string(chars);
    }
}

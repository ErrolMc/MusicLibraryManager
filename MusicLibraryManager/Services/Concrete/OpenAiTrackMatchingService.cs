using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MusicLibraryManager.Models;
using MusicLibraryManager.ViewModels;
using MusicLibraryManager.ViewModels.SoundCloud;
using System.Diagnostics;

namespace MusicLibraryManager.Services.Concrete;

public class OpenAiTrackMatchingService : ITrackMatchingService
{
    private const int MaxLocalTracksPerBatch = 75;
    private const int OpenAiRequestTimeoutSeconds = 180;
    private static readonly string DiagnosticsDirectory = Path.Combine(AppContext.BaseDirectory, "OpenAiDiagnostics");
    private const bool UseHardcodedResponsePath = false;
    private const string HardcodedResponsePath = @"E:\Personal Projects\MusicLibraryManager\MusicLibraryManager\bin\Debug\net10.0-desktop\OpenAiDiagnostics";

    private readonly HttpClient _httpClient;
    private readonly TrackMatchingService _fallback;
    private readonly OpenAiConfig _config;
    private readonly ILogger<OpenAiTrackMatchingService> _logger;

    public OpenAiTrackMatchingService(
        HttpClient httpClient,
        IOptions<OpenAiConfig> config,
        ILogger<OpenAiTrackMatchingService> logger)
    {
        _httpClient = httpClient;
        _fallback = new TrackMatchingService();
        _config = config.Value;
        _logger = logger;
    }

    public SoundCloudSearchItemViewModel? FindBestMatch(
        TrackListItemViewModel local,
        IReadOnlyList<SoundCloudSearchItemViewModel> candidates)
    {
        return _fallback.FindBestMatch(local, candidates);
    }

    public async Task<IReadOnlyList<SoundCloudSearchItemViewModel>> AlignAllTracksAsync(
        IReadOnlyList<TrackListItemViewModel> localTracks,
        IReadOnlyList<SoundCloudSearchItemViewModel> soundCloudTracks)
    {
        _logger.LogInformation(
            "OpenAI alignment started. LocalTracks={LocalCount}, SoundCloudTracks={SoundCloudCount}",
            localTracks.Count,
            soundCloudTracks.Count);

        if (UseHardcodedResponsePath &&
            TryLoadMappingsFromHardcodedPath(localTracks.Count, out var fileMappings))
        {
            _logger.LogInformation("Loaded mappings from hardcoded response path. MappingsCount={MappingsCount}", fileMappings.Count);
            return BuildAlignedResult(localTracks, soundCloudTracks, fileMappings);
        }

        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("OPENAI_API_KEY is not set. Falling back to local matcher.");
            return await _fallback.AlignAllTracksAsync(localTracks, soundCloudTracks);
        }

        try
        {
            var mappings = await RequestMappingsInBatchesAsync(localTracks, soundCloudTracks, apiKey);
            _logger.LogInformation("OpenAI response parsed. MappingsCount={MappingsCount}", mappings.Count);

            if (mappings.Count == 0)
            {
                _logger.LogWarning("OpenAI returned no mappings. Falling back to local matcher.");
                return await _fallback.AlignAllTracksAsync(localTracks, soundCloudTracks);
            }

            _logger.LogInformation("OpenAI alignment succeeded.");
            return BuildAlignedResult(localTracks, soundCloudTracks, mappings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI alignment failed. Falling back to local matcher.");
            return await _fallback.AlignAllTracksAsync(localTracks, soundCloudTracks);
        }
    }

    private bool TryLoadMappingsFromHardcodedPath(int localTrackCount, out IReadOnlyList<TrackMapping> mappings)
    {
        mappings = [];

        try
        {
            if (File.Exists(HardcodedResponsePath))
            {
                var fileMappings = ReadMappingsFromFile(HardcodedResponsePath, 0, localTrackCount);
                if (fileMappings.Count > 0)
                {
                    mappings = fileMappings;
                    return true;
                }

                return false;
            }

            if (!Directory.Exists(HardcodedResponsePath))
            {
                _logger.LogWarning("Hardcoded response path does not exist: {Path}", HardcodedResponsePath);
                return false;
            }

            var parsedFiles = Directory
                .GetFiles(HardcodedResponsePath, "parsed-mappings.json", SearchOption.AllDirectories)
                .Select(path => new
                {
                    Path = path,
                    DirectoryName = Path.GetFileName(Path.GetDirectoryName(path) ?? string.Empty),
                    LastWriteUtc = File.GetLastWriteTimeUtc(path)
                })
                .ToList();

            if (parsedFiles.Count == 0)
            {
                _logger.LogWarning("No parsed-mappings.json files found under hardcoded response path: {Path}", HardcodedResponsePath);
                return false;
            }

            var byBatch = new Dictionary<int, (string Path, DateTime LastWriteUtc, int TotalBatches)>();
            foreach (var file in parsedFiles)
            {
                var match = Regex.Match(file.DirectoryName, @"batch-(\d+)-of-(\d+)", RegexOptions.IgnoreCase);
                var batchIndex = 1;
                var totalBatches = 1;

                if (match.Success)
                {
                    batchIndex = int.Parse(match.Groups[1].Value);
                    totalBatches = int.Parse(match.Groups[2].Value);
                }

                if (!byBatch.TryGetValue(batchIndex, out var current) || file.LastWriteUtc > current.LastWriteUtc)
                {
                    byBatch[batchIndex] = (file.Path, file.LastWriteUtc, totalBatches);
                }
            }

            var loaded = new List<TrackMapping>();
            foreach (var batch in byBatch.OrderBy(x => x.Key))
            {
                var offset = (batch.Key - 1) * MaxLocalTracksPerBatch;
                var batchMappings = ReadMappingsFromFile(batch.Value.Path, offset, localTrackCount);
                loaded.AddRange(batchMappings);
            }

            mappings = loaded;
            return mappings.Count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load mappings from hardcoded response path: {Path}", HardcodedResponsePath);
            return false;
        }
    }

    private static IReadOnlyList<TrackMapping> ReadMappingsFromFile(string filePath, int localIndexOffset, int localTrackCount)
    {
        var content = File.ReadAllText(filePath);
        var envelope = JsonSerializer.Deserialize<MappingEnvelope>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (envelope?.Mappings is null)
        {
            return [];
        }

        return envelope.Mappings
            .Select(m => new TrackMapping
            {
                LocalIndex = m.LocalIndex + localIndexOffset,
                SoundCloudIndex = m.SoundCloudIndex,
                Confidence = m.Confidence
            })
            .Where(m => m.LocalIndex >= 0 && m.LocalIndex < localTrackCount)
            .ToList();
    }

    private async Task<IReadOnlyList<TrackMapping>> RequestMappingsInBatchesAsync(
        IReadOnlyList<TrackListItemViewModel> localTracks,
        IReadOnlyList<SoundCloudSearchItemViewModel> soundCloudTracks,
        string apiKey)
    {
        if (localTracks.Count <= MaxLocalTracksPerBatch)
        {
            return await RequestMappingsAsync(localTracks, soundCloudTracks, apiKey, "batch-1-of-1");
        }

        var mappings = new List<TrackMapping>();
        var globallyUsedSoundCloudIndexes = new HashSet<int>();
        var batchCount = (int)Math.Ceiling(localTracks.Count / (double)MaxLocalTracksPerBatch);

        for (var batchIndex = 0; batchIndex < batchCount; batchIndex++)
        {
            var start = batchIndex * MaxLocalTracksPerBatch;
            var batchLocals = localTracks.Skip(start).Take(MaxLocalTracksPerBatch).ToList();

            _logger.LogInformation(
                "Sending OpenAI batch {Batch}/{TotalBatches}. BatchLocalTracks={BatchLocalCount}, Offset={Offset}",
                batchIndex + 1,
                batchCount,
                batchLocals.Count,
                start);

            var availableCandidates = soundCloudTracks
                .Select((track, index) => new { Track = track, Index = index })
                .Where(x => !globallyUsedSoundCloudIndexes.Contains(x.Index))
                .ToList();

            if (availableCandidates.Count == 0)
            {
                _logger.LogInformation(
                    "No remaining SoundCloud candidates for batch {Batch}/{TotalBatches}.",
                    batchIndex + 1,
                    batchCount);
                break;
            }

            var batchMappings = await RequestMappingsAsync(
                batchLocals,
                availableCandidates.Select(x => x.Track).ToList(),
                apiKey,
                $"batch-{batchIndex + 1}-of-{batchCount}");

            foreach (var mapping in batchMappings)
            {
                int? remappedSoundCloudIndex = null;
                if (mapping.SoundCloudIndex is int batchSoundCloudIndex &&
                    batchSoundCloudIndex >= 0 &&
                    batchSoundCloudIndex < availableCandidates.Count)
                {
                    var globalIndex = availableCandidates[batchSoundCloudIndex].Index;
                    remappedSoundCloudIndex = globalIndex;

                    if (mapping.Confidence >= 0.7)
                    {
                        globallyUsedSoundCloudIndexes.Add(globalIndex);
                    }
                }

                mappings.Add(new TrackMapping
                {
                    LocalIndex = mapping.LocalIndex + start,
                    SoundCloudIndex = remappedSoundCloudIndex,
                    Confidence = mapping.Confidence
                });
            }

            _logger.LogInformation(
                "Completed OpenAI batch {Batch}/{TotalBatches}. ReservedSoundCloudCandidates={ReservedCount}",
                batchIndex + 1,
                batchCount,
                globallyUsedSoundCloudIndexes.Count);
        }

        return mappings;
    }

    private async Task<IReadOnlyList<TrackMapping>> RequestMappingsAsync(
        IReadOnlyList<TrackListItemViewModel> localTracks,
        IReadOnlyList<SoundCloudSearchItemViewModel> soundCloudTracks,
        string apiKey,
        string traceLabel)
    {
        var endpoint = string.IsNullOrWhiteSpace(_config.Endpoint)
            ? "https://api.openai.com/v1/chat/completions"
            : _config.Endpoint;

        var model = string.IsNullOrWhiteSpace(_config.Model)
            ? "gpt-4o-mini"
            : _config.Model;

        var prompt = BuildPrompt(localTracks, soundCloudTracks);
        _logger.LogInformation(
            "OpenAI prompt built. PromptChars={PromptChars}, LocalTracks={LocalCount}, SoundCloudTracks={SoundCloudCount}",
            prompt.Length,
            localTracks.Count,
            soundCloudTracks.Count);
        _logger.LogInformation("OpenAI full prompt: {Prompt}", prompt);

        var traceId = BuildTraceId(traceLabel);
        await PersistDiagnosticTextAsync(traceId, "prompt.txt", prompt);

        var payload = new
        {
            model,
            temperature = 0,
            response_format = new { type = "json_object" },
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content = "You are a music track matcher. Return strict minified JSON only, all on one line, with no newline escape sequences (no \\n or \\r)."
                },
                new
                {
                    role = "user",
                    content = prompt
                }
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, endpoint);

        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        var payloadJson = JsonSerializer.Serialize(payload);
        _logger.LogInformation("Sending OpenAI request. Endpoint={Endpoint}, Model={Model}, PayloadChars={PayloadChars}", endpoint, model, payloadJson.Length);
        _logger.LogInformation("OpenAI payload preview: {PayloadPreview}", TruncateForLog(payloadJson, 5000));
        await PersistDiagnosticTextAsync(traceId, "request.json", payloadJson);

        request.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(OpenAiRequestTimeoutSeconds));
        var stopwatch = Stopwatch.StartNew();

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, timeoutCts.Token);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "OpenAI request timed out after {ElapsedMs}ms (timeout={TimeoutSeconds}s). Falling back to local matcher.", stopwatch.ElapsedMilliseconds, OpenAiRequestTimeoutSeconds);
            throw;
        }

        _logger.LogInformation("OpenAI HTTP response received. StatusCode={StatusCode}, ElapsedMs={ElapsedMs}", (int)response.StatusCode, stopwatch.ElapsedMilliseconds);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        _logger.LogInformation("OpenAI raw response received. ResponseChars={ResponseChars}", json.Length);
        _logger.LogInformation("OpenAI raw response preview: {ResponsePreview}", TruncateForLog(json, 5000));
        await PersistDiagnosticTextAsync(traceId, "response.json", json);
        var completion = JsonSerializer.Deserialize<OpenAiChatCompletionResponse>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var content = completion?.Choices?.FirstOrDefault()?.Message?.Content;
        if (string.IsNullOrWhiteSpace(content))
        {
            _logger.LogWarning("OpenAI response had empty message content. ChoicesCount={ChoicesCount}", completion?.Choices?.Count ?? 0);
            return [];
        }

        _logger.LogInformation("OpenAI message content chars={ContentChars}", content.Length);
        _logger.LogInformation("OpenAI message content preview: {ContentPreview}", TruncateForLog(content, 5000));
        await PersistDiagnosticTextAsync(traceId, "message-content.json", content);

        var mappingEnvelope = JsonSerializer.Deserialize<MappingEnvelope>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (mappingEnvelope?.Mappings is null)
        {
            _logger.LogWarning("OpenAI message content did not parse to mappings envelope.");
            return [];
        }

        _logger.LogInformation("Parsed mapping envelope successfully. MappingCount={MappingCount}", mappingEnvelope.Mappings.Count);
        await PersistDiagnosticTextAsync(traceId, "parsed-mappings.json", JsonSerializer.Serialize(mappingEnvelope));
        return mappingEnvelope?.Mappings ?? [];
    }

    private static string BuildTraceId(string traceLabel)
    {
        var safeLabel = string.Concat(traceLabel.Select(ch => char.IsLetterOrDigit(ch) || ch == '-' || ch == '_' ? ch : '-'));
        return $"{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}-{safeLabel}";
    }

    private async Task PersistDiagnosticTextAsync(string traceId, string fileName, string content)
    {
        try
        {
            Directory.CreateDirectory(DiagnosticsDirectory);
            var traceDirectory = Path.Combine(DiagnosticsDirectory, traceId);
            Directory.CreateDirectory(traceDirectory);
            var path = Path.Combine(traceDirectory, fileName);
            await File.WriteAllTextAsync(path, content);
            _logger.LogInformation("OpenAI diagnostic file written: {Path}", path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist OpenAI diagnostic file {TraceId}/{FileName}", traceId, fileName);
        }
    }

    private static string TruncateForLog(string? value, int maxChars)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var normalized = value.Replace("\r", "\\r").Replace("\n", "\\n");
        if (normalized.Length <= maxChars)
        {
            return normalized;
        }

        return normalized[..maxChars] + "...(truncated)";
    }

    private static string BuildPrompt(
        IReadOnlyList<TrackListItemViewModel> localTracks,
        IReadOnlyList<SoundCloudSearchItemViewModel> soundCloudTracks)
    {
        var local = localTracks.Select((t, i) => new
        {
            index = i,
            fileName = t.FileNameWithoutExtension,
            title = t.SongName,
            artist = t.Artist,
            album = t.AlbumTitle
        });

        var soundCloud = soundCloudTracks.Select((t, i) => new
        {
            index = i,
            title = t.Title,
            artist = t.Artist,
            album = t.Album
        });

        var localJson = JsonSerializer.Serialize(local);
        var scJson = JsonSerializer.Serialize(soundCloud);

        return $"""
                Match local tracks to soundcloud tracks.
                Use filename, title, artist, album similarity.
                For each local track provide the best soundcloud index or null.
                If confidence < 0.7 use null.
                Do not reuse a soundcloud index for multiple locals.
                Return strict JSON with a top-level property named mappings containing an array of objects with localIndex, soundCloudIndex, confidence.
                Return MINIFIED one-line JSON only (no pretty printing).
                Do not include newline characters or escaped newline sequences like \n or \r anywhere in the output.

                Local tracks JSON:
                {localJson}

                SoundCloud tracks JSON:
                {scJson}
                """;
    }

    private static IReadOnlyList<SoundCloudSearchItemViewModel> BuildAlignedResult(
        IReadOnlyList<TrackListItemViewModel> localTracks,
        IReadOnlyList<SoundCloudSearchItemViewModel> soundCloudTracks,
        IReadOnlyList<TrackMapping> mappings)
    {
        var mappingByLocal = mappings
            .Where(m => m.LocalIndex >= 0 && m.LocalIndex < localTracks.Count)
            .GroupBy(m => m.LocalIndex)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(x => x.Confidence).First());

        var usedSoundCloudIndexes = new HashSet<int>();
        var aligned = new List<SoundCloudSearchItemViewModel>();

        for (var localIndex = 0; localIndex < localTracks.Count; localIndex++)
        {
            var local = localTracks[localIndex];
            if (mappingByLocal.TryGetValue(localIndex, out var mapping) &&
                mapping.SoundCloudIndex is int scIndex &&
                scIndex >= 0 &&
                scIndex < soundCloudTracks.Count &&
                mapping.Confidence >= 0.7 &&
                !usedSoundCloudIndexes.Contains(scIndex))
            {
                var item = soundCloudTracks[scIndex];
                item.DisplayIndex = local.DisplayIndex;
                item.IsGap = false;
                item.IsUnmatched = false;
                aligned.Add(item);
                usedSoundCloudIndexes.Add(scIndex);
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

        for (var scIndex = 0; scIndex < soundCloudTracks.Count; scIndex++)
        {
            if (usedSoundCloudIndexes.Contains(scIndex))
            {
                continue;
            }

            var item = soundCloudTracks[scIndex];
            item.DisplayIndex = aligned.Count + 1;
            item.IsGap = false;
            item.IsUnmatched = true;
            aligned.Add(item);
        }

        return aligned;
    }

    private sealed class OpenAiChatCompletionResponse
    {
        public List<Choice>? Choices { get; set; }
    }

    private sealed class Choice
    {
        public Message? Message { get; set; }
    }

    private sealed class Message
    {
        public string? Content { get; set; }
    }

    private sealed class MappingEnvelope
    {
        public List<TrackMapping>? Mappings { get; set; }
    }

    private sealed class TrackMapping
    {
        public int LocalIndex { get; set; }
        public int? SoundCloudIndex { get; set; }
        public double Confidence { get; set; }
    }
}

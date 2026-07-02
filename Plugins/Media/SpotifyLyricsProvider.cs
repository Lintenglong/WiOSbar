using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FluidBar;

/// <summary>
/// Spotify 歌词提供者
/// 支持通过 Spotify Web API 或第三方歌词 API 获取歌词
/// 注意：Spotify 官方 API 需要 OAuth 认证，这里使用公共歌词 API 作为备选
/// </summary>
public sealed class SpotifyLyricsProvider : ILyricsProvider
{
    private static readonly HttpClient Http = CreateHttpClient();

    private readonly Dictionary<string, SpotifyTrackInfo?> _metadataCache = new();
    private readonly Dictionary<string, List<LrcLine>> _lyricsCache = new();
    private readonly Dictionary<string, DateTime> _lyricsMissUntil = new();
    private string? _lastKey;
    private List<LrcLine>? _lastLyrics;
    private DateTime _lastFetchTime = DateTime.MinValue;
    private static readonly TimeSpan LyricsMissTtl = TimeSpan.FromSeconds(90);

    public string? TryGetCurrentLine(MediaSnapshot snapshot, TimeSpan position)
    {
        var enriched = EnrichSnapshot(snapshot, position);
        return enriched.LyricLine;
    }

    public MediaSnapshot EnrichSnapshot(MediaSnapshot snapshot, TimeSpan position)
    {
        MediaSnapshot? best = null;
        foreach (var (title, artist) in BuildLookupCandidates(snapshot.Title, snapshot.Artist))
        {
            if (string.IsNullOrWhiteSpace(title) || IsKnownNonSongString(title))
                continue;

            var enriched = EnrichSnapshotForTrack(snapshot, position, title, artist);
            if (!string.IsNullOrWhiteSpace(enriched.LyricLine))
                return enriched;

            if (best is null ||
                (string.IsNullOrWhiteSpace(best.AlbumArtPath) &&
                 !string.IsNullOrWhiteSpace(enriched.AlbumArtPath)))
            {
                best = enriched;
            }
        }

        return best ?? snapshot;
    }

    private MediaSnapshot EnrichSnapshotForTrack(
        MediaSnapshot snapshot,
        TimeSpan position,
        string title,
        string artist)
    {
        var cacheKey = MakeCacheKey(title, artist);
        var duration = ResolveDuration(snapshot);
        var metadata = GetMetadata(title, artist, cacheKey);

        var albumArtPath = snapshot.AlbumArtPath;
        if (string.IsNullOrWhiteSpace(albumArtPath) && !string.IsNullOrWhiteSpace(metadata?.AlbumArtUrl))
            albumArtPath = DownloadAlbumArt(metadata.AlbumArtUrl, cacheKey);

        var lyricLine = snapshot.LyricLine;
        string? secondaryLyricLine = null;
        if (string.IsNullOrWhiteSpace(lyricLine))
        {
            var lines = GetLyrics(metadata, title, artist, duration, cacheKey);
            lyricLine = SelectCurrentLine(lines, position);
            secondaryLyricLine = SelectNextLine(lines, position);
        }

        return snapshot with
        {
            Title = title,
            Artist = artist,
            AlbumArtPath = albumArtPath,
            LyricLine = lyricLine,
            SecondaryLyricLine = secondaryLyricLine
        };
    }

    private static (string Title, string Artist) NormalizeTrackText(string? rawTitle, string? rawArtist)
    {
        var title = CleanSongText(rawTitle);
        var artist = CleanSongText(rawArtist);
        return NormalizeTitleAndArtist(title, artist);
    }

    private static IEnumerable<(string Title, string Artist)> BuildLookupCandidates(string? rawTitle, string? rawArtist)
    {
        var (title, artist) = NormalizeTrackText(rawTitle, rawArtist);
        if (!string.IsNullOrWhiteSpace(title))
            yield return (title, artist);

        if (!string.IsNullOrWhiteSpace(rawTitle) && rawTitle.Contains(" - "))
        {
            var parts = rawTitle.Split(new[] { " - " }, 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
            {
                var extractedTitle = CleanSongText(parts[0]);
                var extractedArtist = CleanSongText(parts[1]);
                if (!string.IsNullOrWhiteSpace(extractedTitle))
                    yield return (extractedTitle, extractedArtist);
            }
        }
    }

    private static string CleanSongText(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var cleaned = input.Trim();

        var suffixesToRemove = new[]
        {
            "(Official Audio)", "(Official Video)", "(Lyrics)", "(Lyric Video)",
            "【官方MV】", "【歌词版】", "（官方视频）", "（歌词版）",
            "[Official Audio]", "[Official Video]", "[Lyrics]",
            " - Spotify", " | Spotify"
        };

        foreach (var suffix in suffixesToRemove)
        {
            if (cleaned.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                cleaned = cleaned.Substring(0, cleaned.Length - suffix.Length).Trim();
        }

        while (cleaned.Contains("  "))
            cleaned = cleaned.Replace("  ", " ");

        return cleaned;
    }

    private static (string Title, string Artist) NormalizeTitleAndArtist(string title, string artist)
    {
        if (string.IsNullOrWhiteSpace(artist) && title.Contains(" - "))
        {
            var idx = title.IndexOf(" - ", StringComparison.Ordinal);
            return (title.Substring(0, idx).Trim(), title.Substring(idx + 3).Trim());
        }

        return (title, artist);
    }

    private static bool IsKnownNonSongString(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return true;

        var lower = text.ToLowerInvariant();
        return lower.Contains("advertisement") ||
               lower.Contains("广告") ||
               lower.Contains("sponsored") ||
               lower.Contains("notification") ||
               lower.Contains("通知");
    }

    private static string MakeCacheKey(string title, string artist)
    {
        return $"{title}|{artist}".ToLowerInvariant();
    }

    private static TimeSpan ResolveDuration(MediaSnapshot snapshot)
    {
        if (snapshot.EndTicks > snapshot.StartTimeTicks)
            return TimeSpan.FromTicks(snapshot.EndTicks - snapshot.StartTimeTicks);
        return TimeSpan.Zero;
    }

    private SpotifyTrackInfo? GetMetadata(string title, string artist, string cacheKey)
    {
        if (_metadataCache.TryGetValue(cacheKey, out var cached))
            return cached;

        if (_lyricsMissUntil.TryGetValue(cacheKey, out var missUntil) && DateTime.UtcNow < missUntil)
            return null;

        try
        {
            var query = string.IsNullOrWhiteSpace(artist) ? title : $"{title} {artist}";
            // 使用公共歌词搜索 API
            var searchUrl = $"https://api.lyrics.ovh/v1/{Uri.EscapeDataString(artist)}/{Uri.EscapeDataString(title)}";

            using var response = Http.GetAsync(searchUrl).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
            {
                _lyricsMissUntil[cacheKey] = DateTime.UtcNow.Add(LyricsMissTtl);
                return null;
            }

            var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var metadata = ParseLyricsResponse(json, title, artist);

            if (metadata != null)
            {
                _metadataCache[cacheKey] = metadata;
            }
            else
            {
                _lyricsMissUntil[cacheKey] = DateTime.UtcNow.Add(LyricsMissTtl);
            }

            return metadata;
        }
        catch
        {
            _lyricsMissUntil[cacheKey] = DateTime.UtcNow.Add(LyricsMissTtl);
            return null;
        }
    }

    private static SpotifyTrackInfo? ParseLyricsResponse(string json, string title, string artist)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("lyrics", out var lyricsProp))
                return null;

            var lyrics = lyricsProp.GetString();
            if (string.IsNullOrWhiteSpace(lyrics))
                return null;

            // lyrics.ovh 不返回元数据，仅返回歌词文本
            // 我们创建一个简化的 TrackInfo
            return new SpotifyTrackInfo(title, artist, null, null, lyrics);
        }
        catch
        {
            return null;
        }
    }

    private List<LrcLine>? GetLyrics(SpotifyTrackInfo? metadata, string title, string artist, TimeSpan duration, string cacheKey)
    {
        if (_lyricsCache.TryGetValue(cacheKey, out var cached))
            return cached;

        if (_lyricsMissUntil.TryGetValue(cacheKey, out var missUntil) && DateTime.UtcNow < missUntil)
            return null;

        try
        {
            string? lyricsText = metadata?.LyricsText;

            // 如果没有缓存的歌词，尝试直接请求
            if (string.IsNullOrWhiteSpace(lyricsText))
            {
                var searchUrl = $"https://api.lyrics.ovh/v1/{Uri.EscapeDataString(artist)}/{Uri.EscapeDataString(title)}";

                using var response = Http.GetAsync(searchUrl).GetAwaiter().GetResult();
                if (!response.IsSuccessStatusCode)
                {
                    _lyricsMissUntil[cacheKey] = DateTime.UtcNow.Add(LyricsMissTtl);
                    return null;
                }

                var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                var parsed = ParseLyricsResponse(json, title, artist);
                lyricsText = parsed?.LyricsText;
            }

            if (string.IsNullOrWhiteSpace(lyricsText))
            {
                _lyricsMissUntil[cacheKey] = DateTime.UtcNow.Add(LyricsMissTtl);
                return null;
            }

            var lines = ParsePlainLyrics(lyricsText);

            if (lines.Count > 0)
            {
                _lyricsCache[cacheKey] = lines;
                _lastKey = cacheKey;
                _lastLyrics = lines;
                _lastFetchTime = DateTime.UtcNow;
            }
            else
            {
                _lyricsMissUntil[cacheKey] = DateTime.UtcNow.Add(LyricsMissTtl);
            }

            return lines.Count > 0 ? lines : null;
        }
        catch
        {
            _lyricsMissUntil[cacheKey] = DateTime.UtcNow.Add(LyricsMissTtl);
            return null;
        }
    }

    /// <summary>
    /// 解析纯文本歌词（无时间戳）为 LrcLine 列表
    /// </summary>
    private static List<LrcLine> ParsePlainLyrics(string lyricsText)
    {
        var lines = new List<LrcLine>();
        var lineIndex = 0;
        var estimatedDuration = TimeSpan.FromMinutes(3); // 默认 3 分钟

        foreach (var rawLine in lyricsText.Split('\n'))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // 移除常见的歌词标记
            line = Regex.Replace(line, @"\[.*?\]", "").Trim();
            line = Regex.Replace(line, @"\(.*?\)", "").Trim();

            if (!string.IsNullOrWhiteSpace(line))
            {
                // 为无时间戳歌词分配估算时间
                var estimatedTime = TimeSpan.FromSeconds(lineIndex * 15); // 每行约 15 秒
                lines.Add(new LrcLine(estimatedTime, line));
                lineIndex++;
            }
        }

        return lines;
    }

    private static string? SelectCurrentLine(List<LrcLine>? lines, TimeSpan position)
    {
        if (lines == null || lines.Count == 0)
            return null;

        LrcLine? current = null;
        foreach (var line in lines)
        {
            if (line.Time <= position)
                current = line;
            else
                break;
        }

        return current?.Text;
    }

    private static string? SelectNextLine(List<LrcLine>? lines, TimeSpan position)
    {
        if (lines == null || lines.Count == 0)
            return null;

        foreach (var line in lines)
        {
            if (line.Time > position)
                return line.Text;
        }

        return null;
    }

    private static string? DownloadAlbumArt(string url, string cacheKey)
    {
        try
        {
            var cacheDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FluidBar", "albumart");

            Directory.CreateDirectory(cacheDir);

            var fileName = $"{cacheKey.GetHashCode():X8}.jpg";
            var filePath = Path.Combine(cacheDir, fileName);

            if (File.Exists(filePath) && new FileInfo(filePath).Length > 0)
                return filePath;

            using var response = Http.GetAsync(url).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
                return null;

            var bytes = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
            File.WriteAllBytes(filePath, bytes);

            return filePath;
        }
        catch
        {
            return null;
        }
    }

    private static HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };

        var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(8)
        };

        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

        client.DefaultRequestHeaders.Accept.ParseAdd("application/json, text/plain, */*");

        return client;
    }
}

/// <summary>
/// Spotify 歌曲元数据
/// </summary>
public sealed record SpotifyTrackInfo(
    string SongName,
    string ArtistName,
    string? AlbumName,
    string? AlbumArtUrl,
    string? LyricsText);

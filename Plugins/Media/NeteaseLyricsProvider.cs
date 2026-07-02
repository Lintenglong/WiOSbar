using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace FluidBar;

/// <summary>
/// 网易云音乐歌词提供者
/// 支持通过网易云开放 API 搜索歌曲并获取实时歌词
/// </summary>
public sealed class NeteaseLyricsProvider : ILyricsProvider
{
    private static readonly HttpClient Http = CreateHttpClient();

    private readonly Dictionary<string, NeteaseTrackInfo?> _metadataCache = new();
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

        // 尝试从标题中提取艺术家信息（常见格式："Song - Artist"）
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

        // 移除常见后缀
        var suffixesToRemove = new[]
        {
            "(Official Audio)", "(Official Video)", "(Lyrics)", "(Lyric Video)",
            "【官方MV】", "【歌词版】", "（官方视频）", "（歌词版）",
            "[Official Audio]", "[Official Video]", "[Lyrics]"
        };

        foreach (var suffix in suffixesToRemove)
        {
            if (cleaned.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                cleaned = cleaned.Substring(0, cleaned.Length - suffix.Length).Trim();
        }

        // 移除多余空格
        while (cleaned.Contains("  "))
            cleaned = cleaned.Replace("  ", " ");

        return cleaned;
    }

    private static (string Title, string Artist) NormalizeTitleAndArtist(string title, string artist)
    {
        // 如果标题包含艺术家信息，尝试分离
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

    private NeteaseTrackInfo? GetMetadata(string title, string artist, string cacheKey)
    {
        if (_metadataCache.TryGetValue(cacheKey, out var cached))
            return cached;

        // 检查是否在失败冷却期
        if (_lyricsMissUntil.TryGetValue(cacheKey, out var missUntil) && DateTime.UtcNow < missUntil)
            return null;

        try
        {
            var query = string.IsNullOrWhiteSpace(artist) ? title : $"{title} {artist}";
            var searchUrl = $"https://music.163.com/api/search/get/web?csrf_token=&s={Uri.EscapeDataString(query)}&type=1&offset=0&total=true&limit=10";

            using var response = Http.GetAsync(searchUrl).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
            {
                _lyricsMissUntil[cacheKey] = DateTime.UtcNow.Add(LyricsMissTtl);
                return null;
            }

            var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var metadata = ParseSearchMetadata(json);

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

    private static NeteaseTrackInfo? ParseSearchMetadata(string searchJson)
    {
        try
        {
            using var searchDoc = JsonDocument.Parse(searchJson);

            if (!searchDoc.RootElement.TryGetProperty("result", out var result))
                return null;

            if (!result.TryGetProperty("songs", out var songs) || songs.GetArrayLength() == 0)
                return null;

            var first = songs[0];

            long songId = 0;
            if (first.TryGetProperty("id", out var idProp))
                songId = idProp.GetInt64();

            string songName = "";
            if (first.TryGetProperty("name", out var nameProp))
                songName = nameProp.GetString() ?? "";

            string artistName = "";
            if (first.TryGetProperty("artists", out var artists) && artists.GetArrayLength() > 0)
            {
                var firstArtist = artists[0];
                if (firstArtist.TryGetProperty("name", out var artistNameProp))
                    artistName = artistNameProp.GetString() ?? "";
            }

            string? albumArtUrl = null;
            if (first.TryGetProperty("album", out var album) &&
                album.TryGetProperty("picUrl", out var picUrlProp))
            {
                albumArtUrl = picUrlProp.GetString();
            }

            if (songId == 0 || string.IsNullOrWhiteSpace(songName))
                return null;

            return new NeteaseTrackInfo(songId, songName, artistName, albumArtUrl);
        }
        catch
        {
            return null;
        }
    }

    private List<LrcLine>? GetLyrics(NeteaseTrackInfo? metadata, string title, string artist, TimeSpan duration, string cacheKey)
    {
        if (_lyricsCache.TryGetValue(cacheKey, out var cached))
            return cached;

        if (_lyricsMissUntil.TryGetValue(cacheKey, out var missUntil) && DateTime.UtcNow < missUntil)
            return null;

        try
        {
            long songId = metadata?.SongId ?? 0;

            // 如果没有 metadata，尝试通过搜索获取
            if (songId == 0)
            {
                var query = string.IsNullOrWhiteSpace(artist) ? title : $"{title} {artist}";
                var searchUrl = $"https://music.163.com/api/search/get/web?csrf_token=&s={Uri.EscapeDataString(query)}&type=1&offset=0&total=true&limit=5";

                using var searchResponse = Http.GetAsync(searchUrl).GetAwaiter().GetResult();
                if (searchResponse.IsSuccessStatusCode)
                {
                    var searchJson = searchResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    var searchMetadata = ParseSearchMetadata(searchJson);
                    songId = searchMetadata?.SongId ?? 0;
                }
            }

            if (songId == 0)
            {
                _lyricsMissUntil[cacheKey] = DateTime.UtcNow.Add(LyricsMissTtl);
                return null;
            }

            var lyricUrl = $"https://music.163.com/api/song/lyric?id={songId}&lv=-1&kv=-1&tv=-1";

            using var lyricResponse = Http.GetAsync(lyricUrl).GetAwaiter().GetResult();
            if (!lyricResponse.IsSuccessStatusCode)
            {
                _lyricsMissUntil[cacheKey] = DateTime.UtcNow.Add(LyricsMissTtl);
                return null;
            }

            var lyricJson = lyricResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var lines = ParseLyrics(lyricJson);

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

    private static List<LrcLine> ParseLyrics(string lyricJson)
    {
        var lines = new List<LrcLine>();

        try
        {
            using var doc = JsonDocument.Parse(lyricJson);

            // 优先使用带翻译的歌词
            string? lrcContent = null;
            if (doc.RootElement.TryGetProperty("lrc", out var lrc) &&
                lrc.TryGetProperty("lyric", out var lyricProp))
            {
                lrcContent = lyricProp.GetString();
            }

            if (string.IsNullOrWhiteSpace(lrcContent))
                return lines;

            foreach (var rawLine in lrcContent.Split('\n'))
            {
                var line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // 解析 [mm:ss.xx] 格式
                var match = System.Text.RegularExpressions.Regex.Match(line, @"\[(\d+):(\d+\.?\d*)\](.*)");
                if (match.Success)
                {
                    var minutes = int.Parse(match.Groups[1].Value);
                    var seconds = double.Parse(match.Groups[2].Value);
                    var text = match.Groups[3].Value.Trim();

                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        var time = TimeSpan.FromSeconds(minutes * 60 + seconds);
                        lines.Add(new LrcLine(time, text));
                    }
                }
            }

            // 按时间排序
            lines.Sort((a, b) => a.Time.CompareTo(b.Time));
        }
        catch
        {
            // 解析失败返回空列表
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
        client.DefaultRequestHeaders.Referrer = new Uri("https://music.163.com/");

        return client;
    }
}

/// <summary>
/// 网易云歌曲元数据
/// </summary>
public sealed record NeteaseTrackInfo(
    long SongId,
    string SongName,
    string ArtistName,
    string? AlbumArtUrl);

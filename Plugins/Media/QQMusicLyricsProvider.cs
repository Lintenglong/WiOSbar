using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FluidBar;

/// <summary>
/// QQ音乐歌词提供者
/// 使用 QQ 音乐公开 API 获取歌词
/// </summary>
public sealed class QQMusicLyricsProvider : ILyricsProvider
{
    private static readonly HttpClient Http = CreateHttpClient();

    private readonly Dictionary<string, QQTrackInfo?> _metadataCache = new();
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
            "[Official Audio]", "[Official Video]", "[Lyrics]"
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

    private QQTrackInfo? GetMetadata(string title, string artist, string cacheKey)
    {
        if (_metadataCache.TryGetValue(cacheKey, out var cached))
            return cached;

        if (_lyricsMissUntil.TryGetValue(cacheKey, out var missUntil) && DateTime.UtcNow < missUntil)
            return null;

        try
        {
            var query = string.IsNullOrWhiteSpace(artist) ? title : $"{title} {artist}";
            // QQ音乐搜索 API
            var searchUrl = $"https://c.y.qq.com/soso/fcgi-bin/client_search_cp?ct=24&qqmusic_from=98&new_json=1&remoteplace=txt.yqq.top&t=0&aggr=1&cr=1&catZhida=1&lossless=0&flag_qc=0&p=1&n=10&w={Uri.EscapeDataString(query)}&format=json";

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

    private static QQTrackInfo? ParseSearchMetadata(string searchJson)
    {
        try
        {
            using var searchDoc = JsonDocument.Parse(searchJson);

            if (!searchDoc.RootElement.TryGetProperty("data", out var data))
                return null;

            if (!data.TryGetProperty("song", out var song) ||
                !song.TryGetProperty("list", out var list) ||
                list.GetArrayLength() == 0)
                return null;

            var first = list[0];

            string songMid = "";
            if (first.TryGetProperty("mid", out var midProp))
                songMid = midProp.GetString() ?? "";

            string songName = "";
            if (first.TryGetProperty("name", out var nameProp))
                songName = nameProp.GetString() ?? "";

            string artistName = "";
            if (first.TryGetProperty("singer", out var singers) && singers.GetArrayLength() > 0)
            {
                var firstSinger = singers[0];
                if (firstSinger.TryGetProperty("name", out var singerNameProp))
                    artistName = singerNameProp.GetString() ?? "";
            }

            string? albumArtUrl = null;
            if (first.TryGetProperty("album", out var album) &&
                album.TryGetProperty("mid", out var albumMidProp))
            {
                var albumMid = albumMidProp.GetString();
                if (!string.IsNullOrWhiteSpace(albumMid))
                {
                    // QQ音乐专辑封面 URL 格式
                    albumArtUrl = $"https://y.qq.com/music/photo_new/T002R300x300M000{albumMid}.jpg";
                }
            }

            if (string.IsNullOrWhiteSpace(songMid) || string.IsNullOrWhiteSpace(songName))
                return null;

            return new QQTrackInfo(songMid, songName, artistName, albumArtUrl);
        }
        catch
        {
            return null;
        }
    }

    private List<LrcLine>? GetLyrics(QQTrackInfo? metadata, string title, string artist, TimeSpan duration, string cacheKey)
    {
        if (_lyricsCache.TryGetValue(cacheKey, out var cached))
            return cached;

        if (_lyricsMissUntil.TryGetValue(cacheKey, out var missUntil) && DateTime.UtcNow < missUntil)
            return null;

        try
        {
            string? songMid = metadata?.SongMid;

            // 如果没有 metadata，尝试通过搜索获取
            if (string.IsNullOrWhiteSpace(songMid))
            {
                var query = string.IsNullOrWhiteSpace(artist) ? title : $"{title} {artist}";
                var searchUrl = $"https://c.y.qq.com/soso/fcgi-bin/client_search_cp?ct=24&qqmusic_from=98&new_json=1&remoteplace=txt.yqq.top&t=0&aggr=1&cr=1&catZhida=1&lossless=0&flag_qc=0&p=1&n=5&w={Uri.EscapeDataString(query)}&format=json";

                using var searchResponse = Http.GetAsync(searchUrl).GetAwaiter().GetResult();
                if (searchResponse.IsSuccessStatusCode)
                {
                    var searchJson = searchResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    var searchMetadata = ParseSearchMetadata(searchJson);
                    songMid = searchMetadata?.SongMid;
                }
            }

            if (string.IsNullOrWhiteSpace(songMid))
            {
                _lyricsMissUntil[cacheKey] = DateTime.UtcNow.Add(LyricsMissTtl);
                return null;
            }

            // QQ音乐歌词 API
            var lyricUrl = $"https://c.y.qq.com/lyric/fcgi-bin/fcg_query_lyric_new.fcg?songmid={songMid}&g_tk=5381&loginUin=0&hostUin=0&format=json&inCharset=utf8&outCharset=utf-8&notice=0&platform=yqq.json&needNewCode=0";

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

            string? lrcContent = null;

            // QQ音乐返回的歌词在 lyric 字段中（可能是 base64 编码）
            if (doc.RootElement.TryGetProperty("lyric", out var lyricProp))
            {
                lrcContent = lyricProp.GetString();
            }

            if (string.IsNullOrWhiteSpace(lrcContent))
                return lines;

            // 尝试 base64 解码
            try
            {
                var decodedBytes = Convert.FromBase64String(lrcContent);
                lrcContent = Encoding.UTF8.GetString(decodedBytes);
            }
            catch
            {
                // 不是 base64，直接使用原文
            }

            foreach (var rawLine in lrcContent.Split('\n'))
            {
                var line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // 解析 [mm:ss.xx] 格式
                var match = Regex.Match(line, @"\[(\d+):(\d+\.?\d*)\](.*)");
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
        client.DefaultRequestHeaders.Referrer = new Uri("https://y.qq.com/");

        return client;
    }
}

/// <summary>
/// QQ音乐歌曲元数据
/// </summary>
public sealed record QQTrackInfo(
    string SongMid,
    string SongName,
    string ArtistName,
    string? AlbumArtUrl);

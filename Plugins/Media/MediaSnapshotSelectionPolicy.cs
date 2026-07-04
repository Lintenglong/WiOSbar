namespace FluidBar;

public static class MediaSnapshotSelectionPolicy
{
    public static bool ShouldQueryFallback(MediaSnapshot? gsm)
    {
        if (gsm is null)
            return true;

        var priority = GetSourcePriority(gsm.SourceAppUserModelId);
        if (IsAppNameString(gsm.Title))
            return true;

        if (priority >= 100 && !gsm.IsPlaying)
            return true;

        return priority < 100;
    }

    public static MediaSnapshot? ChooseBestSnapshot(MediaSnapshot? gsm, MediaSnapshot? fallback)
    {
        if (gsm is null) return fallback;
        if (fallback is null) return gsm;

        if (IsSamePlayerApp(gsm.SourceAppUserModelId, fallback.SourceAppUserModelId))
        {
            var title = IsAppNameString(gsm.Title) && !IsAppNameString(fallback.Title)
                ? fallback.Title
                : string.IsNullOrWhiteSpace(gsm.Title) ? fallback.Title : gsm.Title;
            var artist = string.IsNullOrWhiteSpace(gsm.Artist) ? fallback.Artist : gsm.Artist;
            var album = string.IsNullOrWhiteSpace(gsm.Album) ? fallback.Album : gsm.Album;
            var lyric = string.IsNullOrWhiteSpace(gsm.LyricLine) ? fallback.LyricLine : gsm.LyricLine;
            var artPath = string.IsNullOrWhiteSpace(gsm.AlbumArtPath) ? fallback.AlbumArtPath : gsm.AlbumArtPath;
            var iconPath = string.IsNullOrWhiteSpace(gsm.SourceIconPath) ? fallback.SourceIconPath : gsm.SourceIconPath;

            return gsm with
            {
                Title = title,
                Artist = artist,
                Album = album,
                IsPlaying = gsm.IsPlaying || fallback.IsPlaying,
                LyricLine = lyric,
                AlbumArtPath = artPath,
                SourceIconPath = iconPath
            };
        }

        if (gsm.IsPlaying != fallback.IsPlaying)
            return gsm.IsPlaying ? gsm : fallback;

        var gsmPriority = GetSourcePriority(gsm.SourceAppUserModelId);
        var fallbackPriority = GetSourcePriority(fallback.SourceAppUserModelId);
        return fallbackPriority >= gsmPriority ? fallback : gsm;
    }

    public static bool ShouldSuppressFallbackAfterGsmLoss(
        bool lastFromGsm,
        bool suppressUntilGsmPlaying,
        MediaSnapshot? gsm,
        MediaSnapshot? fallback)
    {
        // Fallback snapshots are already gated by real per-process audio activity.
        // Keeping an extra "GSMTC must come back first" latch makes Kugou disappear
        // permanently after another media source temporarily wins.
        _ = lastFromGsm;
        _ = suppressUntilGsmPlaying;
        _ = gsm;
        _ = fallback;
        return false;
    }

    public static int GetSourcePriority(string? sourceId)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
            return 0;

        var lower = sourceId.ToLowerInvariant();
        if (lower.Contains("kugou") || lower.Contains("kgmusic") ||
            lower.Contains("酷狗") ||
            lower.Contains("cloudmusic") || lower.Contains("netease") ||
            lower.Contains("网易云") ||
            lower.Contains("qqmusic") || lower.Contains("spotify") ||
            lower.Contains("qq音乐") || lower.Contains("qq 音乐") ||
            lower.Contains("kwmusic") || lower.Contains("酷我") ||
            lower.Contains("applemusic") ||
            lower.Contains("zune"))
        {
            return 100;
        }

        if (lower.Contains("chrome") || lower.Contains("edge") || lower.Contains("firefox"))
            return 50;

        return 10;
    }

    public static bool IsAppNameString(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        ReadOnlySpan<string> appNames = [
            "酷狗音乐", "网易云音乐", "QQ音乐", "QQ 音乐",
            "酷我音乐", "Spotify", "Media Player", "KuGou",
            "kugou", "cloudmusic", "qqmusic", "kwmusic"];
        foreach (var name in appNames)
        {
            if (text.Equals(name, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    public static bool IsSamePlayerApp(string? gsmId, string? fallbackId)
    {
        if (string.IsNullOrWhiteSpace(gsmId) || string.IsNullOrWhiteSpace(fallbackId))
            return false;

        var gsm = gsmId.ToLowerInvariant();
        var fallback = fallbackId.ToLowerInvariant();
        if (gsm == fallback)
            return true;

        var gsmFamily = SourceFamily(gsm);
        var fallbackFamily = SourceFamily(fallback);
        if (!string.IsNullOrWhiteSpace(gsmFamily) &&
            gsmFamily == fallbackFamily)
        {
            return true;
        }

        foreach (var keyword in new[]
        {
            "kugou", "kgmusic", "酷狗",
            "cloudmusic", "netease", "网易云",
            "qqmusic", "qq音乐", "qq 音乐",
            "spotify", "kwmusic", "酷我"
        })
        {
            if (gsm.Contains(keyword, StringComparison.Ordinal) &&
                fallback.Contains(keyword, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsBrowserSource(string? sourceId)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
            return false;

        var lower = sourceId.ToLowerInvariant();
        return lower.Contains("chrome", StringComparison.Ordinal) ||
               lower.Contains("edge", StringComparison.Ordinal) ||
               lower.Contains("msedge", StringComparison.Ordinal) ||
               lower.Contains("firefox", StringComparison.Ordinal);
    }

    public static bool ShouldAcceptBrowserSession(
        string? title,
        bool hasDuration,
        bool hasProgress)
    {
        _ = hasDuration;
        _ = hasProgress;

        if (string.IsNullOrWhiteSpace(title))
            return false;

        var normalized = title.Trim();
        if (normalized.Length < 2)
            return false;

        var lower = normalized.ToLowerInvariant();
        return lower is not "new tab"
            and not "about:blank"
            and not "microsoft edge"
            and not "google chrome"
            and not "chrome"
            and not "mozilla firefox"
            and not "firefox";
    }

    private static string SourceFamily(string sourceId)
    {
        if (sourceId.Contains("kugou", StringComparison.Ordinal) ||
            sourceId.Contains("kgmusic", StringComparison.Ordinal) ||
            sourceId.Contains("酷狗", StringComparison.Ordinal))
            return "kugou";

        if (sourceId.Contains("cloudmusic", StringComparison.Ordinal) ||
            sourceId.Contains("netease", StringComparison.Ordinal) ||
            sourceId.Contains("网易云", StringComparison.Ordinal))
            return "netease";

        if (sourceId.Contains("qqmusic", StringComparison.Ordinal) ||
            sourceId.Contains("qq音乐", StringComparison.Ordinal) ||
            sourceId.Contains("qq 音乐", StringComparison.Ordinal))
            return "qqmusic";

        if (sourceId.Contains("kwmusic", StringComparison.Ordinal) ||
            sourceId.Contains("酷我", StringComparison.Ordinal))
            return "kwmusic";

        if (sourceId.Contains("spotify", StringComparison.Ordinal))
            return "spotify";

        if (sourceId.Contains("msedge", StringComparison.Ordinal) ||
            sourceId.Contains("microsoft edge", StringComparison.Ordinal) ||
            sourceId.Contains("edge", StringComparison.Ordinal))
            return "edge";

        if (sourceId.Contains("chrome", StringComparison.Ordinal))
            return "chrome";

        if (sourceId.Contains("firefox", StringComparison.Ordinal))
            return "firefox";

        return string.Empty;
    }

    public static string BuildSignature(MediaSnapshot snapshot)
    {
        // LyricLine/SecondaryLyricLine intentionally excluded — lyric changes
        // should not trigger a full island re-render (causes visual "jump").
        // AlbumArtPath included — cover art arriving should update the display.
        return string.Join("|",
            snapshot.SourceAppUserModelId,
            snapshot.Title,
            snapshot.Artist,
            snapshot.Album,
            snapshot.IsPlaying.ToString(),
            snapshot.ProgressPercent.ToString(),
            snapshot.AlbumArtPath ?? "",
            snapshot.SourceIconPath ?? "");
    }
}

public static class MediaSnapshotContinuityPolicy
{
    public const int DefaultMissedPollsBeforeStopped = 20;
    public const int BrowserMissedPollsBeforeStopped = 180;
    public const int HighPriorityMissedPollsBeforeStopped = 60;

    public static int ResolveMissedPollsBeforeStopped(
        MediaSnapshot? lastActiveSnapshot,
        int fallbackMissedPolls = DefaultMissedPollsBeforeStopped)
    {
        if (lastActiveSnapshot is null)
            return Math.Max(1, fallbackMissedPolls);

        if (MediaSnapshotSelectionPolicy.IsBrowserSource(lastActiveSnapshot.SourceAppUserModelId) ||
            MediaSnapshotSelectionPolicy.IsBrowserSource(lastActiveSnapshot.SourceName))
        {
            return Math.Max(fallbackMissedPolls, BrowserMissedPollsBeforeStopped);
        }

        if (MediaSnapshotSelectionPolicy.GetSourcePriority(lastActiveSnapshot.SourceAppUserModelId) >= 100)
            return Math.Max(fallbackMissedPolls, HighPriorityMissedPollsBeforeStopped);

        return Math.Max(1, fallbackMissedPolls);
    }

    public static bool ShouldKeepDuringMiss(
        MediaSnapshot? lastActiveSnapshot,
        int missedPolls,
        int fallbackMissedPolls = DefaultMissedPollsBeforeStopped)
    {
        if (lastActiveSnapshot is null)
            return false;

        return missedPolls < ResolveMissedPollsBeforeStopped(lastActiveSnapshot, fallbackMissedPolls);
    }
}

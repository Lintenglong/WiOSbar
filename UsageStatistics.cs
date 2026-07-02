using System.IO;
using System.Text.Json;

namespace FluidBar;

/// <summary>
/// 使用统计与数据洞察
/// </summary>
public sealed class UsageStatistics
{
    private static readonly string StatsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FluidBar", "statistics.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    // 基础统计
    public int TotalEventsTriggered { get; set; }
    public DateTime FirstLaunchTime { get; set; } = DateTime.UtcNow;
    public DateTime LastLaunchTime { get; set; } = DateTime.UtcNow;
    public int LaunchCount { get; set; } = 1;

    // 按类型统计
    public Dictionary<string, int> EventTypeCounts { get; set; } = new();

    // 媒体统计
    public TimeSpan TotalMediaPlaybackTime { get; set; }
    public int MediaTracksPlayed { get; set; }
    public Dictionary<string, int> MediaSourceCounts { get; set; } = new();

    // 剪贴板统计
    public int ClipboardItemsCopied { get; set; }
    public int ClipboardItemsPasted { get; set; }

    // 系统状态统计
    public int VolumeChanges { get; set; }
    public int BrightnessChanges { get; set; }
    public int LockKeyToggles { get; set; }

    // Agent 统计
    public int AgentEventsReceived { get; set; }
    public int BuildSuccesses { get; set; }
    public int BuildFailures { get; set; }

    // 每日摘要（保留最近 30 天）
    public List<DailySummary> DailySummaries { get; set; } = new();

    /// <summary>
    /// 记录事件
    /// </summary>
    public void RecordEvent(string source)
    {
        TotalEventsTriggered++;
        EventTypeCounts[source] = EventTypeCounts.GetValueOrDefault(source, 0) + 1;

        // 分类统计
        switch (source)
        {
            case "volume":
                VolumeChanges++;
                break;
            case "brightness":
                BrightnessChanges++;
                break;
            case "lockkey":
                LockKeyToggles++;
                break;
            case "clipboard":
                ClipboardItemsCopied++;
                break;
            case "agent":
                AgentEventsReceived++;
                break;
            case "media":
                MediaTracksPlayed++;
                break;
        }

        // 更新每日摘要
        UpdateDailySummary(source);
    }

    private void UpdateDailySummary(string source)
    {
        var today = DateTime.UtcNow.Date;
        var summary = DailySummaries.FirstOrDefault(s => s.Date.Date == today);

        if (summary == null)
        {
            summary = new DailySummary(today);
            DailySummaries.Add(summary);

            // 保留最近 30 天
            if (DailySummaries.Count > 30)
            {
                DailySummaries = DailySummaries
                    .OrderByDescending(s => s.Date)
                    .Take(30)
                    .OrderBy(s => s.Date)
                    .ToList();
            }
        }

        summary.EventCount++;
        summary.EventTypeCounts[source] = summary.EventTypeCounts.GetValueOrDefault(source, 0) + 1;
    }

    /// <summary>
    /// 记录媒体播放时长
    /// </summary>
    public void RecordMediaPlayback(TimeSpan duration, string source)
    {
        TotalMediaPlaybackTime = TotalMediaPlaybackTime.Add(duration);
        MediaSourceCounts[source] = MediaSourceCounts.GetValueOrDefault(source, 0) + 1;
    }

    /// <summary>
    /// 记录构建结果
    /// </summary>
    public void RecordBuildResult(bool success)
    {
        if (success)
            BuildSuccesses++;
        else
            BuildFailures++;
    }

    /// <summary>
    /// 获取洞察报告
    /// </summary>
    public InsightReport GenerateReport()
    {
        var uptime = DateTime.UtcNow - FirstLaunchTime;
        var mostActiveSource = EventTypeCounts
            .OrderByDescending(kv => kv.Value)
            .FirstOrDefault();

        var avgEventsPerDay = uptime.TotalDays > 0
            ? TotalEventsTriggered / uptime.TotalDays
            : 0;

        return new InsightReport
        {
            TotalEvents = TotalEventsTriggered,
            Uptime = uptime,
            LaunchCount = LaunchCount,
            MostActiveSource = mostActiveSource.Key ?? "N/A",
            MostActiveSourceCount = mostActiveSource.Value,
            AverageEventsPerDay = avgEventsPerDay,
            MediaPlaybackHours = TotalMediaPlaybackTime.TotalHours,
            ClipboardCopyCount = ClipboardItemsCopied,
            TopSources = EventTypeCounts
                .OrderByDescending(kv => kv.Value)
                .Take(5)
                .ToDictionary(kv => kv.Key, kv => kv.Value)
        };
    }

    /// <summary>
    /// 加载统计数据
    /// </summary>
    public static UsageStatistics Load()
    {
        try
        {
            if (File.Exists(StatsPath))
            {
                var json = File.ReadAllText(StatsPath);
                var stats = JsonSerializer.Deserialize<UsageStatistics>(json, JsonOptions);
                if (stats != null)
                {
                    stats.LastLaunchTime = DateTime.UtcNow;
                    stats.LaunchCount++;
                    return stats;
                }
            }
        }
        catch { }

        return new UsageStatistics();
    }

    /// <summary>
    /// 保存统计数据
    /// </summary>
    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(StatsPath)!);
            var json = JsonSerializer.Serialize(this, JsonOptions);
            File.WriteAllText(StatsPath, json);
        }
        catch { }
    }
}

/// <summary>
/// 每日摘要
/// </summary>
public sealed class DailySummary
{
    public DateTime Date { get; set; }
    public int EventCount { get; set; }
    public Dictionary<string, int> EventTypeCounts { get; set; } = new();

    public DailySummary() { }

    public DailySummary(DateTime date)
    {
        Date = date;
    }
}

/// <summary>
/// 洞察报告
/// </summary>
public sealed class InsightReport
{
    public int TotalEvents { get; set; }
    public TimeSpan Uptime { get; set; }
    public int LaunchCount { get; set; }
    public string MostActiveSource { get; set; } = "";
    public int MostActiveSourceCount { get; set; }
    public double AverageEventsPerDay { get; set; }
    public double MediaPlaybackHours { get; set; }
    public int ClipboardCopyCount { get; set; }
    public Dictionary<string, int> TopSources { get; set; } = new();
}

using System.IO;
using System.Text;
using System.Text.Json;

namespace FluidBar;

/// <summary>
/// 数据导出管理器 - 导出统计和剪贴板历史到CSV/JSON
/// </summary>
public static class DataExportManager
{
    private static readonly string ExportDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FluidBar", "exports");

    /// <summary>
    /// 导出使用统计到JSON
    /// </summary>
    public static ExportResult ExportStatisticsToJson(UsageStatistics stats, string? fileName = null)
    {
        try
        {
            Directory.CreateDirectory(ExportDir);

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var exportFileName = fileName ?? $"statistics_{timestamp}.json";
            var exportPath = Path.Combine(ExportDir, exportFileName);

            var exportData = new
            {
                ExportedAt = DateTime.UtcNow,
                Statistics = stats,
                Report = stats.GenerateReport()
            };

            var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            File.WriteAllText(exportPath, json);

            return new ExportResult
            {
                Success = true,
                FilePath = exportPath,
                Format = "JSON",
                RecordCount = stats.TotalEventsTriggered,
                Message = $"成功导出 {stats.TotalEventsTriggered} 条事件到 JSON"
            };
        }
        catch (Exception ex)
        {
            return new ExportResult
            {
                Success = false,
                Message = $"导出失败: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 导出使用统计到CSV
    /// </summary>
    public static ExportResult ExportStatisticsToCsv(UsageStatistics stats, string? fileName = null)
    {
        try
        {
            Directory.CreateDirectory(ExportDir);

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var exportFileName = fileName ?? $"statistics_{timestamp}.csv";
            var exportPath = Path.Combine(ExportDir, exportFileName);

            var csv = new StringBuilder();

            // 标题行
            csv.AppendLine("EventType,Count,Percentage");

            // 事件类型统计
            var total = stats.TotalEventsTriggered;
            foreach (var kv in stats.EventTypeCounts.OrderByDescending(k => k.Value))
            {
                var percentage = total > 0 ? (kv.Value * 100.0 / total) : 0;
                csv.AppendLine($"{kv.Key},{kv.Value},{percentage:F2}%");
            }

            // 分类统计
            csv.AppendLine();
            csv.AppendLine("Category,Count");
            csv.AppendLine($"Volume Changes,{stats.VolumeChanges}");
            csv.AppendLine($"Brightness Changes,{stats.BrightnessChanges}");
            csv.AppendLine($"Lock Key Toggles,{stats.LockKeyToggles}");
            csv.AppendLine($"Clipboard Copies,{stats.ClipboardItemsCopied}");
            csv.AppendLine($"Agent Events,{stats.AgentEventsReceived}");
            csv.AppendLine($"Build Successes,{stats.BuildSuccesses}");
            csv.AppendLine($"Build Failures,{stats.BuildFailures}");

            // 媒体统计
            csv.AppendLine();
            csv.AppendLine("Media Source,Play Count");
            foreach (var kv in stats.MediaSourceCounts.OrderByDescending(k => k.Value))
            {
                csv.AppendLine($"{kv.Key},{kv.Value}");
            }

            csv.AppendLine();
            csv.AppendLine($"Total Media Playback Hours,{stats.TotalMediaPlaybackTime.TotalHours:F2}");

            File.WriteAllText(exportPath, csv.ToString(), Encoding.UTF8);

            return new ExportResult
            {
                Success = true,
                FilePath = exportPath,
                Format = "CSV",
                RecordCount = stats.EventTypeCounts.Count,
                Message = $"成功导出统计数据到 CSV"
            };
        }
        catch (Exception ex)
        {
            return new ExportResult
            {
                Success = false,
                Message = $"导出失败: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 导出剪贴板历史到JSON
    /// </summary>
    public static ExportResult ExportClipboardHistoryToJson(
        ClipboardHistoryManager history,
        string? fileName = null)
    {
        try
        {
            Directory.CreateDirectory(ExportDir);

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var exportFileName = fileName ?? $"clipboard_history_{timestamp}.json";
            var exportPath = Path.Combine(ExportDir, exportFileName);

            var exportData = new
            {
                ExportedAt = DateTime.UtcNow,
                Stats = history.GetStats(),
                Items = history.History.Select(item => new
                {
                    item.Id,
                    Type = item.Type.ToString(),
                    item.TextContent,
                    item.SourceApp,
                    item.Timestamp,
                    item.IsFavorite,
                    item.PreviewText
                }).ToList()
            };

            var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            File.WriteAllText(exportPath, json);

            return new ExportResult
            {
                Success = true,
                FilePath = exportPath,
                Format = "JSON",
                RecordCount = history.History.Count,
                Message = $"成功导出 {history.History.Count} 条剪贴板记录到 JSON"
            };
        }
        catch (Exception ex)
        {
            return new ExportResult
            {
                Success = false,
                Message = $"导出失败: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 导出剪贴板历史到CSV
    /// </summary>
    public static ExportResult ExportClipboardHistoryToCsv(
        ClipboardHistoryManager history,
        string? fileName = null)
    {
        try
        {
            Directory.CreateDirectory(ExportDir);

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var exportFileName = fileName ?? $"clipboard_history_{timestamp}.csv";
            var exportPath = Path.Combine(ExportDir, exportFileName);

            var csv = new StringBuilder();

            // 标题行
            csv.AppendLine("Timestamp,Type,Preview,Source, Favorite");

            // 数据行
            foreach (var item in history.History.OrderByDescending(i => i.Timestamp))
            {
                var preview = (item.PreviewText ?? item.TextContent ?? "[No Content]")
                    .Replace("\"", "\"\"")  // 转义引号
                    .Replace("\n", " ")
                    .Replace("\r", "");

                csv.AppendLine(
                    $"\"{item.Timestamp:yyyy-MM-dd HH:mm:ss}\"," +
                    $"\"{item.Type}\"," +
                    $"\"{preview}\"," +
                    $"\"{item.SourceApp ?? ""}\"," +
                    $"{(item.IsFavorite ? "Yes" : "No")}"
                );
            }

            File.WriteAllText(exportPath, csv.ToString(), Encoding.UTF8);

            return new ExportResult
            {
                Success = true,
                FilePath = exportPath,
                Format = "CSV",
                RecordCount = history.History.Count,
                Message = $"成功导出 {history.History.Count} 条剪贴板记录到 CSV"
            };
        }
        catch (Exception ex)
        {
            return new ExportResult
            {
                Success = false,
                Message = $"导出失败: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 导出所有数据（一键导出）
    /// </summary>
    public static BatchExportResult ExportAll(
        UsageStatistics? stats = null,
        ClipboardHistoryManager? clipboardHistory = null)
    {
        var results = new List<ExportResult>();
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

        // 导出统计
        if (stats != null)
        {
            results.Add(ExportStatisticsToJson(stats, $"stats_{timestamp}.json"));
            results.Add(ExportStatisticsToCsv(stats, $"stats_{timestamp}.csv"));
        }

        // 导出剪贴板历史
        if (clipboardHistory != null && clipboardHistory.History.Any())
        {
            results.Add(ExportClipboardHistoryToJson(clipboardHistory, $"clipboard_{timestamp}.json"));
            results.Add(ExportClipboardHistoryToCsv(clipboardHistory, $"clipboard_{timestamp}.csv"));
        }

        var successCount = results.Count(r => r.Success);
        var failCount = results.Count(r => !r.Success);

        return new BatchExportResult
        {
            Success = failCount == 0,
            Results = results,
            SuccessCount = successCount,
            FailCount = failCount,
            Message = $"导出完成: {successCount} 成功, {failCount} 失败"
        };
    }

    /// <summary>
    /// 获取导出目录
    /// </summary>
    public static string GetExportDirectory() => ExportDir;

    /// <summary>
    /// 清理旧导出文件（保留最近 N 个）
    /// </summary>
    public static int CleanupOldExports(int keepCount = 20)
    {
        try
        {
            if (!Directory.Exists(ExportDir))
                return 0;

            var files = Directory.GetFiles(ExportDir)
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime)
                .ToList();

            var toDelete = files.Skip(keepCount).ToList();
            var deletedCount = 0;

            foreach (var file in toDelete)
            {
                try
                {
                    file.Delete();
                    deletedCount++;
                }
                catch { }
            }

            return deletedCount;
        }
        catch
        {
            return 0;
        }
    }
}

/// <summary>
/// 导出结果
/// </summary>
public sealed class ExportResult
{
    public bool Success { get; set; }
    public string? FilePath { get; set; }
    public string Format { get; set; } = "";
    public int RecordCount { get; set; }
    public string Message { get; set; } = "";
}

/// <summary>
/// 批量导出结果
/// </summary>
public sealed class BatchExportResult
{
    public bool Success { get; set; }
    public List<ExportResult> Results { get; set; } = new();
    public int SuccessCount { get; set; }
    public int FailCount { get; set; }
    public string Message { get; set; } = "";
}

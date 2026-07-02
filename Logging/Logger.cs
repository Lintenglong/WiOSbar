using System.IO;

namespace FluidBar.Logging;

/// <summary>
/// 结构化日志系统
/// </summary>
public static class Logger
{
    private static readonly string LogDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FluidBar", "logs");

    private static readonly object _lock = new();
    private static LogLevel _minLevel = LogLevel.Info;
    private static bool _enabled = true;

    /// <summary>
    /// 日志级别
    /// </summary>
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        Critical = 4
    }

    /// <summary>
    /// 设置最小日志级别
    /// </summary>
    public static void SetMinLevel(LogLevel level)
    {
        _minLevel = level;
    }

    /// <summary>
    /// 启用/禁用日志
    /// </summary>
    public static void SetEnabled(bool enabled)
    {
        _enabled = enabled;
    }

    /// <summary>
    /// 记录调试信息
    /// </summary>
    public static void Debug(string message, string? source = null)
    {
        Log(LogLevel.Debug, message, source);
    }

    /// <summary>
    /// 记录信息
    /// </summary>
    public static void Info(string message, string? source = null)
    {
        Log(LogLevel.Info, message, source);
    }

    /// <summary>
    /// 记录警告
    /// </summary>
    public static void Warning(string message, string? source = null, Exception? ex = null)
    {
        Log(LogLevel.Warning, message, source, ex);
    }

    /// <summary>
    /// 记录错误
    /// </summary>
    public static void Error(string message, string? source = null, Exception? ex = null)
    {
        Log(LogLevel.Error, message, source, ex);
    }

    /// <summary>
    /// 记录严重错误
    /// </summary>
    public static void Critical(string message, string? source = null, Exception? ex = null)
    {
        Log(LogLevel.Critical, message, source, ex);
    }

    private static void Log(LogLevel level, string message, string? source = null, Exception? ex = null)
    {
        if (!_enabled || level < _minLevel)
            return;

        try
        {
            lock (_lock)
            {
                Directory.CreateDirectory(LogDir);

                var logFile = Path.Combine(LogDir, $"fluidbar_{DateTime.Now:yyyyMMdd}.log");
                var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                var levelStr = level.ToString().ToUpper().PadRight(8);

                var logLine = $"[{timestamp}] [{levelStr}] ";
                if (!string.IsNullOrEmpty(source))
                    logLine += $"[{source}] ";
                logLine += message;

                if (ex != null)
                {
                    logLine += $"\n  Exception: {ex.GetType().Name}";
                    logLine += $"\n  Message: {ex.Message}";
                    logLine += $"\n  StackTrace:\n{ex.StackTrace}";
                }

                File.AppendAllText(logFile, logLine + Environment.NewLine);
            }
        }
        catch
        {
            // 日志写入失败，静默忽略
        }
    }

    /// <summary>
    /// 清理旧日志（保留最近 N 天）
    /// </summary>
    public static void CleanupOldLogs(int keepDays = 7)
    {
        try
        {
            if (!Directory.Exists(LogDir))
                return;

            var cutoffDate = DateTime.Now.AddDays(-keepDays);
            var logFiles = Directory.GetFiles(LogDir, "fluidbar_*.log");

            foreach (var file in logFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                if (fileName.Length >= 17)
                {
                    var dateStr = fileName.Substring(9, 8); // yyyyMMdd
                    if (DateTime.TryParseExact(dateStr, "yyyyMMdd", null,
                        System.Globalization.DateTimeStyles.None, out var fileDate))
                    {
                        if (fileDate < cutoffDate)
                        {
                            try { File.Delete(file); } catch { }
                        }
                    }
                }
            }
        }
        catch { }
    }

    /// <summary>
    /// 获取日志目录
    /// </summary>
    public static string GetLogDirectory() => LogDir;
}

using System.Diagnostics;
using System.Windows.Threading;

namespace FluidBar;

/// <summary>
/// 性能监控器 - 实时监控 FluidBar 自身性能
/// </summary>
public sealed class PerformanceMonitor : IDisposable
{
    private readonly DispatcherTimer _timer;
    private readonly Process _currentProcess;
    private PerformanceData _lastData;
    private Action<PerformanceData>? _onDataUpdated;

    public PerformanceData CurrentData => _lastData;

    public PerformanceMonitor()
    {
        _currentProcess = Process.GetCurrentProcess();
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        _timer.Tick += UpdateMetrics;
        _lastData = new PerformanceData();
    }

    /// <summary>
    /// 启动监控
    /// </summary>
    public void Start(Action<PerformanceData> onDataUpdated)
    {
        _onDataUpdated = onDataUpdated;
        _timer.Start();
        UpdateMetrics(null, EventArgs.Empty); // 首次更新
    }

    /// <summary>
    /// 停止监控
    /// </summary>
    public void Stop()
    {
        _timer.Stop();
    }

    private void UpdateMetrics(object? sender, EventArgs e)
    {
        try
        {
            _currentProcess.Refresh();

            var data = new PerformanceData
            {
                Timestamp = DateTime.UtcNow,
                CpuUsagePercent = GetCpuUsage(),
                MemoryWorkingSetMB = _currentProcess.WorkingSet64 / (1024.0 * 1024.0),
                MemoryPrivateMB = _currentProcess.PrivateMemorySize64 / (1024.0 * 1024.0),
                ThreadCount = _currentProcess.Threads.Count,
                HandleCount = _currentProcess.HandleCount,
                GcGen0Collections = GC.CollectionCount(0),
                GcGen1Collections = GC.CollectionCount(1),
                GcGen2Collections = GC.CollectionCount(2),
                TotalGcMemoryMB = GC.GetTotalMemory(false) / (1024.0 * 1024.0)
            };

            _lastData = data;
            _onDataUpdated?.Invoke(data);
        }
        catch
        {
            // 静默失败
        }
    }

    private double GetCpuUsage()
    {
        try
        {
            // 简化实现：实际应使用 PerformanceCounter
            // 这里返回估算值
            var totalProcessorTime = _currentProcess.TotalProcessorTime.TotalMilliseconds;
            var wallClockTime = (DateTime.UtcNow - _currentProcess.StartTime.ToUniversalTime()).TotalMilliseconds;

            if (wallClockTime > 0)
            {
                var cpuUsage = (totalProcessorTime / wallClockTime) * 100.0 / Environment.ProcessorCount;
                return Math.Min(Math.Max(cpuUsage, 0), 100);
            }
        }
        catch { }

        return 0;
    }

    public void Dispose()
    {
        _timer.Stop();
        _currentProcess?.Dispose();
    }
}

/// <summary>
/// 性能数据
/// </summary>
public sealed class PerformanceData
{
    public DateTime Timestamp { get; set; }
    public double CpuUsagePercent { get; set; }
    public double MemoryWorkingSetMB { get; set; }
    public double MemoryPrivateMB { get; set; }
    public int ThreadCount { get; set; }
    public int HandleCount { get; set; }
    public int GcGen0Collections { get; set; }
    public int GcGen1Collections { get; set; }
    public int GcGen2Collections { get; set; }
    public double TotalGcMemoryMB { get; set; }

    /// <summary>
    /// 格式化显示
    /// </summary>
    public string GetSummary()
    {
        return $"CPU: {CpuUsagePercent:F1}% | " +
               $"内存: {MemoryWorkingSetMB:F1} MB | " +
               $"线程: {ThreadCount} | " +
               $"GC: G0={GcGen0Collections} G1={GcGen1Collections} G2={GcGen2Collections}";
    }

    /// <summary>
    /// 判断是否性能异常
    /// </summary>
    public bool IsPerformanceAnomaly()
    {
        return CpuUsagePercent > 50 ||
               MemoryWorkingSetMB > 200 ||
               ThreadCount > 50;
    }
}

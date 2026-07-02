using System.Diagnostics;
using System.IO;
using System.Windows.Threading;

namespace FluidBar.Monitors;

/// <summary>
/// 纾佺洏娲诲姩鐩戞帶
/// </summary>
public sealed class DiskMonitor : ISystemMonitor
{
    public string Id => "disk";
    public string Name => "纾佺洏";
    public string Description => "纾佺洏璇诲啓娲诲姩鐩戞帶";
    public string Icon => "瞍?; // Segoe MDL2 HardDrive
    public bool Enabled { get; set; } = true;
    public event Action<IslandEvent>? EventTriggered;

    private DispatcherTimer? _timer;
    private bool _isRunning;
    private float _lastReadBytes;
    private float _lastWriteBytes;
    private PerformanceCounter? _diskReadCounter;
    private PerformanceCounter? _diskWriteCounter;
    private bool _counterInitialized;
    private string _systemDrive = "C";

    public void Start()
    {
        if (_isRunning) return;
        _isRunning = true;

        // 鑾峰彇绯荤粺鐩樼
        try
        {
            _systemDrive = Path.GetPathRoot(Environment.SystemDirectory)?.TrimEnd('\\') ?? "C";
        }
        catch
        {
            _systemDrive = "C";
        }

        if (!_counterInitialized)
        {
            try
            {
                var instanceName = $"{_systemDrive}:";
                _diskReadCounter = new PerformanceCounter("LogicalDisk", "Disk Read Bytes/sec", instanceName);
                _diskWriteCounter = new PerformanceCounter("LogicalDisk", "Disk Write Bytes/sec", instanceName);
                _diskReadCounter.NextValue();
                _diskWriteCounter.NextValue();
                _counterInitialized = true;
            }
            catch
            {
                Enabled = false;
                return;
            }
        }

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
        _timer.Tick += (_, _) => CheckDisk();
        _timer.Start();

        // 棣栨寤惰繜妫€鏌?        _ = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        }.Apply(t =>
        {
            t.Tick += (_, _) =>
            {
                t.Stop();
                CheckDisk();
            };
            _timer.Start();
    }

    public void Stop()
    {
        _isRunning = false;
        _timer?.Stop();
        _timer = null;
    }

    private void CheckDisk()
    {
        if (!_isRunning || _diskReadCounter == null || _diskWriteCounter == null)
            return;

        try
        {
            var readBytes = _diskReadCounter.NextValue();
            var writeBytes = _diskWriteCounter.NextValue();

            // 杞崲涓?MB/s
            var readMB = readBytes / (1024 * 1024);
            var writeMB = writeBytes / (1024 * 1024);
            var totalMB = readMB + writeMB;

            // 浠呭湪纾佺洏娲诲姩鏄捐憲锛? 5MB/s锛夋垨鍙樺寲澶ф椂瑙﹀彂
            var prevTotal = (_lastReadBytes + _lastWriteBytes) / (1024 * 1024);
            var shouldTrigger = totalMB > 5 && Math.Abs(totalMB - prevTotal) > 3;

            if (shouldTrigger)
            {
                _lastReadBytes = readBytes;
                _lastWriteBytes = writeBytes;

                string iconKind, title, content;

                if (totalMB > 50)
                {
                    iconKind = "disk_active";
                    title = "纾佺洏绻佸繖";
                    content = $"璇?{readMB:F1} / 鍐?{writeMB:F1} MB/s";
                }
                else if (totalMB > 20)
                {
                    iconKind = "disk";
                    title = "纾佺洏娲诲姩";
                    content = $"璇?{readMB:F1} / 鍐?{writeMB:F1} MB/s";
                }
                else
                {
                    iconKind = "disk";
                    title = "纾佺洏";
                    content = $"{totalMB:F0} MB/s";
                }

                EventTriggered?.Invoke(new IslandEvent(
                    Source: Id,
                    Title: title,
                    Content: content,
                    IconKind: iconKind));
            }
        }
        catch
        {
            // 闈欓粯澶辫触
        }
    }

    public void Dispose()
    {
        Stop();
        _diskReadCounter?.Dispose();
        _diskWriteCounter?.Dispose();
    }
}


using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Windows.Threading;

namespace FluidBar.Monitors;

/// <summary>
/// 网络速度监控器 - 实时显示上传/下载速率
/// </summary>
public sealed class NetworkSpeedMonitor : ISystemMonitor
{
    public string Id => "network_speed";
    public string Name => "网络速度";
    public string Description => "实时网络上传/下载速率";
    public string Icon => ""; // Segoe MDL2 Network
    public bool Enabled { get; set; } = true;
    public event Action<IslandEvent>? EventTriggered;

    private DispatcherTimer? _timer;
    private bool _isRunning;
    private long _lastBytesReceived;
    private long _lastBytesSent;
    private DateTime _lastSampleTime = DateTime.UtcNow;
    private bool _firstSample = true;

    // 网卡缓存（避免每次都枚举）
    private NetworkInterface? _activeInterface;
    private DateTime _interfaceCacheTime = DateTime.MinValue;
    private static readonly TimeSpan InterfaceCacheTtl = TimeSpan.FromSeconds(30);

    public void Start()
    {
        if (_isRunning) return;
        _isRunning = true;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _timer.Tick += (_, _) => SampleNetworkSpeed();
        _timer.Start();

        // 首次延迟 1 秒采样
        _ = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        }.Apply(t =>
        {
            t.Tick += (_, _) =>
            {
                t.Stop();
                SampleNetworkSpeed();
            };
            t.Start();
        });
    }

    public void Stop()
    {
        _isRunning = false;
        _timer?.Stop();
        _timer = null;
    }

    private void SampleNetworkSpeed()
    {
        if (!_isRunning)
            return;

        try
        {
            var ni = GetActiveNetworkInterface();
            if (ni == null)
                return;

            var stats = ni.GetIPv4Statistics();
            var now = DateTime.UtcNow;
            var elapsed = (now - _lastSampleTime).TotalSeconds;

            if (elapsed < 0.5)
                return; // 避免采样过密

            var bytesReceived = stats.BytesReceived;
            var bytesSent = stats.BytesSent;

            if (!_firstSample && elapsed > 0)
            {
                var downloadBps = (bytesReceived - _lastBytesReceived) / elapsed;
                var uploadBps = (bytesSent - _lastBytesSent) / elapsed;

                // 转换为 KB/s 或 MB/s
                var downloadKbps = downloadBps / 1024.0;
                var uploadKbps = uploadBps / 1024.0;

                // 仅在有显著活动时触发（> 10 KB/s）
                if (downloadKbps > 10 || uploadKbps > 10)
                {
                    var downloadStr = FormatSpeed(downloadKbps);
                    var uploadStr = FormatSpeed(uploadKbps);

                    EventTriggered?.Invoke(new IslandEvent(
                        Source: Id,
                        Title: "网络",
                        Content: $"↓ {downloadStr}  ↑ {uploadStr}",
                        IconKind: "network"));
                }
            }

            _lastBytesReceived = bytesReceived;
            _lastBytesSent = bytesSent;
            _lastSampleTime = now;
            _firstSample = false;
        }
        catch
        {
            // 静默失败
        }
    }

    private static string FormatSpeed(double kbps)
    {
        if (kbps >= 1024)
        {
            return $"{kbps / 1024:F1} MB/s";
        }
        return $"{kbps:F0} KB/s";
    }

    private NetworkInterface? GetActiveNetworkInterface()
    {
        // 使用缓存
        if (_activeInterface != null &&
            (DateTime.UtcNow - _interfaceCacheTime) < InterfaceCacheTtl)
        {
            return _activeInterface;
        }

        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();

            // 优先选择已连接的以太网或 WiFi
            var active = interfaces.FirstOrDefault(ni =>
                ni.OperationalStatus == OperationalStatus.Up &&
                (ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                 ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211) &&
                ni.GetIPv4Statistics().BytesReceived > 0);

            if (active == null)
            {
                // 降级：任意已连接的接口
                active = interfaces.FirstOrDefault(ni =>
                    ni.OperationalStatus == OperationalStatus.Up);
            }

            _activeInterface = active;
            _interfaceCacheTime = DateTime.UtcNow;

            return active;
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        Stop();
    }
}

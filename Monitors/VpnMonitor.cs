using System.Management;
using System.Net.NetworkInformation;
using System.Windows.Threading;

namespace FluidBar.Monitors;

/// <summary>
/// VPN 状态监控器
/// </summary>
public sealed class VpnMonitor : ISystemMonitor
{
    public string Id => "vpn";
    public string Name => "VPN";
    public string Description => "VPN 连接状态监控";
    public string Icon => ""; // Segoe MDL2 Lock
    public bool Enabled { get; set; } = true;
    public event Action<IslandEvent>? EventTriggered;

    private DispatcherTimer? _timer;
    private bool _isRunning;
    private bool _lastVpnState;
    private bool _firstCheck = true;

    public void Start()
    {
        if (_isRunning) return;
        _isRunning = true;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
        _timer.Tick += (_, _) => CheckVpnStatus();
        _timer.Start();

        // 首次延迟检查
        _ = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        }.Apply(t =>
        {
            t.Tick += (_, _) =>
            {
                t.Stop();
                CheckVpnStatus();
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

    private void CheckVpnStatus()
    {
        if (!_isRunning)
            return;

        try
        {
            var hasVpn = DetectVpnConnection();
            var stateChanged = hasVpn != _lastVpnState;

            if (stateChanged || _firstCheck)
            {
                if (hasVpn)
                {
                    EventTriggered?.Invoke(new IslandEvent(
                        Source: Id,
                        Title: "VPN 已连接",
                        Content: "安全连接已建立",
                        IconKind: "vpn"));
                }
                else if (!_firstCheck)
                {
                    EventTriggered?.Invoke(new IslandEvent(
                        Source: Id,
                        Title: "VPN 已断开",
                        Content: "连接已断开",
                        IconKind: "vpn"));
                }

                _lastVpnState = hasVpn;
                _firstCheck = false;
            }
        }
        catch
        {
            // 静默失败
        }
    }

    private static bool DetectVpnConnection()
    {
        try
        {
            // 方法 1：检查网络接口类型
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var ni in interfaces)
            {
                if (ni.OperationalStatus != OperationalStatus.Up)
                    continue;

                // VPN 通常使用 PPP 或 Tunnel 接口
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Ppp ||
                    ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
                {
                    // 排除回环和以太网
                    if (!ni.Description.Contains("Loopback", StringComparison.OrdinalIgnoreCase) &&
                        !ni.Description.Contains("Ethernet", StringComparison.OrdinalIgnoreCase) &&
                        !ni.Description.Contains("Wi-Fi", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            // 方法 2：检查路由表中的 VPN 网关（简化）
            // 实际生产环境可能需要更复杂的检测逻辑

            return false;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        Stop();
    }
}

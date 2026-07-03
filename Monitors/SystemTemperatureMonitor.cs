using System.Management;
using System.Windows.Threading;

namespace FluidBar.Monitors;

/// <summary>
/// 系统温度监控器 - 监控CPU/主板温度
/// </summary>
public sealed class SystemTemperatureMonitor : ISystemMonitor
{
    public string Id => "temperature";
    public string Name => "温度";
    public string Description => "系统温度监控";
    public string Icon => "🌡️";
    public bool Enabled { get; set; } = true;
    public event Action<IslandEvent>? EventTriggered;

    private DispatcherTimer? _timer;
    private bool _isRunning;
    private double _lastCpuTemp;

    public void Start()
    {
        if (_isRunning) return;
        _isRunning = true;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(2) };
        _timer.Tick += (_, _) => CheckTemperature();
        _timer.Start();

        // 首次延迟检查
        _ = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(15)
        }.Apply(t =>
        {
            t.Tick += (_, _) =>
            {
                t.Stop();
                CheckTemperature();
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

    private void CheckTemperature()
    {
        if (!_isRunning)
            return;

        try
        {
            // 使用 WMI 查询温度传感器
            var searcher = new ManagementObjectSearcher(
                @"root\WMI",
                "SELECT * FROM MSAcpi_ThermalZoneTemperature");

            foreach (var temp in searcher.Get())
            {
                // 温度以开尔文为单位，转换为摄氏度
                var rawTemp = Convert.ToDouble(temp["CurrentTemperature"]);
                var celsius = (rawTemp / 10.0) - 273.15;

                // 只关注CPU温度（通常 > 30°C 且 < 100°C）
                if (celsius > 30 && celsius < 100)
                {
                    if (ShouldTriggerEvent(celsius))
                    {
                        var iconKind = celsius > 80 ? "temperature_high" : "temperature";

                        EventTriggered?.Invoke(new IslandEvent(
                            Source: Id,
                            Title: "CPU 温度",
                            Content: $"{celsius:F1}°C",
                            IconKind: iconKind));
                    }

                    _lastCpuTemp = celsius;
                }
            }
        }
        catch
        {
            // 某些系统不支持温度传感器，静默失败
        }
    }

    private bool ShouldTriggerEvent(double currentTemp)
    {
        // 首次检测
        if (_lastCpuTemp == 0)
            return true;

        // 温度超过 80°C 警告
        if (currentTemp > 80 && _lastCpuTemp <= 80)
            return true;

        // 温度变化超过 10°C
        if (Math.Abs(currentTemp - _lastCpuTemp) >= 10)
            return true;

        return false;
    }

    public void Dispose()
    {
        Stop();
    }
}

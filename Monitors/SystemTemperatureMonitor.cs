using System.Management;
using System.Windows.Threading;

namespace FluidBar.Monitors;

/// <summary>
/// 绯荤粺娓╁害鐩戞帶鍣?- 鐩戞帶CPU/涓绘澘娓╁害
/// </summary>
public sealed class SystemTemperatureMonitor : ISystemMonitor
{
    public string Id => "temperature";
    public string Name => "娓╁害";
    public string Description => "绯荤粺娓╁害鐩戞帶";
    public string Icon => "馃尅锔?;
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

        // 棣栨寤惰繜妫€鏌?        _ = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(15)
        }.Apply(t =>
        {
            t.Tick += (_, _) =>
            {
                t.Stop();
                CheckTemperature();
            };
            _timer.Start();
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
            // 浣跨敤 WMI 鏌ヨ娓╁害浼犳劅鍣?            var searcher = new ManagementObjectSearcher(
                @"root\WMI",
                "SELECT * FROM MSAcpi_ThermalZoneTemperature");

            foreach (var temp in searcher.Get())
            {
                // 娓╁害浠ュ紑灏旀枃涓哄崟浣嶏紝杞崲涓烘憚姘忓害
                var rawTemp = Convert.ToDouble(temp["CurrentTemperature"]);
                var celsius = (rawTemp / 10.0) - 273.15;

                // 鍙叧娉–PU娓╁害锛堥€氬父 > 30掳C 涓?< 100掳C锛?                if (celsius > 30 && celsius < 100)
                {
                    if (ShouldTriggerEvent(celsius))
                    {
                        var iconKind = celsius > 80 ? "temperature_high" : "temperature";

                        EventTriggered?.Invoke(new IslandEvent(
                            Source: Id,
                            Title: "CPU 娓╁害",
                            Content: $"{celsius:F1}掳C",
                            IconKind: iconKind));
                    }

                    _lastCpuTemp = celsius;
                }
            }
        }
        catch
        {
            // 鏌愪簺绯荤粺涓嶆敮鎸佹俯搴︿紶鎰熷櫒锛岄潤榛樺け璐?        }
    }

    private bool ShouldTriggerEvent(double currentTemp)
    {
        // 棣栨妫€娴?        if (_lastCpuTemp == 0)
            return true;

        // 娓╁害瓒呰繃 80掳C 璀﹀憡
        if (currentTemp > 80 && _lastCpuTemp <= 80)
            return true;

        // 娓╁害鍙樺寲瓒呰繃 10掳C
        if (Math.Abs(currentTemp - _lastCpuTemp) >= 10)
            return true;

        return false;
    }

    public void Dispose()
    {
        Stop();
    }
}


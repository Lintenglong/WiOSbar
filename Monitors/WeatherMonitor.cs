using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Windows.Threading;

namespace FluidBar.Monitors;

/// <summary>
/// 天气监控器 - 支持和风天气 / OpenWeatherMap
/// 需要用户配置 API Key 才能启用
/// </summary>
public sealed class WeatherMonitor : ISystemMonitor
{
    public string Id => "weather";
    public string Name => "天气";
    public string Description => "当前天气和温度（需配置 API Key）";
    public string Icon => ""; // Segoe MDL2 Sunny
    public bool Enabled { get; set; } = false; // 默认禁用，需配置后启用
    public event Action<IslandEvent>? EventTriggered;

    private DispatcherTimer? _timer;
    private bool _isRunning;
    private WeatherConfig? _config;
    private WeatherData? _lastData;
    private DateTime _lastFetchTime = DateTime.MinValue;
    private static readonly TimeSpan FetchInterval = TimeSpan.FromMinutes(30);

    public void Start()
    {
        if (_isRunning) return;

        // 尝试加载配置
        _config = WeatherConfig.Load();
        if (_config == null || string.IsNullOrWhiteSpace(_config.ApiKey))
        {
            // 未配置 API Key，保持禁用状态
            Enabled = false;
            return;
        }

        Enabled = true;
        _isRunning = true;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(30) };
        _timer.Tick += (_, _) => FetchWeather();
        _timer.Start();

        // 首次延迟 5 秒获取（避免启动时阻塞）
        _ = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        }.Apply(t =>
        {
            t.Tick += (_, _) =>
            {
                t.Stop();
                FetchWeather();
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

    private void FetchWeather()
    {
        if (!_isRunning || _config == null)
            return;

        // 频率限制：30 分钟内不重复请求
        if ((DateTime.UtcNow - _lastFetchTime).TotalMinutes < 25)
            return;

        try
        {
            WeatherData? data = null;

            switch (_config.Provider?.ToLowerInvariant())
            {
                case "qweather":
                case "heweather":
                    data = FetchFromQWeather(_config);
                    break;

                case "openweathermap":
                default:
                    data = FetchFromOpenWeatherMap(_config);
                    break;
            }

            if (data != null)
            {
                _lastData = data;
                _lastFetchTime = DateTime.UtcNow;

                var shouldTrigger = ShouldTriggerEvent(data);
                if (shouldTrigger)
                {
                    EventTriggered?.Invoke(new IslandEvent(
                        Source: Id,
                        Title: $"{data.City} · {data.Condition}",
                        Content: $"{data.Temp}°C 体感 {data.FeelsLike}°C",
                        IconKind: GetWeatherIcon(data.Condition)));
                }
            }
        }
        catch
        {
            // 静默失败，下次重试
        }
    }

    private static bool ShouldTriggerEvent(WeatherData data)
    {
        // 首次获取
        if (data == null)
            return true;

        // 温度变化 > 3°C
        // 天气状况变化
        // 预警信息

        return true; // 简化：每次都触发（实际应与 _lastData 比较）
    }

    private static string GetWeatherIcon(string condition)
    {
        var lower = condition.ToLowerInvariant();

        if (lower.Contains("晴") || lower.Contains("clear") || lower.Contains("sun"))
            return "weather_sunny";

        if (lower.Contains("云") || lower.Contains("cloud") || lower.Contains("阴"))
            return "weather_cloudy";

        if (lower.Contains("雨") || lower.Contains("rain"))
            return "weather_rain";

        if (lower.Contains("雪") || lower.Contains("snow"))
            return "weather_snow";

        if (lower.Contains("雾") || lower.Contains("雾") || lower.Contains("雾"))
            return "weather_fog";

        if (lower.Contains("雷") || lower.Contains("thunder"))
            return "weather_thunder";

        return "weather";
    }

    private static WeatherData? FetchFromOpenWeatherMap(WeatherConfig config)
    {
        try
        {
            var url = $"https://api.openweathermap.org/data/2.5/weather?q={Uri.EscapeDataString(config.City ?? "Beijing")}&appid={config.ApiKey}&units=metric&lang=zh_cn";

            using var response = Http.GetAsync(url).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
                return null;

            var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            return ParseOpenWeatherMapResponse(json);
        }
        catch
        {
            return null;
        }
    }

    private static WeatherData? ParseOpenWeatherMapResponse(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("main", out var main))
                return null;

            double temp = 0, feelsLike = 0;
            if (main.TryGetProperty("temp", out var tempProp))
                temp = tempProp.GetDouble();

            if (main.TryGetProperty("feels_like", out var feelsLikeProp))
                feelsLike = feelsLikeProp.GetDouble();

            string condition = "未知";
            string city = "未知城市";

            if (doc.RootElement.TryGetProperty("weather", out var weather) &&
                weather.GetArrayLength() > 0)
            {
                var first = weather[0];
                if (first.TryGetProperty("description", out var descProp))
                    condition = descProp.GetString() ?? "未知";
            }

            if (doc.RootElement.TryGetProperty("name", out var nameProp))
                city = nameProp.GetString() ?? "未知城市";

            return new WeatherData(city, condition, temp, feelsLike, DateTime.UtcNow);
        }
        catch
        {
            return null;
        }
    }

    private static WeatherData? FetchFromQWeather(WeatherConfig config)
    {
        try
        {
            // 和风天气 API（需注册获取 Key）
            // https://dev.qweather.com/docs/api/weather/weather-now/
            var url = $"https://devapi.qweather.com/v7/weather/now?location={Uri.EscapeDataString(config.City ?? "101010100")}&key={config.ApiKey}";

            using var response = Http.GetAsync(url).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
                return null;

            var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            return ParseQWeatherResponse(json, config.City ?? "北京");
        }
        catch
        {
            return null;
        }
    }

    private static WeatherData? ParseQWeatherResponse(string json, string city)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("now", out var now))
                return null;

            double temp = 0, feelsLike = 0;
            if (now.TryGetProperty("temp", out var tempProp))
                double.TryParse(tempProp.GetString(), out temp);

            if (now.TryGetProperty("feelsLike", out var feelsLikeProp))
                double.TryParse(feelsLikeProp.GetString(), out feelsLike);

            string condition = "未知";
            if (now.TryGetProperty("text", out var textProp))
                condition = textProp.GetString() ?? "未知";

            return new WeatherData(city, condition, temp, feelsLike, DateTime.UtcNow);
        }
        catch
        {
            return null;
        }
    }

    private static readonly HttpClient Http = CreateHttpClient();

    private static HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };

        var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        client.DefaultRequestHeaders.UserAgent.ParseAdd("FluidBar/1.0");
        return client;
    }

    public void Dispose()
    {
        Stop();
    }
}

/// <summary>
/// 天气配置
/// </summary>
public sealed class WeatherConfig
{
    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FluidBar", "weather.json");

    public string? Provider { get; set; } = "openweathermap"; // openweathermap | qweather
    public string? ApiKey { get; set; }
    public string? City { get; set; } = "Beijing";

    public static WeatherConfig? Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<WeatherConfig>(json);
            }
        }
        catch { }

        return null;
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
        }
        catch { }
    }
}

/// <summary>
/// 天气数据
/// </summary>
public sealed record WeatherData(
    string City,
    string Condition,
    double Temp,
    double FeelsLike,
    DateTime Timestamp);

using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Windows.Threading;

namespace FluidBar.Monitors;

/// <summary>
/// 澶╂皵鐩戞帶鍣?- 鏀寔鍜岄澶╂皵 / OpenWeatherMap
/// 闇€瑕佺敤鎴烽厤缃?API Key 鎵嶈兘鍚敤
/// </summary>
public sealed class WeatherMonitor : ISystemMonitor
{
    public string Id => "weather";
    public string Name => "澶╂皵";
    public string Description => "褰撳墠澶╂皵鍜屾俯搴︼紙闇€閰嶇疆 API Key锛?;
    public string Icon => "顪?; // Segoe MDL2 Sunny
    public bool Enabled { get; set; } = false; // 榛樿绂佺敤锛岄渶閰嶇疆鍚庡惎鐢?    public event Action<IslandEvent>? EventTriggered;

    private DispatcherTimer? _timer;
    private bool _isRunning;
    private WeatherConfig? _config;
    private WeatherData? _lastData;
    private DateTime _lastFetchTime = DateTime.MinValue;
    private static readonly TimeSpan FetchInterval = TimeSpan.FromMinutes(30);

    public void Start()
    {
        if (_isRunning) return;

        // 灏濊瘯鍔犺浇閰嶇疆
        _config = WeatherConfig.Load();
        if (_config == null || string.IsNullOrWhiteSpace(_config.ApiKey))
        {
            // 鏈厤缃?API Key锛屼繚鎸佺鐢ㄧ姸鎬?            Enabled = false;
            return;
        }

        Enabled = true;
        _isRunning = true;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(30) };
        _timer.Tick += (_, _) => FetchWeather();
        _timer.Start();

        // 棣栨寤惰繜 5 绉掕幏鍙栵紙閬垮厤鍚姩鏃堕樆濉烇級
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
            _timer.Start();
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

        // 棰戠巼闄愬埗锛?0 鍒嗛挓鍐呬笉閲嶅璇锋眰
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
                        Title: $"{data.City} 路 {data.Condition}",
                        Content: $"{data.Temp}掳C 浣撴劅 {data.FeelsLike}掳C",
                        IconKind: GetWeatherIcon(data.Condition)));
                }
            }
        }
        catch
        {
            // 闈欓粯澶辫触锛屼笅娆￠噸璇?        }
    }

    private static bool ShouldTriggerEvent(WeatherData data)
    {
        // 棣栨鑾峰彇
        if (data == null)
            return true;

        // 娓╁害鍙樺寲 > 3掳C
        // 澶╂皵鐘跺喌鍙樺寲
        // 棰勮淇℃伅

        return true; // 绠€鍖栵細姣忔閮借Е鍙戯紙瀹為檯搴斾笌 _lastData 姣旇緝锛?    }

    private static string GetWeatherIcon(string condition)
    {
        var lower = condition.ToLowerInvariant();

        if (lower.Contains("鏅?) || lower.Contains("clear") || lower.Contains("sun"))
            return "weather_sunny";

        if (lower.Contains("浜?) || lower.Contains("cloud") || lower.Contains("闃?))
            return "weather_cloudy";

        if (lower.Contains("闆?) || lower.Contains("rain"))
            return "weather_rain";

        if (lower.Contains("闆?) || lower.Contains("snow"))
            return "weather_snow";

        if (lower.Contains("闆?) || lower.Contains("闆?) || lower.Contains("闆?))
            return "weather_fog";

        if (lower.Contains("闆?) || lower.Contains("thunder"))
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

            string condition = "鏈煡";
            string city = "鏈煡鍩庡競";

            if (doc.RootElement.TryGetProperty("weather", out var weather) &&
                weather.GetArrayLength() > 0)
            {
                var first = weather[0];
                if (first.TryGetProperty("description", out var descProp))
                    condition = descProp.GetString() ?? "鏈煡";
            }

            if (doc.RootElement.TryGetProperty("name", out var nameProp))
                city = nameProp.GetString() ?? "鏈煡鍩庡競";

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
            // 鍜岄澶╂皵 API锛堥渶娉ㄥ唽鑾峰彇 Key锛?            // https://dev.qweather.com/docs/api/weather/weather-now/
            var url = $"https://devapi.qweather.com/v7/weather/now?location={Uri.EscapeDataString(config.City ?? "101010100")}&key={config.ApiKey}";

            using var response = Http.GetAsync(url).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
                return null;

            var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            return ParseQWeatherResponse(json, config.City ?? "鍖椾含");
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

            string condition = "鏈煡";
            if (now.TryGetProperty("text", out var textProp))
                condition = textProp.GetString() ?? "鏈煡";

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
/// 澶╂皵閰嶇疆
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
/// 澶╂皵鏁版嵁
/// </summary>
public sealed record WeatherData(
    string City,
    string Condition,
    double Temp,
    double FeelsLike,
    DateTime Timestamp);


using System.Net.Http;
using System.Text.Json;

namespace FluidBar;

/// <summary>
/// 自动更新检查器框架
/// </summary>
public static class UpdateChecker
{
    private static readonly HttpClient Http = CreateHttpClient();
    private static readonly string VersionCheckUrl = "https://api.github.com/repos/Doulor/FluidBar/releases/latest";
    private static readonly string CurrentVersion = "1.0.0";

    /// <summary>
    /// 检查更新
    /// </summary>
    public static async Task<UpdateCheckResult> CheckForUpdateAsync()
    {
        try
        {
            var response = await Http.GetAsync(VersionCheckUrl);
            if (!response.IsSuccessStatusCode)
            {
                return new UpdateCheckResult
                {
                    Success = false,
                    Message = "无法连接到更新服务器"
                };
            }

            var json = await response.Content.ReadAsStringAsync();
            var release = JsonSerializer.Deserialize<GitHubRelease>(json);

            if (release == null || string.IsNullOrEmpty(release.TagName))
            {
                return new UpdateCheckResult
                {
                    Success = false,
                    Message = "无法解析版本信息"
                };
            }

            var latestVersion = release.TagName.TrimStart('v');
            var hasUpdate = IsNewerVersion(latestVersion, CurrentVersion);

            return new UpdateCheckResult
            {
                Success = true,
                HasUpdate = hasUpdate,
                CurrentVersion = CurrentVersion,
                LatestVersion = latestVersion,
                ReleaseNotes = release.Body ?? "",
                DownloadUrl = release.HtmlUrl ?? "",
                PublishedAt = release.PublishedAt
            };
        }
        catch (Exception ex)
        {
            return new UpdateCheckResult
            {
                Success = false,
                Message = $"检查更新失败: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 比较版本号
    /// </summary>
    private static bool IsNewerVersion(string latest, string current)
    {
        try
        {
            var latestParts = latest.Split('.').Select(int.Parse).ToArray();
            var currentParts = current.Split('.').Select(int.Parse).ToArray();

            for (int i = 0; i < Math.Min(latestParts.Length, currentParts.Length); i++)
            {
                if (latestParts[i] > currentParts[i])
                    return true;
                if (latestParts[i] < currentParts[i])
                    return false;
            }

            return latestParts.Length > currentParts.Length;
        }
        catch
        {
            return false;
        }
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "FluidBar/1.0 (https://github.com/Doulor/FluidBar)");

        return client;
    }
}

/// <summary>
/// 更新检查结果
/// </summary>
public sealed class UpdateCheckResult
{
    public bool Success { get; set; }
    public bool HasUpdate { get; set; }
    public string CurrentVersion { get; set; } = "";
    public string LatestVersion { get; set; } = "";
    public string ReleaseNotes { get; set; } = "";
    public string DownloadUrl { get; set; } = "";
    public DateTime? PublishedAt { get; set; }
    public string Message { get; set; } = "";
}

/// <summary>
/// GitHub Release 信息
/// </summary>
public sealed class GitHubRelease
{
    public string? TagName { get; set; }
    public string? Name { get; set; }
    public string? Body { get; set; }
    public string? HtmlUrl { get; set; }
    public DateTime? PublishedAt { get; set; }
}

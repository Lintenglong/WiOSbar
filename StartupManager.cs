using Microsoft.Win32;
using System.Diagnostics;
using System.IO;

namespace FluidBar;

/// <summary>
/// 开机自启动管理器
/// </summary>
public static class StartupManager
{
    private const string AppName = "FluidBar";
    private const string RegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    /// <summary>
    /// 检查是否已设置开机自启动
    /// </summary>
    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, false);
            var value = key?.GetValue(AppName) as string;
            return !string.IsNullOrEmpty(value);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 启用开机自启动
    /// </summary>
    public static bool Enable()
    {
        try
        {
            var exePath = GetExecutablePath();
            if (string.IsNullOrEmpty(exePath))
                return false;

            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, true);
            if (key == null)
                return false;

            // 添加静默启动参数（如果支持）
            key.SetValue(AppName, $"\"{exePath}\"");
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 禁用开机自启动
    /// </summary>
    public static bool Disable()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, true);
            if (key == null)
                return false;

            key.DeleteValue(AppName, false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 切换自启动状态
    /// </summary>
    public static bool Toggle()
    {
        return IsEnabled() ? Disable() : Enable();
    }

    /// <summary>
    /// 获取可执行文件路径
    /// </summary>
    private static string GetExecutablePath()
    {
        try
        {
            var process = Process.GetCurrentProcess();
            var mainModule = process.MainModule;
            if (mainModule != null && !string.IsNullOrEmpty(mainModule.FileName))
            {
                return mainModule.FileName;
            }

            // 备用方案
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FluidBar.exe");
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 使用任务计划程序创建自启动任务（更可靠）
    /// </summary>
    public static bool EnableViaTaskScheduler()
    {
        try
        {
            var exePath = GetExecutablePath();
            if (string.IsNullOrEmpty(exePath))
                return false;

            // 使用 schtasks 命令创建任务
            var psi = new ProcessStartInfo
            {
                FileName = "schtasks",
                Arguments = $"/create /tn \"{AppName}\" /tr \"\\\"{exePath}\\\"\" /sc onlogon /rl highest /f",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(psi);
            process?.WaitForExit(5000);
            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 删除任务计划程序任务
    /// </summary>
    public static bool DisableViaTaskScheduler()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "schtasks",
                Arguments = $"/delete /tn \"{AppName}\" /f",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(psi);
            process?.WaitForExit(5000);
            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}

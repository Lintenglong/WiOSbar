using System.IO;
using System.IO.Compression;
using System.Text.Json;

namespace FluidBar;

/// <summary>
/// 设置备份与恢复管理器
/// </summary>
public static class SettingsBackupManager
{
    private static readonly string BackupDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FluidBar", "backups");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// 备份配置
    /// </summary>
    public static BackupResult CreateBackup(string? customName = null)
    {
        try
        {
            Directory.CreateDirectory(BackupDir);

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var backupName = customName ?? $"backup_{timestamp}";
            var backupPath = Path.Combine(BackupDir, $"{backupName}.zip");

            // 收集需要备份的文件
            var filesToBackup = new List<string>();

            // 主设置
            var settingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FluidBar", "settings.json");
            if (File.Exists(settingsPath))
                filesToBackup.Add(settingsPath);

            // 媒体设置
            var mediaPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FluidBar", "media.json");
            if (File.Exists(mediaPath))
                filesToBackup.Add(mediaPath);

            // 剪贴板设置
            var clipboardPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FluidBar", "clipboard.json");
            if (File.Exists(clipboardPath))
                filesToBackup.Add(clipboardPath);

            // 主题配置
            var themePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FluidBar", "theme.json");
            if (File.Exists(themePath))
                filesToBackup.Add(themePath);

            // 天气配置
            var weatherPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FluidBar", "weather.json");
            if (File.Exists(weatherPath))
                filesToBackup.Add(weatherPath);

            if (filesToBackup.Count == 0)
            {
                return new BackupResult
                {
                    Success = false,
                    Message = "没有找到可备份的配置文件"
                };
            }

            // 创建 ZIP 备份
            using (var archive = ZipFile.Open(backupPath, ZipArchiveMode.Create))
            {
                foreach (var file in filesToBackup)
                {
                    var entryName = Path.GetFileName(file);
                    archive.CreateEntryFromFile(file, entryName);
                }
            }

            // 创建元数据文件
            var metadata = new BackupMetadata
            {
                BackupName = backupName,
                CreatedAt = DateTime.UtcNow,
                FileCount = filesToBackup.Count,
                Files = filesToBackup.Select(Path.GetFileName).ToList()!,
                AppVersion = "1.0"
            };

            var metadataPath = Path.Combine(BackupDir, $"{backupName}.meta.json");
            File.WriteAllText(metadataPath, JsonSerializer.Serialize(metadata, JsonOptions));

            return new BackupResult
            {
                Success = true,
                BackupPath = backupPath,
                MetadataPath = metadataPath,
                FileCount = filesToBackup.Count,
                Message = $"备份成功，包含 {filesToBackup.Count} 个文件"
            };
        }
        catch (Exception ex)
        {
            return new BackupResult
            {
                Success = false,
                Message = $"备份失败: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 恢复配置
    /// </summary>
    public static RestoreResult RestoreBackup(string backupPath)
    {
        try
        {
            if (!File.Exists(backupPath))
            {
                return new RestoreResult
                {
                    Success = false,
                    Message = "备份文件不存在"
                };
            }

            var settingsDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FluidBar");

            Directory.CreateDirectory(settingsDir);

            var restoredFiles = new List<string>();

            using (var archive = ZipFile.OpenRead(backupPath))
            {
                foreach (var entry in archive.Entries)
                {
                    if (entry.FullName.EndsWith("/"))
                        continue;

                    var targetPath = Path.Combine(settingsDir, entry.Name);
                    entry.ExtractToFile(targetPath, overwrite: true);
                    restoredFiles.Add(entry.Name);
                }
            }

            return new RestoreResult
            {
                Success = true,
                RestoredFiles = restoredFiles,
                Message = $"恢复成功，恢复了 {restoredFiles.Count} 个文件"
            };
        }
        catch (Exception ex)
        {
            return new RestoreResult
            {
                Success = false,
                Message = $"恢复失败: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 获取所有备份列表
    /// </summary>
    public static List<BackupInfo> GetAllBackups()
    {
        var backups = new List<BackupInfo>();

        try
        {
            if (!Directory.Exists(BackupDir))
                return backups;

            var zipFiles = Directory.GetFiles(BackupDir, "*.zip");
            foreach (var zipFile in zipFiles)
            {
                var name = Path.GetFileNameWithoutExtension(zipFile);
                var metaFile = Path.Combine(BackupDir, $"{name}.meta.json");

                BackupMetadata? metadata = null;
                if (File.Exists(metaFile))
                {
                    try
                    {
                        var json = File.ReadAllText(metaFile);
                        metadata = JsonSerializer.Deserialize<BackupMetadata>(json, JsonOptions);
                    }
                    catch { }
                }

                var fileInfo = new FileInfo(zipFile);
                backups.Add(new BackupInfo
                {
                    Name = name,
                    Path = zipFile,
                    SizeBytes = fileInfo.Length,
                    CreatedAt = metadata?.CreatedAt ?? fileInfo.CreationTimeUtc,
                    FileCount = metadata?.FileCount ?? 0,
                    Metadata = metadata
                });
            }
        }
        catch { }

        return backups.OrderByDescending(b => b.CreatedAt).ToList();
    }

    /// <summary>
    /// 删除备份
    /// </summary>
    public static bool DeleteBackup(string backupName)
    {
        try
        {
            var zipPath = Path.Combine(BackupDir, $"{backupName}.zip");
            var metaPath = Path.Combine(BackupDir, $"{backupName}.meta.json");

            if (File.Exists(zipPath))
                File.Delete(zipPath);

            if (File.Exists(metaPath))
                File.Delete(metaPath);

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 清理旧备份（保留最近 N 个）
    /// </summary>
    public static int CleanupOldBackups(int keepCount = 10)
    {
        var backups = GetAllBackups();
        var toDelete = backups.Skip(keepCount).ToList();

        var deletedCount = 0;
        foreach (var backup in toDelete)
        {
            if (DeleteBackup(backup.Name))
                deletedCount++;
        }

        return deletedCount;
    }

    /// <summary>
    /// 导出备份到指定位置
    /// </summary>
    public static bool ExportBackup(string backupName, string destinationPath)
    {
        try
        {
            var sourcePath = Path.Combine(BackupDir, $"{backupName}.zip");
            if (!File.Exists(sourcePath))
                return false;

            File.Copy(sourcePath, destinationPath, overwrite: true);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// 备份结果
/// </summary>
public sealed class BackupResult
{
    public bool Success { get; set; }
    public string? BackupPath { get; set; }
    public string? MetadataPath { get; set; }
    public int FileCount { get; set; }
    public string Message { get; set; } = "";
}

/// <summary>
/// 恢复结果
/// </summary>
public sealed class RestoreResult
{
    public bool Success { get; set; }
    public List<string> RestoredFiles { get; set; } = new();
    public string Message { get; set; } = "";
}

/// <summary>
/// 备份信息
/// </summary>
public sealed class BackupInfo
{
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public long SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
    public int FileCount { get; set; }
    public BackupMetadata? Metadata { get; set; }

    public string SizeFormatted => SizeBytes < 1024 * 1024
        ? $"{SizeBytes / 1024.0:F1} KB"
        : $"{SizeBytes / (1024.0 * 1024.0):F2} MB";
}

/// <summary>
/// 备份元数据
/// </summary>
public sealed class BackupMetadata
{
    public string BackupName { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public int FileCount { get; set; }
    public List<string> Files { get; set; } = new();
    public string AppVersion { get; set; } = "";
}

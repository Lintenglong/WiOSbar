using System.IO;
using System.Text.Json.Serialization;

namespace FluidBar;

/// <summary>
/// 剪贴板项目 - 支持多种内容类型
/// </summary>
public sealed class ClipboardItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public ClipboardContentType Type { get; set; } = ClipboardContentType.Text;
    public string? TextContent { get; set; }
    public string? ImagePath { get; set; } // 临时保存的图片路径
    public string? FilePath { get; set; } // 文件路径（如果是文件）
    public string? SourceApp { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public bool IsFavorite { get; set; }
    public string? PreviewText { get; set; } // 预览文本（截断版）

    [JsonIgnore]
    public bool HasImage => Type == ClipboardContentType.Image && !string.IsNullOrWhiteSpace(ImagePath);

    [JsonIgnore]
    public bool HasFile => Type == ClipboardContentType.File && !string.IsNullOrWhiteSpace(FilePath);

    /// <summary>
    /// 创建文本类型的剪贴板项目
    /// </summary>
    public static ClipboardItem CreateText(string text, string? sourceApp = null)
    {
        var preview = text.Length > 100 ? text.Substring(0, 100) + "..." : text;
        return new ClipboardItem
        {
            Type = ClipboardContentType.Text,
            TextContent = text,
            SourceApp = sourceApp,
            PreviewText = preview
        };
    }

    /// <summary>
    /// 创建图片类型的剪贴板项目
    /// </summary>
    public static ClipboardItem CreateImage(string imagePath, string? sourceApp = null)
    {
        return new ClipboardItem
        {
            Type = ClipboardContentType.Image,
            ImagePath = imagePath,
            SourceApp = sourceApp,
            PreviewText = "[图片]"
        };
    }

    /// <summary>
    /// 创建文件类型的剪贴板项目
    /// </summary>
    public static ClipboardItem CreateFile(string filePath, string? sourceApp = null)
    {
        var fileName = Path.GetFileName(filePath);
        return new ClipboardItem
        {
            Type = ClipboardContentType.File,
            FilePath = filePath,
            SourceApp = sourceApp,
            PreviewText = $"[文件] {fileName}"
        };
    }

    /// <summary>
    /// 创建 URL 类型的剪贴板项目
    /// </summary>
    public static ClipboardItem CreateUrl(string url, string? sourceApp = null)
    {
        return new ClipboardItem
        {
            Type = ClipboardContentType.Url,
            TextContent = url,
            SourceApp = sourceApp,
            PreviewText = url.Length > 80 ? url.Substring(0, 80) + "..." : url
        };
    }
}

/// <summary>
/// 剪贴板内容类型
/// </summary>
public enum ClipboardContentType
{
    Text,
    Image,
    File,
    Url,
    RichText
}

using System.Text.Json.Serialization;

namespace FluidBar;

/// <summary>
/// 鍓创鏉块」鐩?- 鏀寔澶氱鍐呭绫诲瀷
/// </summary>
public sealed class ClipboardItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public ClipboardContentType Type { get; set; } = ClipboardContentType.Text;
    public string? TextContent { get; set; }
    public string? ImagePath { get; set; } // 涓存椂淇濆瓨鐨勫浘鐗囪矾寰?    public string? FilePath { get; set; } // 鏂囦欢璺緞锛堝鏋滄槸鏂囦欢锛?    public string? SourceApp { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public bool IsFavorite { get; set; }
    public string? PreviewText { get; set; } // 棰勮鏂囨湰锛堟埅鏂増锛?
    [JsonIgnore]
    public bool HasImage => Type == ClipboardContentType.Image && !string.IsNullOrWhiteSpace(ImagePath);

    [JsonIgnore]
    public bool HasFile => Type == ClipboardContentType.File && !string.IsNullOrWhiteSpace(FilePath);

    /// <summary>
    /// 鍒涘缓鏂囨湰绫诲瀷鐨勫壀璐存澘椤圭洰
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
    /// 鍒涘缓鍥剧墖绫诲瀷鐨勫壀璐存澘椤圭洰
    /// </summary>
    public static ClipboardItem CreateImage(string imagePath, string? sourceApp = null)
    {
        return new ClipboardItem
        {
            Type = ClipboardContentType.Image,
            ImagePath = imagePath,
            SourceApp = sourceApp,
            PreviewText = "[鍥剧墖]"
        };
    }

    /// <summary>
    /// 鍒涘缓鏂囦欢绫诲瀷鐨勫壀璐存澘椤圭洰
    /// </summary>
    public static ClipboardItem CreateFile(string filePath, string? sourceApp = null)
    {
        var fileName = Path.GetFileName(filePath);
        return new ClipboardItem
        {
            Type = ClipboardContentType.File,
            FilePath = filePath,
            SourceApp = sourceApp,
            PreviewText = $"[鏂囦欢] {fileName}"
        };
    }

    /// <summary>
    /// 鍒涘缓 URL 绫诲瀷鐨勫壀璐存澘椤圭洰
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
/// 鍓创鏉垮唴瀹圭被鍨?/// </summary>
public enum ClipboardContentType
{
    Text,
    Image,
    File,
    Url,
    RichText
}



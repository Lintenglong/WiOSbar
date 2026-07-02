using System.IO;
using System.Text.Json;

namespace FluidBar;

/// <summary>
/// 剪贴板历史管理器 - 支持持久化存储
/// </summary>
public sealed class ClipboardHistoryManager
{
    private static readonly string HistoryDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FluidBar", "clipboard-history");

    private static readonly string HistoryIndexPath = Path.Combine(HistoryDir, "index.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly List<ClipboardItem> _history = new();
    private readonly int _maxItems;
    private readonly bool _saveImages;

    public IReadOnlyList<ClipboardItem> History => _history.AsReadOnly();

    public ClipboardHistoryManager(int maxItems = 50, bool saveImages = true)
    {
        _maxItems = maxItems;
        _saveImages = saveImages;
        Load();
    }

    /// <summary>
    /// 添加新的剪贴板项目
    /// </summary>
    public void Add(ClipboardItem item)
    {
        // 检查重复
        var existing = _history.FirstOrDefault(h =>
            h.Type == item.Type &&
            h.TextContent == item.TextContent &&
            h.ImagePath == item.ImagePath);

        if (existing != null)
        {
            // 更新时间戳
            existing.Timestamp = DateTime.Now;
            _history.Remove(existing);
            _history.Insert(0, existing);
        }
        else
        {
            _history.Insert(0, item);

            // 限制数量
            while (_history.Count > _maxItems)
            {
                var removed = _history.Last();
                _history.RemoveAt(_history.Count - 1);

                // 清理图片文件
                if (_saveImages && removed.HasImage && File.Exists(removed.ImagePath))
                {
                    try { File.Delete(removed.ImagePath); } catch { }
                }
            }
        }

        Save();
    }

    /// <summary>
    /// 标记为收藏
    /// </summary>
    public void ToggleFavorite(string id)
    {
        var item = _history.FirstOrDefault(h => h.Id == id);
        if (item != null)
        {
            item.IsFavorite = !item.IsFavorite;
            Save();
        }
    }

    /// <summary>
    /// 删除项目
    /// </summary>
    public void Remove(string id)
    {
        var item = _history.FirstOrDefault(h => h.Id == id);
        if (item != null)
        {
            _history.Remove(item);

            // 清理图片文件
            if (_saveImages && item.HasImage && File.Exists(item.ImagePath))
            {
                try { File.Delete(item.ImagePath); } catch { }
            }

            Save();
        }
    }

    /// <summary>
    /// 按类型过滤
    /// </summary>
    public IEnumerable<ClipboardItem> Filter(ClipboardContentType? type = null, bool favoritesOnly = false)
    {
        var query = _history.AsEnumerable();

        if (type.HasValue)
            query = query.Where(h => h.Type == type.Value);

        if (favoritesOnly)
            query = query.Where(h => h.IsFavorite);

        return query;
    }

    /// <summary>
    /// 搜索
    /// </summary>
    public IEnumerable<ClipboardItem> Search(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return _history;

        var lower = keyword.ToLowerInvariant();
        return _history.Where(h =>
            (h.TextContent?.ToLowerInvariant().Contains(lower) ?? false) ||
            (h.PreviewText?.ToLowerInvariant().Contains(lower) ?? false) ||
            (h.SourceApp?.ToLowerInvariant().Contains(lower) ?? false));
    }

    /// <summary>
    /// 清空历史（保留收藏）
    /// </summary>
    public void Clear(bool keepFavorites = true)
    {
        if (keepFavorites)
        {
            var favorites = _history.Where(h => h.IsFavorite).ToList();
            _history.Clear();
            _history.AddRange(favorites);
        }
        else
        {
            // 清理所有图片
            if (_saveImages)
            {
                foreach (var item in _history.Where(h => h.HasImage))
                {
                    if (File.Exists(item.ImagePath))
                        try { File.Delete(item.ImagePath); } catch { }
                }
            }
            _history.Clear();
        }

        Save();
    }

    /// <summary>
    /// 加载历史
    /// </summary>
    private void Load()
    {
        try
        {
            Directory.CreateDirectory(HistoryDir);

            if (File.Exists(HistoryIndexPath))
            {
                var json = File.ReadAllText(HistoryIndexPath);
                var items = JsonSerializer.Deserialize<List<ClipboardItem>>(json, JsonOptions);

                if (items != null)
                {
                    _history.Clear();
                    _history.AddRange(items.Take(_maxItems));

                    // 验证图片文件是否存在
                    if (_saveImages)
                    {
                        foreach (var item in _history.Where(h => h.HasImage).ToList())
                        {
                            if (!string.IsNullOrWhiteSpace(item.ImagePath) && !File.Exists(item.ImagePath))
                            {
                                _history.Remove(item);
                            }
                        }
                    }
                }
            }
        }
        catch { }
    }

    /// <summary>
    /// 保存历史
    /// </summary>
    private void Save()
    {
        try
        {
            Directory.CreateDirectory(HistoryDir);
            var json = JsonSerializer.Serialize(_history, JsonOptions);
            File.WriteAllText(HistoryIndexPath, json);
        }
        catch { }
    }

    /// <summary>
    /// 获取统计信息
    /// </summary>
    public ClipboardStats GetStats()
    {
        return new ClipboardStats
        {
            TotalItems = _history.Count,
            TextCount = _history.Count(h => h.Type == ClipboardContentType.Text),
            ImageCount = _history.Count(h => h.Type == ClipboardContentType.Image),
            FileCount = _history.Count(h => h.Type == ClipboardContentType.File),
            UrlCount = _history.Count(h => h.Type == ClipboardContentType.Url),
            FavoriteCount = _history.Count(h => h.IsFavorite),
            OldestItem = _history.Any() ? _history.Min(h => h.Timestamp) : null,
            NewestItem = _history.Any() ? _history.Max(h => h.Timestamp) : null
        };
    }
}

/// <summary>
/// 剪贴板统计信息
/// </summary>
public sealed class ClipboardStats
{
    public int TotalItems { get; set; }
    public int TextCount { get; set; }
    public int ImageCount { get; set; }
    public int FileCount { get; set; }
    public int UrlCount { get; set; }
    public int FavoriteCount { get; set; }
    public DateTime? OldestItem { get; set; }
    public DateTime? NewestItem { get; set; }
}

using System.Collections.Concurrent;

namespace FluidBar.Utils;

/// <summary>
/// 线程安全的 LRU (Least Recently Used) 缓存实现
/// 用于优化专辑封面、歌词等资源的内存使用
/// </summary>
public sealed class LRUCache<TKey, TValue> where TKey : notnull
{
    private readonly int _capacity;
    private readonly ConcurrentDictionary<TKey, LinkedListNode<CacheItem>> _cache;
    private readonly LinkedList<CacheItem> _lruList;
    private readonly object _lock = new();

    private sealed class CacheItem
    {
        public TKey Key { get; }
        public TValue Value { get; set; }
        public DateTime LastAccess { get; set; }

        public CacheItem(TKey key, TValue value)
        {
            Key = key;
            Value = value;
            LastAccess = DateTime.UtcNow;
        }
    }

    public LRUCache(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentException("Capacity must be positive", nameof(capacity));

        _capacity = capacity;
        _cache = new ConcurrentDictionary<TKey, LinkedListNode<CacheItem>>();
        _lruList = new LinkedList<CacheItem>();
    }

    /// <summary>
    /// 获取缓存项，如果不存在则返回 default
    /// </summary>
    public TValue? Get(TKey key)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var node))
            {
                // 移到链表头部（最近使用）
                _lruList.Remove(node);
                _lruList.AddFirst(node);
                node.Value.LastAccess = DateTime.UtcNow;
                return node.Value.Value;
            }
        }
        return default;
    }

    /// <summary>
    /// 尝试获取缓存项
    /// </summary>
    public bool TryGet(TKey key, out TValue? value)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var node))
            {
                _lruList.Remove(node);
                _lruList.AddFirst(node);
                node.Value.LastAccess = DateTime.UtcNow;
                value = node.Value.Value;
                return true;
            }
        }
        value = default;
        return false;
    }

    /// <summary>
    /// 添加或更新缓存项
    /// </summary>
    public void Set(TKey key, TValue value)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var existingNode))
            {
                // 更新现有项
                existingNode.Value.Value = value;
                existingNode.Value.LastAccess = DateTime.UtcNow;
                _lruList.Remove(existingNode);
                _lruList.AddFirst(existingNode);
            }
            else
            {
                // 添加新项
                if (_cache.Count >= _capacity)
                {
                    // 移除最久未使用的项
                    EvictOldest();
                }

                var item = new CacheItem(key, value);
                var node = _lruList.AddFirst(item);
                _cache[key] = node;
            }
        }
    }

    /// <summary>
    /// 移除指定键的缓存项
    /// </summary>
    public bool Remove(TKey key)
    {
        lock (_lock)
        {
            if (_cache.TryRemove(key, out var node))
            {
                _lruList.Remove(node);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 清空缓存
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _cache.Clear();
            _lruList.Clear();
        }
    }

    /// <summary>
    /// 移除最久未使用的项
    /// </summary>
    private void EvictOldest()
    {
        if (_lruList.Last != null)
        {
            var oldest = _lruList.Last;
            _cache.TryRemove(oldest.Value.Key, out _);
            _lruList.RemoveLast();
        }
    }

    /// <summary>
    /// 当前缓存项数量
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _cache.Count;
            }
        }
    }

    /// <summary>
    /// 缓存容量
    /// </summary>
    public int Capacity => _capacity;

    /// <summary>
    /// 获取所有键（按最近使用顺序）
    /// </summary>
    public IEnumerable<TKey> Keys
    {
        get
        {
            lock (_lock)
            {
                return _lruList.Select(item => item.Key).ToList();
            }
        }
    }

    /// <summary>
    /// 检查是否包含指定键
    /// </summary>
    public bool ContainsKey(TKey key)
    {
        return _cache.ContainsKey(key);
    }
}

/// <summary>
/// 带过期时间的 LRU 缓存
/// </summary>
public sealed class ExpiringLRUCache<TKey, TValue> where TKey : notnull
{
    private readonly LRUCache<TKey, CacheEntry> _cache;
    private readonly TimeSpan _defaultTtl;

    private sealed class CacheEntry
    {
        public TValue Value { get; }
        public DateTime ExpiresAt { get; }

        public CacheEntry(TValue value, TimeSpan ttl)
        {
            Value = value;
            ExpiresAt = DateTime.UtcNow.Add(ttl);
        }

        public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    }

    public ExpiringLRUCache(int capacity, TimeSpan defaultTtl)
    {
        _cache = new LRUCache<TKey, CacheEntry>(capacity);
        _defaultTtl = defaultTtl;
    }

    public TValue? Get(TKey key)
    {
        if (_cache.TryGet(key, out var entry) && entry != null)
        {
            if (!entry.IsExpired)
                return entry.Value;

            // 已过期，移除
            _cache.Remove(key);
        }
        return default;
    }

    public void Set(TKey key, TValue value, TimeSpan? ttl = null)
    {
        var entry = new CacheEntry(value, ttl ?? _defaultTtl);
        _cache.Set(key, entry);
    }

    public bool TryGet(TKey key, out TValue? value)
    {
        value = Get(key);
        return value != null;
    }

    public void Remove(TKey key) => _cache.Remove(key);
    public void Clear() => _cache.Clear();
    public int Count => _cache.Count;
}

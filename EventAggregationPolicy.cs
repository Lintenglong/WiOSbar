using System;
using System.Collections.Generic;
using System.Linq;

namespace FluidBar;

/// <summary>
/// 事件聚合策略 - 实现 iOS 灵动岛式的智能防打扰体验
/// </summary>
public static class EventAggregationPolicy
{
    /// <summary>
    /// 判断两个事件是否应该聚合（合并为同一事件）
    /// </summary>
    public static bool ShouldAggregate(IslandEvent e1, IslandEvent e2)
    {
        if (e1.Source != e2.Source)
            return false;

        var timeDiff = Math.Abs((e2.Timestamp - e1.Timestamp).TotalMilliseconds);

        // 同源事件在 800ms 内触发，视为连续操作
        if (timeDiff > 800)
            return false;

        // 音量调节聚合
        if (e1.Source == "volume")
            return true;

        // 亮度调节聚合
        if (e1.Source == "brightness")
            return true;

        // 锁定键快速切换聚合
        if (e1.Source == "lockkey")
            return true;

        // 输入法切换聚合
        if (e1.Source == "inputmethod")
            return true;

        return false;
    }

    /// <summary>
    /// 获取事件优先级（数值越高越优先显示）
    /// </summary>
    public static int GetPriority(IslandEvent evt)
    {
        // 通知类事件最高优先级
        if (evt.Source == "notification")
            return 100;

        // 媒体控制次高
        if (evt.Source == "media")
            return 85;

        // Agent 状态
        if (evt.Source == "agent")
            return 75;

        // 剪贴板操作
        if (evt.Source == "clipboard")
            return 65;

        // 锁定键状态
        if (evt.Source == "lockkey")
            return 55;

        // 系统状态（音量、亮度等）
        if (IsSystemStatusSource(evt.Source))
            return 45;

        // 时钟等低优先级
        if (evt.Source == "clock")
            return 20;

        return 50; // 默认优先级
    }

    private static bool IsSystemStatusSource(string source)
    {
        return source is "volume" or "brightness" or "battery" or "network" or
               "usb" or "bluetooth" or "inputmethod";
    }

    /// <summary>
    /// 判断是否处于静默期（降低非关键事件优先级）
    /// </summary>
    public static bool IsInQuietPeriod(IslandEvent evt)
    {
        var hour = DateTime.Now.Hour;

        // 午夜 00:00-06:00 降低非关键事件
        if (hour is >= 0 and < 6)
        {
            // 仅通知和媒体保持高优先级
            return evt.Source is not ("notification" or "media");
        }

        return false;
    }

    /// <summary>
    /// 聚合多个同类事件为单个显示事件
    /// </summary>
    public static IslandEvent AggregateEvents(IEnumerable<IslandEvent> events)
    {
        var eventList = events.ToList();
        if (eventList.Count == 0)
            throw new ArgumentException("事件列表不能为空");

        if (eventList.Count == 1)
            return eventList[0];

        var first = eventList[0];
        var source = first.Source;

        // 音量聚合
        if (source == "volume")
        {
            var lastVolume = eventList.Last();
            return lastVolume with
            {
                Content = $"{lastVolume.Content} (x{eventList.Count})",
                Title = "音量调节"
            };
        }

        // 亮度聚合
        if (source == "brightness")
        {
            var lastBrightness = eventList.Last();
            return lastBrightness with
            {
                Content = $"{lastBrightness.Content} (x{eventList.Count})",
                Title = "亮度调节"
            };
        }

        // 通用聚合
        return first with
        {
            Content = $"{first.Content} (x{eventList.Count})"
        };
    }

    /// <summary>
    /// 事件优先级排序（用于多岛模式下的显示顺序）
    /// </summary>
    public static IOrderedEnumerable<IslandEvent> OrderByPriority(IEnumerable<IslandEvent> events)
    {
        return events.OrderByDescending(GetPriority)
                     .ThenByDescending(e => e.Timestamp);
    }

    /// <summary>
    /// 判断事件是否应该被静默（不触发灵动岛）
    /// </summary>
    public static bool ShouldSuppress(IslandEvent evt, IslandEvent? lastEvent)
    {
        // Media snapshots may keep the same title/content while progress, artwork,
        // lyrics, or playing state changes. Let the window decide whether it can
        // update them incrementally instead of dropping them as duplicates.
        if (evt.Source == "media")
            return false;

        // 静默期内非关键事件
        if (IsInQuietPeriod(evt))
            return true;

        // 重复事件抑制（相同内容且间隔 < 3 秒）
        if (lastEvent != null)
        {
            var timeDiff = (evt.Timestamp - lastEvent.Timestamp).TotalSeconds;
            if (timeDiff < 3 && evt.Source == lastEvent.Source && evt.Content == lastEvent.Content)
                return true;
        }

        return false;
    }

    /// <summary>
    /// 获取聚合后的显示持续时间（毫秒）
    /// </summary>
    public static int GetDisplayDurationMs(IslandEvent evt, int eventCount = 1)
    {
        // 通知类事件显示更久
        if (evt.Source == "notification")
            return 5000;

        // 媒体控制显示 4 秒
        if (evt.Source == "media")
            return 4000;

        // 聚合事件显示更久
        if (eventCount > 1)
            return 3500;

        // 默认 3 秒
        return 3000;
    }
}

/// <summary>
/// 带时间戳的事件包装（用于聚合判断）
/// </summary>
public sealed record TimestampedEvent(IslandEvent Event, DateTime Timestamp)
{
    public static TimestampedEvent From(IslandEvent evt) =>
        new(evt, DateTime.UtcNow);
}

public static class MediaDisplayInterruptionPolicy
{
    public static bool ShouldDeferToPersistentMedia(
        IslandEvent evt,
        IslandViewKind nextKind,
        bool mediaActive,
        bool hasPersistentMedia)
    {
        if (!mediaActive || !hasPersistentMedia)
            return false;

        if (evt.Source == "media")
            return false;

        if (nextKind == IslandViewKind.Clock)
            return true;

        return IsPassiveBackgroundSource(evt.Source);
    }

    public static bool IsPassiveBackgroundSource(string source)
    {
        return source is "clock"
            or "cpu"
            or "memory"
            or "disk"
            or "network"
            or "network_speed"
            or "weather"
            or "temperature";
    }
}

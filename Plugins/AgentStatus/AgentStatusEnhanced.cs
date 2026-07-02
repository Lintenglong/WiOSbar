using System.Text.Json;

namespace FluidBar;

/// <summary>
/// 增强的 Agent Hook 事件（向后兼容）
/// </summary>
public sealed class AgentHookEventEnhanced
{
    public AgentEventType Type { get; set; } = AgentEventType.Unknown;
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public string? Detail { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? Source { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }

    public static AgentHookEventEnhanced Parse(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var evt = new AgentHookEventEnhanced();

            // 解析类型
            if (root.TryGetProperty("type", out var typeProp))
            {
                var typeStr = typeProp.GetString()?.ToLowerInvariant() ?? "";
                evt.Type = ParseEventType(typeStr);
            }
            else if (root.TryGetProperty("status", out var statusProp))
            {
                // 兼容旧格式
                var status = statusProp.GetString()?.ToLowerInvariant() ?? "";
                evt.Type = status switch
                {
                    "success" or "completed" => AgentEventType.TaskCompleted,
                    "error" or "failed" => AgentEventType.TaskFailed,
                    _ => AgentEventType.Unknown
                };
            }

            // 解析标题和内容
            if (root.TryGetProperty("title", out var titleProp))
                evt.Title = titleProp.GetString() ?? "";

            if (root.TryGetProperty("content", out var contentProp))
                evt.Content = contentProp.GetString() ?? "";

            if (root.TryGetProperty("summary", out var summaryProp))
                evt.Content = summaryProp.GetString() ?? evt.Content;

            if (root.TryGetProperty("detail", out var detailProp))
                evt.Detail = detailProp.GetString();

            if (root.TryGetProperty("source", out var sourceProp))
                evt.Source = sourceProp.GetString();

            if (root.TryGetProperty("tool", out var toolProp))
                evt.Source = toolProp.GetString() ?? evt.Source;

            // 解析时间戳
            if (root.TryGetProperty("timestamp", out var tsProp))
            {
                if (DateTime.TryParse(tsProp.GetString(), out var ts))
                    evt.Timestamp = ts;
            }

            return evt;
        }
        catch
        {
            return new AgentHookEventEnhanced
            {
                Type = AgentEventType.Unknown,
                Title = "Agent 事件",
                Content = "无法解析事件数据"
            };
        }
    }

    private static AgentEventType ParseEventType(string typeStr)
    {
        return typeStr switch
        {
            "task_started" => AgentEventType.TaskStarted,
            "task_completed" => AgentEventType.TaskCompleted,
            "task_failed" => AgentEventType.TaskFailed,
            "build_started" => AgentEventType.BuildStarted,
            "build_succeeded" or "build_success" => AgentEventType.BuildSucceeded,
            "build_failed" => AgentEventType.BuildFailed,
            "test_completed" or "test_run_completed" => AgentEventType.TestRunCompleted,
            "test_failed" => AgentEventType.TestRunFailed,
            "git_status" => AgentEventType.GitStatusChanged,
            "lint_completed" or "linting_completed" => AgentEventType.LintingCompleted,
            "lint_failed" => AgentEventType.LintingFailed,
            _ => AgentEventType.Custom
        };
    }
}

/// <summary>
/// Agent 事件类型枚举
/// </summary>
public enum AgentEventType
{
    Unknown,
    TaskStarted,
    TaskCompleted,
    TaskFailed,
    BuildStarted,
    BuildSucceeded,
    BuildFailed,
    TestRunCompleted,
    TestRunFailed,
    GitStatusChanged,
    LintingCompleted,
    LintingFailed,
    Custom
}

/// <summary>
/// 增强的 Agent 事件转 IslandEvent 工厂
/// </summary>
public static class AgentStatusIslandEventFactoryEnhanced
{
    public static IslandEvent FromHook(AgentHookEventEnhanced hook)
    {
        var iconKind = GetIconKind(hook.Type);
        var title = string.IsNullOrWhiteSpace(hook.Title)
            ? GetDefaultTitle(hook.Type)
            : hook.Title;

        // 构建增强内容
        var content = hook.Content;
        if (!string.IsNullOrWhiteSpace(hook.Detail))
        {
            content = $"{content} · {hook.Detail}";
        }

        return new IslandEvent(
            Source: "agent",
            Title: title,
            Content: content,
            IconKind: iconKind);
    }

    private static string GetIconKind(AgentEventType type)
    {
        return type switch
        {
            AgentEventType.BuildSucceeded or AgentEventType.TaskCompleted
                => "agent_success",
            AgentEventType.BuildFailed or AgentEventType.TaskFailed
                => "agent_error",
            AgentEventType.BuildStarted => "agent_build",
            AgentEventType.TestRunCompleted => "agent_test",
            AgentEventType.GitStatusChanged => "agent_git",
            _ => "agent"
        };
    }

    private static string GetDefaultTitle(AgentEventType type)
    {
        return type switch
        {
            AgentEventType.TaskStarted => "任务开始",
            AgentEventType.TaskCompleted => "任务完成",
            AgentEventType.TaskFailed => "任务失败",
            AgentEventType.BuildStarted => "构建开始",
            AgentEventType.BuildSucceeded => "构建成功",
            AgentEventType.BuildFailed => "构建失败",
            AgentEventType.TestRunCompleted => "测试完成",
            AgentEventType.GitStatusChanged => "Git 状态",
            _ => "Agent 通知"
        };
    }
}

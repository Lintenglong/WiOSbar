using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfColor = System.Windows.Media.Color;
using WpfHorizontalAlignment = System.Windows.HorizontalAlignment;
using WpfOrientation = System.Windows.Controls.Orientation;
using WpfProgressBar = System.Windows.Controls.ProgressBar;

namespace FluidBar;

/// <summary>
/// 悬停卡片内容提供者 - 为不同事件类型提供丰富的悬停内容
/// </summary>
public static class HoverCardContentProvider
{
    /// <summary>
    /// 为媒体事件创建增强悬停内容
    /// </summary>
    public static FrameworkElement CreateMediaHoverContent(IslandViewPresentation view, FluidBarSettings settings)
    {
        var panel = new StackPanel
        {
            Orientation = WpfOrientation.Vertical,
            Margin = new Thickness(16)
        };

        // 歌曲信息
        if (!string.IsNullOrWhiteSpace(view.Title))
        {
            panel.Children.Add(new TextBlock
            {
                Text = view.Title,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = WpfBrushes.White,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 350
            });
        }

        if (!string.IsNullOrWhiteSpace(view.Content))
        {
            panel.Children.Add(new TextBlock
            {
                Text = view.Content,
                FontSize = 12,
                Foreground = WpfBrushes.LightGray,
                Margin = new Thickness(0, 4, 0, 0),
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 350
            });
        }

        // 进度条（如果有）
        if (view.ProgressPercent >= 0)
        {
            var progressPanel = new StackPanel
            {
                Orientation = WpfOrientation.Horizontal,
                Margin = new Thickness(0, 12, 0, 0)
            };

            var progressBar = new WpfProgressBar
            {
                Width = 300,
                Height = 6,
                Value = view.ProgressPercent,
                Background = new SolidColorBrush(WpfColor.FromArgb(60, 255, 255, 255)),
                Foreground = new SolidColorBrush(WpfColor.FromRgb(10, 132, 255))
            };

            progressPanel.Children.Add(progressBar);
            panel.Children.Add(progressPanel);
        }

        // 歌词（如果有）
        if (!string.IsNullOrWhiteSpace(view.LyricLine))
        {
            var lyricBorder = new Border
            {
                Background = new SolidColorBrush(WpfColor.FromArgb(40, 255, 255, 255)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 12, 0, 0)
            };

            var lyricStack = new StackPanel();

            lyricStack.Children.Add(new TextBlock
            {
                Text = "当前歌词",
                FontSize = 10,
                Foreground = WpfBrushes.Gray,
                Margin = new Thickness(0, 0, 0, 4)
            });

            lyricStack.Children.Add(new TextBlock
            {
                Text = view.LyricLine,
                FontSize = 13,
                Foreground = WpfBrushes.White,
                FontWeight = FontWeights.Medium,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 320
            });

            if (!string.IsNullOrWhiteSpace(view.SecondaryLyricLine))
            {
                lyricStack.Children.Add(new TextBlock
                {
                    Text = view.SecondaryLyricLine,
                    FontSize = 11,
                    Foreground = WpfBrushes.LightGray,
                    Margin = new Thickness(0, 4, 0, 0),
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 320
                });
            }

            lyricBorder.Child = lyricStack;
            panel.Children.Add(lyricBorder);
        }

        // 控制提示
        var hintText = new TextBlock
        {
            Text = "按住 Ctrl+Alt 隐藏灵动岛",
            FontSize = 10,
            Foreground = WpfBrushes.Gray,
            Margin = new Thickness(0, 12, 0, 0),
            HorizontalAlignment = WpfHorizontalAlignment.Center
        };
        panel.Children.Add(hintText);

        return panel;
    }

    /// <summary>
    /// 为通知事件创建增强悬停内容
    /// </summary>
    public static FrameworkElement CreateNotificationHoverContent(IslandViewPresentation view)
    {
        var panel = new StackPanel
        {
            Orientation = WpfOrientation.Vertical,
            Margin = new Thickness(16)
        };

        // 应用名称
        if (!string.IsNullOrWhiteSpace(view.SourceName))
        {
            panel.Children.Add(new TextBlock
            {
                Text = view.SourceName,
                FontSize = 11,
                Foreground = new SolidColorBrush(WpfColor.FromRgb(90, 200, 250)),
                FontWeight = FontWeights.Medium
            });
        }

        // 标题
        if (!string.IsNullOrWhiteSpace(view.Title))
        {
            panel.Children.Add(new TextBlock
            {
                Text = view.Title,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = WpfBrushes.White,
                Margin = new Thickness(0, 4, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 320
            });
        }

        // 内容
        if (!string.IsNullOrWhiteSpace(view.Content))
        {
            panel.Children.Add(new TextBlock
            {
                Text = view.Content,
                FontSize = 12,
                Foreground = WpfBrushes.LightGray,
                Margin = new Thickness(0, 4, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 320
            });
        }

        // 操作提示
        var actionHint = new Border
        {
            Background = new SolidColorBrush(WpfColor.FromArgb(30, 255, 255, 255)),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8, 4, 8, 4),
            Margin = new Thickness(0, 12, 0, 0)
        };

        actionHint.Child = new TextBlock
        {
            Text = "点击通知可快速操作",
            FontSize = 10,
            Foreground = WpfBrushes.Gray,
            HorizontalAlignment = WpfHorizontalAlignment.Center
        };

        panel.Children.Add(actionHint);

        return panel;
    }

    /// <summary>
    /// 为系统状态事件创建增强悬停内容
    /// </summary>
    public static FrameworkElement CreateSystemStatusHoverContent(IslandViewPresentation view)
    {
        var panel = new StackPanel
        {
            Orientation = WpfOrientation.Vertical,
            Margin = new Thickness(16)
        };

        // 状态标题
        if (!string.IsNullOrWhiteSpace(view.Title))
        {
            panel.Children.Add(new TextBlock
            {
                Text = view.Title,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = WpfBrushes.White
            });
        }

        // 状态内容
        if (!string.IsNullOrWhiteSpace(view.Content))
        {
            panel.Children.Add(new TextBlock
            {
                Text = view.Content,
                FontSize = 13,
                Foreground = WpfBrushes.LightGray,
                Margin = new Thickness(0, 6, 0, 0)
            });
        }

        // 详细数值（如果有）
        if (view.ProgressPercent >= 0)
        {
            var detailPanel = new Border
            {
                Background = new SolidColorBrush(WpfColor.FromArgb(40, 255, 255, 255)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 12, 0, 0)
            };

            detailPanel.Child = new StackPanel
            {
                Children =
                {
                    new TextBlock
                    {
                        Text = "详细数值",
                        FontSize = 10,
                        Foreground = WpfBrushes.Gray,
                        Margin = new Thickness(0, 0, 0, 4)
                    },
                    new TextBlock
                    {
                        Text = $"{view.ProgressPercent:F1}%",
                        FontSize = 20,
                        FontWeight = FontWeights.Bold,
                        Foreground = WpfBrushes.White
                    }
                }
            };

            panel.Children.Add(detailPanel);
        }

        // 历史趋势提示
        var trendHint = new TextBlock
        {
            Text = "💡 历史趋势将在下个版本提供",
            FontSize = 10,
            Foreground = WpfBrushes.Gray,
            Margin = new Thickness(0, 12, 0, 0)
        };
        panel.Children.Add(trendHint);

        return panel;
    }

    /// <summary>
    /// 为剪贴板事件创建增强悬停内容
    /// </summary>
    public static FrameworkElement CreateClipboardHoverContent(IslandViewPresentation view, ClipboardPluginSettings? settings)
    {
        var panel = new StackPanel
        {
            Orientation = WpfOrientation.Vertical,
            Margin = new Thickness(16)
        };

        // 内容预览
        if (!string.IsNullOrWhiteSpace(view.Content))
        {
            var previewBorder = new Border
            {
                Background = new SolidColorBrush(WpfColor.FromArgb(40, 255, 255, 255)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12),
                MaxWidth = 320
            };

            var previewText = new TextBlock
            {
                Text = view.Content.Length > 200
                    ? view.Content.Substring(0, 200) + "..."
                    : view.Content,
                FontSize = 12,
                Foreground = WpfBrushes.White,
                TextWrapping = TextWrapping.Wrap
            };

            previewBorder.Child = previewText;
            panel.Children.Add(previewBorder);
        }

        // 历史提示
        var historyHint = new TextBlock
        {
            Text = "💡 历史记录功能即将推出",
            FontSize = 10,
            Foreground = WpfBrushes.Gray,
            Margin = new Thickness(0, 12, 0, 0)
        };
        panel.Children.Add(historyHint);

        return panel;
    }

    /// <summary>
    /// 根据事件类型创建对应的悬停内容
    /// </summary>
    public static FrameworkElement? CreateHoverContent(
        IslandViewPresentation view,
        FluidBarSettings settings,
        ClipboardPluginSettings? clipboardSettings = null)
    {
        return view.Kind switch
        {
            IslandViewKind.Media => CreateMediaHoverContent(view, settings),
            IslandViewKind.Notification => CreateNotificationHoverContent(view),
            IslandViewKind.Progress or IslandViewKind.Status => CreateSystemStatusHoverContent(view),
            _ when view.SourceName == "clipboard" => CreateClipboardHoverContent(view, clipboardSettings),
            _ => null
        };
    }
}

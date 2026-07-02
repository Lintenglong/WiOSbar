using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FluidBar;

/// <summary>
/// 鎮仠鍗＄墖鍐呭鎻愪緵鑰?- 涓轰笉鍚屼簨浠剁被鍨嬫彁渚涗赴瀵岀殑鎮仠鍐呭
/// </summary>
public static class HoverCardContentProvider
{
    /// <summary>
    /// 涓哄獟浣撲簨浠跺垱寤哄寮烘偓鍋滃唴瀹?    /// </summary>
    public static FrameworkElement CreateMediaHoverContent(IslandViewPresentation view, FluidBarSettings settings)
    {
        var panel = new StackPanel
        {
            Orientation = System.Windows.Controls.System.Windows.Controls.Orientation.Vertical,
            Margin = new Thickness(16)
        };

        // 姝屾洸淇℃伅
        if (!string.IsNullOrWhiteSpace(view.Title))
        {
            panel.Children.Add(new TextBlock
            {
                Text = view.Title,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = System.Windows.Media.Brushes.White,
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
                Foreground = System.Windows.Media.Brushes.LightGray,
                Margin = new Thickness(0, 4, 0, 0),
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 350
            });
        }

        // 杩涘害鏉★紙濡傛灉鏈夛級
        if (view.Payload?.ProgressPercent.HasValue == true)
        {
            var progressPanel = new StackPanel
            {
                Orientation = System.Windows.Controls.System.Windows.Controls.Orientation.Horizontal,
                Margin = new Thickness(0, 12, 0, 0)
            };

            var progressBar = new ProgressBar
            {
                Width = 300,
                Height = 6,
                Value = view.Payload.ProgressPercent.Value,
                Background = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                Foreground = new SolidColorBrush(Color.FromRgb(10, 132, 255))
            };

            progressPanel.Children.Add(progressBar);
            panel.Children.Add(progressPanel);
        }

        // 姝岃瘝锛堝鏋滄湁锛?        if (!string.IsNullOrWhiteSpace(view.LyricLine))
        {
            var lyricBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 12, 0, 0)
            };

            var lyricStack = new StackPanel();

            lyricStack.Children.Add(new TextBlock
            {
                Text = "褰撳墠姝岃瘝",
                FontSize = 10,
                Foreground = System.Windows.Media.Brushes.Gray,
                Margin = new Thickness(0, 0, 0, 4)
            });

            lyricStack.Children.Add(new TextBlock
            {
                Text = view.LyricLine,
                FontSize = 13,
                Foreground = System.Windows.Media.Brushes.White,
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
                    Foreground = System.Windows.Media.Brushes.LightGray,
                    Margin = new Thickness(0, 4, 0, 0),
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 320
                });
            }

            lyricBorder.Child = lyricStack;
            panel.Children.Add(lyricBorder);
        }

        // 鎺у埗鎻愮ず
        var hintText = new TextBlock
        {
            Text = "鎸変綇 Ctrl+Alt 闅愯棌鐏靛姩宀?,
            FontSize = 10,
            Foreground = System.Windows.Media.Brushes.Gray,
            Margin = new Thickness(0, 12, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        panel.Children.Add(hintText);

        return panel;
    }

    /// <summary>
    /// 涓洪€氱煡浜嬩欢鍒涘缓澧炲己鎮仠鍐呭
    /// </summary>
    public static FrameworkElement CreateNotificationHoverContent(IslandViewPresentation view)
    {
        var panel = new StackPanel
        {
            Orientation = System.Windows.Controls.System.Windows.Controls.Orientation.Vertical,
            Margin = new Thickness(16)
        };

        // 搴旂敤鍚嶇О
        if (!string.IsNullOrWhiteSpace(view.SourceName))
        {
            panel.Children.Add(new TextBlock
            {
                Text = view.SourceName,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(90, 200, 250)),
                FontWeight = FontWeights.Medium
            });
        }

        // 鏍囬
        if (!string.IsNullOrWhiteSpace(view.Title))
        {
            panel.Children.Add(new TextBlock
            {
                Text = view.Title,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = System.Windows.Media.Brushes.White,
                Margin = new Thickness(0, 4, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 320
            });
        }

        // 鍐呭
        if (!string.IsNullOrWhiteSpace(view.Content))
        {
            panel.Children.Add(new TextBlock
            {
                Text = view.Content,
                FontSize = 12,
                Foreground = System.Windows.Media.Brushes.LightGray,
                Margin = new Thickness(0, 4, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 320
            });
        }

        // 鎿嶄綔鎻愮ず
        var actionHint = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8, 4, 8, 4),
            Margin = new Thickness(0, 12, 0, 0)
        };

        actionHint.Child = new TextBlock
        {
            Text = "鐐瑰嚮閫氱煡鍙揩閫熸搷浣?,
            FontSize = 10,
            Foreground = System.Windows.Media.Brushes.Gray,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        panel.Children.Add(actionHint);

        return panel;
    }

    /// <summary>
    /// 涓虹郴缁熺姸鎬佷簨浠跺垱寤哄寮烘偓鍋滃唴瀹?    /// </summary>
    public static FrameworkElement CreateSystemStatusHoverContent(IslandViewPresentation view)
    {
        var panel = new StackPanel
        {
            Orientation = System.Windows.Controls.System.Windows.Controls.Orientation.Vertical,
            Margin = new Thickness(16)
        };

        // 鐘舵€佹爣棰?        if (!string.IsNullOrWhiteSpace(view.Title))
        {
            panel.Children.Add(new TextBlock
            {
                Text = view.Title,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = System.Windows.Media.Brushes.White
            });
        }

        // 鐘舵€佸唴瀹?        if (!string.IsNullOrWhiteSpace(view.Content))
        {
            panel.Children.Add(new TextBlock
            {
                Text = view.Content,
                FontSize = 13,
                Foreground = System.Windows.Media.Brushes.LightGray,
                Margin = new Thickness(0, 6, 0, 0)
            });
        }

        // 璇︾粏鏁板€硷紙濡傛灉鏈夛級
        if (view.Payload?.ProgressPercent.HasValue == true)
        {
            var detailPanel = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
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
                        Text = "璇︾粏鏁板€?,
                        FontSize = 10,
                        Foreground = System.Windows.Media.Brushes.Gray,
                        Margin = new Thickness(0, 0, 0, 4)
                    },
                    new TextBlock
                    {
                        Text = $"{view.Payload.ProgressPercent.Value:F1}%",
                        FontSize = 20,
                        FontWeight = FontWeights.Bold,
                        Foreground = System.Windows.Media.Brushes.White
                    }
                }
            };

            panel.Children.Add(detailPanel);
        }

        // 鍘嗗彶瓒嬪娍鎻愮ず
        var trendHint = new TextBlock
        {
            Text = "馃挕 鍘嗗彶瓒嬪娍灏嗗湪涓嬩釜鐗堟湰鎻愪緵",
            FontSize = 10,
            Foreground = System.Windows.Media.Brushes.Gray,
            Margin = new Thickness(0, 12, 0, 0)
        };
        panel.Children.Add(trendHint);

        return panel;
    }

    /// <summary>
    /// 涓哄壀璐存澘浜嬩欢鍒涘缓澧炲己鎮仠鍐呭
    /// </summary>
    public static FrameworkElement CreateClipboardHoverContent(IslandViewPresentation view, ClipboardPluginSettings? settings)
    {
        var panel = new StackPanel
        {
            Orientation = System.Windows.Controls.System.Windows.Controls.Orientation.Vertical,
            Margin = new Thickness(16)
        };

        // 鍐呭棰勮
        if (!string.IsNullOrWhiteSpace(view.Content))
        {
            var previewBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
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
                Foreground = System.Windows.Media.Brushes.White,
                TextWrapping = TextWrapping.Wrap
            };

            previewBorder.Child = previewText;
            panel.Children.Add(previewBorder);
        }

        // 鍘嗗彶鎻愮ず
        var historyHint = new TextBlock
        {
            Text = "馃挕 鍘嗗彶璁板綍鍔熻兘鍗冲皢鎺ㄥ嚭",
            FontSize = 10,
            Foreground = System.Windows.Media.Brushes.Gray,
            Margin = new Thickness(0, 12, 0, 0)
        };
        panel.Children.Add(historyHint);

        return panel;
    }

    /// <summary>
    /// 鏍规嵁浜嬩欢绫诲瀷鍒涘缓瀵瑰簲鐨勬偓鍋滃唴瀹?    /// </summary>
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
            _ when view.Source == "clipboard" => CreateClipboardHoverContent(view, clipboardSettings),
            _ => null
        };
    }
}



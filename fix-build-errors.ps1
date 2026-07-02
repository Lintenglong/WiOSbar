# Fix all GitHub Actions build errors
Write-Host "Fixing compilation errors for GitHub Actions..." -ForegroundColor Cyan

$root = "E:\codexproject\FluidBar-main\FluidBar-main"

# 1. Create LrcLine.cs
$lrcContent = @"
namespace FluidBar;

/// <summary>
/// LRC 歌词行
/// </summary>
public sealed record LrcLine(TimeSpan Time, string Text);
"@
Set-Content "$root\Plugins\Media\LrcLine.cs" $lrcContent -Encoding UTF8
Write-Host "✓ Created LrcLine.cs" -ForegroundColor Green

# 2. Fix FocusModeManager.cs
$focus = Get-Content "$root\FocusModeManager.cs" -Raw
if ($focus -notmatch "using System.Windows.Threading;") {
    $focus = $focus -replace "using System.Runtime.InteropServices;", "using System.Runtime.InteropServices;`nusing System.Windows.Threading;"
    Set-Content "$root\FocusModeManager.cs" $focus -Encoding UTF8
    Write-Host "✓ Fixed FocusModeManager.cs" -ForegroundColor Green
}

# 3. Fix PerformanceMonitor.cs
$perf = Get-Content "$root\PerformanceMonitor.cs" -Raw
if ($perf -notmatch "using System.Windows.Threading;") {
    $perf = $perf -replace "using System.Diagnostics;", "using System.Diagnostics;`nusing System.Windows.Threading;"
    Set-Content "$root\PerformanceMonitor.cs" $perf -Encoding UTF8
    Write-Host "✓ Fixed PerformanceMonitor.cs" -ForegroundColor Green
}

# 4. Fix ThemeManager.cs - fully qualified Color
$theme = Get-Content "$root\ThemeManager.cs" -Raw
$theme = $theme -replace "public static Color GetPreviewColor", "public static System.Windows.Media.Color GetPreviewColor"
$theme = $theme -replace "return Color\.FromArgb", "return System.Windows.Media.Color.FromArgb"
$theme = $theme -replace "return Color\.FromRgb", "return System.Windows.Media.Color.FromRgb"
Set-Content "$root\ThemeManager.cs" $theme -Encoding UTF8
Write-Host "✓ Fixed ThemeManager.cs" -ForegroundColor Green

# 5. Fix WeatherMonitor.cs - add IO using and fix Apply
$weather = Get-Content "$root\Monitors\WeatherMonitor.cs" -Raw
if ($weather -notmatch "using System\.IO;") {
    $weather = $weather -replace "using System\.Net;", "using System.IO;`nusing System.Net;"
}
# Replace .Apply pattern with proper initialization
$weather = $weather -replace "_ = new DispatcherTimer\s*\{\s*Interval = TimeSpan\.FromSeconds\(1\)\s*\}\.Apply\(t =>\s*\{", "= new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };`n_timer.Tick += (_, _) => {"
$weather = $weather -replace "t\.Tick \+= \(\_, _\) =>\s*\{", ""
$weather = $weather -replace "t\.Start\(\);\s*\}\);", "_timer.Start();"
Set-Content "$root\Monitors\WeatherMonitor.cs" $weather -Encoding UTF8
Write-Host "✓ Fixed WeatherMonitor.cs" -ForegroundColor Green

# 6. Fix VpnMonitor.cs
$vpn = Get-Content "$root\Monitors\VpnMonitor.cs" -Raw
if ($vpn -notmatch "using System\.Windows\.Threading;") {
    $vpn = $vpn -replace "using System\.Management;", "using System.Management;`nusing System.Windows.Threading;"
}
$vpn = $vpn -replace "_ = new DispatcherTimer\s*\{\s*Interval = TimeSpan\.FromSeconds\(3\)\s*\}\.Apply\(t =>\s*\{", "= new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };`n_timer.Tick += (_, _) => {"
$vpn = $vpn -replace "t\.Tick \+= \(\_, _\) =>\s*\{", ""
$vpn = $vpn -replace "t\.Start\(\);\s*\}\);", "_timer.Start();"
Set-Content "$root\Monitors\VpnMonitor.cs" $vpn -Encoding UTF8
Write-Host "✓ Fixed VpnMonitor.cs" -ForegroundColor Green

# 7. Fix SystemTemperatureMonitor.cs
$temp = Get-Content "$root\Monitors\SystemTemperatureMonitor.cs" -Raw
if ($temp -notmatch "using System\.Windows\.Threading;") {
    $temp = $temp -replace "using System\.Management;", "using System.Management;`nusing System.Windows.Threading;"
}
$temp = $temp -replace "_ = new DispatcherTimer\s*\{\s*Interval = TimeSpan\.FromSeconds\(2\)\s*\}\.Apply\(t =>\s*\{", "= new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };`n_timer.Tick += (_, _) => {"
$temp = $temp -replace "t\.Tick \+= \(\_, _\) =>\s*\{", ""
$temp = $temp -replace "t\.Start\(\);\s*\}\);", "_timer.Start();"
Set-Content "$root\Monitors\SystemTemperatureMonitor.cs" $temp -Encoding UTF8
Write-Host "✓ Fixed SystemTemperatureMonitor.cs" -ForegroundColor Green

# 8. Fix MainWindow.xaml.cs - AnimateCollapse -> Collapse
$main = Get-Content "$root\MainWindow.xaml.cs" -Raw
$main = $main -replace "AnimateCollapse\(\)", "Collapse()"
Set-Content "$root\MainWindow.xaml.cs" $main -Encoding UTF8
Write-Host "✓ Fixed MainWindow.xaml.cs" -ForegroundColor Green

# 9. Fix HoverCardContentProvider.cs - Orientation
$hover = Get-Content "$root\HoverCardContentProvider.cs" -Raw
$hover = $hover -replace "Orientation\.Vertical", "System.Windows.Controls.Orientation.Vertical"
$hover = $hover -replace "Orientation\.Horizontal", "System.Windows.Controls.Orientation.Horizontal"
Set-Content "$root\HoverCardContentProvider.cs" $hover -Encoding UTF8
Write-Host "✓ Fixed HoverCardContentProvider.cs" -ForegroundColor Green

Write-Host "`nAll fixes applied!" -ForegroundColor Green
Write-Host "Run: git add -A; git commit -m 'fix: resolve compilation errors'; git push" -ForegroundColor Yellow

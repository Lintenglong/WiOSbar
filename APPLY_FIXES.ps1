# Run this in PowerShell to fix all errors
cd "E:\codexproject\FluidBar-main\FluidBar-main"

# Fix 1: LrcLine.cs (create if missing)
@"
namespace FluidBar;
public sealed record LrcLine(TimeSpan Time, string Text);
"@ | Set-Content "Plugins\Media\LrcLine.cs" -Encoding UTF8

# Fix 2: ThemeManager.cs - already partially fixed by user, ensure Color is qualified
(Get-Content "ThemeManager.cs" -Raw) -replace "public static Color GetPreviewColor","public static System.Windows.Media.Color GetPreviewColor" | Set-Content "ThemeManager.cs" -Encoding UTF8

# Fix 3: ClipboardItem.cs - add using System.IO
$clip = Get-Content "Plugins\Clipboard\ClipboardItem.cs" -Raw
if ($clip -notmatch "using System\.IO;") {
    $clip = $clip -replace "^(using .+?;)$", "`$1`nusing System.IO;" -replace "(?m)^", ""
    Set-Content "Plugins\Clipboard\ClipboardItem.cs" $clip -Encoding UTF8
}

# Fix 4: WeatherMonitor.cs - add using System.IO and fix Apply
$weather = Get-Content "Monitors\WeatherMonitor.cs" -Raw
if ($weather -notmatch "using System\.IO;") {
    $weather = $weather -replace "using System\.Net;", "using System.IO;`nusing System.Net;"
}
# Replace all .Apply patterns
$weather = $weather -replace "_ = new DispatcherTimer\s*\{\s*Interval = TimeSpan\.FromSeconds\((\d+)\)\s*\}\s*\.Apply\(t => \{", "= new DispatcherTimer { Interval = TimeSpan.FromSeconds(`$1) };`n_timer.Tick += (_, _) => {"
$weather = $weather -replace "t\.Tick \+= \(\_, _\) => \{", ""
$weather = $weather -replace "t\.Start\(\);\s*\}\);", "_timer.Start();"
Set-Content "Monitors\WeatherMonitor.cs" $weather -Encoding UTF8

# Fix 5: VpnMonitor.cs - add using and fix Apply
$vpn = Get-Content "Monitors\VpnMonitor.cs" -Raw
if ($vpn -notmatch "using System\.Windows\.Threading;") {
    $vpn = $vpn -replace "using System\.Management;", "using System.Management;`nusing System.Windows.Threading;"
}
$vpn = $vpn -replace "_ = new DispatcherTimer\s*\{\s*Interval = TimeSpan\.FromSeconds\((\d+)\)\s*\}\s*\.Apply\(t => \{", "= new DispatcherTimer { Interval = TimeSpan.FromSeconds(`$1) };`n_timer.Tick += (_, _) => {"
$vpn = $vpn -replace "t\.Tick \+= \(\_, _\) => \{", ""
$vpn = $vpn -replace "t\.Start\(\);\s*\}\);", "_timer.Start();"
Set-Content "Monitors\VpnMonitor.cs" $vpn -Encoding UTF8

# Fix 6: SystemTemperatureMonitor.cs - add using and fix Apply
$temp = Get-Content "Monitors\SystemTemperatureMonitor.cs" -Raw
if ($temp -notmatch "using System\.Windows\.Threading;") {
    $temp = $temp -replace "using System\.Management;", "using System.Management;`nusing System.Windows.Threading;"
}
$temp = $temp -replace "_ = new DispatcherTimer\s*\{\s*Interval = TimeSpan\.FromSeconds\((\d+)\)\s*\}\s*\.Apply\(t => \{", "= new DispatcherTimer { Interval = TimeSpan.FromSeconds(`$1) };`n_timer.Tick += (_, _) => {"
$temp = $temp -replace "t\.Tick \+= \(\_, _\) => \{", ""
$temp = $temp -replace "t\.Start\(\);\s*\}\);", "_timer.Start();"
Set-Content "Monitors\SystemTemperatureMonitor.cs" $temp -Encoding UTF8

# Fix 7: MainWindow.xaml.cs - AnimateCollapse
(Get-Content "MainWindow.xaml.cs" -Raw) -replace "AnimateCollapse\(\)", "Collapse()" | Set-Content "MainWindow.xaml.cs" -Encoding UTF8

# Fix 8: HoverCardContentProvider.cs - Orientation and Brushes
$hover = Get-Content "HoverCardContentProvider.cs" -Raw
$hover = $hover -replace "Orientation\.Vertical", "System.Windows.Controls.Orientation.Vertical"
$hover = $hover -replace "Orientation\.Horizontal", "System.Windows.Controls.Orientation.Horizontal"
$hover = $hover -replace "Brushes\.", "System.Windows.Media.Brushes."
Set-Content "HoverCardContentProvider.cs" $hover -Encoding UTF8

Write-Host "All fixes applied. Now run:" -ForegroundColor Green
Write-Host "git add -A" -ForegroundColor Yellow
Write-Host "git commit -m 'fix: resolve all GitHub Actions compilation errors'" -ForegroundColor Yellow
Write-Host "git push" -ForegroundColor Yellow

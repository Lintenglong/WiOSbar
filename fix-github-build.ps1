# Complete fix for GitHub Actions compilation
Write-Host "Fixing all compilation errors..." -ForegroundColor Cyan

$root = "E:\codexproject\FluidBar-main\FluidBar-main"

# 1. Create LrcLine.cs
@"
namespace FluidBar;
public sealed record LrcLine(TimeSpan Time, string Text);
"@ | Set-Content "$root\Plugins\Media\LrcLine.cs" -Encoding UTF8

# 2. Fix all files with DispatcherTimer - replace .Apply pattern
$files = @(
    "$root\FocusModeManager.cs",
    "$root\PerformanceMonitor.cs",
    "$root\Monitors\WeatherMonitor.cs",
    "$root\Monitors\VpnMonitor.cs",
    "$root\Monitors\SystemTemperatureMonitor.cs"
)

foreach ($file in $files) {
    if (Test-Path $file) {
        $content = Get-Content $file -Raw

        # Add using if missing
        if ($content -notmatch "using System\.Windows\.Threading;") {
            $content = $content -replace "^(using .+?;)$", "`$1`nusing System.Windows.Threading;" -Replace "(?m)^", ""
            # Simpler approach
            $lines = Get-Content $file
            $newLines = @()
            $added = $false
            foreach ($line in $lines) {
                $newLines += $line
                if ($line -match "^using " -and -not $added) {
                    if ($line -match "System\.Windows\.Threading") { $added = $true }
                }
            }
            if (-not $added -and $lines -match "DispatcherTimer") {
                $content = $content -replace "(using System\.[^;]+?;)", "`$1`nusing System.Windows.Threading;"
            }
        }

        # Replace .Apply pattern with direct assignment
        $content = $content -replace "_ = new DispatcherTimer\s*\{\s*Interval = ([^}]+)\}\s*\.Apply\(t => \{", "= new DispatcherTimer { Interval = `$1 };`n_timer.Tick += (_, _) => {"
        $content = $content -replace "t\.Tick \+= \(\_, _\) => \{", ""
        $content = $content -replace "t\.Start\(\);\s*\}\);", "_timer.Start();"

        Set-Content $file $content -Encoding UTF8
        Write-Host "Fixed: $file" -ForegroundColor Green
    }
}

# 3. Fix ThemeManager.cs Color
$theme = Get-Content "$root\ThemeManager.cs" -Raw
$theme = $theme -replace "public static Color GetPreviewColor", "public static System.Windows.Media.Color GetPreviewColor"
$theme = $theme -replace "return Color\.FromArgb", "return System.Windows.Media.Color.FromArgb"
$theme = $theme -replace "return Color\.FromRgb", "return System.Windows.Media.Color.FromRgb"
Set-Content "$root\ThemeManager.cs" $theme -Encoding UTF8

# 4. Fix HoverCardContentProvider Orientation
$hover = Get-Content "$root\HoverCardContentProvider.cs" -Raw
$hover = $hover -replace "Orientation\.Vertical", "System.Windows.Controls.Orientation.Vertical"
$hover = $hover -replace "Orientation\.Horizontal", "System.Windows.Controls.Orientation.Horizontal"
Set-Content "$root\HoverCardContentProvider.cs" $hover -Encoding UTF8

# 5. Fix MainWindow AnimateCollapse
$main = Get-Content "$root\MainWindow.xaml.cs" -Raw
$main = $main -replace "AnimateCollapse\(\)", "Collapse()"
Set-Content "$root\MainWindow.xaml.cs" $main -Encoding UTF8

Write-Host "`nAll fixes complete!" -ForegroundColor Green
Write-Host "Run these commands:" -ForegroundColor Yellow
Write-Host "  git add -A" -ForegroundColor White
Write-Host "  git commit -m 'fix: resolve all compilation errors'" -ForegroundColor White
Write-Host "  git push" -ForegroundColor White

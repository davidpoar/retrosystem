Add-Type -AssemblyName System.Drawing

function Create-IconAtSize {
    param([int]$size, [string]$outPath)

    $bmp = New-Object System.Drawing.Bitmap $size, $size
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit

    $bg = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 12, 12, 24))
    $g.FillRectangle($bg, 0, 0, $size, $size)

    $borderWidth = [int]($size * 0.05)
    $screenRect = New-Object System.Drawing.RectangleF($borderWidth, $borderWidth, ($size - 2 * $borderWidth), ($size - 2 * $borderWidth))
    $screenBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::Black)
    $g.FillRectangle($screenBrush, $screenRect)

    $rowCount = 8
    $rowH = ($size - 2 * $borderWidth) / $rowCount
    $colors = @(
        [System.Drawing.Color]::FromArgb(255, 0, 0, 0),
        [System.Drawing.Color]::FromArgb(255, 200, 0, 0),
        [System.Drawing.Color]::FromArgb(255, 0, 0, 200),
        [System.Drawing.Color]::FromArgb(255, 200, 0, 200),
        [System.Drawing.Color]::FromArgb(255, 0, 200, 0),
        [System.Drawing.Color]::FromArgb(255, 200, 200, 0),
        [System.Drawing.Color]::FromArgb(255, 0, 200, 200),
        [System.Drawing.Color]::FromArgb(255, 200, 200, 200)
    )
    for ($i = 0; $i -lt $rowCount; $i++) {
        $brush = New-Object System.Drawing.SolidBrush ($colors[$i])
        $y = $borderWidth + $i * $rowH
        $g.FillRectangle($brush, $borderWidth, $y, ($size - 2 * $borderWidth), $rowH)
    }

    $pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 0, 200, 0), [int]($size * 0.012))
    $g.DrawRectangle($pen, $borderWidth, $borderWidth, ($size - 2 * $borderWidth), ($size - 2 * $borderWidth))

    $bmp.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose()
    $bmp.Dispose()
}

function Create-AdaptiveIcon {
    param([int]$size, [string]$outPath)

    $bmp = New-Object System.Drawing.Bitmap $size, $size
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit

    $bg = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 12, 12, 24))
    $g.FillRectangle($bg, 0, 0, $size, $size)

    $center = [int]($size / 2)
    $radius = [int]($size * 0.35)

    $rowCount = 8
    $rowH = ($radius * 2) / $rowCount
    $colors = @(
        [System.Drawing.Color]::FromArgb(255, 0, 0, 0),
        [System.Drawing.Color]::FromArgb(255, 200, 0, 0),
        [System.Drawing.Color]::FromArgb(255, 0, 0, 200),
        [System.Drawing.Color]::FromArgb(255, 200, 0, 200),
        [System.Drawing.Color]::FromArgb(255, 0, 200, 0),
        [System.Drawing.Color]::FromArgb(255, 200, 200, 0),
        [System.Drawing.Color]::FromArgb(255, 0, 200, 200),
        [System.Drawing.Color]::FromArgb(255, 200, 200, 200)
    )
    for ($i = 0; $i -lt $rowCount; $i++) {
        $brush = New-Object System.Drawing.SolidBrush ($colors[$i])
        $y = ($center - $radius) + $i * $rowH
        $g.FillRectangle($brush, ($center - $radius), $y, ($radius * 2), $rowH)
    }

    $pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 0, 200, 0), [int]($size * 0.015))
    $g.DrawRectangle($pen, ($center - $radius), ($center - $radius), ($radius * 2), ($radius * 2))

    $bmp.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose()
    $bmp.Dispose()
}

$drawableDir = Join-Path $PSScriptRoot "Resources/Drawable"
$resourcesDir = Join-Path $PSScriptRoot "Resources"

if (-not (Test-Path $drawableDir)) { New-Item -ItemType Directory -Path $drawableDir -Force | Out-Null }

Create-IconAtSize -size 192 -outPath (Join-Path $drawableDir "Icon.png")

$mipmapConfigs = @{
    "mipmap-mdpi" = 48
    "mipmap-hdpi" = 72
    "mipmap-xhdpi" = 96
    "mipmap-xxhdpi" = 144
    "mipmap-xxxhdpi" = 192
}

foreach ($name in $mipmapConfigs.Keys) {
    $dir = Join-Path $resourcesDir $name
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
    Create-IconAtSize -size $mipmapConfigs[$name] -outPath (Join-Path $dir "ic_launcher.png")
}

$anydpiV26Dir = Join-Path $resourcesDir "mipmap-anydpi-v26"
if (-not (Test-Path $anydpiV26Dir)) { New-Item -ItemType Directory -Path $anydpiV26Dir -Force | Out-Null }
Create-AdaptiveIcon -size 432 -outPath (Join-Path $anydpiV26Dir "ic_launcher_foreground.png")

$bgColor = New-Object System.Drawing.Bitmap 1, 1
$bgColor.SetPixel(0, 0, [System.Drawing.Color]::FromArgb(255, 12, 12, 24))
$bgColor.Save((Join-Path $drawableDir "ic_launcher_background.png"), [System.Drawing.Imaging.ImageFormat]::Png)
$bgColor.Dispose()

Write-Host "Iconos generados:"
Get-ChildItem -Recurse $resourcesDir -Filter "*.png" | ForEach-Object { Write-Host "  $($_.FullName.Substring($PSScriptRoot.Length))" }

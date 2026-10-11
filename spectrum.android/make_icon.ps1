Add-Type -AssemblyName System.Drawing
$size = 192
$bmp = New-Object System.Drawing.Bitmap $size,$size
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit

$bg = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255,15,15,25))
$g.FillRectangle($bg, 0, 0, $size, $size)

$border = 8
$screenW = $size - 2 * $border
$screenH = $screenW
$screenRect = New-Object System.Drawing.RectangleF($border, $border, $screenW, $screenH)
$screenBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::Black)
$g.FillRectangle($screenBrush, $screenRect)

$rowCount = 8
$rowH = $screenH / $rowCount
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
    $y = $border + $i * $rowH
    $g.FillRectangle($brush, $border, $y, $screenW, $rowH)
}

$pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 0, 200, 0), 2)
$g.DrawRectangle($pen, $border, $border, $screenW, $screenH)

$font = New-Object System.Drawing.Font("Consolas", 14, [System.Drawing.FontStyle]::Bold)
$textBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 0, 200, 0))
$shadowBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::Black)
$g.FillRectangle($shadowBrush, 0, 0, $size, 20)
$g.DrawString("ZX 48K", $font, $textBrush, 70, 1)

$bmp.Save("$PSScriptRoot\Resources\Drawable\Icon.png", [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose()
$bmp.Dispose()
Write-Host "Icon created"

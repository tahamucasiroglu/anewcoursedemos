<#
.SYNOPSIS
  Taha Konsept masaüstü ajanı için geçici uygulama ikonunu (app.ico + app.png) üretir.
.DESCRIPTION
  Gerçek ikon source/icon.* olarak eklenene kadar kullanılır. Web logosunun (wwwroot/img/logo.svg) aynısını çizer:
  turuncu degrade yuvarlak kare, beyaz "T" ve krem oynat üçgeni. ICO içinde 16/24/32/48/64/128/256 px PNG kareleri bulunur.
  source/icon.ico varsa onu kopyalar ve çizim yapmaz.
#>
param(
    [string]$OutDir = (Join-Path $PSScriptRoot '..\..\src\Desktop\TahaKonsept.Agent\Assets')
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$OutDir = [System.IO.Path]::GetFullPath($OutDir)
New-Item -ItemType Directory -Force $OutDir | Out-Null
$realIcon = Join-Path $PSScriptRoot '..\..\source\icon.ico'
if (Test-Path $realIcon) {
    Copy-Item $realIcon (Join-Path $OutDir 'app.ico') -Force
    Write-Host "Gerçek ikon kopyalandı: $realIcon"
    return
}

function New-LogoBitmap([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)
    $s = $size / 64.0
    $rect = New-Object System.Drawing.RectangleF 0, 0, $size, $size
    $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rect, ([System.Drawing.Color]::FromArgb(242,154,74)), ([System.Drawing.Color]::FromArgb(194,90,28)), 45.0
    $r = 18 * $s; $d = 2 * $r
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.AddArc(0, 0, $d, $d, 180, 90)
    $path.AddArc($size - $d, 0, $d, $d, 270, 90)
    $path.AddArc($size - $d, $size - $d, $d, $d, 0, 90)
    $path.AddArc(0, $size - $d, $d, $d, 90, 90)
    $path.CloseFigure()
    $g.FillPath($brush, $path)
    $pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::White), ([single](6 * $s))
    $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $g.DrawLine($pen, 18 * $s, 20 * $s, 38 * $s, 20 * $s)
    $g.DrawLine($pen, 28 * $s, 20 * $s, 28 * $s, 46 * $s)
    $cream = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255,244,230))
    $pts = [System.Drawing.PointF[]]@(
        (New-Object System.Drawing.PointF (38 * $s), (33 * $s)),
        (New-Object System.Drawing.PointF (49 * $s), (39.5 * $s)),
        (New-Object System.Drawing.PointF (38 * $s), (46 * $s)))
    $g.FillPolygon($cream, $pts)
    $g.FillEllipse($cream, 43.5 * $s, 15.5 * $s, 7 * $s, 7 * $s)
    $g.Dispose()
    return $bmp
}

$sizes = 16, 24, 32, 48, 64, 128, 256
$pngs = @()
foreach ($sz in $sizes) {
    $bmp = New-LogoBitmap $sz
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    if ($sz -eq 256) { $bmp.Save((Join-Path $OutDir 'app.png'), [System.Drawing.Imaging.ImageFormat]::Png) }
    $bmp.Dispose()
    $pngs += , $ms.ToArray()
}

$out = New-Object System.IO.MemoryStream
$w = New-Object System.IO.BinaryWriter $out
$w.Write([UInt16]0); $w.Write([UInt16]1); $w.Write([UInt16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $sz = $sizes[$i]; $b = if ($sz -ge 256) { 0 } else { $sz }
    $w.Write([byte]$b); $w.Write([byte]$b); $w.Write([byte]0); $w.Write([byte]0)
    $w.Write([UInt16]1); $w.Write([UInt16]32)
    $w.Write([UInt32]$pngs[$i].Length); $w.Write([UInt32]$offset)
    $offset += $pngs[$i].Length
}
foreach ($p in $pngs) { $w.Write($p) }
$w.Flush()
[System.IO.File]::WriteAllBytes((Join-Path $OutDir 'app.ico'), $out.ToArray())
Write-Host "İkon üretildi: $(Join-Path $OutDir 'app.ico')"

# GenerateIcon.ps1 - Generates a simple sync folder icon for the app
# Run this script on Windows to create Resources/app.ico

Add-Type -AssemblyName System.Drawing

$sizes = @(16, 32, 48, 64, 128, 256)
$bitmaps = @()

foreach ($size in $sizes) {
    $bmp = New-Object System.Drawing.Bitmap($size, $size)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)

    # Draw blue circle background
    $brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(59, 130, 246))
    $margin = [int]($size * 0.06)
    $g.FillEllipse($brush, $margin, $margin, $size - 2 * $margin, $size - 2 * $margin)

    # Draw white sync arrows
    $pen = New-Object System.Drawing.Pen([System.Drawing.Color]::White, [float]($size * 0.08))
    $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round

    $arcRect = New-Object System.Drawing.Rectangle(
        [int]($size * 0.25), [int]($size * 0.25),
        [int]($size * 0.50), [int]($size * 0.50))
    $g.DrawArc($pen, $arcRect, 200, 160)
    $g.DrawArc($pen, $arcRect, 20, 160)

    $g.Dispose()
    $bitmaps += $bmp
}

# Save as ICO
$icoPath = Join-Path $PSScriptRoot "syncFolder\Resources\app.ico"
$dir = Split-Path $icoPath -Parent
if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }

# Create ICO file manually
$stream = [System.IO.File]::Create($icoPath)
$writer = New-Object System.IO.BinaryWriter($stream)

# ICO header
$writer.Write([UInt16]0)      # Reserved
$writer.Write([UInt16]1)      # Type (1 = ICO)
$writer.Write([UInt16]$bitmaps.Count) # Number of images

$headerSize = 6 + ($bitmaps.Count * 16)
$offset = $headerSize

$pngDatas = @()
foreach ($bmp in $bitmaps) {
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngDatas += , $ms.ToArray()
    $ms.Dispose()
}

# Directory entries
for ($i = 0; $i -lt $bitmaps.Count; $i++) {
    $size = $sizes[$i]
    $writer.Write([byte]$(if ($size -ge 256) { 0 } else { $size })) # Width
    $writer.Write([byte]$(if ($size -ge 256) { 0 } else { $size })) # Height
    $writer.Write([byte]0)     # Color palette
    $writer.Write([byte]0)     # Reserved
    $writer.Write([UInt16]1)   # Color planes
    $writer.Write([UInt16]32)  # Bits per pixel
    $writer.Write([UInt32]$pngDatas[$i].Length) # Size
    $writer.Write([UInt32]$offset) # Offset
    $offset += $pngDatas[$i].Length
}

# Image data
foreach ($pngData in $pngDatas) {
    $writer.Write($pngData)
}

$writer.Close()
$stream.Close()

foreach ($bmp in $bitmaps) { $bmp.Dispose() }

Write-Host "Icon generated at: $icoPath"

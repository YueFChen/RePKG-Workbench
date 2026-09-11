[CmdletBinding()]
param(
    [string]$InputPath = '',
    [string]$OutputPath = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

if ([string]::IsNullOrWhiteSpace($InputPath)) {
    $InputPath = Join-Path $PSScriptRoot '..\src\RePKG.App\Resources\icon.ico'
}
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $PSScriptRoot '..\src\RePKG.App\Resources\icon.ico'
}

$inputFullPath = [IO.Path]::GetFullPath($InputPath)
$outputFullPath = [IO.Path]::GetFullPath($OutputPath)
$sizes = @(16, 20, 24, 32, 40, 48, 64, 128, 256)
$images = [Collections.Generic.List[byte[]]]::new()

$sourceIcon = [Drawing.Icon]::new($inputFullPath)
$sourceBitmap = $sourceIcon.ToBitmap()
try {
    foreach ($size in $sizes) {
        $bitmap = [Drawing.Bitmap]::new($size, $size, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
        try {
            $graphics = [Drawing.Graphics]::FromImage($bitmap)
            try {
                $graphics.Clear([Drawing.Color]::Transparent)
                $graphics.CompositingMode = [Drawing.Drawing2D.CompositingMode]::SourceCopy
                $graphics.CompositingQuality = [Drawing.Drawing2D.CompositingQuality]::HighQuality
                $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                $graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::HighQuality
                $graphics.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::HighQuality
                $graphics.DrawImage($sourceBitmap, 0, 0, $size, $size)
            }
            finally {
                $graphics.Dispose()
            }

            $stream = [IO.MemoryStream]::new()
            try {
                $bitmap.Save($stream, [Drawing.Imaging.ImageFormat]::Png)
                $images.Add($stream.ToArray())
            }
            finally {
                $stream.Dispose()
            }
        }
        finally {
            $bitmap.Dispose()
        }
    }
}
finally {
    $sourceBitmap.Dispose()
    $sourceIcon.Dispose()
}

$outputDirectory = [IO.Path]::GetDirectoryName($outputFullPath)
[IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
$temporaryPath = "$outputFullPath.$([Guid]::NewGuid().ToString('N')).tmp"
$fileStream = [IO.File]::Open($temporaryPath, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
$writer = [IO.BinaryWriter]::new($fileStream)
try {
    $writer.Write([UInt16]0)
    $writer.Write([UInt16]1)
    $writer.Write([UInt16]$images.Count)

    $offset = 6 + (16 * $images.Count)
    for ($index = 0; $index -lt $images.Count; $index++) {
        $size = $sizes[$index]
        $writer.Write([byte]$(if ($size -eq 256) { 0 } else { $size }))
        $writer.Write([byte]$(if ($size -eq 256) { 0 } else { $size }))
        $writer.Write([byte]0)
        $writer.Write([byte]0)
        $writer.Write([UInt16]1)
        $writer.Write([UInt16]32)
        $writer.Write([UInt32]$images[$index].Length)
        $writer.Write([UInt32]$offset)
        $offset += $images[$index].Length
    }

    foreach ($image in $images) {
        $writer.Write($image)
    }
}
finally {
    $writer.Dispose()
    $fileStream.Dispose()
}

if ([IO.File]::Exists($outputFullPath)) {
    [IO.File]::Copy($temporaryPath, $outputFullPath, $true)
    [IO.File]::Delete($temporaryPath)
}
else {
    [IO.File]::Move($temporaryPath, $outputFullPath)
}
Write-Host "Generated $($images.Count)-size Windows icon: $outputFullPath"

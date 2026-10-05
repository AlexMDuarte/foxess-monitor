Add-Type -AssemblyName PresentationCore
Add-Type -AssemblyName WindowsBase

$assetDir = Split-Path -Parent $MyInvocation.MyCommand.Path
[xml]$svg = Get-Content -Raw (Join-Path $assetDir 'foxess.svg')
$visual = [System.Windows.Media.DrawingVisual]::new()
$context = $visual.RenderOpen()
$context.PushTransform([System.Windows.Media.ScaleTransform]::new(256 / 24, 256 / 24))

foreach ($path in $svg.svg.g.path) {
    $gradientId = $path.fill -replace '^url\(#', '' -replace '\)$', ''
    $gradient = $svg.svg.defs.linearGradient | Where-Object { $_.id -eq $gradientId }
    $brush = [System.Windows.Media.LinearGradientBrush]::new()
    $brush.MappingMode = [System.Windows.Media.BrushMappingMode]::Absolute
    $brush.StartPoint = [System.Windows.Point]::new([double]$gradient.x1, [double]$gradient.y1)
    $brush.EndPoint = [System.Windows.Point]::new([double]$gradient.x2, [double]$gradient.y2)
    foreach ($stop in $gradient.stop) {
        $color = [System.Windows.Media.ColorConverter]::ConvertFromString($stop.'stop-color')
        $brush.GradientStops.Add([System.Windows.Media.GradientStop]::new($color, [double]$stop.offset))
    }
    $geometry = [System.Windows.Media.Geometry]::Parse($path.d)
    $context.DrawGeometry($brush, $null, $geometry)
}
$context.Pop()
$context.Close()

$bitmap = [System.Windows.Media.Imaging.RenderTargetBitmap]::new(256, 256, 96, 96, [System.Windows.Media.PixelFormats]::Pbgra32)
$bitmap.Render($visual)
$encoder = [System.Windows.Media.Imaging.PngBitmapEncoder]::new()
$encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
$pngPath = Join-Path $assetDir 'foxess.png'
$pngStream = [System.IO.File]::Create($pngPath)
$encoder.Save($pngStream)
$pngStream.Dispose()
$pngBytes = [System.IO.File]::ReadAllBytes($pngPath)

$icoPath = Join-Path $assetDir 'foxess.ico'
$icoStream = [System.IO.File]::Create($icoPath)
$writer = [System.IO.BinaryWriter]::new($icoStream)
$writer.Write([uint16]0)
$writer.Write([uint16]1)
$writer.Write([uint16]1)
$writer.Write([byte]0)
$writer.Write([byte]0)
$writer.Write([byte]0)
$writer.Write([byte]0)
$writer.Write([uint16]1)
$writer.Write([uint16]32)
$writer.Write([uint32]$pngBytes.Length)
$writer.Write([uint32]22)
$writer.Write($pngBytes)
$writer.Dispose()

Write-Output "Generated $pngPath and $icoPath from foxess.svg"

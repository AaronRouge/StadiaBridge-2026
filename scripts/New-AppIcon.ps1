# Original vector artwork; no third-party image or logo is required.
# Geometry matches src/StadiaBridge/Assets/stadia-bridge.svg.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$assetDir = Join-Path (Split-Path -Parent $PSScriptRoot) 'src\StadiaBridge\Assets'
$sizes = @(16,20,24,32,40,48,64,128,256)
$frames = @()
foreach ($size in $sizes) {
    $bitmap = [Drawing.Bitmap]::new($size,$size)
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.Clear([Drawing.Color]::Transparent)
    $graphics.ScaleTransform($size / 256.0, $size / 256.0)
    $cyan = [Drawing.ColorTranslator]::FromHtml('#32dfe7')
    $dark = [Drawing.ColorTranslator]::FromHtml('#17253b')
    $tile = [Drawing.Drawing2D.GraphicsPath]::new()
    $tile.AddArc(6,6,112,112,180,90)
    $tile.AddArc(138,6,112,112,270,90)
    $tile.AddArc(138,138,112,112,0,90)
    $tile.AddArc(6,138,112,112,90,90)
    $tile.CloseFigure()
    $tileBrush = [Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#101827'))
    $edge = [Drawing.Pen]::new($cyan,5)
    $graphics.FillPath($tileBrush,$tile); $graphics.DrawPath($edge,$tile)
    $body = [Drawing.Drawing2D.GraphicsPath]::new()
    $body.AddBezier(73,75,49,75,41,96,34,124)
    $body.AddBezier(34,124,27,151,20,183,36,192)
    $body.AddBezier(36,192,51,202,71,175,85,166)
    $body.AddLine(85,166,171,166)
    $body.AddBezier(171,166,185,175,205,202,220,192)
    $body.AddBezier(220,192,236,183,229,151,222,124)
    $body.AddBezier(222,124,215,96,207,75,183,75)
    $body.CloseFigure()
    $bodyBrush = [Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#e8f0f9'))
    $edge.Width = 4; $edge.LineJoin = [Drawing.Drawing2D.LineJoin]::Round
    $graphics.FillPath($bodyBrush,$body); $graphics.DrawPath($edge,$body)
    $darkBrush = [Drawing.SolidBrush]::new($dark)
    $graphics.FillRectangle($darkBrush,65,96,12,36); $graphics.FillRectangle($darkBrush,53,108,36,12)
    $stickBrush = [Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#2c425f'))
    foreach ($x in @(100,156)) {
        $graphics.FillEllipse($darkBrush,($x-17),127,34,34)
        $graphics.FillEllipse($stickBrush,($x-10),134,20,20)
    }
    $purpleBrush = [Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#8b5cf6'))
    $blueBrush = [Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#25b8d3'))
    $graphics.FillEllipse($purpleBrush,174,88,16,16); $graphics.FillEllipse($purpleBrush,174,120,16,16)
    $graphics.FillEllipse($blueBrush,190,104,16,16); $graphics.FillEllipse($blueBrush,158,104,16,16)
    $graphics.FillRectangle($darkBrush,115,101,26,7)
    $memory = [IO.MemoryStream]::new()
    $bitmap.Save($memory,[Drawing.Imaging.ImageFormat]::Png)
    $frames += ,$memory.ToArray()
    if ($size -eq 256) { $bitmap.Save((Join-Path $assetDir 'stadia-bridge.png'),[Drawing.Imaging.ImageFormat]::Png) }
    foreach ($resource in @($memory,$purpleBrush,$blueBrush,$stickBrush,$darkBrush,$bodyBrush,$body,$tileBrush,$edge,$tile,$graphics,$bitmap)) { $resource.Dispose() }
}
$file = [IO.File]::Create((Join-Path $assetDir 'stadia-bridge.ico'))
$writer = [IO.BinaryWriter]::new($file)
try {
    $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$sizes.Count)
    $offset = 6 + 16 * $sizes.Count
    for ($i=0; $i -lt $sizes.Count; $i++) {
        $dimension = if ($sizes[$i] -eq 256) { 0 } else { $sizes[$i] }
        $writer.Write([byte]$dimension); $writer.Write([byte]$dimension)
        $writer.Write([byte]0); $writer.Write([byte]0)
        $writer.Write([uint16]1); $writer.Write([uint16]32)
        $writer.Write([uint32]$frames[$i].Length); $writer.Write([uint32]$offset)
        $offset += $frames[$i].Length
    }
    foreach ($frame in $frames) { $writer.Write([byte[]]$frame) }
} finally { $writer.Dispose() }
Write-Output 'Icono original generado: 16, 20, 24, 32, 40, 48, 64, 128 y 256 px.'

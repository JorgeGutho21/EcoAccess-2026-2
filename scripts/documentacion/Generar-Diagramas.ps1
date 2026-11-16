<#
.SYNOPSIS
    Dibuja los diagramas de la documentación académica (PNG) a partir de especificaciones de texto en
    docs/entrega/fuentes_md/diagramas/*.txt. Así los diagramas se versionan como texto y se regeneran igual.
.DESCRIPTION
    Dos tipos de especificación (primera línea):
      tipo: secuencia   participantes y mensajes «A -> B: texto» (o «A --> B: texto» para respuestas)
      tipo: capas       filas «Nombre de la capa | caja1; caja2; ...» y flechas «dep: Capa A -> Capa B»
    Salida: docs/entrega/fuentes_md/img/<nombre>.png
#>
param()
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$raiz = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$origen = Join-Path $raiz 'docs\entrega\fuentes_md\diagramas'
$destino = Join-Path $raiz 'docs\entrega\fuentes_md\img'
New-Item -ItemType Directory -Force $destino | Out-Null

$verde = [Drawing.Color]::FromArgb(31, 107, 58); $verdeClaro = [Drawing.Color]::FromArgb(227, 240, 230)
$gris = [Drawing.Color]::FromArgb(90, 100, 95); $texto = [Drawing.Color]::FromArgb(29, 43, 34)
$fuente = New-Object Drawing.Font('Segoe UI', 11); $fuenteN = New-Object Drawing.Font('Segoe UI', 11, [Drawing.FontStyle]::Bold)
$fuenteP = New-Object Drawing.Font('Segoe UI', 9.5)

function Lienzo([int] $ancho, [int] $alto) {
    $bmp = New-Object Drawing.Bitmap($ancho, $alto); $bmp.SetResolution(144, 144)
    $g = [Drawing.Graphics]::FromImage($bmp); $g.SmoothingMode = 'AntiAlias'; $g.TextRenderingHint = 'ClearTypeGridFit'; $g.Clear([Drawing.Color]::White)
    return $bmp, $g
}
function Flecha($g, [Drawing.Pen] $pluma, [float] $x1, [float] $y1, [float] $x2, [float] $y2) {
    $pluma.CustomEndCap = New-Object Drawing.Drawing2D.AdjustableArrowCap(4, 5); $g.DrawLine($pluma, $x1, $y1, $x2, $y2)
}

function Secuencia([string[]] $lineas, [string] $salida) {
    $part = @(); $mensajes = @()
    foreach ($l in $lineas) {
        if ($l -match '^participantes:\s*(.+)$') { $part = $Matches[1].Split(';') | % { $_.Trim() } }
        elseif ($l -match '^(.+?)\s*(-->|->)\s*(.+?):\s*(.+)$') { $mensajes += [pscustomobject]@{ De = $Matches[1].Trim(); A = $Matches[3].Trim(); Resp = $Matches[2] -eq '-->'; Texto = $Matches[4].Trim() } }
    }
    $col = 250; $margen = 30; $ancho = $margen * 2 + $col * $part.Count; $paso = 46; $alto = 120 + $paso * $mensajes.Count + 40
    $bmp, $g = Lienzo $ancho $alto
    $x = @{}; for ($i = 0; $i -lt $part.Count; $i++) { $x[$part[$i]] = $margen + $col * $i + $col / 2 }
    $caja = New-Object Drawing.SolidBrush($verdeClaro); $borde = New-Object Drawing.Pen($verde, 1.6)
    $vida = New-Object Drawing.Pen($gris, 1); $vida.DashStyle = 'Dash'
    $fmt = New-Object Drawing.StringFormat; $fmt.Alignment = 'Center'; $fmt.LineAlignment = 'Center'
    foreach ($p in $part) {
        $r = New-Object Drawing.RectangleF(($x[$p] - $col / 2 + 8), 14, ($col - 16), 58)
        $g.FillRectangle($caja, $r); $g.DrawRectangle($borde, $r.X, $r.Y, $r.Width, $r.Height)
        $g.DrawString($p, $fuenteN, (New-Object Drawing.SolidBrush($texto)), $r, $fmt)
        $g.DrawLine($vida, $x[$p], 72, $x[$p], $alto - 20)
    }
    $y = 110
    foreach ($m in $mensajes) {
        $pl = New-Object Drawing.Pen($(if ($m.Resp) { $gris } else { $verde }), 1.6); if ($m.Resp) { $pl.DashStyle = 'Dash' }
        $x1 = $x[$m.De]; $x2 = $x[$m.A]
        if ($null -eq $x1 -or $null -eq $x2) { throw "Participante desconocido en: $($m.De) -> $($m.A)" }
        if ($x1 -eq $x2) { $g.DrawLines($pl, [Drawing.PointF[]]@((New-Object Drawing.PointF($x1, $y)), (New-Object Drawing.PointF(($x1 + 40), $y)), (New-Object Drawing.PointF(($x1 + 40), ($y + 14))))); Flecha $g $pl ($x1 + 40) ($y + 14) ($x1 + 2) ($y + 14) }
        else { Flecha $g $pl $x1 $y $x2 $y }
        $medida = $g.MeasureString($m.Texto, $fuenteP).Width + 12
        $izq = [Math]::Min($x1, $x2) + 4; $an = [Math]::Max([Math]::Abs($x2 - $x1) - 8, $medida)
        if ($izq + $an -gt $ancho - 6) { $izq = $ancho - 6 - $an }
        $rt = New-Object Drawing.RectangleF($izq, ($y - 30), $an, 28)
        $g.FillRectangle([Drawing.Brushes]::White, $izq + ($an - $medida) / 2, $y - 24, $medida, 20)
        $f2 = New-Object Drawing.StringFormat; $f2.Alignment = "Center"; $f2.LineAlignment = "Far"; $f2.FormatFlags = "NoWrap"
        $g.DrawString($m.Texto, $fuenteP, (New-Object Drawing.SolidBrush($texto)), $rt, $f2)
        $y += $paso
    }
    $bmp.Save($salida, [Drawing.Imaging.ImageFormat]::Png); $g.Dispose(); $bmp.Dispose()
}

function Capas([string[]] $lineas, [string] $salida) {
    $filas = @(); $deps = @()
    foreach ($l in $lineas) {
        if ($l -match '^dep:\s*(.+?)\s*->\s*(.+)$') { $deps += , @($Matches[1].Trim(), $Matches[2].Trim()) }
        elseif ($l -match '^(.+?)\|(.+)$') { $filas += [pscustomobject]@{ Nombre = $Matches[1].Trim(); Cajas = $Matches[2].Split(';') | % { $_.Trim() } | ? { $_ } } }
    }
    $ancho = 1500; $altoFila = 150; $sep = 46; $alto = 30 + $filas.Count * ($altoFila + $sep)
    $bmp, $g = Lienzo $ancho $alto
    $fondo = New-Object Drawing.SolidBrush([Drawing.Color]::FromArgb(246, 250, 247)); $borde = New-Object Drawing.Pen($verde, 1.8)
    $caja = New-Object Drawing.SolidBrush($verdeClaro); $fmt = New-Object Drawing.StringFormat; $fmt.Alignment = 'Center'; $fmt.LineAlignment = 'Center'
    $pos = @{}
    for ($i = 0; $i -lt $filas.Count; $i++) {
        $f = $filas[$i]; $y = 20 + $i * ($altoFila + $sep)
        $g.FillRectangle($fondo, 20, $y, $ancho - 40, $altoFila); $g.DrawRectangle($borde, 20, $y, $ancho - 40, $altoFila)
        $g.DrawString($f.Nombre, $fuenteN, (New-Object Drawing.SolidBrush($verde)), (60 + 70 * [Math]::Max($deps.Count, 1)), ($y + 8))
        $n = $f.Cajas.Count; $izqCajas = 60 + 70 * [Math]::Max($deps.Count, 1); $w = [Math]::Min(250, ($ancho - $izqCajas - 40 - ($n - 1) * 14) / [Math]::Max($n, 1)); $x0 = $izqCajas
        for ($j = 0; $j -lt $n; $j++) {
            $r = New-Object Drawing.RectangleF(($x0 + $j * ($w + 14)), ($y + 44), $w, ($altoFila - 60))
            $g.FillRectangle($caja, $r); $g.DrawRectangle((New-Object Drawing.Pen($verde, 1.2)), $r.X, $r.Y, $r.Width, $r.Height)
            $g.DrawString($f.Cajas[$j], $fuenteP, (New-Object Drawing.SolidBrush($texto)), $r, $fmt)
        }
        $pos[$f.Nombre] = @($y, ($y + $altoFila))
    }
    $pl = New-Object Drawing.Pen($gris, 2.2)
    $k = 0
    foreach ($d in $deps) {
        $a = $pos[$d[0]]; $b = $pos[$d[1]]; if (-not $a -or -not $b) { throw "Capa desconocida: $($d -join ' -> ')" }
        $xx = 60 + 70 * $k; $k++
        if ($a[0] -lt $b[0]) { Flecha $g $pl $xx $a[1] $xx $b[0] } else { Flecha $g $pl $xx $a[0] $xx $b[1] }
    }
    $bmp.Save($salida, [Drawing.Imaging.ImageFormat]::Png); $g.Dispose(); $bmp.Dispose()
}

foreach ($spec in Get-ChildItem $origen -Filter *.txt) {
    $lineas = Get-Content $spec.FullName -Encoding utf8 | ? { $_.Trim() -and -not $_.StartsWith('#') }
    $salida = Join-Path $destino ($spec.BaseName + '.png')
    switch -Regex ($lineas[0]) { 'secuencia' { Secuencia $lineas[1..($lineas.Count - 1)] $salida } 'capas' { Capas $lineas[1..($lineas.Count - 1)] $salida } }
    Write-Host "diagrama: $($spec.BaseName)"
}

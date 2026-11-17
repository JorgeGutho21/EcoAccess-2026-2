<#
.SYNOPSIS
    Arma hojas de contacto (cuadrículas numeradas) de las fotos candidatas para etiquetarlas a ojo ANTES de ejecutar el modelo.
.DESCRIPTION
    Cada hoja muestra hasta Columnas x Filas miniaturas con el nombre del archivo debajo. Solo lee las fotos; no las modifica.
.EXAMPLE
    pwsh -File Crear-HojasContacto.ps1 -Fotos C:\corpus\banco330\fotos -Destino C:\corpus\banco330\hojas
#>
param([Parameter(Mandatory)][string] $Fotos, [Parameter(Mandatory)][string] $Destino, [int] $Columnas = 6, [int] $Filas = 4, [int] $Lado = 220,
    [string] $Prefijo = '')
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
New-Item -ItemType Directory -Force $Destino | Out-Null
$archivos = Get-ChildItem $Fotos -File | Where-Object { $_.Extension -in '.jpg', '.jpeg', '.png' -and $_.Name -like "$Prefijo*" } | Sort-Object Name
$porHoja = $Columnas * $Filas
$alto = $Lado + 22
for ($h = 0; $h * $porHoja -lt $archivos.Count; $h++) {
    $lote = $archivos | Select-Object -Skip ($h * $porHoja) -First $porHoja
    $hoja = New-Object System.Drawing.Bitmap ($Columnas * $Lado), ($Filas * $alto)
    $g = [System.Drawing.Graphics]::FromImage($hoja)
    $g.Clear([System.Drawing.Color]::White)
    $g.InterpolationMode = 'HighQualityBicubic'
    $fuente = New-Object System.Drawing.Font 'Arial', 11, ([System.Drawing.FontStyle]::Bold)
    $i = 0
    foreach ($a in $lote) {
        $x = ($i % $Columnas) * $Lado; $y = [math]::Floor($i / $Columnas) * $alto
        try {
            $img = [System.Drawing.Image]::FromFile($a.FullName)
            $escala = [math]::Min(($Lado - 6) / $img.Width, ($Lado - 6) / $img.Height)
            $w = [int]($img.Width * $escala); $hh = [int]($img.Height * $escala)
            $g.DrawImage($img, $x + [int](($Lado - $w) / 2), $y + [int](($Lado - $hh) / 2), $w, $hh)
            $img.Dispose()
        } catch { $g.DrawString('ERROR', $fuente, [System.Drawing.Brushes]::Red, $x + 5, $y + 5) }
        $g.DrawString($a.BaseName, $fuente, [System.Drawing.Brushes]::Black, $x + 4, $y + $Lado + 2)
        $i++
    }
    $salida = Join-Path $Destino ('hoja-{0:D2}.jpg' -f ($h + 1))
    $hoja.Save($salida, [System.Drawing.Imaging.ImageFormat]::Jpeg)
    $g.Dispose(); $hoja.Dispose()
    $salida
}

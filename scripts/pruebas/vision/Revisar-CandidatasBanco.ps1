<#
.SYNOPSIS
    Prepara la revisión a ojo de las candidatas de un banco: detecta duplicados por huella visual (también contra bancos
    anteriores) y arma hojas de contacto numeradas por clase.
.DESCRIPTION
    - Huella dHash de 64 bits (9x8 en gris). Dos fotos con distancia de Hamming <= -Umbral se consideran la misma imagen
      (recortes leves, otra resolución). Se conserva la primera y la otra se anota en duplicados.csv.
    - No decide etiquetas ni excluye por contenido: eso lo hace una persona mirando las hojas, ANTES de ejecutar el modelo.
    - -Clases limita las hojas a esas clases (prefijo del archivo antes del guion).
.EXAMPLE
    pwsh -File Revisar-CandidatasBanco.ps1 -Candidatas C:\banco\candidatas -Anteriores C:\banco330\fotos,C:\colombia\fotos -Hojas C:\banco\hojas
#>
param([Parameter(Mandatory)][string] $Candidatas, [string[]] $Anteriores = @(), [Parameter(Mandatory)][string] $Hojas,
      [string[]] $Clases, [int] $Umbral = 5, [int] $Columnas = 6, [int] $Filas = 4)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
New-Item -ItemType Directory -Force $Hojas | Out-Null

function Huella([string] $ruta) {
    $img = [Drawing.Image]::FromFile($ruta)
    try {
        $b = New-Object Drawing.Bitmap 9, 8
        $g = [Drawing.Graphics]::FromImage($b); $g.InterpolationMode = 'HighQualityBilinear'; $g.DrawImage($img, 0, 0, 9, 8); $g.Dispose()
        [uint64] $h = 0
        for ($y = 0; $y -lt 8; $y++) { for ($x = 0; $x -lt 8; $x++) {
            $a = $b.GetPixel($x, $y); $c = $b.GetPixel($x + 1, $y)
            $h = $h -shl 1
            if (($a.R * 299 + $a.G * 587 + $a.B * 114) -gt ($c.R * 299 + $c.G * 587 + $c.B * 114)) { $h = $h -bor 1 }
        } }
        $b.Dispose(); return $h
    } finally { $img.Dispose() }
}
function Distancia([uint64] $a, [uint64] $b) { $x = $a -bxor $b; $n = 0; while ($x -ne 0) { $n += [int]($x -band 1); $x = $x -shr 1 }; $n }

# Huellas con caché por carpeta (nombre + tamaño), para no recalcular en cada pasada.
function HuellasDe([string] $carpeta) {
    $cache = Join-Path $carpeta 'huellas.csv'; $previas = @{}
    if (Test-Path $cache) { foreach ($f in Import-Csv $cache) { $previas["$($f.Archivo)|$($f.Bytes)"] = [uint64]$f.Huella } }
    $filas = foreach ($a in Get-ChildItem $carpeta -Filter *.jpg | Sort-Object Name) {
        $k = "$($a.Name)|$($a.Length)"
        $h = if ($previas.ContainsKey($k)) { $previas[$k] } else { try { Huella $a.FullName } catch { continue } }
        [pscustomobject]@{ Archivo = $a.Name; Bytes = $a.Length; Huella = $h; Ruta = $a.FullName }
    }
    $filas | Select-Object Archivo, Bytes, Huella | Export-Csv $cache -Encoding utf8
    $filas
}

$viejas = foreach ($c in $Anteriores) { if (Test-Path $c) { HuellasDe $c } }
$nuevas = @(HuellasDe $Candidatas)
$duplicados = New-Object System.Collections.Generic.List[object]
$unicas = New-Object System.Collections.Generic.List[object]
foreach ($n in $nuevas) {
    $igual = $null
    foreach ($v in $viejas) { if ((Distancia $n.Huella $v.Huella) -le $Umbral) { $igual = $v.Ruta; break } }
    if (-not $igual) { foreach ($u in $unicas) { if ((Distancia $n.Huella $u.Huella) -le $Umbral) { $igual = $u.Archivo; break } } }
    if ($igual) { $duplicados.Add([pscustomobject]@{ Archivo = $n.Archivo; IgualA = $igual }) } else { $unicas.Add($n) }
}
$duplicados | Export-Csv (Join-Path (Split-Path $Candidatas -Parent) 'duplicados.csv') -Encoding utf8

$celda = 250; $pie = 22; $porHoja = $Columnas * $Filas
$fuente = New-Object Drawing.Font 'Arial', 11, ([Drawing.FontStyle]::Bold)
$grupos = $unicas | Group-Object { $_.Archivo.Split('-')[0] } | Where-Object { -not $Clases -or $_.Name -in $Clases }
foreach ($grupo in $grupos) {
    $lista = @($grupo.Group); $hoja = 0
    for ($i = 0; $i -lt $lista.Count; $i += $porHoja) {
        $hoja++
        $bmp = New-Object Drawing.Bitmap ($Columnas * $celda), ($Filas * ($celda + $pie))
        $g = [Drawing.Graphics]::FromImage($bmp); $g.Clear([Drawing.Color]::White); $g.InterpolationMode = 'HighQualityBicubic'
        for ($j = 0; $j -lt $porHoja -and $i + $j -lt $lista.Count; $j++) {
            $f = $lista[$i + $j]; $col = $j % $Columnas; $fil = [Math]::Floor($j / $Columnas)
            $img = [Drawing.Image]::FromFile($f.Ruta)
            $esc = [Math]::Min(($celda - 6) / $img.Width, ($celda - 6) / $img.Height)
            $g.DrawImage($img, $col * $celda + 3, $fil * ($celda + $pie) + 3, [int]($img.Width * $esc), [int]($img.Height * $esc)); $img.Dispose()
            $g.DrawString($f.Archivo.Replace('.jpg', ''), $fuente, [Drawing.Brushes]::Red, $col * $celda + 3, $fil * ($celda + $pie) + $celda)
        }
        $g.Dispose(); $bmp.Save((Join-Path $Hojas ('{0}_{1:D2}.png' -f $grupo.Name, $hoja))); $bmp.Dispose()
    }
}
"Candidatas: $($nuevas.Count); duplicadas: $($duplicados.Count); únicas: $($unicas.Count); hojas en $Hojas"

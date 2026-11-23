[CmdletBinding()]
param(
    [string]$CarpetaImagenes,
    [switch]$EjecutarOllama,
    [string]$Modelo = 'lfm3-vl:latest'
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$RaizRepo = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
if (-not $CarpetaImagenes) {
    $padreEco = Split-Path -Parent (Split-Path -Parent $RaizRepo)
    $candidatos = @(
        (Join-Path $padreEco '02_Imagenes_Reales_EcoAcess'),
        (Join-Path $padreEco '02_Imagenes_Reales_EcoAccess')
    )
    $CarpetaImagenes = $candidatos | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}
if (-not $CarpetaImagenes -or -not (Test-Path -LiteralPath $CarpetaImagenes)) {
    throw "No se encontro la carpeta de imagenes reales."
}

function Get-DHash([System.Drawing.Bitmap]$bmp) {
    $small = New-Object System.Drawing.Bitmap(9, 8)
    $g = [System.Drawing.Graphics]::FromImage($small)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBilinear
    $g.DrawImage($bmp, 0, 0, 9, 8)
    $g.Dispose()
    [uint64]$hash = 0
    for ($y = 0; $y -lt 8; $y++) {
        for ($x = 0; $x -lt 8; $x++) {
            $c1 = $small.GetPixel($x, $y)
            $c2 = $small.GetPixel($x + 1, $y)
            $l1 = 0.299 * $c1.R + 0.587 * $c1.G + 0.114 * $c1.B
            $l2 = 0.299 * $c2.R + 0.587 * $c2.G + 0.114 * $c2.B
            $hash = $hash -shl 1
            if ($l1 -gt $l2) { $hash = $hash -bor 1 }
        }
    }
    $small.Dispose()
    return ('{0:x16}' -f $hash)
}

function Get-Hamming([string]$h1, [string]$h2) {
    $v1 = [Convert]::ToUInt64($h1, 16)
    $v2 = [Convert]::ToUInt64($h2, 16)
    $x = $v1 -bxor $v2
    $d = 0
    while ($x -ne 0) { $d += [int]($x -band 1); $x = $x -shr 1 }
    return $d
}

$InvDir = Join-Path $CarpetaImagenes 'inventario'
$RepRepo = Join-Path $RaizRepo 'datasets\ecoaccess35\reportes'
New-Item -ItemType Directory -Force -Path $InvDir, $RepRepo | Out-Null

$BotExe = Join-Path $RaizRepo 'src\EcoAccess.P3.Bot\bin\Release\net48\EcoAccess.P3.Bot.exe'
$archivos = @(Get-ChildItem -LiteralPath $CarpetaImagenes -File | Where-Object { $_.Extension -match '^\.(jpg|jpeg|png|webp)$' } | Sort-Object Name)

$filas = [System.Collections.Generic.List[object]]::new()
$escenasMulti = [System.Collections.Generic.List[object]]::new()
$shaMapa = @{}
$duplicados = [System.Collections.Generic.List[object]]::new()
$idx = 0

foreach ($f in $archivos) {
    $idx++
    $id = 'REAL_20261123_' + ('{0:D3}' -f $idx)
    $disponible = (($f.Attributes -band [IO.FileAttributes]::Offline) -eq 0)
    $sha = (Get-FileHash -LiteralPath $f.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    $w = 0; $h = 0; $fmt = ''; $dhash = ''; $valida = $true
    try {
        $img = [System.Drawing.Image]::FromFile($f.FullName)
        $w = $img.Width; $h = $img.Height; $fmt = $img.RawFormat.ToString()
        $bmp = New-Object System.Drawing.Bitmap($img)
        $dhash = Get-DHash $bmp
        $bmp.Dispose(); $img.Dispose()
    } catch {
        $valida = $false
    }

    $esDupExacto = $false
    if ($shaMapa.ContainsKey($sha)) {
        $esDupExacto = $true
        $duplicados.Add([pscustomobject]@{
            TipoDuplicado = 'EXACTO_SHA256'
            ArchivoA = $shaMapa[$sha]
            ArchivoB = $f.Name
            DistanciaDHash = 0
            Sha256 = $sha
        })
    } else {
        $shaMapa[$sha] = $f.Name
    }

    $objetosTxt = ''; $cantObj = 0; $materiales = ''; $combinacion = 'PENDIENTE'; $confMax = 0; $tipoFoto = 'PENDIENTE'; $estadoRev = 'PENDIENTE_REVISION'
    if ($EjecutarOllama -and $valida -and (Test-Path -LiteralPath $BotExe)) {
        try {
            $raw = & $BotExe --probar-imagen35 $f.FullName --modelo $Modelo 2>$null | Out-String
            if ($raw -and $raw.Trim().StartsWith('{')) {
                $res = $raw | ConvertFrom-Json
                $objs = @($res.Dictamen.Objetos)
                $cantObj = $objs.Count
                $objetosTxt = ($objs | ForEach-Object { "$($_.Observacion.NombreDetectado) [$($_.Estado):$($_.CanecaAsignada)]" }) -join '; '
                $materiales = ($objs | ForEach-Object { if ($_.Material35) { $_.Material35.Codigo } else { 'SIN_CODIGO' } }) -join ','
                $combinacion = $res.Dictamen.Combinacion
                if ($objs.Count -gt 0) { $confMax = ($objs | ForEach-Object { [double]$_.Observacion.Confianza } | Measure-Object -Maximum).Maximum }
                $tipoFoto = if ($cantObj -gt 1) { 'MULTIRRESIDUO' } elseif ($cantObj -eq 1) { 'INDIVIDUAL' } else { 'SIN_DETECCION' }
                $estadoRev = if ($res.Dictamen.EstadoGlobal -eq 'Clasificado') { 'DETECTADO_IA_PENDIENTE_REVISION_HUMANA' } else { 'REQUIERE_REVISION_HUMANA' }
                if ($cantObj -gt 1) {
                    $escenasMulti.Add([ordered]@{
                        id = $id
                        archivo = $f.Name
                        sha256 = $sha
                        combinacion_ia = $combinacion
                        estado_global = $res.Dictamen.EstadoGlobal
                        puede_abrir = $res.Dictamen.PuedeAbrirCanecas
                        objetos = @($objs | ForEach-Object {
                            [ordered]@{
                                nombre = $_.Observacion.NombreDetectado
                                material_obs = $_.Observacion.MaterialObservable
                                estado_obs = $_.Observacion.EstadoObservable
                                codigo35 = if ($_.Material35) { $_.Material35.Codigo } else { $null }
                                estado_regla = $_.Estado
                                caneca = $_.CanecaAsignada
                                confianza = $_.Observacion.Confianza
                            }
                        })
                    })
                }
            }
        } catch {
            $estadoRev = 'ERROR_INFERENCIA'
        }
    }

    $filas.Add([pscustomobject]@{
        Id = $id
        Archivo = $f.Name
        Formato = $fmt
        Ancho = $w
        Alto = $h
        TamanoBytes = $f.Length
        Sha256 = $sha
        DHash = $dhash
        DisponibleLocal = $disponible
        ImagenValida = $valida
        EsDuplicadoExacto = $esDupExacto
        TipoFotografia = $tipoFoto
        CantidadObjetosIA = $cantObj
        ObjetosDetectadosIA = $objetosTxt
        CodigosCatalogoIA = $materiales
        CombinacionIA = $combinacion
        ConfianzaMaxIA = $confMax
        EstadoRevision = $estadoRev
        Origen = 'FotoReal_EcoAccess_2026-11-23'
    })
}

# Detectar similares perceptuales (Hamming <= 8)
for ($i = 0; $i -lt $filas.Count; $i++) {
    if (-not $filas[$i].DHash) { continue }
    for ($j = $i + 1; $j -lt $filas.Count; $j++) {
        if (-not $filas[$j].DHash) { continue }
        if ($filas[$i].Sha256 -eq $filas[$j].Sha256) { continue }
        $dist = Get-Hamming $filas[$i].DHash $filas[$j].DHash
        if ($dist -le 8) {
            $duplicados.Add([pscustomobject]@{
                TipoDuplicado = 'SIMILAR_PERCEPTUAL_DHASH'
                ArchivoA = $filas[$i].Archivo
                ArchivoB = $filas[$j].Archivo
                DistanciaDHash = $dist
                Sha256 = "$($filas[$i].Sha256.Substring(0,12))/$($filas[$j].Sha256.Substring(0,12))"
            })
        }
    }
}

foreach ($dirOut in @($InvDir, $RepRepo)) {
    $filas | Export-Csv -LiteralPath (Join-Path $dirOut 'inventario_imagenes.csv') -NoTypeInformation -Encoding UTF8
    $duplicados | Export-Csv -LiteralPath (Join-Path $dirOut 'duplicados_detectados.csv') -NoTypeInformation -Encoding UTF8
    $jsonMulti = if ($escenasMulti.Count -eq 0) { '[]' } else { ConvertTo-Json -InputObject @($escenasMulti) -Depth 10 }
    [System.IO.File]::WriteAllText((Join-Path $dirOut 'escenas_multirresiduo.json'), $jsonMulti, [System.Text.UTF8Encoding]::new($false))
}

Write-Host "AUDITORIA COMPLETADA: Total=$($filas.Count) | Validas=$(@($filas | Where-Object ImagenValida).Count) | DuplicadosExactos=$(@($duplicados | Where-Object { $_.TipoDuplicado -eq 'EXACTO_SHA256' }).Count) | SimilaresPerceptuales=$(@($duplicados | Where-Object { $_.TipoDuplicado -eq 'SIMILAR_PERCEPTUAL_DHASH' }).Count) | MultiobjetoIA=$($escenasMulti.Count)"
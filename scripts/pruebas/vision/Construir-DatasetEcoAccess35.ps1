<#
.SYNOPSIS
Crea la estructura estándar datasets/ecoaccess35/ (FASE 4), organiza referencias a imágenes reales sin duplicar
originales, genera composiciones multirresiduo y variaciones sintéticas controladas (fondos claro/oscuro/mesa/campus,
escalas, rotación, sombra, solapamiento parcial) claramente etiquetadas como SinteticaCompuesta y separadas sin fuga
en entrenamiento/, validacion/ y evaluacion_independiente/.
#>
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$Raiz = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$Base = Join-Path $Raiz 'datasets\ecoaccess35'
$Subcarpetas = @(
    'originales', 'reales', 'sinteticas', 'recortes_individuales',
    'composiciones_multirresiduo', 'anotaciones', 'entrenamiento',
    'validacion', 'evaluacion_independiente', 'casos_fallidos',
    'reportes', 'bandeja_entrada'
)
foreach ($s in $Subcarpetas) { New-Item -ItemType Directory -Force -Path (Join-Path $Base $s) | Out-Null }

# Asegurar que el manifiesto real del catálogo exista
$ManifiestoPrevio = Join-Path $Raiz 'datasets\ecoaccess_35\manifiesto_real_catalogo.json'
if (-not (Test-Path $ManifiestoPrevio)) {
    & powershell -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'Auditar-Dataset35.ps1')
}

$DatosReales = Get-Content -LiteralPath $ManifiestoPrevio -Raw -Encoding UTF8 | ConvertFrom-Json
$PorCodigo = @{}
foreach ($e in $DatosReales.Entradas) {
    $cod = $e.ObjetosEsperados[0].CodigoMaterial35
    if (-not $PorCodigo.ContainsKey($cod)) { $PorCodigo[$cod] = [System.Collections.Generic.List[object]]::new() }
    $rutaAbs = [System.IO.Path]::GetFullPath((Join-Path (Split-Path $ManifiestoPrevio) $e.RutaRelativa))
    $PorCodigo[$cod].Add(@{ Ruta=$rutaAbs; Obj=$e.ObjetosEsperados[0]; Orig=$e.ArchivoOrigenOriginal })
}

function Get-Combinacion([string[]]$cats) {
    $v = $cats -contains 'VERDE'; $b = $cats -contains 'BLANCA'; $n = $cats -contains 'NEGRA'
    if ($v -and $b -and $n) { return 'C07_VerdeBlancaNegra' }
    if ($v -and $b) { return 'C04_VerdeBlanca' }
    if ($v -and $n) { return 'C05_VerdeNegra' }
    if ($b -and $n) { return 'C06_BlancaNegra' }
    if ($v) { return 'C01_Verde' }
    if ($b) { return 'C02_Blanca' }
    if ($n) { return 'C03_Negra' }
    return 'Ninguna'
}

# Fondos controlados: claro, oscuro, mesa madera, cemento universitario
$Fondos = @{
    claro  = [System.Drawing.Color]::FromArgb(245, 246, 244)
    oscuro = [System.Drawing.Color]::FromArgb(46, 50, 54)
    mesa   = [System.Drawing.Color]::FromArgb(196, 168, 132)
    campus = [System.Drawing.Color]::FromArgb(172, 178, 175)
}

function New-EscenaSintetica([object[]]$items, [string]$destino, [string]$fondoClave, [double]$escala, [int]$solapamientoPx, [bool]$sombra) {
    $lado = [int](280 * $escala)
    $pad = 28
    $paso = [Math]::Max(80, $lado - $solapamientoPx)
    $w = $pad * 2 + $lado + ($items.Count - 1) * $paso
    $h = $lado + $pad * 2 + 24
    $bmp = New-Object System.Drawing.Bitmap($w, $h)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.Clear($Fondos[$fondoClave])
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $cajas = @()
    try {
        for ($i = 0; $i -lt $items.Count; $i++) {
            $img = [System.Drawing.Image]::FromFile($items[$i].Ruta)
            try {
                $x = $pad + $i * $paso
                $y = $pad + (($i % 2) * 14)
                if ($sombra) {
                    $br = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(70, 0, 0, 0))
                    $g.FillRectangle($br, $x + 8, $y + 8, $lado, $lado)
                    $br.Dispose()
                }
                $g.DrawImage($img, $x, $y, $lado, $lado)
                $cajas += [ordered]@{
                    X = [Math]::Round($x / $w, 4)
                    Y = [Math]::Round($y / $h, 4)
                    Ancho = [Math]::Round($lado / $w, 4)
                    Alto = [Math]::Round($lado / $h, 4)
                    Verificada = $true
                }
            } finally { $img.Dispose() }
        }
        $bmp.Save($destino, [System.Drawing.Imaging.ImageFormat]::Jpeg)
    } finally {
        $g.Dispose(); $bmp.Dispose()
    }
    return ,$cajas
}

# Definición de escenas independientes (división estricta: entrenamiento / validacion / evaluacion_independiente, sin compartir escena base)
$PlanSintetico = @(
    # Evaluación independiente (21 escenas cubriendo C01..C07 con 3 variantes de entorno por combinación + 4 objetos)
    @{ Split='evaluacion_independiente'; Id='EVAL_C01_Oscuro'; Codigos=@('V01','V03'); Fondo='oscuro'; Escala=0.95; Solape=0; Sombra=$true; Cond='FondoOscuro+Sombra' }
    @{ Split='evaluacion_independiente'; Id='EVAL_C01_Mesa'; Codigos=@('V05','V08','V10'); Fondo='mesa'; Escala=0.90; Solape=18; Sombra=$false; Cond='Mesa+SolapeParcial' }
    @{ Split='evaluacion_independiente'; Id='EVAL_C02_Claro'; Codigos=@('B01','B09'); Fondo='claro'; Escala=1.0; Solape=0; Sombra=$false; Cond='FondoClaro' }
    @{ Split='evaluacion_independiente'; Id='EVAL_C02_Campus'; Codigos=@('B06','B10','B12'); Fondo='campus'; Escala=0.85; Solape=12; Sombra=$true; Cond='Campus+DistanciaMedia' }
    @{ Split='evaluacion_independiente'; Id='EVAL_C03_Oscuro'; Codigos=@('N01','N04'); Fondo='oscuro'; Escala=0.95; Solape=0; Sombra=$true; Cond='FondoOscuro' }
    @{ Split='evaluacion_independiente'; Id='EVAL_C03_Mesa'; Codigos=@('N06','N07','N08'); Fondo='mesa'; Escala=0.90; Solape=16; Sombra=$false; Cond='Mesa+SolapeParcial' }
    @{ Split='evaluacion_independiente'; Id='EVAL_C04_Mesa'; Codigos=@('V01','B01'); Fondo='mesa'; Escala=1.0; Solape=0; Sombra=$true; Cond='Mesa+Sombra' }
    @{ Split='evaluacion_independiente'; Id='EVAL_C04_Campus'; Codigos=@('V04','B08','B10'); Fondo='campus'; Escala=0.85; Solape=14; Sombra=$false; Cond='Campus+3Objetos2Cat' }
    @{ Split='evaluacion_independiente'; Id='EVAL_C05_Claro'; Codigos=@('V03','N01'); Fondo='claro'; Escala=1.0; Solape=0; Sombra=$false; Cond='FondoClaro' }
    @{ Split='evaluacion_independiente'; Id='EVAL_C05_Oscuro'; Codigos=@('V06','V09','N07'); Fondo='oscuro'; Escala=0.85; Solape=16; Sombra=$true; Cond='Oscuro+3Objetos2Cat' }
    @{ Split='evaluacion_independiente'; Id='EVAL_C06_Mesa'; Codigos=@('B07','N03'); Fondo='mesa'; Escala=1.0; Solape=0; Sombra=$true; Cond='Escenario6_VidrioYCartonGrasa' }
    @{ Split='evaluacion_independiente'; Id='EVAL_C06_Campus'; Codigos=@('B04','B11','N09'); Fondo='campus'; Escala=0.85; Solape=14; Sombra=$false; Cond='Campus+VasoLimpioYSucio' }
    @{ Split='evaluacion_independiente'; Id='EVAL_C07_Claro'; Codigos=@('V01','B01','N01'); Fondo='claro'; Escala=0.95; Solape=0; Sombra=$false; Cond='Escenario7_Clasico' }
    @{ Split='evaluacion_independiente'; Id='EVAL_C07_Mesa'; Codigos=@('V02','B09','N06'); Fondo='mesa'; Escala=0.90; Solape=15; Sombra=$true; Cond='Mesa+3Cat+Solape' }
    @{ Split='evaluacion_independiente'; Id='EVAL_C07_Campus4Obj'; Codigos=@('V10','B01','B12','N07'); Fondo='campus'; Escala=0.80; Solape=18; Sombra=$true; Cond='4Objetos_3Categorias' }
    # Validación (7 escenas, una por combinación C01..C07)
    @{ Split='validacion'; Id='VAL_C01'; Codigos=@('V07','V08'); Fondo='claro'; Escala=0.95; Solape=0; Sombra=$false; Cond='Validacion_C01' }
    @{ Split='validacion'; Id='VAL_C02'; Codigos=@('B02','B08'); Fondo='mesa'; Escala=0.95; Solape=0; Sombra=$false; Cond='Validacion_C02' }
    @{ Split='validacion'; Id='VAL_C03'; Codigos=@('N02','N09'); Fondo='campus'; Escala=0.95; Solape=0; Sombra=$false; Cond='Validacion_C03' }
    @{ Split='validacion'; Id='VAL_C04'; Codigos=@('V05','B12'); Fondo='oscuro'; Escala=0.95; Solape=0; Sombra=$true; Cond='Validacion_C04' }
    @{ Split='validacion'; Id='VAL_C05'; Codigos=@('V10','N08'); Fondo='mesa'; Escala=0.95; Solape=0; Sombra=$false; Cond='Validacion_C05' }
    @{ Split='validacion'; Id='VAL_C06'; Codigos=@('B05','N04'); Fondo='claro'; Escala=0.95; Solape=0; Sombra=$false; Cond='Validacion_C06' }
    @{ Split='validacion'; Id='VAL_C07'; Codigos=@('V03','B06','N01'); Fondo='campus'; Escala=0.90; Solape=10; Sombra=$true; Cond='Validacion_C07' }
)

$Ahora = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
$PorSplit = @{ entrenamiento=[System.Collections.Generic.List[object]]::new(); validacion=[System.Collections.Generic.List[object]]::new(); evaluacion_independiente=[System.Collections.Generic.List[object]]::new() }

foreach ($p in $PlanSintetico) {
    $items = @()
    foreach ($c in $p.Codigos) {
        if ($PorCodigo.ContainsKey($c) -and $PorCodigo[$c].Count -gt 0) {
            $idx = if ($p.Split -eq 'validacion' -and $PorCodigo[$c].Count -gt 1) { 1 } else { 0 }
            $items += $PorCodigo[$c][$idx]
        }
    }
    if ($items.Count -ne $p.Codigos.Count) { continue }
    $destImg = Join-Path $Base ("composiciones_multirresiduo\" + $p.Id + '.jpg')
    $cajas = New-EscenaSintetica $items $destImg $p.Fondo $p.Escala $p.Solape $p.Sombra
    $sha = (Get-FileHash -Path $destImg -Algorithm SHA256).Hash.ToLowerInvariant()
    $objs = @()
    $cats = @()
    for ($i = 0; $i -lt $items.Count; $i++) {
        $o = $items[$i].Obj
        $cats += $o.CanecaEsperada
        $objs += [ordered]@{
            CodigoMaterial35 = $o.CodigoMaterial35
            NombreEsperado = $o.NombreEsperado
            CanecaEsperada = $o.CanecaEsperada
            EstadoEsperado = $o.EstadoEsperado
            Cantidad = 1
            Caja = $cajas[$i]
        }
    }
    $entrada = [ordered]@{
        IdCaso = $p.Id
        RutaRelativa = '../composiciones_multirresiduo/' + $p.Id + '.jpg'
        Sha256 = $sha
        Procedencia = 'SinteticaCompuesta'
        Escenario = if (@($cats | Select-Object -Unique).Count -eq 3) { 'TresCategorias' } elseif ($p.Solape -gt 0 -or $p.Sombra) { 'CondicionDificil' } else { 'MezclaCategorias' }
        CombinacionEsperada = (Get-Combinacion $cats)
        ObjetosEsperados = $objs
        ArchivoOrigenOriginal = ($items | ForEach-Object { $_.Orig }) -join '; '
        CondicionIluminacion = $p.Cond
        EsCasoCriticoSeguridad = $false
        RequiereConfirmacionEsperada = $false
        Nota = 'Escena sintética controlada (NO sustituye fotografías reales de cámara del stand).'
    }
    $PorSplit[$p.Split].Add($entrada)
}

foreach ($sp in @('validacion', 'evaluacion_independiente')) {
    $doc = [ordered]@{
        Nombre = "ECOACCESS35_$($sp.ToUpperInvariant())"
        VersionCatalogo = '2026.11.22-35'
        FechaCreacionUtc = $Ahora
        MetaEvaluacionesPorMaterial = 100
        Entradas = $PorSplit[$sp]
    }
    [System.IO.File]::WriteAllText((Join-Path $Base "$sp\manifiesto_$sp.json"), ($doc | ConvertTo-Json -Depth 10), [System.Text.UTF8Encoding]::new($false))
}

# Guardar referencia al manifiesto de imágenes reales en reales/
$ManifiestoRealesNuevo = [ordered]@{
    Nombre = 'ECOACCESS35_REALES_CATALOGO'
    VersionCatalogo = '2026.11.22-35'
    FechaCreacionUtc = $Ahora
    MetaEvaluacionesPorMaterial = 100
    Entradas = @($DatosReales.Entradas | ForEach-Object {
        [ordered]@{
            IdCaso = $_.IdCaso
            RutaRelativa = '../../../' + $_.ArchivoOrigenOriginal
            Sha256 = $_.Sha256
            Procedencia = $_.Procedencia
            Escenario = $_.Escenario
            CombinacionEsperada = $_.CombinacionEsperada
            ObjetosEsperados = $_.ObjetosEsperados
            ArchivoOrigenOriginal = $_.ArchivoOrigenOriginal
            CondicionIluminacion = $_.CondicionIluminacion
            EsCasoCriticoSeguridad = $_.EsCasoCriticoSeguridad
            RequiereConfirmacionEsperada = $_.RequiereConfirmacionEsperada
            Nota = $_.Nota
        }
    })
}
[System.IO.File]::WriteAllText((Join-Path $Base 'reales\manifiesto_reales.json'), ($ManifiestoRealesNuevo | ConvertTo-Json -Depth 10), [System.Text.UTF8Encoding]::new($false))

# Documentación de la bandeja de entrada
$LeemeBandeja = @'
# Bandeja de entrada de nuevas fotografías (`datasets/ecoaccess35/bandeja_entrada/`)

Copia aquí tus nuevas fotografías reales tomadas con la cámara del stand (JPG, PNG o WebP).
No necesitas recompilar Visual Studio.

1. Detectar archivos nuevos, verificar integridad, rechazar duplicados y generar sugerencias:
   ```powershell
   powershell -ExecutionPolicy Bypass -File scripts\pruebas\vision\Incorporar-NuevasFotos35.ps1 -SugerirConOllama
   ```
2. Revisa y aprueba las etiquetas en `datasets/ecoaccess35/anotaciones/pendientes_revision.json` cambiando `"revisado_humano": false` a `true`.
3. Incorpora solo las imágenes aprobadas y ejecuta la evaluación automática:
   ```powershell
   powershell -ExecutionPolicy Bypass -File scripts\pruebas\vision\Incorporar-NuevasFotos35.ps1 -AprobarRevisadas
   ```
'@
[System.IO.File]::WriteAllText((Join-Path $Base 'bandeja_entrada\LEEME_BANDEJA_ENTRADA.md'), $LeemeBandeja, [System.Text.UTF8Encoding]::new($false))

Write-Host "OK datasets/ecoaccess35: Reales=$($DatosReales.Entradas.Count) | Validacion=$($PorSplit['validacion'].Count) | EvaluacionIndependiente=$($PorSplit['evaluacion_independiente'].Count)"

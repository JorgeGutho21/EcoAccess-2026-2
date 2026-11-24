<#
.SYNOPSIS
Audita todas las imágenes disponibles en assets/imagenes/residuos/, valida integridad y SHA-256,
rechaza duplicados exactos, mapea cada archivo al catálogo de los 35 residuos prioritarios
(V01–V10, B01–B13, N01–N12), genera el informe real de cobertura frente a la meta de 3.500
evaluaciones y crea un subconjunto controlado de composiciones multirresiduo (C01–C07) etiquetado
explícitamente como SinteticaCompuesta.
#>
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing

$Raiz = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$Assets = Join-Path $Raiz 'assets\imagenes\residuos'
$OutDir = Join-Path $Raiz 'datasets\ecoaccess_35'
$CompDir = Join-Path $OutDir 'compuestas_evaluacion'
New-Item -ItemType Directory -Force -Path $OutDir, $CompDir | Out-Null

$Materiales35 = @(
    @{ Codigo='V01'; Nombre='Cáscara de banano'; Caneca='VERDE' }
    @{ Codigo='V02'; Nombre='Cáscara de limón'; Caneca='VERDE' }
    @{ Codigo='V03'; Nombre='Cáscara de papa'; Caneca='VERDE' }
    @{ Codigo='V04'; Nombre='Cáscara de aguacate'; Caneca='VERDE' }
    @{ Codigo='V05'; Nombre='Medio limón'; Caneca='VERDE' }
    @{ Codigo='V06'; Nombre='Restos de tomate crudo'; Caneca='VERDE' }
    @{ Codigo='V07'; Nombre='Medio kiwi'; Caneca='VERDE' }
    @{ Codigo='V08'; Nombre='Hojas secas de árbol'; Caneca='VERDE' }
    @{ Codigo='V09'; Nombre='Restos de césped o poda vegetal'; Caneca='VERDE' }
    @{ Codigo='V10'; Nombre='Cáscara de naranja'; Caneca='VERDE' }
    @{ Codigo='B01'; Nombre='Botella plástica pequeña de agua, limpia y vacía'; Caneca='BLANCA' }
    @{ Codigo='B02'; Nombre='Botella plástica pequeña de gaseosa, limpia y vacía'; Caneca='BLANCA' }
    @{ Codigo='B03'; Nombre='Tapa plástica aprovechable'; Caneca='BLANCA' }
    @{ Codigo='B04'; Nombre='Vaso plástico limpio y seco'; Caneca='BLANCA' }
    @{ Codigo='B05'; Nombre='Envase plástico pequeño, limpio y vacío'; Caneca='BLANCA' }
    @{ Codigo='B06'; Nombre='Frasco de vidrio limpio'; Caneca='BLANCA' }
    @{ Codigo='B07'; Nombre='Botella pequeña de vidrio, vacía'; Caneca='BLANCA' }
    @{ Codigo='B08'; Nombre='Bola de papel de cuaderno limpio'; Caneca='BLANCA' }
    @{ Codigo='B09'; Nombre='Cartón pequeño limpio y seco'; Caneca='BLANCA' }
    @{ Codigo='B10'; Nombre='Caja de jugo HIT o similar, vacía y escurrida'; Caneca='BLANCA' }
    @{ Codigo='B11'; Nombre='Caja de leche multicapa, vacía y escurrida'; Caneca='BLANCA' }
    @{ Codigo='B12'; Nombre='Lata pequeña de bebida, limpia y vacía'; Caneca='BLANCA' }
    @{ Codigo='B13'; Nombre='Película plástica limpia aceptada por la ruta de aprovechamiento'; Caneca='BLANCA' }
    @{ Codigo='N01'; Nombre='Servilleta usada o contaminada'; Caneca='NEGRA' }
    @{ Codigo='N02'; Nombre='Papel higiénico usado'; Caneca='NEGRA' }
    @{ Codigo='N03'; Nombre='Cartón impregnado con grasa de comida'; Caneca='NEGRA' }
    @{ Codigo='N04'; Nombre='Colilla de cigarrillo completamente apagada'; Caneca='NEGRA' }
    @{ Codigo='N05'; Nombre='Condón usado, correctamente envuelto'; Caneca='NEGRA' }
    @{ Codigo='N06'; Nombre='Pitillo usado no aprovechable'; Caneca='NEGRA' }
    @{ Codigo='N07'; Nombre='Bandeja pequeña de icopor contaminada'; Caneca='NEGRA' }
    @{ Codigo='N08'; Nombre='Envoltura contaminada de alimentos'; Caneca='NEGRA' }
    @{ Codigo='N09'; Nombre='Vaso plástico contaminado no aprovechable'; Caneca='NEGRA' }
    @{ Codigo='N10'; Nombre='Chicle usado'; Caneca='NEGRA' }
    @{ Codigo='N11'; Nombre='Toallita húmeda usada'; Caneca='NEGRA' }
    @{ Codigo='N12'; Nombre='Tapabocas desechable usado'; Caneca='NEGRA' }
)

$MapaMat = @{}
$ImgsPorCodigo = @{}
foreach ($m in $Materiales35) {
    $MapaMat[$m.Codigo] = $m
    $ImgsPorCodigo[$m.Codigo] = [System.Collections.Generic.List[string]]::new()
}

$Prefijos = [ordered]@{
    'verde/banano.jpg'                    = 'V01'
    'verde/fotos/cascara-banano-'         = 'V01'
    'verde/fotos/cascara-limon-'          = 'V02'
    'verde/fotos/cascara-papa-'           = 'V03'
    'verde/fotos/cascara-aguacate-'       = 'V04'
    'verde/limon.jpg'                     = 'V05'
    'verde/fotos/restos-tomate-'          = 'V06'
    'verde/kiwi.jpg'                      = 'V07'
    'verde/hojas.jpg'                     = 'V08'
    'verde/fotos/hojas-secas-'            = 'V08'
    'verde/fotos/poda-jardin-'            = 'V09'
    'verde/naranja.jpg'                   = 'V10'
    'verde/fotos/cascara-naranja-'        = 'V10'
    'blanca/botella-agua.jpg'             = 'B01'
    'blanca/botella-gaseosa.jpg'          = 'B02'
    'blanca/vasos.jpg'                    = 'B04'
    'blanca/fotos/vasos-plasticos-'       = 'B04'
    'blanca/fotos/envase-plastico-'       = 'B05'
    'blanca/fotos/vidrio-1.jpg'           = 'B06'
    'blanca/fotos/vidrio-2.jpg'           = 'B07'
    'blanca/papel.jpg'                    = 'B08'
    'blanca/fotos/papel-limpio-'          = 'B08'
    'blanca/fotos/carton-limpio-'         = 'B09'
    'blanca/caja-jugo.jpg'                = 'B10'
    'blanca/fotos/caja-jugo-'             = 'B10'
    'blanca/fotos/caja-leche-'            = 'B11'
    'blanca/fotos/lata-aluminio-'         = 'B12'
    'negra/servilletas.jpg'               = 'N01'
    'negra/fotos/servilletas-usadas-'     = 'N01'
    'negra/papel-higienico.jpg'           = 'N02'
    'negra/fotos/papel-higienico-'        = 'N02'
    'negra/fotos/papel-contaminado-'      = 'N03'
    'negra/fotos/colilla-'                = 'N04'
    'negra/fotos/condon-usado-'           = 'N05'
    'negra/fotos/cubiertos-desechables-'  = 'N06'
    'negra/plato-icopor.jpg'              = 'N07'
    'negra/fotos/icopor-sucio-'           = 'N07'
    'negra/fotos/envolturas-'             = 'N08'
    'negra/vaso-contaminado.jpg'          = 'N09'
    'negra/fotos/vaso-contaminado-'       = 'N09'
}

function Get-Combinacion([string[]]$cats) {
    $v = $cats -contains 'VERDE'
    $b = $cats -contains 'BLANCA'
    $n = $cats -contains 'NEGRA'
    if ($v -and $b -and $n) { return 'C07_VerdeBlancaNegra' }
    if ($v -and $b) { return 'C04_VerdeBlanca' }
    if ($v -and $n) { return 'C05_VerdeNegra' }
    if ($b -and $n) { return 'C06_BlancaNegra' }
    if ($v) { return 'C01_Verde' }
    if ($b) { return 'C02_Blanca' }
    if ($n) { return 'C03_Negra' }
    return 'Ninguna'
}

function Resolve-Codigo([string]$relUnix) {
    if ($relUnix -match '/pendientes/') { return $null }
    foreach ($k in $Prefijos.Keys) {
        if ($relUnix -eq $k -or $relUnix.StartsWith($k, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $Prefijos[$k]
        }
    }
    return $null
}

function New-Composicion([string[]]$rutas, [string]$destino) {
    $lado = 320
    $pad = 24
    $w = $rutas.Count * $lado + ($rutas.Count + 1) * $pad
    $h = $lado + 2 * $pad
    $bmp = New-Object System.Drawing.Bitmap($w, $h)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.Clear([System.Drawing.Color]::FromArgb(245, 245, 242))
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $cajas = @()
    try {
        for ($i = 0; $i -lt $rutas.Count; $i++) {
            $img = [System.Drawing.Image]::FromFile($rutas[$i])
            try {
                $x = $pad + $i * ($lado + $pad)
                $y = $pad
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
        $g.Dispose()
        $bmp.Dispose()
    }
    return ,$cajas
}

$Hashes = @{}
$Duplicados = @()
$Corruptas = @()
$Pendientes = @()
$Extras = @()
$EntradasReales = [System.Collections.Generic.List[object]]::new()

$Archivos = Get-ChildItem -Path $Assets -Recurse -File | Where-Object { $_.Extension -match '^\.(jpg|jpeg|png|webp)$' } | Sort-Object FullName
$casoNum = 0

foreach ($f in $Archivos) {
    $relAssets = $f.FullName.Substring($Assets.Length + 1).Replace('\', '/')
    try {
        $img = [System.Drawing.Image]::FromFile($f.FullName)
        $w = $img.Width; $h = $img.Height
        $img.Dispose()
        if ($w -le 0 -or $h -le 0) { throw 'Dimensiones vacías' }
    } catch {
        $Corruptas += $relAssets
        continue
    }

    $sha = (Get-FileHash -Path $f.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($Hashes.ContainsKey($sha)) {
        $Duplicados += [ordered]@{ archivo=$relAssets; duplicado_de=$Hashes[$sha]; sha256=$sha }
        continue
    }
    $Hashes[$sha] = $relAssets

    if ($relAssets -match '/pendientes/') {
        $Pendientes += $relAssets
        continue
    }

    $cod = Resolve-Codigo $relAssets
    if (-not $cod) {
        $Extras += $relAssets
        continue
    }

    $m = $MapaMat[$cod]
    $estadoEsp = if ($m.Caneca -eq 'NEGRA') { 'Contaminado' } else { 'LimpioYVacio' }
    $casoNum++
    $ImgsPorCodigo[$cod].Add($f.FullName)
    $EntradasReales.Add([ordered]@{
        IdCaso = ('REAL_{0:D3}_{1}' -f $casoNum, $cod)
        RutaRelativa = '../../assets/imagenes/residuos/' + $relAssets
        Sha256 = $sha
        Procedencia = 'RealCatalogo'
        Escenario = 'Individual'
        CombinacionEsperada = (Get-Combinacion @($m.Caneca))
        ObjetosEsperados = @(
            [ordered]@{
                CodigoMaterial35 = $cod
                NombreEsperado = $m.Nombre
                CanecaEsperada = $m.Caneca
                EstadoEsperado = $estadoEsp
                Cantidad = 1
                Caja = $null
            }
        )
        ArchivoOrigenOriginal = 'assets/imagenes/residuos/' + $relAssets
        CondicionIluminacion = 'ReferenciaCatalogo'
        EsCasoCriticoSeguridad = $false
        RequiereConfirmacionEsperada = $false
        Nota = 'Imagen individual real del catálogo verificada con SHA-256.'
    })
}

$AhoraUtc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')

$ManifiestoReal = [ordered]@{
    Nombre = 'ECOACCESS_35_REAL_CATALOGO'
    VersionCatalogo = '2026.11.22-35'
    FechaCreacionUtc = $AhoraUtc
    MetaEvaluacionesPorMaterial = 100
    Entradas = $EntradasReales
}
[System.IO.File]::WriteAllText((Join-Path $OutDir 'manifiesto_real_catalogo.json'), ($ManifiestoReal | ConvertTo-Json -Depth 10), [System.Text.UTF8Encoding]::new($false))

# Escenarios compuestos multirresiduo cubriendo las 7 combinaciones C01..C07 (explícitamente SinteticaCompuesta)
$Escenarios = @(
    @{ Id='COMP_C01_T017_3Organicos'; Codigos=@('V01','V04','V10'); Esc='MezclaCategorias'; Nota='T017: Tres cáscaras orgánicas -> SOLO VERDE' }
    @{ Id='COMP_C02_T018_3Blancas'; Codigos=@('B01','B02','B12'); Esc='MezclaCategorias'; Nota='T018: Tres envases aprovechables -> SOLO BLANCA' }
    @{ Id='COMP_C03_T019_3Negras'; Codigos=@('N01','N02','N05'); Esc='MezclaCategorias'; Nota='T019: Tres residuos sanitarios simulados -> SOLO NEGRA' }
    @{ Id='COMP_C04_T004_LimonBotella'; Codigos=@('V02','B01'); Esc='MezclaCategorias'; Nota='T004: Cáscara limón + botella plástica -> VERDE + BLANCA' }
    @{ Id='COMP_C04_T005_PapaCarton'; Codigos=@('V03','B09'); Esc='MezclaCategorias'; Nota='T005: Cáscara papa + cartón limpio -> VERDE + BLANCA' }
    @{ Id='COMP_C04_T006_HojasVidrio'; Codigos=@('V08','B07'); Esc='MezclaCategorias'; Nota='T006: Hojas secas + botella vidrio -> VERDE + BLANCA' }
    @{ Id='COMP_C05_T007_AguacateColilla'; Codigos=@('V04','N04'); Esc='MezclaCategorias'; Nota='T007: Cáscara aguacate + colilla -> VERDE + NEGRA' }
    @{ Id='COMP_C05_T008_TomateServilleta'; Codigos=@('V06','N01'); Esc='MezclaCategorias'; Nota='T008: Restos tomate + servilleta usada -> VERDE + NEGRA' }
    @{ Id='COMP_C06_T009_CartonLimpioYGrasa'; Codigos=@('B09','N03'); Esc='MezclaCategorias'; Nota='T009: Cartón limpio + cartón con grasa -> BLANCA + NEGRA' }
    @{ Id='COMP_C06_T010_BotellaPitillo'; Codigos=@('B01','N06'); Esc='MezclaCategorias'; Nota='T010: Botella plástica + pitillo usado -> BLANCA + NEGRA' }
    @{ Id='COMP_C06_T011_JugoIcopor'; Codigos=@('B10','N07'); Esc='MezclaCategorias'; Nota='T011: Caja jugo vacía + icopor sucio -> BLANCA + NEGRA' }
    @{ Id='COMP_C07_T012_BananoBotellaServilleta'; Codigos=@('V01','B01','N01'); Esc='TresCategorias'; Nota='T012: Cáscara banano + botella + servilleta -> LAS TRES' }
    @{ Id='COMP_C07_T013_LimonFrascoColilla'; Codigos=@('V05','B06','N04'); Esc='TresCategorias'; Nota='T013: Medio limón + frasco vidrio + colilla -> LAS TRES' }
    @{ Id='COMP_C07_T014_PapaJugoCartonGrasa'; Codigos=@('V03','B10','N03'); Esc='TresCategorias'; Nota='T014: Cáscara papa + caja jugo + cartón grasa -> LAS TRES' }
    @{ Id='COMP_C07_T015_HojasPapelPitillo'; Codigos=@('V08','B08','N06'); Esc='TresCategorias'; Nota='T015: Hojas secas + papel limpio + pitillo -> LAS TRES' }
    @{ Id='COMP_C07_T016_KiwiBotellaIcopor'; Codigos=@('V07','B02','N07'); Esc='TresCategorias'; Nota='T016: Medio kiwi + botella gaseosa + icopor sucio -> LAS TRES' }
    @{ Id='COMP_C07_NaranjaLataEnvoltura'; Codigos=@('V10','B12','N08'); Esc='TresCategorias'; Nota='Tres categorías: naranja + lata + envoltura' }
    @{ Id='COMP_C07_PodaLecheVasoSucio'; Codigos=@('V09','B11','N09'); Esc='TresCategorias'; Nota='Tres categorías: poda + caja leche + vaso contaminado' }
)

$EntradasComp = [System.Collections.Generic.List[object]]::new()
foreach ($esc in $Escenarios) {
    $rutas = @()
    foreach ($c in $esc.Codigos) {
        if ($ImgsPorCodigo[$c].Count -gt 0) { $rutas += $ImgsPorCodigo[$c][0] }
    }
    if ($rutas.Count -ne $esc.Codigos.Count) { continue }
    $dest = Join-Path $CompDir ($esc.Id + '.jpg')
    $cajas = New-Composicion $rutas $dest
    $objs = @()
    $cats = @()
    for ($i = 0; $i -lt $esc.Codigos.Count; $i++) {
        $c = $esc.Codigos[$i]
        $m = $MapaMat[$c]
        $cats += $m.Caneca
        $objs += [ordered]@{
            CodigoMaterial35 = $c
            NombreEsperado = $m.Nombre
            CanecaEsperada = $m.Caneca
            EstadoEsperado = if ($m.Caneca -eq 'NEGRA') { 'Contaminado' } else { 'LimpioYVacio' }
            Cantidad = 1
            Caja = $cajas[$i]
        }
    }
    $shaComp = (Get-FileHash -Path $dest -Algorithm SHA256).Hash.ToLowerInvariant()
    $origenes = ($rutas | ForEach-Object { $_.Substring($Raiz.Length + 1).Replace('\', '/') }) -join '; '
    $EntradasComp.Add([ordered]@{
        IdCaso = $esc.Id
        RutaRelativa = 'compuestas_evaluacion/' + $esc.Id + '.jpg'
        Sha256 = $shaComp
        Procedencia = 'SinteticaCompuesta'
        Escenario = $esc.Esc
        CombinacionEsperada = (Get-Combinacion $cats)
        ObjetosEsperados = $objs
        ArchivoOrigenOriginal = $origenes
        CondicionIluminacion = 'CompuestaReferencia'
        EsCasoCriticoSeguridad = $false
        RequiereConfirmacionEsperada = $false
        Nota = $esc.Nota + ' (PROCEDENCIA: SinteticaCompuesta — no sustituye capturas reales del stand).'
    })
}

$ManifiestoComp = [ordered]@{
    Nombre = 'ECOACCESS_35_MULTIRRESIDUO_COMPUESTO'
    VersionCatalogo = '2026.11.22-35'
    FechaCreacionUtc = $AhoraUtc
    MetaEvaluacionesPorMaterial = 100
    Entradas = $EntradasComp
}
[System.IO.File]::WriteAllText((Join-Path $OutDir 'manifiesto_multirresiduo_compuesto.json'), ($ManifiestoComp | ConvertTo-Json -Depth 10), [System.Text.UTF8Encoding]::new($false))

$Cobertura = @()
$SinImagen = @()
$TotalReales35 = 0
foreach ($m in $Materiales35) {
    $cnt = $ImgsPorCodigo[$m.Codigo].Count
    $TotalReales35 += $cnt
    if ($cnt -eq 0) { $SinImagen += "$($m.Codigo) ($($m.Nombre))" }
    $Cobertura += [ordered]@{
        codigo = $m.Codigo
        nombre = $m.Nombre
        caneca = $m.Caneca
        imagenes_reales_disponibles = $cnt
        meta_individual = 25
        meta_mezcla = 35
        meta_tres_categorias = 30
        meta_dificil = 10
        meta_total = 100
        faltantes_para_meta = [Math]::Max(0, 100 - $cnt)
    }
}

$Resumen = [ordered]@{
    fecha_utc = $AhoraUtc
    total_archivos_escaneados_assets = $Archivos.Count
    corruptos = $Corruptas.Count
    duplicados_exactos_rechazados = $Duplicados.Count
    pendientes_en_assets = $Pendientes.Count
    extras_fuera_de_35 = $Extras.Count
    imagenes_reales_mapeadas_a_35 = $TotalReales35
    materiales_con_imagen_real = ($Cobertura | Where-Object { $_.imagenes_reales_disponibles -gt 0 }).Count
    materiales_sin_imagen_real = $SinImagen
    meta_global_evaluaciones = 3500
    evaluaciones_pendientes_captura_real = [Math]::Max(0, 3500 - $TotalReales35)
    escenas_multirresiduo_reales = 0
    escenas_multirresiduo_compuestas_sinteticas = $EntradasComp.Count
    detalle_por_material = $Cobertura
}
[System.IO.File]::WriteAllText((Join-Path $OutDir 'cobertura_35_materiales.json'), ($Resumen | ConvertTo-Json -Depth 8), [System.Text.UTF8Encoding]::new($false))

$md = [System.Text.StringBuilder]::new()
[void]$md.AppendLine('# Auditoría de Cobertura del Dataset ECOACCESS 35')
[void]$md.AppendLine()
[void]$md.AppendLine("- **Fecha UTC:** $AhoraUtc")
[void]$md.AppendLine("- **Archivos escaneados en ``assets/imagenes/residuos/``:** $($Archivos.Count)")
[void]$md.AppendLine("- **Imágenes corruptas:** $($Corruptas.Count) · **Duplicados exactos rechazados:** $($Duplicados.Count)")
[void]$md.AppendLine("- **Imágenes reales mapeadas a los 35 prioritarios:** **$TotalReales35** ($($Resumen.materiales_con_imagen_real)/35 materiales cubiertos)")
[void]$md.AppendLine("- **Imágenes reales adicionales (otros residuos del catálogo de 54):** $($Extras.Count)")
[void]$md.AppendLine("- **Imágenes en ``pendientes/`` (no clasificables automáticamente):** $($Pendientes.Count)")
[void]$md.AppendLine("- **Escenas multirresiduo reales de cámara:** **0** (pendientes de captura física en el stand)")
[void]$md.AppendLine("- **Escenas multirresiduo compuestas (``SinteticaCompuesta``):** $($EntradasComp.Count) (cubriendo C01–C07)")
[void]$md.AppendLine("- **Meta objetivo de evaluaciones reales (35 × 100):** 3.500 · **Pendientes de captura real:** **$($Resumen.evaluaciones_pendientes_captura_real)**")
[void]$md.AppendLine()
[void]$md.AppendLine('## Materiales prioritarios sin imagen real todavía')
[void]$md.AppendLine()
foreach ($s in $SinImagen) { [void]$md.AppendLine("- $s") }
[void]$md.AppendLine()
[void]$md.AppendLine('## Cobertura real por material (V01–N12)')
[void]$md.AppendLine()
[void]$md.AppendLine('| Código | Material | Caneca | Reales | Meta (25/35/30/10) | Faltantes |')
[void]$md.AppendLine('|---|---|---|---:|---:|---:|')
foreach ($c in $Cobertura) {
    [void]$md.AppendLine("| $($c.codigo) | $($c.nombre) | $($c.caneca) | $($c.imagenes_reales_disponibles) | $($c.meta_total) | $($c.faltantes_para_meta) |")
}
[System.IO.File]::WriteAllText((Join-Path $OutDir 'COBERTURA_35_MATERIALES.md'), $md.ToString(), [System.Text.UTF8Encoding]::new($false))

Write-Host "OK: Escaneados=$($Archivos.Count) | Reales35=$TotalReales35 ($($Resumen.materiales_con_imagen_real)/35) | Extras=$($Extras.Count) | Pendientes=$($Pendientes.Count) | CompuestasC01-C07=$($EntradasComp.Count) | Duplicados=$($Duplicados.Count) | Corruptas=$($Corruptas.Count)"

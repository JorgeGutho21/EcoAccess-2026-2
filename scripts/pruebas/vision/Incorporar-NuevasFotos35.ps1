<#
.SYNOPSIS
Mecanismo de incorporación de nuevas fotografías sin recompilar (FASE 4).
Escanea datasets/ecoaccess35/bandeja_entrada/, verifica integridad, calcula SHA-256, rechaza duplicados,
genera sugerencias opcionales con Ollama y guarda pendientes para revisión humana antes de promoverlas
a las etiquetas de referencia y ejecutar la evaluación automática.
#>
[CmdletBinding()]
param(
    [switch]$SugerirConOllama,
    [switch]$AprobarRevisadas,
    [string]$Modelo = 'lfm3-vl:latest'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$Raiz = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$Base = Join-Path $Raiz 'datasets\ecoaccess35'
$Bandeja = Join-Path $Base 'bandeja_entrada'
$AnotDir = Join-Path $Base 'anotaciones'
$PendArchivo = Join-Path $AnotDir 'pendientes_revision.json'
$IncArchivo = Join-Path $Base 'reales\manifiesto_incorporadas.json'
$BotExe = Join-Path $Raiz 'src\EcoAccess.P3.Bot\bin\Release\net48\EcoAccess.P3.Bot.exe'

New-Item -ItemType Directory -Force -Path $Bandeja, $AnotDir, (Join-Path $Base 'reales'), (Join-Path $Base 'casos_fallidos'), (Join-Path $Base 'reportes') | Out-Null

# Cargar hashes ya existentes para evitar duplicados exactos
$HashesExistentes = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
foreach ($m in @( (Join-Path $Base 'reales\manifiesto_reales.json'), $IncArchivo )) {
    if (Test-Path $m) {
        $doc = Get-Content -LiteralPath $m -Raw -Encoding UTF8 | ConvertFrom-Json
        foreach ($e in $doc.Entradas) { if ($e.Sha256) { [void]$HashesExistentes.Add($e.Sha256) } }
    }
}

$Pendientes = [System.Collections.Generic.List[object]]::new()
if (Test-Path $PendArchivo) {
    $rawPrev = Get-Content -LiteralPath $PendArchivo -Raw -Encoding UTF8
    if (-not [string]::IsNullOrWhiteSpace($rawPrev)) {
        $prev = $rawPrev | ConvertFrom-Json
        if ($null -ne $prev) {
            foreach ($p in @($prev)) {
                $Pendientes.Add($p)
                if ($p.sha256) { [void]$HashesExistentes.Add($p.sha256) }
            }
        }
    }
}

# 1-5: Escanear bandeja de entrada
$Nuevos = 0; $Duplicados = 0; $Corruptos = 0
$Archivos = Get-ChildItem -Path $Bandeja -File | Where-Object { $_.Extension -match '^\.(jpg|jpeg|png|webp)$' } | Sort-Object Name
foreach ($f in $Archivos) {
    try {
        $img = [System.Drawing.Image]::FromFile($f.FullName)
        $w = $img.Width; $h = $img.Height; $img.Dispose()
        if ($w -le 0 -or $h -le 0) { throw 'Imagen vacía' }
    } catch {
        $Corruptos++
        Write-Warning "Imagen dañada rechazada: $($f.Name)"
        continue
    }

    $sha = (Get-FileHash -Path $f.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    if (-not $HashesExistentes.Add($sha)) {
        $Duplicados++
        continue
    }

    $sugerencia = $null
    if ($SugerirConOllama -and (Test-Path $BotExe)) {
        try {
            $jsonSalida = & $BotExe --probar-imagen35 $f.FullName --modelo $Modelo 2>$null | Out-String
            if ($LASTEXITCODE -eq 0 -and $jsonSalida) {
                $parsed = $jsonSalida | ConvertFrom-Json
                $sugerencia = [ordered]@{
                    modelo = $Modelo
                    combinacion = $parsed.Dictamen.Combinacion
                    objetos = @($parsed.Dictamen.Objetos | ForEach-Object {
                        [ordered]@{ codigo = $_.Material35.Codigo; nombre = $_.Observacion.NombreDetectado; caneca = $_.CanecaAsignada }
                    })
                }
            }
        } catch { }
    }

    $Nuevos++
    $id = 'IN_' + (Get-Date -Format 'yyyyMMdd') + '_' + ('{0:D3}' -f ($Pendientes.Count + 1))
    $Pendientes.Add([ordered]@{
        id_caso = $id
        archivo_bandeja = $f.Name
        sha256 = $sha
        procedencia = 'RealStand'
        revisado_humano = $false
        combinacion_esperada = if ($sugerencia) { $sugerencia.combinacion } else { 'POR_DEFINIR' }
        codigos_esperados = @()
        sugerencia_ia = $sugerencia
        nota = 'Revisar manualmente y cambiar revisado_humano=true antes de ejecutar -AprobarRevisadas.'
    })
}

$jsonPend = if ($Pendientes.Count -eq 0) { '[]' } else { ConvertTo-Json -InputObject @($Pendientes) -Depth 10 }
[System.IO.File]::WriteAllText($PendArchivo, $jsonPend, [System.Text.UTF8Encoding]::new($false))
Write-Host "Bandeja escaneada: Nuevos=$Nuevos | DuplicadosRechazados=$Duplicados | Corruptos=$Corruptos | TotalPendientes=$($Pendientes.Count)"

# 6-10: Promover únicamente entradas con revisado_humano=true y evaluar automáticamente
if ($AprobarRevisadas) {
    $Aprobadas = @($Pendientes | Where-Object { $_.revisado_humano -eq $true -and $_.codigos_esperados.Count -gt 0 })
    if ($Aprobadas.Count -eq 0) {
        Write-Host 'No hay entradas con revisado_humano=true y codigos_esperados definidos. Ninguna imagen sin revisar fue promovida.'
        return
    }

    $EntradasInc = [System.Collections.Generic.List[object]]::new()
    if (Test-Path $IncArchivo) {
        $docExist = Get-Content -LiteralPath $IncArchivo -Raw -Encoding UTF8 | ConvertFrom-Json
        foreach ($e in $docExist.Entradas) { $EntradasInc.Add($e) }
    }

    foreach ($a in $Aprobadas) {
        $orig = Join-Path $Bandeja $a.archivo_bandeja
        $dest = Join-Path $Base ("reales\" + $a.id_caso + [System.IO.Path]::GetExtension($a.archivo_bandeja))
        if (Test-Path $orig) { Copy-Item -LiteralPath $orig -Destination $dest -Force }
        $objs = @($a.codigos_esperados | ForEach-Object {
            $cat = if ($_.StartsWith('V')) { 'VERDE' } elseif ($_.StartsWith('B')) { 'BLANCA' } else { 'NEGRA' }
            [ordered]@{ CodigoMaterial35=$_; NombreEsperado=$_; CanecaEsperada=$cat; EstadoEsperado='LimpioYVacio'; Cantidad=1; Caja=$null }
        })
        $EntradasInc.Add([ordered]@{
            IdCaso = $a.id_caso
            RutaRelativa = [System.IO.Path]::GetFileName($dest)
            Sha256 = $a.sha256
            Procedencia = 'RealStand'
            Escenario = if ($objs.Count -gt 1) { 'MezclaCategorias' } else { 'Individual' }
            CombinacionEsperada = $a.combinacion_esperada
            ObjetosEsperados = $objs
            ArchivoOrigenOriginal = 'datasets/ecoaccess35/reales/' + [System.IO.Path]::GetFileName($dest)
            CondicionIluminacion = 'CapturaStandReal'
            EsCasoCriticoSeguridad = $false
            RequiereConfirmacionEsperada = $false
            Nota = 'Aprobada tras revisión humana desde bandeja_entrada.'
        })
    }

    $DocInc = [ordered]@{
        Nombre = 'ECOACCESS35_INCORPORADAS_REALES'
        VersionCatalogo = '2026.11.22-35'
        FechaCreacionUtc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
        MetaEvaluacionesPorMaterial = 100
        Entradas = $EntradasInc
    }
    [System.IO.File]::WriteAllText($IncArchivo, ($DocInc | ConvertTo-Json -Depth 10), [System.Text.UTF8Encoding]::new($false))

    # Quitar aprobadas de pendientes
    $Restantes = @($Pendientes | Where-Object { -not ($_.revisado_humano -eq $true -and $_.codigos_esperados.Count -gt 0) })
    [System.IO.File]::WriteAllText($PendArchivo, ($Restantes | ConvertTo-Json -Depth 10), [System.Text.UTF8Encoding]::new($false))

    Write-Host "Promovidas $($Aprobadas.Count) imágenes aprobadas a $IncArchivo."
    if (Test-Path $BotExe) {
        $prefSalida = Join-Path $Base ("reportes\eval_incorporadas_" + (Get-Date -Format 'yyyyMMdd_HHmmss'))
        & $BotExe --evaluar-catalogo35 $IncArchivo --modelo $Modelo --salida $prefSalida
    }
}

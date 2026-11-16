<#
.SYNOPSIS
    Convierte la revisión a ojo de las candidatas (revision.txt) en el banco listo para Preparar-ManifiestosBanco.ps1.
.DESCRIPTION
    Entradas, en la carpeta del banco:
      candidatas\            fotos descargadas y candidatas\fuentes.json (procedencia)
      clases.json            etiqueta por defecto de cada clase: claves|Contexto|admisible|grupo
      revision.txt           una línea por foto ACEPTADA: archivo=ok | archivo=L | archivo=claves|Contexto|admisible
      duplicados.csv         (opcional) fotos que Revisar-CandidatasBanco.ps1 marcó como repetidas: nunca entran
    Salidas: fotos\ (solo las aceptadas), fotos\fuentes.json, etiquetas.txt y resumen por grupo.
    «L» es un envase con contenido visible: container_with_content|ObjetoCotidiano|false.
#>
param([Parameter(Mandatory)][string] $Banco)
$ErrorActionPreference = 'Stop'
$cand = Join-Path $Banco 'candidatas'; $fotos = Join-Path $Banco 'fotos'
New-Item -ItemType Directory -Force $fotos | Out-Null
$clases = Get-Content (Join-Path $Banco 'clases.json') -Raw -Encoding utf8 | ConvertFrom-Json
$fuentes = @{}; foreach ($f in (Get-Content (Join-Path $cand 'fuentes.json') -Raw -Encoding utf8 | ConvertFrom-Json)) { $fuentes[$f.Archivo] = $f }
$dup = @{}; $csv = Join-Path $Banco 'duplicados.csv'
if (Test-Path $csv) { foreach ($d in Import-Csv $csv) { $dup[$d.Archivo] = $true } }
$lineas = New-Object System.Collections.Generic.List[string]
$aceptadas = New-Object System.Collections.Generic.List[object]
$vistas = @{}
foreach ($l in Get-Content (Join-Path $Banco 'revision.txt') -Encoding utf8) {
    if ($l -match '^\s*(#|$)') { continue }
    $nombre, $valor = $l.Split('=', 2)
    $archivo = "$($nombre.Trim()).jpg"; $valor = $valor.Trim()
    if ($vistas[$archivo]) { throw "Foto repetida en la revisión: $archivo" }; $vistas[$archivo] = $true
    if (-not $fuentes[$archivo]) { throw "Sin procedencia: $archivo" }
    if ($dup[$archivo]) { continue }
    $clase = $archivo.Split('-')[0]
    $def = "$($clases.$clase)".Split('|'); if ($def.Count -ne 4) { throw "Clase sin etiqueta: $clase" }
    $etiqueta = switch ($valor) {
        'ok' { "$($def[0])|$($def[1])|$($def[2])" }
        'L'  { 'container_with_content|ObjetoCotidiano|false' }
        default { if ($valor.Split('|').Count -ne 3) { throw "Etiqueta mal formada: $l" }; $valor }
    }
    $lineas.Add("$archivo|$etiqueta|$($def[3])")
    Copy-Item (Join-Path $cand $archivo) (Join-Path $fotos $archivo) -Force
    $aceptadas.Add($fuentes[$archivo])
}
@('# Banco ampliado: etiquetas escritas a ojo ANTES de ejecutar el modelo (ver revision.txt y clases.json).') + $lineas |
    Set-Content (Join-Path $Banco 'etiquetas.txt') -Encoding utf8
$aceptadas | ConvertTo-Json -Depth 3 | Set-Content (Join-Path $fotos 'fuentes.json') -Encoding utf8
"Candidatas: $($fuentes.Count); aceptadas: $($lineas.Count)"
$lineas | ForEach-Object { $_.Split('|')[-1] } | Group-Object | ForEach-Object { "  {0}: {1}" -f $_.Name, $_.Count }

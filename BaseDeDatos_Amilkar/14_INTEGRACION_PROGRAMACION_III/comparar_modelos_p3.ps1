<#
  Compara el modelo académico (19 tablas, DDL_FINAL_EXTRAIDO.sql) con el modelo de Programación III
  leído SOLO de los scripts versionados en Git (git show, sin cambiar de rama ni tocar archivos del repositorio P3).
  Salida: MATRIZ_P3_vs_ACADEMICO.csv y .md en esta carpeta.
  Uso: pwsh -File comparar_modelos_p3.ps1 [-RepoP3 <ruta>] [-Rama <rama>]
#>
param(
  [string]$RepoP3 = 'C:\Users\Usuario\Documents\01_UNIVERSIDAD\2026-2\Programación de Computadores III-SS462-\ProyectoDeAula_EcoAccess\EcoAccess_V2',
  [string]$Rama = 'desarrollo/telegram-doble-ia')   # rama con las migraciones más recientes verificadas (V002–V025)
$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent $PSScriptRoot
$utf8 = New-Object Text.UTF8Encoding $true
$tiposSql = 'NUMBER|VARCHAR2|NVARCHAR2|CHAR|NCHAR|DATE|TIMESTAMP|CLOB|BLOB|RAW|INTEGER|FLOAT|BINARY_DOUBLE|BOOLEAN|INTERVAL|JSON'
$noCol = '^(CONSTRAINT|PRIMARY|FOREIGN|UNIQUE|CHECK|SUPPLEMENTAL|PERIOD)$'

function Columnas-De-Bloque([string]$cuerpo) {
  $cols = @(); $nivel = 0; $actual = ''
  foreach ($ch in $cuerpo.ToCharArray()) {
    if ($ch -eq '(') { $nivel++ } elseif ($ch -eq ')') { $nivel-- }
    if ($ch -eq ',' -and $nivel -eq 0) { $cols += $actual; $actual = '' } else { $actual += $ch }
  }
  $cols += $actual
  foreach ($c in $cols) {
    $c = ($c -replace '--[^\n]*', '').Trim()
    if ($c -match '^"?(\w+)"?\s+(' + $tiposSql + ')') { $n = $Matches[1].ToUpper(); if ($n -notmatch $noCol) { $n } }
  }
}
function Cuerpo-Parentesis([string]$t, [int]$desde) {
  $i = $t.IndexOf('(', $desde); if ($i -lt 0) { return '' }
  $nivel = 0
  for ($k = $i; $k -lt $t.Length; $k++) { if ($t[$k] -eq '(') { $nivel++ } elseif ($t[$k] -eq ')') { $nivel--; if ($nivel -eq 0) { return $t.Substring($i + 1, $k - $i - 1) } } }
  ''
}
function Leer-Modelo([string]$texto, $modelo) {
  foreach ($m in [regex]::Matches($texto, '(?i)CREATE\s+TABLE\s+(?:"?\w+"?\.)?"?(\w+)"?\s*\(')) {
    $tab = $m.Groups[1].Value.ToUpper()
    if (-not $modelo.Contains($tab)) { $modelo[$tab] = [Collections.Generic.List[string]]::new() }
    foreach ($c in (Columnas-De-Bloque (Cuerpo-Parentesis $texto ($m.Index + $m.Length - 1)))) { if (-not $modelo[$tab].Contains($c)) { $modelo[$tab].Add($c) } }
  }
  foreach ($m in [regex]::Matches($texto, '(?i)ALTER\s+TABLE\s+(?:"?\w+"?\.)?"?(\w+)"?\s+ADD\s*\(')) {
    $tab = $m.Groups[1].Value.ToUpper()
    if (-not $modelo.Contains($tab)) { continue }
    foreach ($c in (Columnas-De-Bloque (Cuerpo-Parentesis $texto ($m.Index + $m.Length - 1)))) { if (-not $modelo[$tab].Contains($c)) { $modelo[$tab].Add($c) } }
  }
}

# Académico
$aca = [ordered]@{}
Leer-Modelo ([IO.File]::ReadAllText((Join-Path $raiz '04_SCRIPTS_ORACLE\DDL_FINAL_EXTRAIDO.sql'))) $aca
# P3 (solo lectura con git show)
$p3 = [ordered]@{}
$archivos = git -C $RepoP3 ls-tree -r --name-only $Rama | Where-Object { $_ -match '^database/(02_tablas/01_|06_migraciones/V\d+)' } | Sort-Object
foreach ($f in $archivos) { Leer-Modelo ((git -C $RepoP3 show "${Rama}:$f") -join "`n") $p3 }
$commit = git -C $RepoP3 rev-parse --short $Rama

$notas = @{
  USUARIO = 'Misma idea (persona registrada). En P3 el ciudadano de Telegram puede ser invitado; en la base académica los ciudadanos son seudónimos sintéticos.'
  ROL = 'Igual: CIUDADANO, OPERARIO, ADMINISTRADOR.'; USUARIO_ROL = 'Igual: relación N:M usuario–rol.'
  MATERIAL = 'Mismo catálogo conceptual; P3 añadió orientación y tarifas por unidad (V011, V012).'
  PUNTO_INSTALACION = 'Misma estación física con QR.'; CONTENEDOR = 'Misma caneca; P3 agrega configuración de tres canecas en CONFIGURACION_CANECA (V021).'
  CONTENEDOR_MATERIAL = 'Igual: qué materiales admite cada caneca.'
  INTERACCION = 'Misma conversación/solicitud del ciudadano.'; ORDEN_APERTURA = 'Misma orden de apertura con idempotencia.'
  EVENTO_DISPOSITIVO = 'Mismo evento del ESP32 / dispositivo.'
  DEPOSITO = 'Mismo depósito declarado y verificado; P3 añadió cantidad declarada (V009) y depósito sin sensor (V016).'
  MOVIMIENTO_ECOPUNTOS = 'Mismo libro de puntos (créditos y reversos).'; REGLA_ECOPUNTOS = 'Misma tarifa versionada.'
  CREDENCIAL_ACCESO = 'Igual idea: credencial con hash, nunca la contraseña.'; VINCULO_TELEGRAM = 'Mismo vínculo cuenta ↔ chat de Telegram.'
  CONSULTA_VISION = 'En P3 se llama ANALISIS_IMAGEN (V008): misma idea, foto analizada que NO es un depósito.'
  AUDITORIA_DEPOSITO = 'Solo académica: la auditoría por trigger AFTER se añadió para la asignatura de Base de Datos.'
  HISTORIAL_ESTADO_CONTENEDOR = 'Solo académica: historial del estado de la caneca escrito por trigger.'
  PESO_REFERENCIAL = 'Solo académica: catálogo de pesos de referencia para estimar kg sin báscula.'
  ANALISIS_IMAGEN = 'Equivale a CONSULTA_VISION académica.'
  OPERACION_ECOPUNTOS = 'Solo P3: agrupa operaciones de puntos para idempotencia de la aplicación.'
  CONVERSACION_TELEGRAM = 'Solo P3: estado de la conversación del bot.'; UPDATE_TELEGRAM = 'Solo P3: deduplicación de updates de Telegram.'
  TOKEN_VINCULACION = 'Solo P3: códigos temporales para vincular Telegram.'; VINCULACION_PENDIENTE = 'Solo P3: vinculación de cuenta existente (V007).'
  OPINION = 'Solo P3: opiniones (V010).'; COMENTARIO_CIUDADANO = 'Solo P3: comentarios (V020).'
  CONSENTIMIENTO_VISION_EXTERNA = 'Solo P3: consentimiento para enviar fotos a un proveedor externo de visión (V018).'
  CONFIGURACION_CANECA = 'Solo P3: color y materiales de las tres canecas (V021).'
}
$equiv = @{ CONSULTA_VISION = 'ANALISIS_IMAGEN'; ANALISIS_IMAGEN = 'CONSULTA_VISION' }
$filas = foreach ($t in (@($aca.Keys) + @($p3.Keys) | Sort-Object -Unique)) {
  $otro = if ($equiv[$t]) { $equiv[$t] } else { $t }
  $enA = $aca.Contains($t); $enP = $p3.Contains($otro) -or $p3.Contains($t)
  if (-not $enA -and $equiv[$t] -and $aca.Contains($equiv[$t])) { continue }   # ya listada desde el lado académico
  $cA = if ($enA) { @($aca[$t]) } else { @() }
  $cP = if ($p3.Contains($otro)) { @($p3[$otro]) } elseif ($p3.Contains($t)) { @($p3[$t]) } else { @() }
  $comunes = @($cA | Where-Object { $cP -contains $_ })
  [pscustomobject]@{
    Tabla_academica = $(if ($enA) { $t } else { '—' }); Tabla_P3 = $(if ($p3.Contains($otro)) { $otro } elseif ($p3.Contains($t)) { $t } else { '—' })
    Existe_en = $(if ($enA -and $enP) { 'Ambos' } elseif ($enA) { 'Solo académico' } else { 'Solo P3' })
    Columnas_academico = $cA.Count; Columnas_P3 = $cP.Count; Columnas_con_mismo_nombre = $comunes.Count
    Solo_academico = (@($cA | Where-Object { $cP -notcontains $_ }) -join ' ')
    Solo_P3 = (@($cP | Where-Object { $cA -notcontains $_ }) -join ' ')
    Adaptacion = $(if ($enA -and $enP) { 'Vista de correspondencia (no migrar tablas)' } elseif ($enA) { 'Ninguna en P3: es extensión académica' } else { 'No se copia a la base académica' })
    Nota = $notas[$t]
  }
}
$filas | Export-Csv (Join-Path $PSScriptRoot 'MATRIZ_P3_vs_ACADEMICO.csv') -NoTypeInformation -Encoding utf8BOM
$sb = New-Object Text.StringBuilder
[void]$sb.AppendLine("# Matriz comparativa: modelo académico (ECOACCESS_BD) vs Programación III (ECOACCESS_P3_APP)`n")
[void]$sb.AppendLine("Fuente P3: rama ``$Rama`` (commit $commit), scripts ``database/02_tablas/01_*`` y ``database/06_migraciones/V002–$((($archivos | Where-Object { $_ -match 'V\d+' } | ForEach-Object { [regex]::Match($_, 'V\d+').Value } | Sort-Object | Select-Object -Last 1)))``, leídos con ``git show`` (sin modificar el repositorio). Fuente académica: ``04_SCRIPTS_ORACLE/DDL_FINAL_EXTRAIDO.sql``.`n")
[void]$sb.AppendLine("Tablas académicas: $($aca.Count) · tablas P3 en scripts: $($p3.Count) · en ambos: $(@($filas | Where-Object Existe_en -eq 'Ambos').Count)`n")
[void]$sb.AppendLine('| Académico | P3 | Existe en | Col. A | Col. P3 | Mismo nombre | Adaptación | Nota |')
[void]$sb.AppendLine('|---|---|---|---:|---:|---:|---|---|')
foreach ($f in $filas) { [void]$sb.AppendLine("| $($f.Tabla_academica) | $($f.Tabla_P3) | $($f.Existe_en) | $($f.Columnas_academico) | $($f.Columnas_P3) | $($f.Columnas_con_mismo_nombre) | $($f.Adaptacion) | $($f.Nota) |") }
[void]$sb.AppendLine("`nLas columnas exactas que difieren están en ``MATRIZ_P3_vs_ACADEMICO.csv`` (columnas Solo_academico y Solo_P3). Las columnas de P3 se leyeron de CREATE TABLE y ALTER TABLE … ADD; si una migración cambia columnas con otra sintaxis, la matriz puede quedarse corta y debe confirmarse con la consulta de solo lectura de Oracle Cloud.")
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'MATRIZ_P3_vs_ACADEMICO.md'), $sb.ToString(), $utf8)
"Académico: $($aca.Count) tablas, $(($aca.Values | ForEach-Object Count | Measure-Object -Sum).Sum) columnas · P3: $($p3.Count) tablas ($commit)"
$filas | Format-Table Tabla_academica, Tabla_P3, Existe_en, Columnas_academico, Columnas_P3, Columnas_con_mismo_nombre -AutoSize | Out-String -Width 200

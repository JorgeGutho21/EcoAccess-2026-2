<#
  Genera los cinco archivos DDL por módulo (M1–M5) de 20_ABRIR_MODELOS_EN_ORACLE\04_DDL_POR_MODULOS
  a partir EXCLUSIVAMENTE de 03_DDL_MODELO_COMPLETO\DDL_FINAL_EXTRAIDO.sql (el DDL que Oracle devolvió
  con DBMS_METADATA). No inventa columnas ni restricciones: copia los bloques originales.

  Reglas (las mismas que usan las subvistas M1–M5 del DMD):
    * Tablas del módulo: bloque CREATE TABLE completo, con PK, UNIQUE, CHECK y todas sus FK.
    * Tablas de contexto (de otros módulos, conectadas por FK): bloque CREATE TABLE con PK, UNIQUE y
      CHECK, conservando solo las FK que apuntan a tablas presentes en el archivo. Las FK que se
      quitan se listan en la cabecera (no se suprimen en silencio).
    * Índices y comentarios: solo los de las tablas del módulo.
  Uso: pwsh -File generar_ddl_por_modulos.ps1
#>
$ErrorActionPreference = 'Stop'
$base = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$ddl = [IO.File]::ReadAllText((Join-Path $base '03_DDL_MODELO_COMPLETO\DDL_FINAL_EXTRAIDO.sql')) -replace "`r`n", "`n"
$utf8 = New-Object Text.UTF8Encoding $false

# --- 1. partir el DDL original en bloques ---
$tablas = [ordered]@{}
foreach ($m in [regex]::Matches($ddl, '(?ms)^[ \t]*CREATE TABLE "(\w+)".*?^[ \t]*\)[^\n]*;[ \t]*$')) { $tablas[$m.Groups[1].Value] = $m.Value.TrimEnd() }
$seccionIdx = $ddl.Substring($ddl.IndexOf('-- ================= ÍNDICES'), $ddl.IndexOf('-- ================= VISTAS') - $ddl.IndexOf('-- ================= ÍNDICES'))
$indices = @(foreach ($m in [regex]::Matches($seccionIdx, '(?ms)^[ \t]*CREATE (?:UNIQUE )?INDEX "\w+" ON "(\w+)".*?;[ \t]*$')) { [pscustomobject]@{ Tabla = $m.Groups[1].Value; Texto = $m.Value.TrimEnd() } })
$comentarios = @(foreach ($m in [regex]::Matches($ddl, '(?m)^[ \t]*COMMENT ON (?:TABLE|COLUMN) "(\w+)".*?;[ \t]*$')) { [pscustomobject]@{ Tabla = $m.Groups[1].Value; Texto = $m.Value.Trim() } })
$fks = @()
foreach ($t in $tablas.Keys) {
  foreach ($m in [regex]::Matches($tablas[$t], '(?s),\s*CONSTRAINT "(\w+)" FOREIGN KEY \(([^)]*)\)\s*REFERENCES "(\w+)" \(([^)]*)\)( ON DELETE \w+)? ENABLE')) {
    $fks += [pscustomobject]@{ Nombre = $m.Groups[1].Value; Hija = $t; Padre = $m.Groups[3].Value; Columnas = $m.Groups[2].Value -replace '"', ''; Texto = $m.Value }
  }
}
if ($tablas.Count -ne 19 -or $fks.Count -ne 38) { throw "Se esperaban 19 tablas y 38 FK; hay $($tablas.Count) y $($fks.Count)" }

# --- 2. módulos (iguales a las subvistas del DMD) ---
$MOD = [ordered]@{
  M1_CATALOGOS = @{ n = 'M1 Catálogos base'; sub = 'M1 Catálogos base'; t = 'MATERIAL', 'PESO_REFERENCIAL'; ctx = 'CONTENEDOR_MATERIAL', 'INTERACCION', 'CONSULTA_VISION', 'DEPOSITO', 'REGLA_ECOPUNTOS' }
  M2_USUARIOS = @{ n = 'M2 Usuarios e identidad'; sub = 'M2 Usuarios e identidad'; t = 'ROL', 'USUARIO_ROL', 'USUARIO', 'CREDENCIAL_ACCESO', 'VINCULO_TELEGRAM'; ctx = @() }
  M3_INFRAESTRUCTURA = @{ n = 'M3 Infraestructura y QR'; sub = 'M3 Infraestructura y QR'; t = 'PUNTO_INSTALACION', 'CONTENEDOR', 'CONTENEDOR_MATERIAL'; ctx = 'MATERIAL' }
  M4_OPERACION = @{ n = 'M4 Operación del sistema'; sub = 'M4 Operación del sistema'; t = 'INTERACCION', 'CONSULTA_VISION', 'ORDEN_APERTURA', 'EVENTO_DISPOSITIVO', 'DEPOSITO'; ctx = 'USUARIO', 'PUNTO_INSTALACION', 'CONTENEDOR', 'PESO_REFERENCIAL', 'MATERIAL' }
  M5_ECOPUNTOS_AUDITORIA = @{ n = 'M5 EcoPuntos y auditoría'; sub = 'M5 EcoPuntos y auditoría'; t = 'REGLA_ECOPUNTOS', 'MOVIMIENTO_ECOPUNTOS', 'AUDITORIA_DEPOSITO', 'HISTORIAL_ESTADO_CONTENEDOR'; ctx = 'USUARIO', 'MATERIAL', 'CONTENEDOR', 'DEPOSITO' }
}
$todas = @(); foreach ($k in $MOD.Keys) { $todas += $MOD[$k].t }
if (@($todas | Sort-Object -Unique).Count -ne 19) { throw 'Los módulos no cubren las 19 tablas' }

# orden de creación: padres antes que hijas (dentro del archivo)
function Ordenar($lista) {
  $pend = [Collections.Generic.List[string]]$lista; $res = New-Object Collections.Generic.List[string]
  while ($pend.Count) {
    $ok = @($pend | Where-Object { $t = $_; -not ($fks | Where-Object { $_.Hija -eq $t -and $_.Padre -ne $t -and $pend.Contains($_.Padre) }) })
    if (-not $ok) { $ok = @($pend[0]) }
    foreach ($t in $ok) { $res.Add($t); [void]$pend.Remove($t) }
  }
  $res
}

$resumen = @()
foreach ($k in $MOD.Keys) {
  $m = $MOD[$k]; $presentes = @($m.t) + @($m.ctx)
  $fkIncl = @($fks | Where-Object { ($presentes -contains $_.Hija) -and ($presentes -contains $_.Padre) -and (($m.t -contains $_.Hija) -or ($m.t -contains $_.Padre)) })
  $fkQuit = @($fks | Where-Object { ($presentes -contains $_.Hija) -and -not ($fkIncl -contains $_) })
  $sb = New-Object Text.StringBuilder
  $L = { param($s) [void]$sb.AppendLine($s) }
  & $L '-- ============================================================================'
  & $L '-- Archivo preparado para importación de modelo. No ejecutar sobre ECOACCESS_BD.'
  & $L '-- ============================================================================'
  & $L "-- EcoAccess · Base de Datos (profesor Amílkar) · Universidad Popular del Cesar"
  & $L "-- Módulo: $($m.n)   (subvista del DMD: «$($m.sub)»)"
  & $L "-- Origen: 03_DDL_MODELO_COMPLETO/DDL_FINAL_EXTRAIDO.sql (DBMS_METADATA, Oracle FREEPDB1)."
  & $L "-- Uso: SQL Developer → Archivo → Data Modeler → Importar → Archivo DDL (ver 07_GUIA_IMPORTAR_DDL)."
  & $L "-- Solo contiene CREATE TABLE, CREATE INDEX y COMMENT: no tiene DROP, DELETE, UPDATE ni INSERT."
  & $L '--'
  & $L "-- Tablas del módulo ($($m.t.Count)): $($m.t -join ', ')"
  if ($m.ctx.Count) { & $L "-- Tablas de contexto de otros módulos ($($m.ctx.Count)): $($m.ctx -join ', ')" } else { & $L '-- Tablas de contexto: ninguna (el módulo no tiene FK hacia otros módulos ni otros módulos hacia él dentro de su subvista).' }
  & $L "-- FK incluidas ($($fkIncl.Count)):"
  foreach ($f in $fkIncl) { $c = if (($m.t -contains $f.Hija) -and ($m.t -contains $f.Padre)) { 'interna' } else { 'con otro módulo' }; & $L ("--   {0,-28} {1}({2}) -> {3}   [{4}]" -f $f.Nombre, $f.Hija, $f.Columnas, $f.Padre, $c) }
  if ($fkQuit.Count) {
    & $L "-- FK de las tablas de contexto que NO se incluyen ($($fkQuit.Count)), porque su tabla padre no está en este archivo:"
    foreach ($f in $fkQuit) { & $L ("--   {0,-28} {1} -> {2}  (ver el módulo de {1} o el DDL completo)" -f $f.Nombre, $f.Hija, $f.Padre) }
  }
  & $L '-- ============================================================================'
  & $L ''
  foreach ($t in (Ordenar $presentes)) {
    $bloque = $tablas[$t]
    if ($m.ctx -contains $t) {
      foreach ($f in $fkQuit | Where-Object { $_.Hija -eq $t }) { $bloque = $bloque.Replace($f.Texto, '') }
      & $L "-- ---- Tabla de CONTEXTO (otro módulo): $t ----"
    } else { & $L "-- ---- Tabla del módulo: $t ----" }
    & $L $bloque; & $L ''
  }
  $idx = @($indices | Where-Object { $m.t -contains $_.Tabla })
  if ($idx.Count) { & $L '-- ---- Índices de las tablas del módulo ----'; foreach ($i in $idx) { & $L $i.Texto; & $L '' } }
  $com = @($comentarios | Where-Object { $m.t -contains $_.Tabla })
  & $L '-- ---- Comentarios (diccionario) de las tablas del módulo ----'; foreach ($c in $com) { & $L $c.Texto }
  $dest = Join-Path $base "04_DDL_POR_MODULOS\$k"; New-Item -ItemType Directory -Force $dest | Out-Null
  $arch = Join-Path $dest "DDL_$k.sql"
  [IO.File]::WriteAllText($arch, ($sb.ToString() -replace "\r?\n", "`r`n"), $utf8)
  # comprobación: lo que quedó en el archivo
  $txt = [IO.File]::ReadAllText($arch)
  $resumen += [pscustomobject]@{ Modulo = $k; TablasModulo = $m.t.Count; TablasContexto = @($m.ctx).Count; CreateTable = ([regex]::Matches($txt, 'CREATE TABLE')).Count; FK = ([regex]::Matches($txt, 'FOREIGN KEY')).Count; FKesperadas = $fkIncl.Count; FKquitadas = $fkQuit.Count; Indices = $idx.Count; Comentarios = $com.Count; Peligrosas = ([regex]::Matches($txt, '(?im)^\s*(DROP|DELETE|UPDATE|INSERT|TRUNCATE)\b')).Count }
}
$resumen | Format-Table -AutoSize | Out-String -Width 200
$resumen | Export-Csv (Join-Path $base '09_VERIFICACIONES\resumen_ddl_por_modulos.csv') -Encoding UTF8

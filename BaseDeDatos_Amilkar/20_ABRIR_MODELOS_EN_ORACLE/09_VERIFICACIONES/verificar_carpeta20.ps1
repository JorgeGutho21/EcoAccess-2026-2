<#
  Verificación automática de 20_ABRIR_MODELOS_EN_ORACLE (solo lectura; no toca Oracle).
  Comprueba:
    1. El DMD completo: 19 tablas, 173 columnas, 38 FK, 5 subvistas con sus nombres exactos,
       y que el archivo .dmd tenga su carpeta acompañante.
    2. Los 5 DMD por módulo: tablas, columnas y FK esperadas.
    3. Los DDL: el completo es idéntico a 04_SCRIPTS_ORACLE/DDL_FINAL_EXTRAIDO.sql (si existe) y
       los 5 por módulo llevan la advertencia y no contienen DROP/DELETE/UPDATE/INSERT/TRUNCATE.
    4. Enlaces relativos de todos los .md de la carpeta.
  Uso: pwsh -File verificar_carpeta20.ps1   (funciona también en una copia extraída del ZIP)
#>
$ErrorActionPreference = 'Stop'
$base = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$res = New-Object Collections.Generic.List[object]
function Anotar($id, $que, $ok, $det) { $res.Add([pscustomobject]@{ Id = $id; Comprobacion = $que; Resultado = $(if ($ok) { 'PASA' } else { 'FALLA' }); Detalle = $det }) }

function Contar($dmd) {
  $carpeta = Join-Path (Split-Path $dmd) ([IO.Path]::GetFileNameWithoutExtension($dmd))
  if (-not (Test-Path $carpeta)) { return $null }
  $rel = Get-ChildItem (Join-Path $carpeta 'rel') -Directory | Select-Object -First 1
  $tablas = @(Get-ChildItem (Join-Path $rel.FullName 'table\seg_0') -Filter *.xml)
  $cols = 0; $nombres = @()
  foreach ($t in $tablas) { $x = [xml](Get-Content $t.FullName -Raw -Encoding UTF8); $cols += $x.SelectNodes('//Column').Count; $nombres += $x.DocumentElement.name }
  $fk = @(Get-ChildItem (Join-Path $rel.FullName 'foreignkey\seg_0') -Filter *.xml).Count
  $sub = @()
  $diag = Join-Path $rel.FullName 'Diagrams.local'
  if (Test-Path $diag) { $sub = @([regex]::Matches((Get-Content $diag -Raw -Encoding UTF8), 'objectType="sub_view"[^>]*name="([^"]+)"') | ForEach-Object { $_.Groups[1].Value } | Sort-Object) }
  [pscustomobject]@{ Tablas = $tablas.Count; Columnas = $cols; FK = $fk; Subvistas = $sub; Nombres = ($nombres | Sort-Object) }
}

# 1. DMD completo
$dmd = Join-Path $base '01_DMD_MODELO_COMPLETO\EcoAccess_BD.dmd'
$c = Contar $dmd
Anotar 'C01' 'El DMD completo tiene su carpeta acompañante EcoAccess_BD' ($null -ne $c) $dmd
if ($c) {
  Anotar 'C02' 'DMD completo: 19 tablas' ($c.Tablas -eq 19) "$($c.Tablas) tablas"
  Anotar 'C03' 'DMD completo: 173 columnas (atributos)' ($c.Columnas -eq 173) "$($c.Columnas) columnas"
  Anotar 'C04' 'DMD completo: 38 claves foráneas' ($c.FK -eq 38) "$($c.FK) FK"
  $esperadas = 'M1 Catálogos base', 'M2 Usuarios e identidad', 'M3 Infraestructura y QR', 'M4 Operación del sistema', 'M5 EcoPuntos y auditoría'
  Anotar 'C05' 'DMD completo: 5 subvistas con sus nombres exactos' ((@(Compare-Object $esperadas $c.Subvistas)).Count -eq 0) ($c.Subvistas -join ' | ')
}
# 2. DMD por módulo
$mods = [ordered]@{ M1_CATALOGOS = @(7, 81, 10); M2_USUARIOS = @(5, 26, 4); M3_INFRAESTRUCTURA = @(4, 23, 3); M4_OPERACION = @(10, 106, 20); M5_ECOPUNTOS_AUDITORIA = @(8, 86, 10) }
$i = 6
foreach ($m in $mods.Keys) {
  $f = Get-ChildItem (Join-Path $base "02_DMD_MODELOS_INDIVIDUALES\$m") -Filter *.dmd | Select-Object -First 1
  if (-not $f) { Anotar ('C{0:00}' -f $i) "DMD de $m existe" $false 'no hay .dmd'; $i++; continue }
  $x = Contar $f.FullName; $e = $mods[$m]
  Anotar ('C{0:00}' -f $i) "DMD independiente $m ($($f.Name)): tablas, columnas y FK" ($x -and $x.Tablas -eq $e[0] -and $x.Columnas -eq $e[1] -and $x.FK -eq $e[2]) $(if ($x) { "$($x.Tablas) tablas, $($x.Columnas) columnas, $($x.FK) FK (esperado $($e -join '/'))" } else { 'sin carpeta acompañante' })
  $i++
}
# 3. DDL
$ddl = Join-Path $base '03_DDL_MODELO_COMPLETO\DDL_FINAL_EXTRAIDO.sql'
$orig = Join-Path (Split-Path $base) '04_SCRIPTS_ORACLE\DDL_FINAL_EXTRAIDO.sql'
if (Test-Path $orig) { Anotar 'C11' 'DDL completo idéntico al de 04_SCRIPTS_ORACLE' ((Get-FileHash $ddl).Hash -eq (Get-FileHash $orig).Hash) (Get-FileHash $ddl).Hash.Substring(0, 16) }
else { Anotar 'C11' 'DDL completo presente' (Test-Path $ddl) '(copia extraída: no se compara con 04)' }
$t = Get-Content $ddl -Raw -Encoding UTF8
Anotar 'C12' 'DDL completo: 19 CREATE TABLE y 38 FOREIGN KEY' (([regex]::Matches($t, '(?m)^\s*CREATE TABLE')).Count -eq 19 -and ([regex]::Matches($t, 'FOREIGN KEY')).Count -eq 38) ''
$j = 13
foreach ($m in $mods.Keys) {
  $f = Join-Path $base "04_DDL_POR_MODULOS\$m\DDL_$m.sql"; $x = Get-Content $f -Raw -Encoding UTF8
  $okAviso = $x.Contains('Archivo preparado para importación de modelo. No ejecutar sobre ECOACCESS_BD.')
  $peligro = ([regex]::Matches($x, '(?im)^\s*(DROP|DELETE|UPDATE|INSERT|TRUNCATE|ALTER)\b')).Count
  Anotar ('C{0:00}' -f $j) "DDL de $m con advertencia y sin sentencias destructivas" ($okAviso -and $peligro -eq 0) "FK: $(([regex]::Matches($x, 'FOREIGN KEY')).Count) · peligrosas: $peligro"
  $j++
}
# 4. Enlaces relativos de los .md
$rotos = @()
foreach ($md in Get-ChildItem $base -Recurse -Filter *.md) {
  foreach ($l in [regex]::Matches((Get-Content $md.FullName -Raw -Encoding UTF8), '\]\(([^)#]+?)(#[^)]*)?\)')) {
    $u = $l.Groups[1].Value; if ($u -match '^(https?|mailto):') { continue }
    $p = Join-Path $md.DirectoryName ([uri]::UnescapeDataString($u) -replace '/', '\')
    if (-not (Test-Path $p)) { $rotos += "$($md.Name) -> $u" }
  }
}
Anotar 'C18' 'Enlaces relativos de los .md de la carpeta 20' ($rotos.Count -eq 0) $(if ($rotos) { $rotos -join ' ; ' } else { 'todos existen' })

$res | Format-Table -AutoSize -Wrap | Out-String -Width 220
"Resultado: $(@($res | Where-Object Resultado -eq 'PASA').Count) de $($res.Count) comprobaciones PASAN."

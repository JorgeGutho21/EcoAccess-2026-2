<#
  Diccionario AMPLIADO de EcoAccess BD (173 atributos, 19 tablas, 38 FK).
  Combina:
    metadatos.txt                    columnas reales de Oracle (tipo, NULL, default, PK/FK/UK/CHECK)
    fuente_diccionario.txt           ejemplo, quién lo registra, dónde se usa, si es NULL, por qué existe
    ampliado\M*.txt                  explicación ampliada de cada tabla (T|) y de cada atributo (C|)
    ampliado\restricciones_oracle.txt  FK, PK/UNIQUE y CHECK extraídos de USER_CONSTRAINTS
    ampliado\relaciones.txt          explicación de cada una de las 38 FK
  Salidas en ampliado\salida\ (fragmentos HTML para Word) y el registro de verificación.
  Se detiene si falta o sobra algún atributo, tabla o FK.
  Uso:  pwsh -File 03_DICCIONARIO_DE_DATOS\generar_diccionario_ampliado.ps1
#>
$ErrorActionPreference = 'Stop'
$aqui = Split-Path -Parent $MyInvocation.MyCommand.Path
$amp = Join-Path $aqui 'ampliado'; $sal = Join-Path $amp 'salida'
New-Item -ItemType Directory -Force $sal | Out-Null
$utf8 = New-Object Text.UTF8Encoding $false
function Esc([string]$t) { [Net.WebUtility]::HtmlEncode($t) }

$modulos = [ordered]@{
  'Módulo 1 · Catálogos base' = @('MATERIAL','PESO_REFERENCIAL')
  'Módulo 2 · Usuarios e identidad' = @('ROL','USUARIO','USUARIO_ROL','CREDENCIAL_ACCESO','VINCULO_TELEGRAM')
  'Módulo 3 · Infraestructura y QR' = @('PUNTO_INSTALACION','CONTENEDOR','CONTENEDOR_MATERIAL')
  'Módulo 4 · Operación (QR → foto → orden → evento → depósito)' = @('INTERACCION','CONSULTA_VISION','ORDEN_APERTURA','EVENTO_DISPOSITIVO','DEPOSITO')
  'Módulo 5 · EcoPuntos y auditoría' = @('REGLA_ECOPUNTOS','MOVIMIENTO_ECOPUNTOS','AUDITORIA_DEPOSITO','HISTORIAL_ESTADO_CONTENEDOR')
}
$esperado = @{ MATERIAL=6; PESO_REFERENCIAL=9; ROL=3; USUARIO=8; USUARIO_ROL=3; CREDENCIAL_ACCESO=7; VINCULO_TELEGRAM=5
  PUNTO_INSTALACION=6; CONTENEDOR=8; CONTENEDOR_MATERIAL=3; INTERACCION=15; CONSULTA_VISION=20; ORDEN_APERTURA=8
  EVENTO_DISPOSITIVO=8; DEPOSITO=18; REGLA_ECOPUNTOS=10; MOVIMIENTO_ECOPUNTOS=11; AUDITORIA_DEPOSITO=17; HISTORIAL_ESTADO_CONTENEDOR=8 }

# --- 1. Metadatos reales ---
$meta = @{}
foreach ($l in Get-Content "$aqui\metadatos.txt" -Encoding UTF8) {
  $p = $l -split '\|'
  if ($p[0] -eq 'COL') { $meta["$($p[1]).$($p[2])"] = [pscustomobject]@{ Tabla=$p[1]; Columna=$p[2]; Pos=[int]$p[3]; Tipo=$p[4]; Nulo=$p[5]; Default=$p[6]; Identidad=$p[7]; PK=$p[8]; UK=$p[9]; FK=$p[10]; CK=$p[11] } }
}
$corto = @{}
foreach ($l in Get-Content "$aqui\fuente_diccionario.txt" -Encoding UTF8) {
  if ($l -match '^#' -or -not $l.Trim()) { continue }
  $p = $l -split '\|'
  $corto["$($p[0]).$($p[1])"] = [pscustomobject]@{ Breve=$p[2]; Ejemplo=$p[3]; Registra=$p[4]; Uso=$p[5]; SiNull=$p[6]; Porque=$p[7] }
}
# --- 2. Explicaciones ampliadas ---
$tabTxt = @{}; $colTxt = @{}; $origen = @{}
foreach ($f in Get-ChildItem $amp -Filter 'M*.txt' | Sort-Object Name) {
  foreach ($l in Get-Content $f.FullName -Encoding UTF8) {
    if ($l -match '^T\|([A-Z_]+)\|(.+)$') { if ($tabTxt.ContainsKey($Matches[1])) { throw "Tabla repetida: $($Matches[1])" }; $tabTxt[$Matches[1]] = $Matches[2] }
    elseif ($l -match '^C\|([A-Z_]+)\|([A-Z_0-9]+)\|(.+)$') { $k = "$($Matches[1]).$($Matches[2])"; if ($colTxt.ContainsKey($k)) { throw "Atributo repetido: $k" }; $colTxt[$k] = $Matches[3].Trim(); $origen[$k] = $f.Name }
  }
}
$faltan = @($meta.Keys | Where-Object { -not $colTxt.ContainsKey($_) }); $sobran = @($colTxt.Keys | Where-Object { -not $meta.ContainsKey($_) })
if ($faltan -or $sobran) { throw "Atributos sin explicación: $($faltan -join ', ') | explicaciones de columnas inexistentes: $($sobran -join ', ')" }
$faltaT = @($esperado.Keys | Where-Object { -not $tabTxt.ContainsKey($_) }); if ($faltaT) { throw "Tablas sin explicación: $($faltaT -join ', ')" }

# --- 3. Restricciones de Oracle ---
$fks = @(); $keys = @(); $ckn = @{}; $uix = @{}
foreach ($l in Get-Content "$amp\restricciones_oracle.txt" -Encoding UTF8) {
  $p = $l -split '\|'
  switch ($p[0]) {
    'FK'  { $fks += [pscustomobject]@{ Nombre=$p[1]; Tabla=$p[2]; Cols=$p[3]; RTabla=$p[4]; RClave=$p[5]; RTipo=$p[6]; RCols=$p[7]; Nula=$p[8]; Unica=$p[9] } }
    'KEY' { $keys += [pscustomobject]@{ Tabla=$p[1]; Nombre=$p[2]; Tipo=$p[3]; Cols=$p[4] } }
    'CKN' { $ckn[$p[1]] = [int]$p[2] }
    'UIX' { if (-not $uix[$p[1]]) { $uix[$p[1]] = @() }; $uix[$p[1]] += $p[2] }
  }
}
$relTxt = @{}
foreach ($l in Get-Content "$amp\relaciones.txt" -Encoding UTF8) { if ($l -match '^R\|([A-Z_]+)\|(.+)$') { $relTxt[$Matches[1]] = $Matches[2] } }
if ($fks.Count -ne 38) { throw "Se esperaban 38 FK y Oracle tiene $($fks.Count)" }
$faltaR = @($fks | Where-Object { -not $relTxt.ContainsKey($_.Nombre) } | ForEach-Object Nombre); if ($faltaR) { throw "FK sin explicación: $($faltaR -join ', ')" }

function Cardinalidad($fk) {
  $padre = if ($fk.Unica) { 'uno a cero o uno (1:0..1)' } else { 'uno a muchos (1:N)' }
  $hijo  = if ($fk.Nula -eq 'Y') { 'opcional (la columna admite NULL)' } else { 'obligatoria (NOT NULL)' }
  [pscustomobject]@{ Texto = $padre; Opc = $hijo; Linea = $(if ($fk.Nula -eq 'Y') { 'discontinua (FK opcional)' } else { 'continua (FK obligatoria)' }) }
}
function Restr($m) {
  $r = @(); if ($m.PK) { $r += 'PK' }; if ($m.FK) { $r += "FK: $($m.FK)" }; if ($m.UK) { $r += "UNIQUE: $($m.UK)" }
  if ($m.CK) { $r += ($m.CK -split ', ' | ForEach-Object { "CHECK $_" }) }; if ($m.Identidad) { $r += $m.Identidad }
  if ($r) { $r -join '; ' } else { '—' }
}
function Palabras([string]$t) { (($t -replace '<[^>]+>', ' ') -split '\s+' | Where-Object { $_ -match '\w' }).Count }
function Oraciones([string]$t) { ([regex]::Matches(($t -replace '\d,\d', '0'), '[\.!?](\s|$)')).Count }

# --- 4. Fragmentos HTML ---
$hTab = New-Object Text.StringBuilder; $hCol = New-Object Text.StringBuilder; $hAnx = New-Object Text.StringBuilder
$verif = New-Object Collections.Generic.List[object]; $n = 0
foreach ($mod in $modulos.Keys) {
  $colsMod = ($modulos[$mod] | ForEach-Object { $esperado[$_] } | Measure-Object -Sum).Sum
  foreach ($sb in $hTab, $hCol, $hAnx) { [void]$sb.AppendLine("<h2>$(Esc $mod) — $($modulos[$mod].Count) tablas, $colsMod atributos</h2>") }
  foreach ($t in $modulos[$mod]) {
    $cols = @($meta.Values | Where-Object Tabla -eq $t | Sort-Object Pos)
    $pk = $keys | Where-Object { $_.Tabla -eq $t -and $_.Tipo -eq 'P' }
    $uks = @($keys | Where-Object { $_.Tabla -eq $t -and $_.Tipo -eq 'U' })
    $sal_ = @($fks | Where-Object Tabla -eq $t); $ent = @($fks | Where-Object RTabla -eq $t)
    $tec = New-Object Text.StringBuilder
    [void]$tec.Append("<div class=""tec""><p><b>Ficha técnica (leída de Oracle).</b> Atributos: $($cols.Count) · Llave primaria: <b>$($pk.Nombre)</b> ($($pk.Cols -replace ',', ', ')$(if ($pk.Cols -match ',') { ' — PK compuesta' }))</p>")
    $lsal = foreach ($x in $sal_) { $c = Cardinalidad $x; $comp = if ($x.Cols -match ',') { ' [compuesta]' } else { '' }; $op = $c.Opc -replace ' \(.*', ''; '{0} ({1}) → {2}{3}, {4}' -f $x.Nombre, ($x.Cols -replace ',', ', '), $x.RTabla, $comp, $op }
    $lent = foreach ($x in $ent) { '{0}.{1} ({2})' -f $x.Tabla, ($x.Cols -replace ',', '+'), $x.Nombre }
    if ($sal_) { [void]$tec.Append('<p><b>FK que salen de esta tabla (' + $sal_.Count + '):</b> ' + ($lsal -join '; ') + '</p>') } else { [void]$tec.Append('<p><b>FK que salen de esta tabla:</b> ninguna.</p>') }
    if ($ent) { [void]$tec.Append('<p><b>FK que llegan a esta tabla (' + $ent.Count + '):</b> ' + ($lent -join '; ') + '</p>') } else { [void]$tec.Append('<p><b>FK que llegan a esta tabla:</b> ninguna.</p>') }
    $ukTxt = if ($uks) { ($uks | ForEach-Object { "$($_.Nombre) ($($_.Cols -replace ',', ', '))" }) -join '; ' } else { 'ninguna' }
    $uixTxt = if ($uix[$t]) { ' · Índices únicos basados en función: ' + ($uix[$t] -join ', ') } else { '' }
    [void]$tec.Append("<p><b>Claves únicas:</b> $ukTxt$uixTxt · <b>CHECK con nombre:</b> $(if ($ckn[$t]) { $ckn[$t] } else { 0 })</p></div>")
    $intro = "<h3>$t ($($cols.Count) atributos)</h3>" + $tabTxt[$t] + $tec.ToString()
    [void]$hTab.AppendLine($intro)
    [void]$hAnx.AppendLine($intro)
    [void]$hCol.AppendLine("<h3>$t ($($cols.Count) atributos)</h3>")
    $docOk = 0
    foreach ($c in $cols) {
      $n++; $k = "$t.$($c.Columna)"; $f = $corto[$k]; $s = $colTxt[$k]
      $ob = if ($c.Nulo -eq 'N') { 'Sí (NOT NULL)' } else { 'No (admite NULL)' }
      $def = if ($c.Default) { $c.Default } else { '—' }
      $ficha = "<table class=""fichaa""><tr><th colspan=""2"">$n. $t.$($c.Columna)</th></tr>" +
        "<tr><td class=""fk"">Tipo · Obligatorio · Default</td><td>$(Esc $c.Tipo) · $ob · $(Esc $def)</td></tr>" +
        "<tr><td class=""fk"">Restricciones</td><td>$(Esc (Restr $c))</td></tr>" +
        "<tr><td class=""fk"">Significado</td><td class=""sig"">$(Esc $s)</td></tr>" +
        "<tr><td class=""fk"">Ejemplo</td><td>$(Esc $f.Ejemplo)</td></tr>" +
        "<tr><td class=""fk"">Quién lo registra</td><td>$(Esc $f.Registra)</td></tr>" +
        "<tr><td class=""fk"">Dónde se usa</td><td>$(Esc $f.Uso)</td></tr>" +
        "<tr><td class=""fk"">Si es NULL</td><td>$(Esc $f.SiNull)</td></tr>" +
        "<tr><td class=""fk"">Por qué existe</td><td>$(Esc $f.Porque)</td></tr></table>"
      [void]$hCol.AppendLine($ficha); [void]$hAnx.AppendLine($ficha)
      $pal = Palabras $s; $ora = Oraciones $s
      $okC = ($pal -ge 90 -and $ora -ge 5); if ($okC) { $docOk++ }
      $verif.Add([pscustomobject]@{ N=$n; Tabla=$t; Columna=$c.Columna; Tipo=$c.Tipo; Nulo=$c.Nulo; PK=[bool]$c.PK; FK=$c.FK; Palabras=$pal; Oraciones=$ora; Ok=$okC; Mod=$mod })
    }
  }
}

# --- 5. Relaciones (38 FK) ---
$hRel = New-Object Text.StringBuilder
[void]$hRel.AppendLine('<p>El <b>MER conceptual</b> tiene <b>15 entidades y 19 relaciones</b>: describe el negocio y no incluye las dos tablas puente (USUARIO_ROL y CONTENEDOR_MATERIAL, que en el MER son relaciones N:M) ni las dos tablas de auditoría (que no son entidades del negocio, sino registros que producen los disparadores). El <b>esquema físico</b> tiene <b>38 restricciones FOREIGN KEY</b>: es la implementación en Oracle. Los números no coinciden porque miden cosas distintas: una relación N:M del MER se convierte en dos FK (una por lado de la tabla puente), una entidad puede relacionarse dos veces con la misma tabla con papeles distintos (por ejemplo, INTERACCION tiene ciudadano y operario, ambos USUARIO), y las tablas de auditoría agregan FK que no existen en el MER. Por eso no deben sumarse ni compararse uno a uno.</p>')
[void]$hRel.AppendLine('<p>Cómo leer cada ficha: la <b>cardinalidad</b> se calculó con las restricciones reales: si las columnas de la FK forman también una PK o UNIQUE en la tabla hija, la relación es <b>1:0..1</b>; si no, es <b>1:N</b>. La FK es <b>opcional</b> si alguna de sus columnas admite NULL; en Data Modeler se dibuja con línea <b>discontinua</b>, y una FK obligatoria con línea <b>continua</b>. Si se intenta registrar una referencia inexistente, Oracle responde <code>ORA-02291: restricción de integridad violada - clave principal no encontrada</code>; si se intenta borrar un padre que tiene hijos, responde <code>ORA-02292</code>, porque ninguna FK usa ON DELETE CASCADE. Una FK compuesta cuenta como <b>una sola</b> restricción aunque tenga dos columnas: por eso son 38 FK y no más.</p>')
$compuestas = @($fks | Where-Object { $_.Cols -match ',' })
[void]$hRel.AppendLine("<p>Resumen: 38 FK = $($fks.Count - $compuestas.Count) simples + $($compuestas.Count) compuestas; $(@($fks | Where-Object Nula -eq 'N').Count) obligatorias y $(@($fks | Where-Object Nula -eq 'Y').Count) opcionales; $(@($fks | Where-Object Unica).Count) de tipo 1:0..1 y $(@($fks | Where-Object { -not $_.Unica }).Count) de tipo 1:N.</p>")
$i = 0
foreach ($mod in $modulos.Keys) {
  [void]$hRel.AppendLine("<h3>FK de las tablas del $(Esc ($mod -replace ' ·.*', ''))</h3>")
  foreach ($t in $modulos[$mod]) {
    foreach ($fk in ($fks | Where-Object Tabla -eq $t)) {
      $i++; $c = Cardinalidad $fk
      $tipoRef = if ($fk.RTipo -eq 'P') { 'clave primaria' } else { 'clave única' }
      [void]$hRel.AppendLine("<table class=""fichaa""><tr><th colspan=""2"">R$i. $($fk.Nombre): $($fk.Tabla) → $($fk.RTabla)</th></tr>" +
        "<tr><td class=""fk"">Tabla de origen (hija) · columnas</td><td>$($fk.Tabla) ($($fk.Cols -replace ',', ', '))</td></tr>" +
        "<tr><td class=""fk"">Tabla referenciada (padre) · clave</td><td>$($fk.RTabla) ($($fk.RCols -replace ',', ', ')) — $tipoRef $($fk.RClave)</td></tr>" +
        "<tr><td class=""fk"">Tipo · obligatoriedad</td><td>$(if ($fk.Cols -match ',') { 'FK compuesta' } else { 'FK simple' }) · $($c.Opc)</td></tr>" +
        "<tr><td class=""fk"">Cardinalidad</td><td>$($fk.RTabla) $($c.Texto) $($fk.Tabla)$(if ($fk.Unica) { " (las columnas hijas también son $($fk.Unica))" })</td></tr>" +
        "<tr><td class=""fk"">Por qué existe · ejemplo</td><td class=""sig"">$(Esc $relTxt[$fk.Nombre])</td></tr>" +
        "<tr><td class=""fk"">Referencia inexistente</td><td>ORA-02291 (Oracle rechaza la fila)$(if ($fk.Nula -eq 'Y') { '; si alguna columna es NULL, Oracle no evalúa la FK' })</td></tr>" +
        "<tr><td class=""fk"">En Data Modeler</td><td>Línea $($c.Linea) entre $($fk.Tabla) (lado «muchos») y $($fk.RTabla) (lado «uno»)</td></tr></table>")
    }
  }
}

# --- 6. Llaves primarias ---
$hPk = New-Object Text.StringBuilder
[void]$hPk.AppendLine('<table class="t"><tr><th>#</th><th>Tabla</th><th>PK</th><th>Columnas</th><th>Tipo</th><th>Cómo se genera</th></tr>')
$j = 0
foreach ($mod in $modulos.Keys) { foreach ($t in $modulos[$mod]) {
  $j++; $pk = $keys | Where-Object { $_.Tabla -eq $t -and $_.Tipo -eq 'P' }
  $c1 = $meta["$t.$(($pk.Cols -split ',')[0])"]
  $gen = if ($pk.Cols -match ',') { 'Las escribe la aplicación (son FK); la combinación no se repite' } elseif ($c1.Identidad) { $c1.Identidad } else { 'Es a la vez FK hacia USUARIO (relación 1:1)' }
  [void]$hPk.AppendLine("<tr><td>$j</td><td>$t</td><td>$($pk.Nombre)</td><td>$($pk.Cols -replace ',', ', ')</td><td>$(if ($pk.Cols -match ',') { '<b>Compuesta</b>' } else { 'Simple' })</td><td>$(Esc $gen)</td></tr>")
} }
[void]$hPk.AppendLine('</table>')

[IO.File]::WriteAllText("$sal\tablas_19.html", $hTab.ToString(), $utf8)
[IO.File]::WriteAllText("$sal\atributos_173.html", $hCol.ToString(), $utf8)
[IO.File]::WriteAllText("$sal\A_anexo_diccionario_ampliado.html", $hAnx.ToString(), $utf8)
[IO.File]::WriteAllText("$sal\relaciones_38.html", $hRel.ToString(), $utf8)
[IO.File]::WriteAllText("$sal\pk_19.html", $hPk.ToString(), $utf8)

# --- 7. Registro de verificación ---
$md = New-Object Text.StringBuilder
$tot = $verif.Count; $ok = @($verif | Where-Object Ok).Count
[void]$md.AppendLine('# EcoAccess — Verificación del diccionario ampliado (173 atributos)')
[void]$md.AppendLine('')
[void]$md.AppendLine("Generado el $(Get-Date -Format 'yyyy-MM-dd HH:mm') por ``03_DICCIONARIO_DE_DATOS/generar_diccionario_ampliado.ps1``.")
[void]$md.AppendLine('')
[void]$md.AppendLine('## Cómo se comprobó')
[void]$md.AppendLine('')
[void]$md.AppendLine('- Las columnas, tipos, NULL, DEFAULT, PK, FK, UNIQUE y CHECK salen de los metadatos reales de Oracle (`metadatos.txt` y `ampliado/restricciones_oracle.txt`, extraídos de `USER_TAB_COLUMNS` y `USER_CONSTRAINTS` del esquema `ECOACCESS_BD`). No se escribieron a mano.')
[void]$md.AppendLine('- El script se detiene si una columna real no tiene explicación, si se explica una columna que no existe, si falta la explicación de una tabla o si alguna de las 38 FK no está explicada.')
[void]$md.AppendLine('- Criterio de extensión de «Significado»: al menos 90 palabras y al menos 5 oraciones.')
[void]$md.AppendLine('- Las cifras de ejemplo (21 materiales, 105 depósitos, 48 movimientos, 91 auditorías, etc.) se consultaron en Oracle el 13/11/2026 y son **datos sintéticos**.')
[void]$md.AppendLine('- El esquema de prácticas `ECOACCESS_PRACTICAS` y la base de Programación III no forman parte de las 19 tablas.')
[void]$md.AppendLine('')
[void]$md.AppendLine("**Resultado: $tot atributos documentados de 173 esperados; $ok cumplen el criterio de extensión; $($fks.Count) FK explicadas de 38; $($tabTxt.Count) tablas explicadas de 19.**")
[void]$md.AppendLine('')
[void]$md.AppendLine('## Matriz de comprobación por tabla')
[void]$md.AppendLine('')
[void]$md.AppendLine('| Módulo | Tabla | Esperados | En Oracle | Documentados | Cumplen extensión | Palabras de la explicación de la tabla | Resultado |')
[void]$md.AppendLine('|---|---|---:|---:|---:|---:|---:|---|')
foreach ($mod in $modulos.Keys) { foreach ($t in $modulos[$mod]) {
  $v = @($verif | Where-Object Tabla -eq $t); $vo = @($v | Where-Object Ok).Count
  $real = @($meta.Values | Where-Object Tabla -eq $t).Count
  $res = if ($v.Count -eq $esperado[$t] -and $real -eq $esperado[$t] -and $vo -eq $v.Count) { 'CORRECTO' } else { 'REVISAR' }
  [void]$md.AppendLine("| $($mod -replace ' ·.*', '') | $t | $($esperado[$t]) | $real | $($v.Count) | $vo | $(Palabras $tabTxt[$t]) | $res |")
} }
[void]$md.AppendLine("| **Total** | **19 tablas** | **173** | **$($meta.Count)** | **$tot** | **$ok** | | $(if ($tot -eq 173 -and $ok -eq 173) { '**CORRECTO**' } else { '**REVISAR**' }) |")
[void]$md.AppendLine('')
[void]$md.AppendLine('## Los 173 atributos, uno por uno')
[void]$md.AppendLine('')
[void]$md.AppendLine('| # | Tabla | Atributo | Tipo (Oracle) | NULL | PK | FK | Palabras | Oraciones | Extensión |')
[void]$md.AppendLine('|---:|---|---|---|---|---|---|---:|---:|---|')
foreach ($v in $verif) { [void]$md.AppendLine("| $($v.N) | $($v.Tabla) | $($v.Columna) | $($v.Tipo) | $(if ($v.Nulo -eq 'Y') { 'Sí' } else { 'No' }) | $(if ($v.PK) { 'PK' } else { '' }) | $($v.FK -replace '\|', '/') | $($v.Palabras) | $($v.Oraciones) | $(if ($v.Ok) { 'OK' } else { 'CORTA' }) |") }
[void]$md.AppendLine('')
[void]$md.AppendLine('## Las 38 claves foráneas')
[void]$md.AppendLine('')
[void]$md.AppendLine('| # | FK | Origen (columnas) | Destino (clave) | Simple/compuesta | Obligatoria | Cardinalidad |')
[void]$md.AppendLine('|---:|---|---|---|---|---|---|')
$i = 0; foreach ($fk in $fks) { $i++; [void]$md.AppendLine("| $i | $($fk.Nombre) | $($fk.Tabla) ($($fk.Cols)) | $($fk.RTabla) ($($fk.RClave)) | $(if ($fk.Cols -match ',') { 'Compuesta' } else { 'Simple' }) | $(if ($fk.Nula -eq 'N') { 'Sí' } else { 'No' }) | $(if ($fk.Unica) { '1:0..1' } else { '1:N' }) |") }
[IO.File]::WriteAllText("$amp\verificacion_generada.md", $md.ToString(), $utf8)
$verif | Select-Object N, Tabla, Columna, Tipo, @{n='Admite_NULL';e={ if ($_.Nulo -eq 'Y') { 'Sí' } else { 'No' } }}, @{n='PK';e={ if ($_.PK) { 'Sí' } else { '' } }}, @{n='FK';e={ $_.FK }}, Palabras, Oraciones, @{n='Cumple_extension';e={ if ($_.Ok) { 'Sí' } else { 'No' } }} | Export-Csv (Join-Path $aqui 'MATRIZ_ATRIBUTOS_vs_ORACLE.csv') -Delimiter ';' -Encoding utf8BOM

"Atributos: $tot (cumplen extensión: $ok) · Tablas: $($tabTxt.Count) · FK: $($fks.Count) ($($compuestas.Count) compuestas)"
$verif | Where-Object { -not $_.Ok } | ForEach-Object { "CORTA: $($_.Tabla).$($_.Columna) — $($_.Palabras) palabras, $($_.Oraciones) oraciones" }
$esperado.Keys | Sort-Object | ForEach-Object { $p = Palabras $tabTxt[$_]; if ($p -lt 200 -or $p -gt 400) { "TABLA $_ : $p palabras" } }

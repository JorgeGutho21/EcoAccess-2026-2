<#
  Genera el diccionario de datos de EcoAccess BD combinando:
    1) metadatos REALES de Oracle (metadatos.txt, salida de extraer_metadatos.sql)
    2) la explicación humana de cada columna (fuente_diccionario.txt)
  Salidas: DICCIONARIO_DATOS.md, diccionario.csv, diccionario.html (fragmento)
           y ../04_SCRIPTS_ORACLE/15_comentarios_diccionario.sql (COMMENT ON para Oracle).
  Se detiene si alguna columna real no tiene explicación o si sobra alguna.
  Uso:  powershell -File generar_diccionario.ps1
#>
$ErrorActionPreference = 'Stop'
$aqui = Split-Path -Parent $MyInvocation.MyCommand.Path
$utf8 = New-Object Text.UTF8Encoding $false

$modulos = [ordered]@{
  # Los mismos 5 módulos del modelo relacional ordenado (02_MODELOS_DE_DATOS)
  'M1 · Catálogos base' = @('MATERIAL','PESO_REFERENCIAL')
  'M2 · Usuarios e identidad' = @('ROL','USUARIO','USUARIO_ROL','CREDENCIAL_ACCESO','VINCULO_TELEGRAM')
  'M3 · Infraestructura y QR' = @('PUNTO_INSTALACION','CONTENEDOR','CONTENEDOR_MATERIAL')
  'M4 · Operación (QR → foto → orden → evento → depósito)' = @('INTERACCION','CONSULTA_VISION','ORDEN_APERTURA','EVENTO_DISPOSITIVO','DEPOSITO')
  'M5 · EcoPuntos y auditoría' = @('REGLA_ECOPUNTOS','MOVIMIENTO_ECOPUNTOS','AUDITORIA_DEPOSITO','HISTORIAL_ESTADO_CONTENEDOR')
}
$tablas = @{
  ROL = 'Catálogo cerrado de los tres roles de negocio: Ciudadano, Operario y Administrador.'
  USUARIO = 'Cada persona registrada. No guarda contraseñas.'
  USUARIO_ROL = 'Relación muchos a muchos: una persona puede tener varios roles.'
  CREDENCIAL_ACCESO = 'Contraseña de la aplicación de escritorio guardada solo como hash PBKDF2 (relación 1:1 con USUARIO).'
  VINCULO_TELEGRAM = 'Une la cuenta EcoAccess con la cuenta de Telegram; las bajas se conservan.'
  PUNTO_INSTALACION = 'Estación física con un QR compartido para sus tres canecas.'
  CONTENEDOR = 'Caneca de un color (blanca, verde o negra) dentro de una estación.'
  MATERIAL = 'Catálogo validado de materiales; la IA solo puede apuntar a estos códigos.'
  CONTENEDOR_MATERIAL = 'Qué materiales recibe cada caneca (N:M), con el color como garantía de coherencia.'
  PESO_REFERENCIAL = 'Pesos típicos por objeto y estado visual para ESTIMAR (nunca medir) el peso de una foto.'
  INTERACCION = 'Sesión iniciada al escanear el QR (o apertura manual): estación, ciudadano, sugerencia y confirmación.'
  CONSULTA_VISION = 'Cada foto analizada por Eco. Es orientación: NUNCA cuenta como depósito.'
  ORDEN_APERTURA = 'Orden idempotente de abrir una caneca; máximo una vigente por caneca.'
  EVENTO_DISPOSITIVO = 'Lo que informa la caneca (acuse, apertura, detección, cierre, fallo, cierre sin sensor).'
  DEPOSITO = 'Residuo detectado por sensor o declarado sin sensor, y su verificación independiente.'
  REGLA_ECOPUNTOS = 'Tarifa de EcoPuntos por material, versionada en el tiempo.'
  MOVIMIENTO_ECOPUNTOS = 'Libro contable de EcoPuntos: créditos y reversos; el saldo es su suma.'
  AUDITORIA_DEPOSITO = 'Valores :OLD y :NEW de cada verificación, rechazo, corrección o reapertura (trigger 3).'
  HISTORIAL_ESTADO_CONTENEDOR = 'Cada cambio de disponibilidad de una caneca (trigger 1).'
}

# --- 1. Leer metadatos reales ---
$meta = @{}; $checks = @{}
foreach ($l in Get-Content "$aqui\metadatos.txt" -Encoding UTF8) {
  $p = $l -split '\|'
  if ($p[0] -eq 'COL') {
    $meta["$($p[1]).$($p[2])"] = [pscustomobject]@{ Tabla=$p[1]; Columna=$p[2]; Pos=[int]$p[3]; Tipo=$p[4]; Nulo=$p[5];
      Default=$p[6]; Identidad=$p[7]; PK=$p[8]; UK=$p[9]; FK=$p[10]; CK=$p[11] }
  } elseif ($p[0] -eq 'CK') { $checks[$p[2]] = ($p[3..($p.Count-1)] -join '|') }
}
# --- 2. Leer explicaciones ---
$fuente = @{}
foreach ($l in Get-Content "$aqui\fuente_diccionario.txt" -Encoding UTF8) {
  if ($l -match '^#' -or $l.Trim() -eq '') { continue }
  $p = $l -split '\|'
  $fuente["$($p[0]).$($p[1])"] = [pscustomobject]@{ Significado=$p[2]; Ejemplo=$p[3]; Registra=$p[4]; Uso=$p[5]; SiNull=$p[6]; Porque=$p[7] }
}
$faltan = $meta.Keys | Where-Object { -not $fuente.ContainsKey($_) }
$sobran = $fuente.Keys | Where-Object { -not $meta.ContainsKey($_) }
if ($faltan -or $sobran) { throw "Diccionario incompleto. Faltan: $($faltan -join ', ') | Sobran: $($sobran -join ', ')" }

function Esc([string]$t) { [Net.WebUtility]::HtmlEncode($t) }
function Restr($m) {
  $r = @()
  if ($m.PK) { $r += 'PK' }
  if ($m.FK) { $r += "FK: $($m.FK)" }
  if ($m.UK) { $r += "UNIQUE: $($m.UK)" }
  if ($m.CK) { $r += ($m.CK -split ', ' | ForEach-Object { "CHECK $_" }) }
  if ($m.Identidad) { $r += $m.Identidad }
  ($r -join '; ')
}

# --- 3. Markdown, HTML y CSV ---
$md = New-Object Text.StringBuilder; $html = New-Object Text.StringBuilder
$csv = New-Object Collections.Generic.List[string]
$csv.Add('N;Modulo;Tabla;Columna;Tipo;Obligatoria;Default;Restricciones;Significado;Ejemplo;Quien_registra;Uso;Si_es_NULL;Por_que_existe')
[void]$md.AppendLine('# Diccionario de datos — EcoAccess BD (esquema ECOACCESS_BD)')
[void]$md.AppendLine('')
[void]$md.AppendLine("Generado el $(Get-Date -Format 'yyyy-MM-dd HH:mm') desde los metadatos reales de Oracle (``USER_TAB_COLUMNS``, ``USER_CONSTRAINTS``) más la explicación humana de ``fuente_diccionario.txt``. **$($meta.Count) columnas en 19 tablas, 0 sin explicar.**")
[void]$md.AppendLine('')
[void]$md.AppendLine('Columnas: **Tipo** exacto · **Oblig.** (NOT NULL) · **Default** · **Restricciones** (PK, FK, UNIQUE, CHECK, IDENTITY) · **Significado** · **Ejemplo ficticio** · **Quién lo registra** · **Dónde se usa** · **Si es NULL** · **Por qué existe / qué anomalía previene**.')
$n = 0
foreach ($mod in $modulos.Keys) {
  [void]$md.AppendLine(''); [void]$md.AppendLine("## Módulo: $mod")
  [void]$html.AppendLine("<h2>Módulo: $(Esc $mod)</h2>")
  foreach ($t in $modulos[$mod]) {
    $cols = $meta.Values | Where-Object Tabla -eq $t | Sort-Object Pos
    [void]$md.AppendLine(''); [void]$md.AppendLine("### $t ($($cols.Count) columnas)"); [void]$md.AppendLine(''); [void]$md.AppendLine($tablas[$t]); [void]$md.AppendLine('')
    [void]$md.AppendLine('| # | Columna | Tipo | Oblig. | Default | Restricciones | Significado | Ejemplo | Quién lo registra | Dónde se usa | Si es NULL | Por qué existe |')
    [void]$md.AppendLine('|---|---|---|---|---|---|---|---|---|---|---|---|')
    [void]$html.AppendLine("<h3>$t ($($cols.Count) columnas)</h3><p>$(Esc $tablas[$t])</p>")
    [void]$html.AppendLine('<table class="dic"><tr><th>#</th><th>Columna</th><th>Tipo</th><th>Oblig.</th><th>Default</th><th>Restricciones</th><th>Significado</th><th>Ejemplo</th><th>Quién lo registra</th><th>Dónde se usa</th><th>Si es NULL</th><th>Por qué existe</th></tr>')
    foreach ($c in $cols) {
      $n++; $f = $fuente["$t.$($c.Columna)"]; $ob = if ($c.Nulo -eq 'N') { 'Sí' } else { 'No' }; $re = Restr $c
      $cel = @($c.Pos, "``$($c.Columna)``", $c.Tipo, $ob, $c.Default, $re, $f.Significado, $f.Ejemplo, $f.Registra, $f.Uso, $f.SiNull, $f.Porque) | ForEach-Object { ([string]$_) -replace '\|', '\|' }
      [void]$md.AppendLine('| ' + ($cel -join ' | ') + ' |')
      [void]$html.AppendLine('<tr>' + ((@($c.Pos, $c.Columna, $c.Tipo, $ob, $c.Default, $re, $f.Significado, $f.Ejemplo, $f.Registra, $f.Uso, $f.SiNull, $f.Porque) | ForEach-Object { "<td>$(Esc ([string]$_))</td>" }) -join '') + '</tr>')
      $csv.Add((@($n, $mod, $t, $c.Columna, $c.Tipo, $ob, $c.Default, $re, $f.Significado, $f.Ejemplo, $f.Registra, $f.Uso, $f.SiNull, $f.Porque) | ForEach-Object { '"' + (([string]$_) -replace '"', '""') + '"' }) -join ';')
    }
    [void]$html.AppendLine('</table>')
  }
}
[void]$md.AppendLine(''); [void]$md.AppendLine('## Anexo: texto exacto de cada CHECK con nombre'); [void]$md.AppendLine('')
[void]$md.AppendLine('| Restricción | Condición |'); [void]$md.AppendLine('|---|---|')
foreach ($k in ($checks.Keys | Sort-Object)) { [void]$md.AppendLine("| ``$k`` | ``$($checks[$k] -replace '\|','\|')`` |") }
[IO.File]::WriteAllText("$aqui\DICCIONARIO_DATOS.md", $md.ToString(), $utf8)
[IO.File]::WriteAllText("$aqui\diccionario.html", $html.ToString(), $utf8)
[IO.File]::WriteAllLines("$aqui\diccionario.csv", $csv, (New-Object Text.UTF8Encoding $true))

# --- 3b. FICHA COMPLETA por atributo (diccionario_fichas.html y DICCIONARIO_COMPLETO.md) ---
# Campos derivados de fuentes verificables:
#   * tipo, tamaño, NULL, default, PK/FK/UK/CHECK y tabla referenciada → metadatos.txt (Oracle)
#   * ejemplo inválido → el error real que produce cada restricción
#   * objetos que la leen o escriben → búsqueda en 06_vistas, 07_PL/SQL y 08_triggers
#   * Power BI → aparece o no en las vistas VW_BI_*; C# y Telegram → diagnóstico de P3 (por tabla)
function Bloques([string]$archivo, [string]$patron) {
  $txt = [IO.File]::ReadAllText((Join-Path $aqui "..\04_SCRIPTS_ORACLE\$archivo"), $utf8)
  $ms = [regex]::Matches($txt, $patron, 'IgnoreCase,Multiline'); $res = @()
  for ($i = 0; $i -lt $ms.Count; $i++) {
    $fin = if ($i + 1 -lt $ms.Count) { $ms[$i + 1].Index } else { $txt.Length }
    $res += [pscustomobject]@{ Nombre = $ms[$i].Groups['n'].Value.ToUpper(); Tipo = $ms[$i].Groups['t'].Value.ToUpper(); Texto = $txt.Substring($ms[$i].Index, $fin - $ms[$i].Index).ToUpper() }
  }
  $res
}
$objetos = @(Bloques '06_vistas.sql' '^CREATE OR REPLACE (?<t>VIEW)\s+(?<n>\w+)') +
           @(Bloques '07_procedimientos_funciones_paquetes.sql' '^CREATE OR REPLACE (?<t>PACKAGE BODY|FUNCTION|PROCEDURE)\s+(?<n>\w+)') +
           @(Bloques '08_triggers.sql' '^CREATE OR REPLACE (?<t>TRIGGER)\s+(?<n>\w+)')
function Usan([string]$tabla, [string]$col) {
  @($objetos | Where-Object { $_.Texto -match "\b$tabla\b" -and $_.Texto -match "\b$col\b" } | ForEach-Object { $_.Nombre } | Sort-Object -Unique)
}
$p3 = @{ ROL = 15; USUARIO = 20; USUARIO_ROL = 14; MATERIAL = 27; PUNTO_INSTALACION = 8; CONTENEDOR = 18; CONTENEDOR_MATERIAL = 11; CREDENCIAL_ACCESO = 7; INTERACCION = 30; ORDEN_APERTURA = 25; EVENTO_DISPOSITIVO = 13; DEPOSITO = 22; VINCULO_TELEGRAM = 5; REGLA_ECOPUNTOS = 3; MOVIMIENTO_ECOPUNTOS = 9; CONSULTA_VISION = 3 }
$telegram = @{
  ROL = 'El bot muestra comandos distintos según el rol.'; USUARIO = 'Se crea al registrarse con /start.'; USUARIO_ROL = 'Decide qué comandos acepta el bot.'
  CREDENCIAL_ACCESO = 'No se usa: el bot no pide contraseña.'; VINCULO_TELEGRAM = 'Identifica el chat de la persona en cada mensaje.'
  PUNTO_INSTALACION = 'El QR de la estación llega al bot con /start.'; CONTENEDOR = 'El bot dice el color de la caneca.'; MATERIAL = 'La IA responde con un código de este catálogo.'
  CONTENEDOR_MATERIAL = 'Decide a qué caneca manda el bot cada material.'; PESO_REFERENCIAL = 'Permite mostrar un peso ESTIMADO en la respuesta.'
  INTERACCION = 'Cada /start con QR abre una sesión.'; CONSULTA_VISION = 'Cada foto enviada a Eco queda aquí.'; ORDEN_APERTURA = 'Se emite cuando la persona confirma «abrir caneca».'
  EVENTO_DISPOSITIVO = 'El bot avisa apertura y cierre de la caneca.'; DEPOSITO = 'El bot confirma el registro del depósito (pendiente).'
  REGLA_ECOPUNTOS = 'No se usa directamente.'; MOVIMIENTO_ECOPUNTOS = 'El bot responde el saldo de EcoPuntos.'; AUDITORIA_DEPOSITO = 'No se usa.'; HISTORIAL_ESTADO_CONTENEDOR = 'Permite explicar por qué una caneca está fuera de servicio.'
}
$vistasBi = @($objetos | Where-Object { $_.Nombre -like 'VW_BI_*' })
function Tamano([string]$tipo) {
  if ($tipo -match '^VARCHAR2\((\d+) CHAR\)') { "hasta $($matches[1]) caracteres" }
  elseif ($tipo -match '^NUMBER\((\d+),(\d+)\)') { "$($matches[1]) dígitos en total, $($matches[2]) decimales" }
  elseif ($tipo -match '^NUMBER\((\d+)\)') { if ($matches[1] -eq '1') { 'entero de 1 dígito' } else { "entero de hasta $($matches[1]) dígitos" } }
  elseif ($tipo -match '^TIMESTAMP\((\d)\)') { "fecha y hora con $($matches[1]) decimales de segundo (UTC)" }
  elseif ($tipo -match '^RAW\((\d+)\)') { "$($matches[1]) bytes binarios" }
  else { $tipo }
}
function Invalido($c) {
  $r = @()
  if ($c.Identidad -match 'ALWAYS') { $r += 'Escribir el número a mano en un INSERT (ORA-32795: la identidad la asigna Oracle)' }
  if ($c.FK) { $ref = ($c.FK -split '->')[-1]; $r += "Un número que no exista en $ref (ORA-02291)" }
  if ($c.CK) { foreach ($k in ($c.CK -split ', ')) { if ($checks[$k]) { $cond = $checks[$k].Trim(); if ($cond.Length -gt 110) { $r += "Una combinación que rompa $k (condición completa en el anexo de CHECK) (ORA-02290)" } else { $r += "Un valor que rompa ${k}: $cond (ORA-02290)" } } } }
  if ($c.UK -and -not $c.PK) { $r += "Repetir un valor ya usado (ORA-00001 por $($c.UK))" }
  if ($c.Nulo -eq 'N' -and -not $c.Identidad -and -not $c.Default) { $r += 'Dejarlo vacío (ORA-01400)' }
  if ($c.Tipo -match '^VARCHAR2\((\d+)') { $r += "Un texto de más de $($matches[1]) caracteres (ORA-12899)" }
  if ($r.Count -eq 0) { $r += 'Un valor de otro tipo de dato (ORA-01722 o ORA-01843)' }
  ($r | Select-Object -First 3) -join ' · '
}
function Regla($t, $c, $usan) {
  $r = @()
  if ($c.PK) { $r += 'Identidad: cada fila se distingue de las demás' }
  if ($c.FK) { $r += "Integridad referencial con $(($c.FK -split '->')[-1])" }
  if ($c.CK) { $r += "Reglas CHECK: $($c.CK)" }
  if ($c.UK -and -not $c.PK) { $r += "Unicidad: $($c.UK)" }
  $trg = @($usan | Where-Object { $_ -like 'TRG_*' }); if ($trg) { $r += "Vigilada por: $($trg -join ', ')" }
  if ($r.Count -eq 0) { $r += 'Dato descriptivo: no tiene regla adicional; lo protegen su tipo y su longitud' }
  $r -join ' · '
}
# --- Campos añadidos el 11/11/2026: operaciones, preguntas del profesor y SELECT de comprobación (todos derivados de metadatos) ---
function PkDe([string]$tabla) { ($meta.Values | Where-Object { $_.Tabla -eq $tabla -and $_.PK } | Select-Object -First 1).Columna }
function Modifican($t, $c, $usan) {
  $r = @()
  if ($c.Identidad) { $r += 'INSERT (Oracle asigna el número; no se actualiza)' }
  $col = $c.Columna
  $esc = @($objetos | Where-Object { $usan -contains $_.Nombre -and $_.Nombre -notlike 'VW_*' -and (
      $_.Texto -match "UPDATE\s+$t\b[^;]*?\b$col\b" -or $_.Texto -match "INSERT\s+INTO\s+$t\b[^;]*?\b$col\b" -or
      ($_.Tipo -eq 'TRIGGER' -and $_.Texto -match "\bON\s+$t\b" -and $_.Texto -match ":NEW\.$col\s*:=") ) } | ForEach-Object Nombre | Sort-Object -Unique)
  if ($esc) { $r += "INSERT/UPDATE a través de: $($esc -join ', ')" }
  if (-not $c.Identidad -and -not $esc) { $r += "INSERT/UPDATE directo sobre $t (catálogo o datos de prueba)" }
  $r += 'Nunca DELETE desde la aplicación (se conserva la historia)'
  $r -join ' · '
}
function Consultan($usan) {
  $v = @($usan | Where-Object { $_ -like 'VW_*' })
  if ($v) { "SELECT en las vistas $($v -join ', ') y en las consultas de exposición (13_consultas_exposicion.sql)" } else { 'SELECT directo sobre la tabla (consultas de exposición y SQL Developer)' }
}
function Preguntas($t, $c) {
  $q = @()
  if ($c.PK) { $q += "¿Por qué $($c.Columna) es un número subrogado y no un dato del negocio que podría cambiar?" }
  if ($c.FK) { $q += "¿Qué pasa si intento guardar un $($c.Columna) que no existe en $(($c.FK -split '->')[-1])? (respuesta: ORA-02291)" }
  if ($c.CK) { $q += "¿Qué valores permite $(($c.CK -split ', ')[0]) y por qué esos y no otros?" }
  if ($c.UK -and -not $c.PK) { $q += "¿Por qué $($c.Columna) no puede repetirse? (UNIQUE $($c.UK))" }
  if ($c.Columna -match 'PESO_MEDIDO') { $q += '¿Por qué no se inventa el peso cuando no hay báscula? (queda NULL)' }
  if ($c.Columna -match 'PESO_ESTIMADO') { $q += '¿En qué se diferencia un peso estimado de uno medido?' }
  if ($c.Tipo -match '^TIMESTAMP') { $q += '¿Por qué la fecha se guarda en UTC y se muestra en hora de Colombia?' }
  if ($c.Nulo -eq 'Y') { $q += "¿Qué significa que $($c.Columna) esté vacío (NULL)?" } elseif (-not $c.PK) { $q += "¿Por qué $($c.Columna) es obligatorio (NOT NULL)?" }
  ($q | Select-Object -First 3) -join ' · '
}
function SelectDe($t, $c) {
  $col = $c.Columna
  if ($c.PK) { return "SELECT * FROM $t WHERE $col = (SELECT MIN($col) FROM $t);" }
  if ($c.FK) { $rt = ($c.FK -split '->')[-1].Trim(); $rp = PkDe $rt; if ($rp) { return "SELECT t.$col, COUNT(*) AS filas FROM $t t JOIN $rt r ON r.$rp = t.$col GROUP BY t.$col ORDER BY 2 DESC FETCH FIRST 5 ROWS ONLY;" } }
  if ($c.Tipo -match '^TIMESTAMP|^DATE') { return "SELECT MIN($col) AS primera, MAX($col) AS ultima, COUNT($col) AS con_valor FROM $t;" }
  if ($c.Tipo -match '^RAW|^BLOB|^CLOB') { return "SELECT COUNT(*) AS filas, COUNT($col) AS con_valor FROM $t;" }
  if ($c.Tipo -match '^NUMBER\(\d+,\d+\)') { return "SELECT COUNT($col) AS con_valor, MIN($col) AS minimo, MAX($col) AS maximo, SUM($col) AS total FROM $t;" }
  if ($c.CK -or $c.Tipo -match '^NUMBER\([12]\)$') { return "SELECT $col, COUNT(*) AS filas FROM $t GROUP BY $col ORDER BY $col;" }
  if ($c.Nulo -eq 'Y') { return "SELECT COUNT(*) AS filas, COUNT($col) AS con_valor FROM $t;" }
  "SELECT $col FROM $t FETCH FIRST 5 ROWS ONLY;"
}
# --- Campos añadidos (2.ª versión): los 24 puntos pedidos, numerados, en lenguaje sencillo ---
$ampliada = @{}
foreach ($l in Get-Content "$aqui\fuente_explicacion_ampliada.txt" -Encoding UTF8) { if ($l -match '^#' -or -not $l.Trim()) { continue }; $p = $l -split '\|', 3; $ampliada["$($p[0]).$($p[1])"] = $p[2] }
foreach ($k in $ampliada.Keys) { if (-not $meta.ContainsKey($k)) { throw "Explicación ampliada para una columna que no existe: $k" } }
$filaEs = @{ ROL = 'un rol'; USUARIO = 'una persona registrada'; USUARIO_ROL = 'la asignación de un rol a una persona'; CREDENCIAL_ACCESO = 'la contraseña (en hash) de una persona'; VINCULO_TELEGRAM = 'la unión de una cuenta con un chat de Telegram'
  PUNTO_INSTALACION = 'una estación de reciclaje'; CONTENEDOR = 'una caneca'; MATERIAL = 'un tipo de material'; CONTENEDOR_MATERIAL = 'un material que una caneca acepta'; PESO_REFERENCIAL = 'el peso típico de un objeto'
  INTERACCION = 'una sesión iniciada con el QR'; CONSULTA_VISION = 'una foto analizada por la IA'; ORDEN_APERTURA = 'una orden de abrir una caneca'; EVENTO_DISPOSITIVO = 'un aviso enviado por la caneca'; DEPOSITO = 'un residuo depositado'
  REGLA_ECOPUNTOS = 'una tarifa de EcoPuntos'; MOVIMIENTO_ECOPUNTOS = 'un crédito o reverso de EcoPuntos'; AUDITORIA_DEPOSITO = 'un cambio auditado de un depósito'; HISTORIAL_ESTADO_CONTENEDOR = 'un cambio de estado de una caneca' }
$proceso = @{ ROL = 'Control de acceso: decide qué puede hacer cada persona'; USUARIO = 'Registro de personas; identifica al actor en todos los procesos'; USUARIO_ROL = 'Control de acceso por rol'; CREDENCIAL_ACCESO = 'Inicio de sesión de la aplicación de escritorio'; VINCULO_TELEGRAM = 'Reconocer a la persona cuando escribe al bot'
  PUNTO_INSTALACION = 'Lectura del QR de la estación e inicio de la sesión'; CONTENEDOR = 'Elegir, abrir y controlar la disponibilidad de la caneca'; MATERIAL = 'Clasificar el residuo (IA, ciudadano y operario)'; CONTENEDOR_MATERIAL = 'Decidir a qué caneca va cada material'; PESO_REFERENCIAL = 'Estimar el peso a partir de una foto'
  INTERACCION = 'Sesión QR, desde el escaneo hasta el cierre'; CONSULTA_VISION = 'Análisis de la foto por la IA Eco (solo orientación)'; ORDEN_APERTURA = 'Orden de abrir la caneca'; EVENTO_DISPOSITIVO = 'Avisos de la caneca: abrió, detectó, cerró, falló'; DEPOSITO = 'Registro del depósito y su verificación'
  REGLA_ECOPUNTOS = 'Cálculo de EcoPuntos'; MOVIMIENTO_ECOPUNTOS = 'Acreditar y revertir EcoPuntos (el saldo es la suma)'; AUDITORIA_DEPOSITO = 'Auditoría de verificaciones (disparador 3)'; HISTORIAL_ESTADO_CONTENEDOR = 'Historial de disponibilidad de canecas (disparador 1)' }
$quienVe = @{ ROL = 'Administrador'; USUARIO = 'Administrador; cada persona ve sus propios datos'; USUARIO_ROL = 'Administrador y la lógica de permisos'; CREDENCIAL_ACCESO = 'Solo el proceso de inicio de sesión (nadie la lee en claro)'; VINCULO_TELEGRAM = 'El bot de Telegram'
  PUNTO_INSTALACION = 'Ciudadano (bot), operario y administrador'; CONTENEDOR = 'Ciudadano (bot), operario y administrador'; MATERIAL = 'Todos los roles y la IA'; CONTENEDOR_MATERIAL = 'La función que resuelve la caneca'; PESO_REFERENCIAL = 'El bot y el servicio de depósitos'
  INTERACCION = 'Operario, administrador y Power BI'; CONSULTA_VISION = 'Administrador y Power BI'; ORDEN_APERTURA = 'Servicio de la caneca y operario'; EVENTO_DISPOSITIVO = 'Operario y administrador'; DEPOSITO = 'Operario (verifica), administrador y Power BI'
  REGLA_ECOPUNTOS = 'Administrador y PKG_ECOPUNTOS'; MOVIMIENTO_ECOPUNTOS = 'Ciudadano (su saldo), administrador y Power BI'; AUDITORIA_DEPOSITO = 'Administrador y auditor (Power BI)'; HISTORIAL_ESTADO_CONTENEDOR = 'Administrador' }
function Hijas([string]$tabla) { @($meta.Values | Where-Object { $_.FK -match "->\s*$tabla(,|$)" } | ForEach-Object Tabla | Sort-Object -Unique) }
function Relaciona($t, $c) {
  $r = @()
  if ($c.FK) { foreach ($x in ($c.FK -split ', ')) { $r += "Apunta a $(($x -split '->')[-1]) (FK $(($x -split '->')[0]))" } }
  if ($c.PK) { $h = Hijas $t; if ($h) { $r += "Es referenciada por: $($h -join ', ')" } else { $r += 'Ninguna tabla la referencia' } }
  if (-not $r) { $r += 'No es clave: no relaciona tablas' }
  $r -join ' · '
}
function Evita($t, $c) {
  $r = @()
  if ($c.PK) { $r += 'filas repetidas o imposibles de distinguir' }
  if ($c.FK) { $r += "registros huérfanos (no puede apuntar a un(a) $(($c.FK -split '->')[-1]) que no existe)" }
  if ($c.UK -and -not $c.PK) { $r += 'valores duplicados' }
  if ($c.CK) { $r += 'valores fuera de las opciones permitidas' }
  if ($c.Nulo -eq 'N' -and -not $c.PK) { $r += 'filas incompletas (no se puede guardar sin este dato)' }
  if ($c.Columna -match 'SINTETIC') { $r += 'mezclar datos de demostración con datos del sistema' }
  if ($c.Columna -eq 'PESO_ESTIMADO_KG') { $r += 'presentar una estimación como si fuera una medición' }
  if ($c.Columna -eq 'PESO_MEDIDO_KG') { $r += 'inventar pesos cuando no hubo báscula' }
  if ($c.Columna -match 'IDEMPOTENCIA') { $r += 'procesar dos veces la misma operación' }
  if ($c.Tipo -match '^TIMESTAMP') { $r += 'errores de hora (se guarda en UTC)' }
  if ($c.Tipo -match '^RAW') { $r += 'guardar la contraseña en texto legible' }
  if (-not $r) { return 'Perder información descriptiva; su tipo y su longitud impiden guardar datos deformes' }
  'Evita ' + ($r -join '; ')
}
function SinEl($t, $c, $f) {
  if ($c.PK) { return "No habría forma segura de señalar una fila concreta de $t, y las tablas que dependen de ella no podrían relacionarse." }
  if ($c.FK) { $ref = ($c.FK -split '->')[-1]; return "Se perdería el vínculo entre $t y ${ref}: no sabríamos a qué $ref pertenece cada fila y no se podrían cruzar los datos (ni en SQL ni en Power BI)." }
  $sig = $f.Significado.TrimEnd('.'); if ($sig.Length -gt 1) { $sig = $sig.Substring(0, 1).ToLower() + $sig.Substring(1) }
  "La tabla $t no podría guardar este dato ($sig). $($f.Porque)"
}
function Sencilla($t, $c, $f) {
  $s = "Piensa en $t como una hoja de cálculo donde cada fila es $($filaEs[$t]). $($c.Columna) es la casilla que guarda: $($f.Significado)"
  if ($c.PK) { $s += ' Funciona como la «cédula» de la fila: no se repite y no cambia.' }
  if ($c.FK) { $s += " Es un «puntero»: copia el número de una fila de $(($c.FK -split '->')[-1]) para unir las dos tablas." }
  if ($c.Nulo -eq 'N') { $s += ' Nunca puede quedar vacía.' } else { $s += " Puede quedar vacía: $($f.SiNull)" }
  $s
}
$selects = New-Object Collections.Generic.List[string]
$fichas = New-Object Text.StringBuilder; $mdc = New-Object Text.StringBuilder
[void]$mdc.AppendLine('# Diccionario de datos COMPLETO — una ficha por atributo'); [void]$mdc.AppendLine('')
[void]$mdc.AppendLine("Generado el $(Get-Date -Format 'yyyy-MM-dd HH:mm') desde Oracle (metadatos reales) y el código del proyecto. $($meta.Count) atributos en 19 tablas."); [void]$mdc.AppendLine('')
$csv2 = New-Object Collections.Generic.List[string]
$csv2.Add('N;Modulo;Tabla;Atributo;Que_significa;Para_que_sirve;Por_que_existe;Tipo;Acepta_NULL;Default;PK_FK;Se_relaciona_con;Proceso;Quien_lo_llena;Quien_lo_consulta;Ejemplo_correcto;Ejemplo_incorrecto;Problema_que_evita;Si_no_existiera;Objetos_que_lo_usan;CSharp;Telegram;Power_BI;Pregunta_profesor;SELECT;Explicacion_sencilla;Explicacion_ampliada')
$n2 = 0
foreach ($mod in $modulos.Keys) {
  [void]$fichas.AppendLine("<h2>Módulo: $(Esc $mod)</h2>"); [void]$mdc.AppendLine("## Módulo: $mod"); [void]$mdc.AppendLine('')
  foreach ($t in $modulos[$mod]) {
    $cols = $meta.Values | Where-Object Tabla -eq $t | Sort-Object Pos
    [void]$fichas.AppendLine("<h3>$t ($($cols.Count) atributos)</h3><p>$(Esc $tablas[$t])</p>"); [void]$mdc.AppendLine("### $t ($($cols.Count) atributos)"); [void]$mdc.AppendLine(''); [void]$mdc.AppendLine($tablas[$t]); [void]$mdc.AppendLine('')
    foreach ($c in $cols) {
      $n2++; $f = $fuente["$t.$($c.Columna)"]; $usan = Usan $t $c.Columna
      $ref = if ($c.FK) { ($c.FK -split '->')[-1] } else { '—' }
      $bi = @($vistasBi | Where-Object { $_.Texto -match "\b$t\b" -and $_.Texto -match "\b$($c.Columna)\b" } | ForEach-Object Nombre)
      $pbi = if ($c.Columna -match 'CORREO|NOMBRE$|HASH|SAL$|TELEGRAM_USER_ID') { 'Nunca se expone: es dato personal o secreto.' } elseif ($bi) { "Llega a Power BI por $($bi -join ', ') (solo lectura, con seudónimos)." } else { 'No se expone en Power BI.' }
      $cs = if ($p3.ContainsKey($t)) { "En P3 la tabla la usa la capa EcoAccess.P3.Datos ($($p3[$t]) sentencias SQL). La app académica de C# solo lee vistas." } else { 'Tabla nueva del modelo académico (no existe en P3); C# la consulta por vistas o paquetes.' }
      $sel = SelectDe $t $c
      $campos = [ordered]@{
        '1. Tabla' = "$t — $($tablas[$t])"
        '2. Atributo' = $c.Columna
        '3. Qué significa' = $f.Significado
        '4. Para qué sirve' = $(
          $sig4 = $f.Significado.TrimEnd('.'); if ($sig4.Length -gt 1) { $sig4 = $sig4.Substring(0, 1).ToLower() + $sig4.Substring(1) }
          $base = if ($c.PK -and $c.FK) { "Sirve a la vez para identificar cada fila de $t y para atarla a su fila de $ref (relación 1:1 o tabla puente)." }
                  elseif ($c.PK) { "Sirve para identificar sin ambigüedad cada fila de $t, de modo que otras tablas puedan apuntarle." }
                  elseif ($c.FK) { "Sirve para saber a qué fila de $ref pertenece cada fila de $t y poder unirlas con JOIN." }
                  else { "Sirve para que EcoAccess sepa, en cada fila de ${t}: $sig4." }
          "$base Se usa en: $($f.Uso).")
        '5. Por qué existe' = $f.Porque
        '6. Tipo de dato' = "$($c.Tipo) ($(Tamano $c.Tipo))"
        '7. ¿Acepta NULL?' = $(if ($c.Nulo -eq 'N') { 'No (obligatoria). ' + $f.SiNull } else { 'Sí. ' + $f.SiNull })
        '8. Valor por defecto' = $(if ($c.Default) { $c.Default } elseif ($c.Identidad) { $c.Identidad } else { 'No tiene' })
        '9. ¿Es PK o FK? (restricciones)' = $(if (Restr $c) { Restr $c } else { 'Ni PK ni FK; sin restricciones adicionales' })
        '10. Con qué tabla se relaciona' = (Relaciona $t $c)
        '11. Qué proceso lo usa' = $proceso[$t]
        '12. Quién lo llena' = "$($f.Registra). $(Modifican $t $c $usan)"
        '13. Quién lo consulta' = "$($quienVe[$t]). $(Consultan $usan)"
        '14. Ejemplo de valor correcto' = $f.Ejemplo
        '15. Ejemplo de valor incorrecto' = (Invalido $c)
        '16. Qué problema evita' = "$(Evita $t $c). $(Regla $t $c $usan)"
        '17. Qué pasaría si no existiera' = (SinEl $t $c $f)
        '18. Consultas, procedimientos y triggers que lo usan' = $(if ($usan) { $usan -join ', ' } else { 'Ningún paquete ni disparador la nombra; se consulta con SELECT directo' })
        '19. ¿Se usa en C#?' = $cs
        '20. ¿Se usa en Telegram?' = $telegram[$t]
        '21. ¿Se usa en Power BI?' = $pbi
        '22. Cómo podría preguntarlo el profesor' = (Preguntas $t $c)
        '23. SELECT para verlo' = $sel
        '24. Explicación sencilla (para quien empieza)' = (Sencilla $t $c $f)
      }
      $amp = if ($ampliada.ContainsKey("$t.$($c.Columna)")) { $ampliada["$t.$($c.Columna)"] } else { '' }
      if ($amp) { $campos['Explicación ampliada'] = $amp }
      $selects.Add("PROMPT $n2 $t.$($c.Columna)"); $selects.Add($sel)
      [void]$fichas.AppendLine("<table class='ficha'><tr><th colspan='2'>$n2. $t.$($c.Columna)</th></tr>")
      foreach ($k in $campos.Keys) { [void]$fichas.AppendLine("<tr><td class='fk'>$(Esc $k)</td><td>$(Esc ([string]$campos[$k]))</td></tr>") }
      [void]$fichas.AppendLine('</table>')
      [void]$mdc.AppendLine("#### $n2. ``$t.$($c.Columna)``"); [void]$mdc.AppendLine('')
      foreach ($k in $campos.Keys) { [void]$mdc.AppendLine("- **${k}:** $($campos[$k])") }
      [void]$mdc.AppendLine('')
      $csv2.Add((@($n2, $mod, $t) + @($campos.Values)[1..23] + @($amp) | ForEach-Object { '"' + (([string]$_) -replace '"', '""') + '"' }) -join ';')
    }
  }
}
[IO.File]::WriteAllText("$aqui\diccionario_fichas.html", $fichas.ToString(), $utf8)
$ach = New-Object Text.StringBuilder; [void]$ach.AppendLine("<table class='t'><tr><th>Restricción CHECK</th><th>Condición exacta en Oracle</th></tr>")
foreach ($k in ($checks.Keys | Sort-Object)) { [void]$ach.AppendLine("<tr><td>$(Esc $k)</td><td><code>$(Esc $checks[$k].Trim())</code></td></tr>") }
[void]$ach.AppendLine('</table>'); [IO.File]::WriteAllText("$aqui\anexo_checks.html", $ach.ToString(), $utf8)
[IO.File]::WriteAllText("$aqui\DICCIONARIO_COMPLETO.md", $mdc.ToString(), $utf8)
[IO.File]::WriteAllLines("$aqui\diccionario_completo.csv", $csv2, (New-Object Text.UTF8Encoding $true))
[IO.File]::WriteAllLines("$aqui\comprobar_173_select.sql", (@('-- 173 SELECT de comprobación (uno por atributo). GENERADO por generar_diccionario.ps1. Solo lectura.', 'SET PAGESIZE 50 LINESIZE 200 FEEDBACK ON') + $selects), $utf8)

# --- 4. COMMENT ON para que SQL Developer muestre el diccionario ---
$sql = New-Object Text.StringBuilder
[void]$sql.AppendLine('-- =====================================================================')
[void]$sql.AppendLine('-- EcoAccess BD — 15 · Comentarios del diccionario (COMMENT ON)')
[void]$sql.AppendLine('-- GENERADO por 03_DICCIONARIO_DE_DATOS/generar_diccionario.ps1. Ejecutar como ECOACCESS_BD.')
[void]$sql.AppendLine('-- Se ven en SQL Developer: clic en la tabla > pestaña «Columnas» > columna COMMENTS.')
[void]$sql.AppendLine('-- =====================================================================')
foreach ($t in ($tablas.Keys | Sort-Object)) { [void]$sql.AppendLine("COMMENT ON TABLE $($t.ToLower()) IS '$($tablas[$t] -replace "'", "''")';") }
foreach ($k in ($fuente.Keys | Sort-Object)) {
  $t, $c = $k -split '\.'; $txt = $fuente[$k].Significado
  if ($txt.Length -gt 3900) { $txt = $txt.Substring(0, 3900) }
  [void]$sql.AppendLine("COMMENT ON COLUMN $($t.ToLower()).$($c.ToLower()) IS '$($txt -replace "'", "''")';")
}
[void]$sql.AppendLine("SELECT COUNT(*) AS columnas_comentadas FROM user_col_comments c JOIN user_tables t ON t.table_name = c.table_name WHERE c.comments IS NOT NULL;")
[IO.File]::WriteAllText("$aqui\..\04_SCRIPTS_ORACLE\15_comentarios_diccionario.sql", $sql.ToString(), $utf8)
"Diccionario generado: $n columnas, $($checks.Count) CHECK con nombre."

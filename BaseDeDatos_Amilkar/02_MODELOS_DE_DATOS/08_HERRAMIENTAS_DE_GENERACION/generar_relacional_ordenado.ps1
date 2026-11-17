<#
  MODELO RELACIONAL ORDENADO POR MÓDULOS (versión 2, 11/11/2026)
  - Lee los metadatos REALES de Oracle (../03_DICCIONARIO_DE_DATOS/metadatos.txt): el dibujo
    no puede tener columnas o FK distintas a las instaladas.
  - Ubica las 19 tablas en bloques por módulo y traza las 38 FK con líneas en ángulo recto
    que viajan solo por pasillos (enrutador_modelo.ps1). Comprueba al final que ninguna línea
    quede encima de otra ni atraviese una tabla.
  - Salidas en EXPORTACIONES\: MR_00_general_por_modulos.svg y MR_M1..M5_*.svg
    (convertir_exportaciones.ps1 los pasa a PNG y PDF).
  Uso:  pwsh -File generar_relacional_ordenado.ps1
#>
$ErrorActionPreference = 'Stop'
$aqui = Split-Path -Parent $MyInvocation.MyCommand.Path
. "$aqui\enrutador_modelo.ps1"
$utf8 = New-Object Text.UTF8Encoding $false

# ---------- metadatos reales ----------
$cols = @{}; $fks = [ordered]@{}
foreach ($l in Get-Content "$aqui\..\03_DICCIONARIO_DE_DATOS\metadatos.txt" -Encoding UTF8) {
  $p = $l -split '\|'; if ($p[0] -ne 'COL') { continue }
  if (-not $cols.ContainsKey($p[1])) { $cols[$p[1]] = New-Object Collections.Generic.List[object] }
  $cols[$p[1]].Add([pscustomobject]@{ N = $p[2]; Pos = [int]$p[3]; Tipo = $p[4]; Nulo = $p[5]; PK = $p[8]; UK = $p[9]; FK = $p[10] })
  if ($p[10]) { foreach ($f in $p[10] -split ', ') { $nm, $ref = $f -split '->'; if (-not $fks.Contains($nm)) { $fks[$nm] = @{ hija = $p[1]; padre = $ref; nombre = $nm } } } }
}

# ---------- módulos ----------
$MOD = [ordered]@{
  M1 = @{ n = 'Módulo 1 · Catálogos base';           c = '#9A7D0A'; f = '#FCF8E8'; t = @('MATERIAL','PESO_REFERENCIAL') }
  M2 = @{ n = 'Módulo 2 · Usuarios e identidad';     c = '#2E6DA4'; f = '#EEF4FB'; t = @('ROL','USUARIO_ROL','USUARIO','CREDENCIAL_ACCESO','VINCULO_TELEGRAM') }
  M3 = @{ n = 'Módulo 3 · Infraestructura y QR';     c = '#1E8449'; f = '#EDF7F0'; t = @('PUNTO_INSTALACION','CONTENEDOR','CONTENEDOR_MATERIAL') }
  M4 = @{ n = 'Módulo 4 · Operación del sistema';    c = '#B03A2E'; f = '#FBEFED'; t = @('INTERACCION','CONSULTA_VISION','ORDEN_APERTURA','EVENTO_DISPOSITIVO','DEPOSITO') }
  M5 = @{ n = 'Módulo 5 · EcoPuntos y auditoría';    c = '#6C3483'; f = '#F4EEF7'; t = @('REGLA_ECOPUNTOS','MOVIMIENTO_ECOPUNTOS','AUDITORIA_DEPOSITO','HISTORIAL_ESTADO_CONTENEDOR') }
}
$modDe = @{}; foreach ($k in $MOD.Keys) { foreach ($t in $MOD[$k].t) { $modDe[$t] = $k } }
if ($modDe.Count -ne 19 -or $cols.Count -ne 19) { throw "Se esperaban 19 tablas (metadatos: $($cols.Count))" }

$W = 300; $FILA = 16; $CAB = 28
function Esc([string]$s) { [Net.WebUtility]::HtmlEncode($s) }
function ListaCols($t, $modo) {
  if ($modo -eq 'clave') { @($cols[$t] | Where-Object { $_.PK } | Sort-Object Pos) } else { @($cols[$t] | Sort-Object Pos) }
}
function AltoCaja($t, $modo) { $CAB + 10 + $FILA * (ListaCols $t $modo).Count }

function Caja($t, $b, $modo) {
  $col = if ($modo -eq 'clave') { '#7F8C8D' } else { $MOD[$modDe[$t]].c }
  $x = $b.x; $y = $b.y; $w = $b.w; $h = $b.h
  $sb = New-Object Text.StringBuilder
  [void]$sb.Append("<g><rect x='$x' y='$y' width='$w' height='$h' rx='5' fill='#FFFFFF' stroke='$col' stroke-width='1.8'/>")
  [void]$sb.Append("<path d='M $x $($y+$CAB) L $x $($y+5) Q $x $y $($x+5) $y L $($x+$w-5) $y Q $($x+$w) $y $($x+$w) $($y+5) L $($x+$w) $($y+$CAB) Z' fill='$col'/>")
  $tit = if ($modo -eq 'clave') { "$t  (otro módulo)" } else { $t }
  [void]$sb.Append("<text x='$($x+$w/2)' y='$($y+19)' font-size='13' font-weight='bold' fill='#FFFFFF' text-anchor='middle'>$(Esc $tit)</text>")
  $i = 0
  foreach ($c in (ListaCols $t $modo)) {
    $yy = $y + $CAB + 14 + $FILA * $i
    if ($i % 2 -eq 1) { [void]$sb.Append("<rect x='$($x+1)' y='$($yy-12)' width='$($w-2)' height='$FILA' fill='#F6F7F8'/>") }
    $i++
    $m = @(); if ($c.PK) { $m += 'PK' }; if ($c.FK) { $m += 'FK' }; if ($c.UK) { $m += 'UQ' }
    $color = if ($c.PK) { '#A93226' } elseif ($c.FK) { '#1F618D' } else { '#1C2833' }
    $peso = if ($c.PK) { 'bold' } else { 'normal' }
    $ob = if ($c.Nulo -eq 'N') { '*' } else { '' }
    [void]$sb.Append("<text x='$($x+7)' y='$yy' font-size='9.5' font-weight='bold' fill='#5D6D7E'>$($m -join ' ')</text>")
    [void]$sb.Append("<text x='$($x+52)' y='$yy' font-size='11' font-weight='$peso' fill='$color'>$(Esc $c.N.ToLower())$ob</text>")
    [void]$sb.Append("<text x='$($x+$w-7)' y='$yy' font-size='9.5' fill='#7B7D7D' text-anchor='end'>$(Esc $c.Tipo)</text>")
  }
  [void]$sb.Append('</g>'); $sb.ToString()
}

function Extremos($pts, $esUno) {
  # pata de gallo (N) en la hija = primer punto; barra doble (1) en la padre = último punto
  $a = $pts[0]; $b = $pts[1]; $dx = [math]::Sign($b[0] - $a[0]); $dy = [math]::Sign($b[1] - $a[1])
  $px = -$dy; $py = $dx
  if ($esUno) { $hija = "M $($a[0]+$dx*9+$px*6) $($a[1]+$dy*9+$py*6) L $($a[0]+$dx*9-$px*6) $($a[1]+$dy*9-$py*6)" }
  else { $hija = "M $($a[0]+$px*7) $($a[1]+$py*7) L $($a[0]+$dx*12) $($a[1]+$dy*12) L $($a[0]-$px*7) $($a[1]-$py*7) M $($a[0]+$dx*15+$px*6) $($a[1]+$dy*15+$py*6) L $($a[0]+$dx*15-$px*6) $($a[1]+$dy*15-$py*6)" }
  $z = $pts[-1]; $y0 = $pts[-2]; $dx = [math]::Sign($y0[0] - $z[0]); $dy = [math]::Sign($y0[1] - $z[1]); $px = -$dy; $py = $dx
  $padre = "M $($z[0]+$dx*7+$px*6) $($z[1]+$dy*7+$py*6) L $($z[0]+$dx*7-$px*6) $($z[1]+$dy*7-$py*6) M $($z[0]+$dx*12+$px*6) $($z[1]+$dy*12+$py*6) L $($z[0]+$dx*12-$px*6) $($z[1]+$dy*12-$py*6)"
  "$hija $padre"
}

function Diagrama {
  param($archivo, $titulo, $nota, [System.Collections.IDictionary]$ubic, $bloques, [string[]]$claves = @())
  $cajas = [ordered]@{}
  foreach ($t in $ubic.Keys) {
    $modo = if ($claves -contains $t) { 'clave' } else { 'todo' }
    $cajas[$t] = @{ w = $W; h = (AltoCaja $t $modo); r = $ubic[$t][0]; c = $ubic[$t][1]; modo = $modo }
  }
  $ar = @($fks.Values | Where-Object { $cajas.Contains($_.hija) -and $cajas.Contains($_.padre) -and -not ($claves -contains $_.hija -and $claves -contains $_.padre) })
  $res = Invoke-Enrutar -Cajas $cajas -Aristas $ar -Banda 30 -PasilloMin 40
  $chk = Test-Enrutado $res
  $OX = 20; $OY = 78
  $sb = New-Object Text.StringBuilder
  $ancho = $res.Ancho + 2 * $OX; $alto = $res.Alto + $OY + 64
  [void]$sb.Append("<svg xmlns='http://www.w3.org/2000/svg' width='$ancho' height='$alto' viewBox='0 0 $ancho $alto' font-family='Segoe UI, Arial, sans-serif'><rect width='100%' height='100%' fill='#FFFFFF'/>")
  [void]$sb.Append("<text x='$OX' y='34' font-size='22' font-weight='bold' fill='#1B2631'>$(Esc $titulo)</text><text x='$OX' y='58' font-size='12.5' fill='#566573'>$(Esc $nota)</text>")
  [void]$sb.Append("<g transform='translate($OX,$OY)'>")
  foreach ($bl in $bloques) {
    $m = $MOD[$bl.m]
    $x1 = $res.X[$bl.c1] - $res.VW[$bl.c1] / 2 + 4; $x2 = $res.X[$bl.c2] + $res.ColW[$bl.c2] + $res.VW[$bl.c2 + 1] / 2 - 4
    $y1 = $res.Y[$bl.r1] - $res.HW[$bl.r1] / 2 + 4; $y2 = $res.Y[$bl.r2] + $res.RowH[$bl.r2] + $res.HW[$bl.r2 + 1] / 2 - 4
    [void]$sb.Append("<rect x='$x1' y='$y1' width='$($x2-$x1)' height='$($y2-$y1)' rx='12' fill='$($m.f)' stroke='$($m.c)' stroke-width='1.2' stroke-dasharray='6 4'/>")
    [void]$sb.Append("<text x='$($res.X[$bl.c1])' y='$($res.Y[$bl.r1]+19)' font-size='15' font-weight='bold' fill='$($m.c)'>$(Esc $m.n)</text>")
  }
  foreach ($ru in $res.Rutas) {
    $id = $res.Rutas.IndexOf($ru); $pts = $res.Polis[$id]
    $color = $MOD[$modDe[$ru.e.padre]].c
    $d = 'M ' + (($pts | ForEach-Object { "$($_[0]) $($_[1])" }) -join ' L ')
    $uno = $ru.e.hija -eq 'CREDENCIAL_ACCESO'
    [void]$sb.Append("<path d='$d' fill='none' stroke='$color' stroke-width='1.5' stroke-linejoin='round'/><path d='$(Extremos $pts $uno)' fill='none' stroke='$color' stroke-width='1.5'/>")
  }
  foreach ($t in $cajas.Keys) { [void]$sb.Append((Caja $t $res.Pos[$t] $cajas[$t].modo)) }
  [void]$sb.Append('</g>')
  $ley = 'PK clave primaria · FK clave foránea · UQ única · * obligatoria (NOT NULL) · pata de gallo = lado «muchos» (tabla hija) · doble barra = lado «uno» (tabla padre) · color de la línea = módulo de la tabla padre.'
  [void]$sb.Append("<text x='$OX' y='$($alto-36)' font-size='11.5' fill='#566573'>$(Esc $ley)</text>")
  [void]$sb.Append("<text x='$OX' y='$($alto-16)' font-size='11' fill='#7F8C8D'>Generado desde los metadatos reales de ECOACCESS_BD (Oracle 26ai, FREEPDB1). FK dibujadas: $($ar.Count) · líneas superpuestas: $($chk.Solapes) · líneas que atraviesan tablas: $($chk.AtraviesaTablas) · cruces: $($chk.Cruces).</text></svg>")
  [IO.File]::WriteAllText("$aqui\..\$(if ($archivo -like 'MR_00*') { '03_MODELO_RELACIONAL_GENERAL' } else { '04_MODELO_RELACIONAL_POR_MODULOS' })\$archivo.svg", $sb.ToString(), $utf8)
  Write-Host "  $archivo iter=$($res.Iter) final=$($res.Final)"; $chk.Detalle | ForEach-Object { Write-Host "  SOLAPE $archivo : $_" }
  [pscustomobject]@{ Archivo = "$archivo.svg"; Ancho = $ancho; Alto = $alto; FK = $ar.Count; Solapes = $chk.Solapes; Atraviesa = $chk.AtraviesaTablas; Cruces = $chk.Cruces }
}

$out = @()
$out += Diagrama 'MR_00_general_por_modulos' 'Modelo relacional EcoAccess BD — vista general ordenada por módulos (19 tablas · 173 columnas · 38 FK)' 'Lectura: arriba las PERSONAS (módulo 2), abajo los CATÁLOGOS (módulo 1). En el centro, de izquierda a derecha: infraestructura → operación → EcoPuntos y auditoría.' ([ordered]@{
  ROL = 0, 0; USUARIO_ROL = 0, 1; USUARIO = 0, 2; CREDENCIAL_ACCESO = 0, 3; VINCULO_TELEGRAM = 0, 4
  PUNTO_INSTALACION = 1, 0; INTERACCION = 1, 1; CONSULTA_VISION = 1, 2; HISTORIAL_ESTADO_CONTENEDOR = 1, 3
  CONTENEDOR = 2, 0; ORDEN_APERTURA = 2, 1; EVENTO_DISPOSITIVO = 2, 2; MOVIMIENTO_ECOPUNTOS = 2, 3; REGLA_ECOPUNTOS = 2, 4
  CONTENEDOR_MATERIAL = 3, 0; DEPOSITO = 3, 2; AUDITORIA_DEPOSITO = 3, 3
  PESO_REFERENCIAL = 4, 1; MATERIAL = 4, 2
}) @(
  @{ m = 'M2'; r1 = 0; r2 = 0; c1 = 0; c2 = 4 }, @{ m = 'M3'; r1 = 1; r2 = 3; c1 = 0; c2 = 0 }, @{ m = 'M4'; r1 = 1; r2 = 3; c1 = 1; c2 = 2 },
  @{ m = 'M5'; r1 = 1; r2 = 3; c1 = 3; c2 = 4 }, @{ m = 'M1'; r1 = 4; r2 = 4; c1 = 0; c2 = 4 })

$out += Diagrama 'MR_M1_catalogos_base' 'Módulo 1 · Catálogos base' 'MATERIAL es el catálogo central (código, nombre, categoría de caneca). PESO_REFERENCIAL guarda pesos típicos por objeto. Arriba, en gris, las tablas que los usan.' ([ordered]@{
  CONTENEDOR_MATERIAL = 0, 0; INTERACCION = 0, 1; CONSULTA_VISION = 0, 2; DEPOSITO = 0, 3; REGLA_ECOPUNTOS = 0, 4
  PESO_REFERENCIAL = 1, 1; MATERIAL = 1, 2
}) @(@{ m = 'M1'; r1 = 1; r2 = 1; c1 = 0; c2 = 4 }) @('CONTENEDOR_MATERIAL', 'INTERACCION', 'CONSULTA_VISION', 'DEPOSITO', 'REGLA_ECOPUNTOS')

$out += Diagrama 'MR_M2_usuarios_identidad' 'Módulo 2 · Usuarios e identidad' 'USUARIO_ROL resuelve la relación N:M entre USUARIO y ROL. CREDENCIAL_ACCESO es 1:1 (la contraseña vive aparte, solo como hash). VINCULO_TELEGRAM une la cuenta con Telegram.' ([ordered]@{
  ROL = 0, 0; USUARIO_ROL = 0, 1; USUARIO = 0, 2; CREDENCIAL_ACCESO = 0, 3; VINCULO_TELEGRAM = 1, 2
}) @(@{ m = 'M2'; r1 = 0; r2 = 1; c1 = 0; c2 = 3 })

$out += Diagrama 'MR_M3_infraestructura_qr' 'Módulo 3 · Infraestructura y QR' 'Una estación (PUNTO_INSTALACION, con su código QR) tiene varias canecas (CONTENEDOR). CONTENEDOR_MATERIAL dice qué materiales admite cada caneca (N:M con MATERIAL).' ([ordered]@{
  PUNTO_INSTALACION = 0, 0; CONTENEDOR = 0, 1; CONTENEDOR_MATERIAL = 0, 2; MATERIAL = 0, 3
}) @(@{ m = 'M3'; r1 = 0; r2 = 0; c1 = 0; c2 = 2 }) @('MATERIAL')

$out += Diagrama 'MR_M4_operacion' 'Módulo 4 · Operación: sesión QR → foto → orden → evento → depósito' 'Las tablas grises son de otros módulos (solo su clave). Consultar una foto ≠ abrir la caneca ≠ registrar un depósito ≠ verificarlo.' ([ordered]@{
  USUARIO = 0, 2
  PUNTO_INSTALACION = 1, 0; INTERACCION = 1, 1; CONSULTA_VISION = 1, 2
  CONTENEDOR = 2, 0; ORDEN_APERTURA = 2, 1; EVENTO_DISPOSITIVO = 2, 2
  PESO_REFERENCIAL = 3, 1; DEPOSITO = 3, 2; MATERIAL = 3, 3
}) @(@{ m = 'M4'; r1 = 1; r2 = 3; c1 = 1; c2 = 2 }) @('USUARIO', 'PUNTO_INSTALACION', 'CONTENEDOR', 'PESO_REFERENCIAL', 'MATERIAL')

$out += Diagrama 'MR_M5_ecopuntos_auditoria' 'Módulo 5 · EcoPuntos y auditoría' 'El saldo es la SUMA de MOVIMIENTO_ECOPUNTOS (nunca se guarda). AUDITORIA_DEPOSITO e HISTORIAL_ESTADO_CONTENEDOR las llenan solo los disparadores.' ([ordered]@{
  USUARIO = 0, 1
  HISTORIAL_ESTADO_CONTENEDOR = 1, 0; MOVIMIENTO_ECOPUNTOS = 1, 1; REGLA_ECOPUNTOS = 1, 2; MATERIAL = 1, 3
  CONTENEDOR = 2, 0; DEPOSITO = 2, 1; AUDITORIA_DEPOSITO = 2, 2
}) @(@{ m = 'M5'; r1 = 1; r2 = 2; c1 = 0; c2 = 2 }) @('USUARIO', 'MATERIAL', 'CONTENEDOR', 'DEPOSITO')

$out | Format-Table -AutoSize | Out-String -Width 200

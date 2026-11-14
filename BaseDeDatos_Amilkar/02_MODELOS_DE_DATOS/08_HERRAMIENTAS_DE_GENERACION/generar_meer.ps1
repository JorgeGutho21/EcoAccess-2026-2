<#
  Genera el MEER (modelo entidad-relación EXTENDIDO) de EcoAccess en SVG:
   1. USUARIO se especializa en CIUDADANO, OPERARIO y ADMINISTRADOR:
      especialización SOLAPADA (o) y TOTAL (doble línea): toda persona tiene al menos un rol
      y puede tener varios (el «Operario Demo Uno» es Ciudadano y Operario).
      Cada subclase participa en relaciones propias (eso es lo que justifica dibujarlas).
   2. DEPOSITO se especializa en CON SENSOR y SIN SENSOR: DISJUNTA (d) y TOTAL. Discriminante: sin_sensor.
   3. MOVIMIENTO_ECOPUNTOS se especializa en CRÉDITO y REVERSO: DISJUNTA (d) y TOTAL. Discriminante: tipo.
  La implementación relacional de cada especialización se explica en el recuadro inferior.
  Uso: pwsh -File generar_meer.ps1   (salida: EXPORTACIONES\MEER_extendido.svg)
#>
$aqui = Split-Path -Parent $MyInvocation.MyCommand.Path
$W = 1500; $H = 1180
$sb = New-Object Text.StringBuilder
function A([string]$s) { [void]$sb.AppendLine($s) }
function Esc([string]$t) { [Net.WebUtility]::HtmlEncode($t) }
function Entidad($x, $y, $nombre, $color = '#1E6B3A', $w = 210, $h = 52, $sub = '') {
    A "<rect x='$($x - $w/2)' y='$($y - $h/2)' width='$w' height='$h' rx='4' fill='#FFFFFF' stroke='$color' stroke-width='2.5'/>"
    A "<text x='$x' y='$($y + 6 - $(if ($sub) { 8 } else { 0 }))' text-anchor='middle' font-size='17' font-weight='700' fill='$color'>$(Esc $nombre)</text>"
    if ($sub) { A "<text x='$x' y='$($y + 16)' text-anchor='middle' font-size='12' fill='#555'>$(Esc $sub)</text>" }
}
function Rombo($x, $y, $texto, $w = 150, $h = 64) {
    A "<polygon points='$($x - $w/2),$y $x,$($y - $h/2) $($x + $w/2),$y $x,$($y + $h/2)' fill='#F4F8F4' stroke='#2E4D3A' stroke-width='1.8'/>"
    A "<text x='$x' y='$($y + 5)' text-anchor='middle' font-size='13' fill='#2E4D3A'>$(Esc $texto)</text>"
}
function Linea($x1, $y1, $x2, $y2, $doble = $false, $etq = '', $ex = 0, $ey = 0) {
    if ($doble) {
        $dx = $x2 - $x1; $dy = $y2 - $y1; $l = [Math]::Sqrt($dx * $dx + $dy * $dy); $nx = - $dy / $l * 3; $ny = $dx / $l * 3
        A "<line x1='$($x1 + $nx)' y1='$($y1 + $ny)' x2='$($x2 + $nx)' y2='$($y2 + $ny)' stroke='#333' stroke-width='1.6'/>"
        A "<line x1='$($x1 - $nx)' y1='$($y1 - $ny)' x2='$($x2 - $nx)' y2='$($y2 - $ny)' stroke='#333' stroke-width='1.6'/>"
    } else { A "<line x1='$x1' y1='$y1' x2='$x2' y2='$y2' stroke='#333' stroke-width='1.6'/>" }
    if ($etq) { A "<text x='$ex' y='$ey' text-anchor='middle' font-size='14' font-weight='700' fill='#B00020'>$etq</text>" }
}
function Circulo($x, $y, $letra) {
    A "<circle cx='$x' cy='$y' r='17' fill='#FFF8E1' stroke='#8A6D00' stroke-width='2'/>"
    A "<text x='$x' y='$($y + 6)' text-anchor='middle' font-size='17' font-weight='700' fill='#8A6D00'>$letra</text>"
}
function Subconjunto($x1, $y1, $x2, $y2) {
    # símbolo ⊂ a mitad de la línea que va del círculo a la subclase
    $mx = ($x1 + $x2) / 2; $my = ($y1 + $y2) / 2
    $ang = [Math]::Atan2($y2 - $y1, $x2 - $x1) * 180 / [Math]::PI
    A "<path d='M -7 -8 A 8 8 0 0 0 -7 8' transform='translate($mx,$my) rotate($ang)' fill='none' stroke='#8A6D00' stroke-width='2'/>"
}
function Nota($x, $y, $w, $lineas, $color = '#555', $tam = 13) {
    $i = 0; foreach ($l in $lineas) { A "<text x='$x' y='$($y + $i * ($tam + 5))' font-size='$tam' fill='$color'>$(Esc $l)</text>"; $i++ }
}

A "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 $W $H' width='$W' height='$H' font-family='Segoe UI, Arial, sans-serif'>"
A "<rect width='$W' height='$H' fill='#FFFFFF'/>"
A "<text x='30' y='42' font-size='26' font-weight='700' fill='#1E6B3A'>MEER · Modelo entidad-relación extendido de EcoAccess</text>"
A "<text x='30' y='68' font-size='14' fill='#555'>Especializaciones vigentes en P3 (no se copia la especialización de la V3). o = solapada · d = disjunta · doble línea = participación total · ⊂ = subclase.</text>"

# ---------- Panel 1: roles
A "<rect x='20' y='88' width='1460' height='610' rx='8' fill='none' stroke='#C8D2C8' stroke-dasharray='6 4'/>"
A "<text x='36' y='112' font-size='15' font-weight='700' fill='#2E4D3A'>1. Roles de la persona: especialización SOLAPADA y TOTAL</text>"
Entidad 750 160 'USUARIO' '#1E6B3A' 230 56 'id_usuario, nombre, correo, activo'
Circulo 750 250 'o'
Linea 750 188 750 233 $true
foreach ($s in @(@(330, 'CIUDADANO', 'rol 1'), @(750, 'OPERARIO', 'rol 2'), @(1170, 'ADMINISTRADOR', 'rol 3'))) {
    Linea 750 267 $s[0] 334; Subconjunto 750 267 $s[0] 334
    Entidad $s[0] 360 $s[1] '#2E6DA4' 220 52 $s[2]
}
# relaciones propias de cada subclase
Rombo 190 480 'realiza'; Linea 300 386 190 448 $false '1' 236 420; Entidad 190 610 'INTERACCION' '#B03A2E' 200 50; Linea 190 512 190 585 $false 'N' 204 556
Rombo 470 480 'acumula'; Linea 360 386 470 448 $false '1' 426 420; Entidad 470 610 'MOVIMIENTO_ECOPUNTOS' '#7D3C98' 250 50; Linea 470 512 470 585 $false 'N' 484 556
Rombo 870 480 'verifica'; Linea 750 386 845 464 $false '1' 790 430; Linea 1100 386 895 464 $false '1' 1000 418; Entidad 870 610 'DEPOSITO' '#B03A2E' 200 50; Linea 870 512 870 585 $false 'N' 884 556
Rombo 1170 480 'cambia estado'; Linea 1170 386 1170 448 $false '1' 1184 424; Entidad 1170 610 'CONTENEDOR' '#1E6B3A' 200 50; Linea 1170 512 1170 585 $false 'N' 1184 556
Rombo 1380 480 'registra tarifa' 140 64; Linea 1240 386 1360 462 $false '1' 1310 414; Entidad 1380 610 'REGLA_ECOPUNTOS' '#7D3C98' 180 50; Linea 1380 512 1380 585 $false 'N' 1394 556
Nota 36 662 1400 @('Regla del negocio: quien verifica un depósito debe ser OPERARIO o ADMINISTRADOR y distinto del CIUDADANO titular (trigger TRG_DEPOSITO_COHERENCIA, ORA-20101/20104).', 'Solo el ADMINISTRADOR cambia el estado de una caneca y registra tarifas (PKG_CONTEXTO_APP.exigir_rol, ORA-20120).') '#5B3A00' 13

# ---------- Panel 2: depósito
A "<rect x='20' y='712' width='720' height='300' rx='8' fill='none' stroke='#C8D2C8' stroke-dasharray='6 4'/>"
A "<text x='36' y='736' font-size='15' font-weight='700' fill='#2E4D3A'>2. Evidencia del depósito: especialización DISJUNTA y TOTAL</text>"
Entidad 380 790 'DEPOSITO' '#B03A2E' 220 52 'discriminante: sin_sensor'
Circulo 380 878 'd'; Linea 380 816 380 861 $true
foreach ($s in @(@(190, 'CON SENSOR', 'sin_sensor = 0 · evento tipo 3'), @(570, 'SIN SENSOR', 'sin_sensor = 1 · evento tipo 6'))) {
    Linea 380 895 $s[0] 952; Subconjunto 380 895 $s[0] 952; Entidad $s[0] 975 $s[1] '#B03A2E' 260 48 $s[2]
}
# ---------- Panel 3: movimientos
A "<rect x='760' y='712' width='720' height='300' rx='8' fill='none' stroke='#C8D2C8' stroke-dasharray='6 4'/>"
A "<text x='776' y='736' font-size='15' font-weight='700' fill='#2E4D3A'>3. Libro de EcoPuntos: especialización DISJUNTA y TOTAL</text>"
Entidad 1120 790 'MOVIMIENTO_ECOPUNTOS' '#7D3C98' 260 52 'discriminante: tipo'
Circulo 1120 878 'd'; Linea 1120 816 1120 861 $true
Linea 1120 895 930 952; Subconjunto 1120 895 930 952; Entidad 930 975 'CRÉDITO' '#7D3C98' 200 48 'tipo = 1 · puntos > 0'
Linea 1120 895 1310 952; Subconjunto 1120 895 1310 952; Entidad 1310 975 'REVERSO' '#7D3C98' 200 48 'tipo = 2 · puntos < 0'
Rombo 1120 975 'revierte' 120 50; Linea 1030 975 1060 975 $false '1' 1045 965; Linea 1180 975 1210 975 $false '1' 1195 965

# ---------- Implementación relacional
A "<rect x='20' y='1026' width='1460' height='140' rx='8' fill='#F4F8F4' stroke='#9DB09D'/>"
Nota 36 1050 1420 @(
    'Cómo se implementa cada especialización en las 19 tablas (y por qué):',
    '• Roles → tabla puente USUARIO_ROL (N:M) + catálogo ROL. Las subclases no tienen atributos propios, así que una tabla por rol solo duplicaría USUARIO; la superposición (un operario que también es ciudadano) cabe naturalmente en N:M.',
    '• Depósito → una sola tabla DEPOSITO con la columna discriminante sin_sensor y un CHECK; el trigger 2 exige que coincida con el tipo de evento (3 detección / 6 cierre sin sensor).',
    '• Movimiento → una sola tabla MOVIMIENTO_ECOPUNTOS con tipo (1 crédito / 2 reverso), CHECK de signo de puntos y FK id_movimiento_revertido para la relación «revierte» (1:1).',
    'Se descarta la especialización de la V3 (CIUDADANO/SUPERVISOR/OPERARIO/ADMINISTRADOR como tablas): el rol SUPERVISOR no existe en P3 y los subtipos no tenían atributos que P3 use.') '#2E4D3A' 13
A '</svg>'
$dest = Join-Path $aqui '..\02_MEER'; New-Item -ItemType Directory -Force $dest | Out-Null
[IO.File]::WriteAllText((Join-Path $dest 'MEER_extendido.svg'), $sb.ToString(), (New-Object Text.UTF8Encoding $false))
"MEER_extendido.svg ($W x $H)"

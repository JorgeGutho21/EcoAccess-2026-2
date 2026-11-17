<#
  Diagrama del FLUJO DE LOS DATOS de EcoAccess (de la persona a Power BI), en 11 pasos.
  Cada caja dice qué pasa y qué tablas se escriben; el color es el del módulo.
  Salida: EXPORTACIONES\FLUJO_datos_ecoaccess.svg (convertir_exportaciones.ps1 -Filtro FLUJO_*)
#>
$ErrorActionPreference = 'Stop'
$aqui = Split-Path -Parent $MyInvocation.MyCommand.Path
$COLORES = @{ M1 = @('#FCF3CF', '#9A7D0A'); M2 = @('#D6EAF8', '#2E6DA4'); M3 = @('#D5F5E3', '#1E8449'); M4 = @('#FADBD8', '#B03A2E'); M5 = @('#E8DAEF', '#6C3483'); BI = @('#EAECEE', '#424949') }
$pasos = @(
  ,@('M2', 'La persona existe', 'USUARIO · USUARIO_ROL · VINCULO_TELEGRAM', 'Tiene cuenta, rol y chat de Telegram.')
  ,@('M3', 'Escanea el QR de la estación', 'PUNTO_INSTALACION', 'El QR dice en qué estación está.')
  ,@('M4', 'Se abre la sesión', 'INTERACCION', 'Une persona + estación (+ caneca).')
  ,@('M4', 'Envía una foto a la IA', 'CONSULTA_VISION (+ MATERIAL, PESO_REFERENCIAL)', 'Solo orienta: NO es un depósito.')
  ,@('M3', 'El sistema elige la caneca', 'CONTENEDOR · CONTENEDOR_MATERIAL', 'fn_resolver_caneca_disponible.')
  ,@('M4', 'Orden de abrir la caneca', 'ORDEN_APERTURA', 'Idempotente: una vigente por caneca.')
  ,@('M4', 'La caneca informa', 'EVENTO_DISPOSITIVO', 'Acuse, apertura, detección, cierre.')
  ,@('M4', 'Se registra el depósito', 'DEPOSITO (estado 0 = pendiente)', 'El disparador 2 valida la coherencia.')
  ,@('M5', 'Un operario lo verifica', 'DEPOSITO → AUDITORIA_DEPOSITO', 'Disparador 3: :OLD/:NEW. El 4 bloquea cerrados.')
  ,@('M5', 'Se acreditan EcoPuntos', 'MOVIMIENTO_ECOPUNTOS (+ REGLA_ECOPUNTOS)', 'PKG_ECOPUNTOS.acreditar; saldo = SUMA.')
  ,@('BI', 'Power BI lo muestra', 'Vistas VW_BI_* (cuenta ECOACCESS_BD_LECTOR)', 'Solo lectura, con seudónimos.')
)
$bw = 360; $bh = 118; $gx = 56; $gy = 80; $porFila = 4; $x0 = 30; $y0 = 90
$pos = @()
for ($i = 0; $i -lt $pasos.Count; $i++) {
  $f = [math]::Floor($i / $porFila); $k = $i % $porFila; if ($f % 2 -eq 1) { $k = $porFila - 1 - $k }
  $pos += , @(($x0 + $k * ($bw + $gx)), ($y0 + $f * ($bh + $gy)))
}
$W = $x0 * 2 + $porFila * $bw + ($porFila - 1) * $gx; $H = $y0 + 3 * ($bh + $gy) - $gy + 30
$sb = New-Object Text.StringBuilder
[void]$sb.Append("<svg xmlns='http://www.w3.org/2000/svg' width='$W' height='$H' viewBox='0 0 $W $H' font-family='Segoe UI, Arial, sans-serif'><defs><marker id='f' markerWidth='10' markerHeight='10' refX='9' refY='5' orient='auto'><path d='M0,0 L10,5 L0,10 z' fill='#34495E'/></marker></defs><rect width='100%' height='100%' fill='#fff'/>")
[void]$sb.Append("<text x='$x0' y='38' font-size='22' font-weight='bold' fill='#1B2631'>Flujo de los datos en EcoAccess: de la persona a Power BI</text><text x='$x0' y='62' font-size='12.5' fill='#566573'>Se lee como una serpiente: fila 1 de izquierda a derecha, fila 2 de derecha a izquierda, fila 3 de izquierda a derecha. Aparte: cada cambio de estado de una caneca queda en HISTORIAL_ESTADO_CONTENEDOR (disparador 1).</text>")
for ($i = 0; $i -lt $pasos.Count - 1; $i++) {
  $a = $pos[$i]; $b = $pos[$i + 1]
  if ($a[1] -eq $b[1]) {
    if ($b[0] -gt $a[0]) { $x1 = $a[0] + $bw; $x2 = $b[0] - 2 } else { $x1 = $a[0]; $x2 = $b[0] + $bw + 2 }
    $y = $a[1] + $bh / 2; [void]$sb.Append("<line x1='$x1' y1='$y' x2='$x2' y2='$y' stroke='#34495E' stroke-width='2.2' marker-end='url(#f)'/>")
  } else { $x = $a[0] + $bw / 2; [void]$sb.Append("<line x1='$x' y1='$($a[1]+$bh)' x2='$x' y2='$($b[1]-2)' stroke='#34495E' stroke-width='2.2' marker-end='url(#f)'/>") }
}
for ($i = 0; $i -lt $pasos.Count; $i++) {
  $p = $pasos[$i]; $c = $COLORES[$p[0]]; $x = $pos[$i][0]; $y = $pos[$i][1]
  [void]$sb.Append("<rect x='$x' y='$y' width='$bw' height='$bh' rx='10' fill='$($c[0])' stroke='$($c[1])' stroke-width='2'/>")
  [void]$sb.Append("<circle cx='$($x+22)' cy='$($y+24)' r='14' fill='$($c[1])'/><text x='$($x+22)' y='$($y+29)' font-size='14' font-weight='bold' fill='#fff' text-anchor='middle'>$($i+1)</text>")
  [void]$sb.Append("<text x='$($x+44)' y='$($y+30)' font-size='15' font-weight='bold' fill='#1B2631'>$($p[1])</text>")
  [void]$sb.Append("<text x='$($x+14)' y='$($y+62)' font-size='12' font-weight='bold' fill='$($c[1])'>$([Net.WebUtility]::HtmlEncode($p[2]))</text>")
  [void]$sb.Append("<text x='$($x+14)' y='$($y+90)' font-size='12' fill='#34495E'>$([Net.WebUtility]::HtmlEncode($p[3]))</text>")
}
[void]$sb.Append('</svg>')   # sin el cierre, el SVG no abre como imagen (XML incompleto)
[IO.File]::WriteAllText("$aqui\..\06_FLUJO_DE_DATOS\FLUJO_datos_ecoaccess.svg", $sb.ToString(), (New-Object Text.UTF8Encoding $false))
"FLUJO_datos_ecoaccess.svg ($W x $H)"

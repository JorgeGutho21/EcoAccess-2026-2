<#
  MER conceptual (notación de Chen) de EcoAccess BD — versión 2, ordenada por módulos.
  Rectángulo = entidad · rombo = relación · número junto a cada entidad = cardinalidad.
  Las entidades están en una cuadrícula y las líneas solo van en horizontal o vertical:
  ninguna línea cruza a otra ni pasa por encima de una entidad (lo comprueba el script).
  El color de cada entidad es el de su módulo (los mismos 5 del modelo relacional).
  Las tablas de auditoría no aparecen: son registros técnicos que crean los disparadores.
  Uso: pwsh -File generar_mer.ps1   (luego convertir_exportaciones.ps1 -Filtro MER_*)
#>
$ErrorActionPreference = 'Stop'
$aqui = Split-Path -Parent $MyInvocation.MyCommand.Path
$cx = @(170, 520, 870, 1220); $ry = @(70, 200, 370, 540, 710, 870)
$MOD = @{ M1 = @('#FCF3CF', '#9A7D0A', 'M1 Catálogos base'); M2 = @('#D6EAF8', '#2E6DA4', 'M2 Usuarios e identidad'); M3 = @('#D5F5E3', '#1E8449', 'M3 Infraestructura y QR'); M4 = @('#FADBD8', '#B03A2E', 'M4 Operación'); M5 = @('#E8DAEF', '#6C3483', 'M5 EcoPuntos y auditoría') }
# clave = columna, fila, texto, módulo
$E = [ordered]@{
  CRED = @(1, 0, 'CREDENCIAL DE ACCESO', 'M2'); ROL = @(0, 1, 'ROL', 'M2'); USU = @(1, 1, 'USUARIO', 'M2'); VIN = @(2, 1, 'VÍNCULO TELEGRAM', 'M2')
  EST = @(0, 2, 'ESTACIÓN', 'M3'); SES = @(1, 2, 'SESIÓN QR (interacción)', 'M4'); CON = @(2, 2, 'CONSULTA VISUAL (foto)', 'M4')
  CAN = @(0, 3, 'CANECA', 'M3'); ORD = @(1, 3, 'ORDEN DE APERTURA', 'M4'); DEP = @(2, 3, 'DEPÓSITO', 'M4'); MOV = @(3, 3, 'MOVIMIENTO ECOPUNTOS', 'M5')
  EVE = @(1, 4, 'EVENTO DEL DISPOSITIVO', 'M4'); MAT = @(2, 4, 'MATERIAL', 'M1'); REG = @(3, 4, 'TARIFA (regla EcoPuntos)', 'M5')
  PES = @(2, 5, 'PESO REFERENCIAL', 'M1')
}
$ew = 210; $eh = 54
function P($k) { @($cx[$E[$k][0]], $ry[$E[$k][1]]) }
function Lado($k, $lado, $d = 0) { $p = P $k; switch ($lado) { 'arr' { @(($p[0] + $d), ($p[1] - $eh / 2)) } 'aba' { @(($p[0] + $d), ($p[1] + $eh / 2)) } 'izq' { @(($p[0] - $ew / 2), ($p[1] + $d)) } 'der' { @(($p[0] + $ew / 2), ($p[1] + $d)) } } }
# relación: origen, destino, verbo, card. origen, card. destino, puntos de la línea (ortogonal)
$RELS = @(
  ,@('USU', 'ROL', 'tiene', 'N', 'M', @((Lado USU izq), (Lado ROL der)))
  ,@('USU', 'CRED', 'posee', '1', '0..1', @((Lado USU arr), (Lado CRED aba)))
  ,@('USU', 'VIN', 'vincula', '1', 'N', @((Lado USU der), (Lado VIN izq)))
  ,@('USU', 'SES', 'inicia', '0..1', 'N', @((Lado USU aba), (Lado SES arr)))
  ,@('EST', 'SES', 'origina', '1', 'N', @((Lado EST der), (Lado SES izq)))
  ,@('EST', 'CAN', 'contiene', '1', 'N (máx. 3 activas)', @((Lado EST aba), (Lado CAN arr)))
  ,@('SES', 'CON', 'incluye', '0..1', 'N', @((Lado SES der), (Lado CON izq)))
  ,@('SES', 'ORD', 'genera', '1', '0..1', @((Lado SES aba), (Lado ORD arr)))
  ,@('CAN', 'ORD', 'abre', '1', 'N', @((Lado CAN der), (Lado ORD izq)))
  ,@('ORD', 'EVE', 'reporta', '1', 'N', @((Lado ORD aba), (Lado EVE arr)))
  ,@('CAN', 'EVE', 'informa', '1', 'N', @((Lado CAN aba 40), @(($cx[0] + 40), $ry[4]), (Lado EVE izq)))
  ,@('ORD', 'DEP', 'produce', '1', '0..1', @((Lado ORD der), (Lado DEP izq)))
  ,@('USU', 'DEP', 'verifica', '0..1', 'N', @((Lado USU aba 70), @(($cx[1] + 70), (($ry[1] + $ry[2]) / 2)), @((($cx[2] + $cx[3]) / 2), (($ry[1] + $ry[2]) / 2)), @((($cx[2] + $cx[3]) / 2), (($ry[2] + $ry[3]) / 2)), @(($cx[2] + 60), (($ry[2] + $ry[3]) / 2)), (Lado DEP arr 60)))
  ,@('DEP', 'MAT', 'clasifica', 'N', '1', @((Lado DEP aba), (Lado MAT arr)))
  ,@('DEP', 'MOV', 'acredita', '1', 'N', @((Lado DEP der), (Lado MOV izq)))
  ,@('REG', 'MOV', 'aplica', '1', 'N', @((Lado REG arr), (Lado MOV aba)))
  ,@('MAT', 'REG', 'tarifa', '1', 'N', @((Lado MAT der), (Lado REG izq)))
  ,@('MAT', 'PES', 'pesa', '1', 'N', @((Lado MAT aba), (Lado PES arr)))
  ,@('CAN', 'MAT', 'recibe', 'N', 'M', @((Lado CAN aba -60), @(($cx[0] - 60), (($ry[4] + $ry[5]) / 2)), @(($cx[2] - 60), (($ry[4] + $ry[5]) / 2)), (Lado MAT aba -60)))
)
# --- comprobación: ningún tramo cruza otro ni atraviesa una entidad ---
$tr = @(); $i = 0
foreach ($r in $RELS) { $p = $r[5]; for ($j = 0; $j -lt $p.Count - 1; $j++) { $tr += , [double[]]@($i, $p[$j][0], $p[$j][1], $p[$j + 1][0], $p[$j + 1][1]) }; $i++ }
$cruces = 0; $choques = 0
for ($a = 0; $a -lt $tr.Count; $a++) { for ($b = $a + 1; $b -lt $tr.Count; $b++) {
    $s = $tr[$a]; $t = $tr[$b]; if ($s[0] -eq $t[0]) { continue }
    $sH = $s[2] -eq $s[4]; $tH = $t[2] -eq $t[4]
    if ($sH -ne $tH) { $h = if ($sH) { $s } else { $t }; $v = if ($sH) { $t } else { $s }
      if ($v[1] -gt [math]::Min($h[1], $h[3]) -and $v[1] -lt [math]::Max($h[1], $h[3]) -and $h[2] -gt [math]::Min($v[2], $v[4]) -and $h[2] -lt [math]::Max($v[2], $v[4])) { $cruces++ } } } }
foreach ($s in $tr) { foreach ($k in $E.Keys) { $c = P $k
    if ([math]::Max($s[1], $s[3]) -gt $c[0] - $ew / 2 + 1 -and [math]::Min($s[1], $s[3]) -lt $c[0] + $ew / 2 - 1 -and [math]::Max($s[2], $s[4]) -gt $c[1] - $eh / 2 + 1 -and [math]::Min($s[2], $s[4]) -lt $c[1] + $eh / 2 - 1) { $choques++ } } }
if ($cruces -or $choques) { throw "MER con $cruces cruces y $choques líneas sobre entidades" }

$W = 1470; $H = 1080
$sb = New-Object Text.StringBuilder
[void]$sb.Append("<svg xmlns='http://www.w3.org/2000/svg' width='$W' height='$H' viewBox='0 -70 $W $H' font-family='Segoe UI, Arial, sans-serif'><rect x='0' y='-70' width='$W' height='$H' fill='#fff'/>")
[void]$sb.Append("<text x='20' y='-38' font-size='21' font-weight='bold' fill='#1B2631'>MER conceptual EcoAccess BD (notación de Chen) — ordenado por módulos</text>")
[void]$sb.Append("<text x='20' y='-16' font-size='12' fill='#566573'>15 entidades · 19 relaciones · 0 cruces de líneas. Los atributos de cada entidad están en el diccionario de datos. La auditoría se omite: no es un concepto del negocio.</text>")
foreach ($r in $RELS) {
  $p = $r[5]
  [void]$sb.Append("<polyline points='$(($p | ForEach-Object { "$($_[0]),$($_[1])" }) -join ' ')' fill='none' stroke='#5D6D7E' stroke-width='1.6'/>")
  # rombo en el tramo más largo
  $mejor = 0; $len = 0; for ($j = 0; $j -lt $p.Count - 1; $j++) { $l = [math]::Abs($p[$j + 1][0] - $p[$j][0]) + [math]::Abs($p[$j + 1][1] - $p[$j][1]); if ($l -gt $len) { $len = $l; $mejor = $j } }
  $mx = ($p[$mejor][0] + $p[$mejor + 1][0]) / 2; $my = ($p[$mejor][1] + $p[$mejor + 1][1]) / 2
  $dw = 46 + 6.5 * $r[2].Length; $dh = 32
  [void]$sb.Append("<polygon points='$($mx-$dw/2),$my $mx,$($my-$dh/2) $($mx+$dw/2),$my $mx,$($my+$dh/2)' fill='#FEF9E7' stroke='#B7950B' stroke-width='1.5'/><text x='$mx' y='$($my+4)' font-size='11.5' text-anchor='middle' fill='#7D6608'>$($r[2])</text>")
  foreach ($q in @(@($p[0], $p[1], $r[3]), @($p[-1], $p[-2], $r[4]))) {
    $a = $q[0]; $b = $q[1]; $dx = [math]::Sign($b[0] - $a[0]); $dy = [math]::Sign($b[1] - $a[1])
    $tx = $a[0] + $dx * 14 + $(if ($dy -ne 0) { 6 } else { 0 }); $ty = $a[1] + $dy * 16 + $(if ($dx -ne 0) { -6 } else { 4 })
    $anc = if ($dx -lt 0) { 'end' } else { 'start' }
    [void]$sb.Append("<text x='$tx' y='$ty' font-size='12.5' font-weight='bold' fill='#C0392B' text-anchor='$anc'>$($q[2])</text>")
  }
}
foreach ($k in $E.Keys) {
  $c = P $k; $m = $MOD[$E[$k][3]]
  [void]$sb.Append("<rect x='$($c[0]-$ew/2)' y='$($c[1]-$eh/2)' width='$ew' height='$eh' rx='5' fill='$($m[0])' stroke='$($m[1])' stroke-width='2'/><text x='$($c[0])' y='$($c[1]+5)' font-size='13' font-weight='bold' text-anchor='middle' fill='#1B2631'>$($E[$k][2])</text>")
}
$lx = 20
foreach ($k in 'M1', 'M2', 'M3', 'M4', 'M5') { $m = $MOD[$k]; [void]$sb.Append("<rect x='$lx' y='945' width='16' height='16' fill='$($m[0])' stroke='$($m[1])' stroke-width='2'/><text x='$($lx+22)' y='958' font-size='12' fill='#1B2631'>$($m[2])</text>"); $lx += 250 }
[void]$sb.Append("<text x='20' y='992' font-size='11.5' fill='#566573'>Lectura: «un USUARIO inicia N SESIONES QR; una SESIÓN pertenece a 0..1 usuario (visitante sin cuenta)». Las relaciones N:M se implementan con tabla puente (USUARIO_ROL, CONTENEDOR_MATERIAL).</text></svg>")
[IO.File]::WriteAllText("$aqui\..\01_MER\MER_conceptual.svg", $sb.ToString(), (New-Object Text.UTF8Encoding $false))
"MER_conceptual.svg — 15 entidades, $($RELS.Count) relaciones, $cruces cruces, $choques choques"

<#
.SYNOPSIS
    Escribe ECOACCESS_GUIA_DEMOSTRACION_FERIA.txt en el escritorio con las cuentas de demostración recomendadas para cada
    escenario, elegidas con datos REALES del lote en Oracle (consulta de solo lectura).

.DESCRIPTION
    No contiene contraseñas, nombres, teléfonos ni documentos: solo el correo de demostración (@feria.ecoaccess.test), el
    módulo que hay que abrir y lo que debe verse, con las cifras del momento en que se generó. Las contraseñas están en
    ECOACCESS_CREDENCIALES_FERIA_2026.txt, que solo existe en el equipo donde se creó el lote y NO se genera aquí.
    El archivo no se sube a GitHub (está en el escritorio, fuera del repositorio).

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\feria\Generar-GuiaDemostracion.ps1
#>
[CmdletBinding()]
param(
    [ValidatePattern('^SIM-[A-Z0-9-]{4,36}$')][string]$Lote = 'SIM-FERIA-2026',
    [string]$Destino = ([Environment]::GetFolderPath('Desktop'))
)
. (Join-Path $PSScriptRoot 'ComunOracle.ps1')
$cn = Open-OracleSoloLectura
$p = @{ lote = $Lote }
try {
    if ([int](Invoke-Escalar -Conexion $cn -Parametros $p -Sql "SELECT COUNT(*) FROM lote_simulacion WHERE id_lote=:lote AND estado='COMPLETO'") -ne 1) {
        throw "El lote $Lote no existe o no está COMPLETO en esta base: no se genera la guía."
    }
    # Una fila por ciudadano del lote con sus indicadores reales.
    $ciu = Invoke-Consulta -Conexion $cn -Parametros $p -Sql @"
WITH c AS (SELECT r.id_registro id FROM registro_simulacion r WHERE r.id_lote=:lote AND r.tabla='USUARIO' AND r.clave LIKE 'ciu-%'),
     d AS (SELECT i.id_usuario id, dp.id_deposito, dp.fecha_deteccion_utc f, pkg_deposito.estado(dp.id_deposito) e
             FROM deposito dp JOIN orden_apertura o ON o.id_orden=dp.id_orden JOIN interaccion i ON i.id_interaccion=o.id_interaccion
            WHERE dp.id_deposito IN (SELECT id_registro FROM registro_simulacion WHERE id_lote=:lote AND tabla='DEPOSITO'))
SELECT u.correo_electronico correo, TO_CHAR(u.fecha_registro_utc - INTERVAL '5' HOUR,'YYYY-MM-DD') registro,
       TRUNC(CAST(SYS_EXTRACT_UTC(SYSTIMESTAMP) AS DATE) - CAST(u.fecha_registro_utc AS DATE)) dias_cuenta,
       (SELECT COUNT(*) FROM d WHERE d.id=c.id) depositos,
       (SELECT COUNT(*) FROM d WHERE d.id=c.id AND d.e=0) pendientes,
       (SELECT COUNT(*) FROM d WHERE d.id=c.id AND d.e=2) rechazados,
       (SELECT COUNT(*) FROM d WHERE d.id=c.id AND d.f BETWEEN SYS_EXTRACT_UTC(SYSTIMESTAMP)-17 AND SYS_EXTRACT_UTC(SYSTIMESTAMP)-11) hace2sem,
       (SELECT COUNT(*) FROM d WHERE d.id=c.id AND d.f BETWEEN SYS_EXTRACT_UTC(SYSTIMESTAMP)-24 AND SYS_EXTRACT_UTC(SYSTIMESTAMP)-18) hace3sem,
       (SELECT COUNT(*) FROM d WHERE d.id=c.id AND d.f >= SYS_EXTRACT_UTC(SYSTIMESTAMP)-14) ultimos14,
       NVL((SELECT SUM(m.puntos) FROM movimiento_ecopuntos m WHERE m.id_usuario=c.id),0) saldo
  FROM c JOIN usuario u ON u.id_usuario=c.id
"@
    $staff = Invoke-Consulta -Conexion $cn -Parametros $p -Sql @"
SELECT r.clave, u.correo_electronico correo,
       (SELECT COUNT(*) FROM auditoria_deposito a WHERE a.id_actor=u.id_usuario AND a.operacion='VERIFICACION') verificaciones,
       (SELECT COUNT(*) FROM auditoria_deposito a WHERE a.id_actor=u.id_usuario AND a.operacion='RECHAZO') rechazos,
       (SELECT COUNT(*) FROM operacion_ecopuntos o WHERE o.id_actor=u.id_usuario AND o.tipo=1) acreditaciones
  FROM registro_simulacion r JOIN usuario u ON u.id_usuario=r.id_registro
 WHERE r.id_lote=:lote AND r.tabla='USUARIO' AND REGEXP_LIKE(r.clave,'^(op|sup|adm)-')
"@
    $pend = [int](Invoke-Escalar -Conexion $cn -Parametros $p -Sql "SELECT COUNT(*) FROM registro_simulacion WHERE id_lote=:lote AND tabla='DEPOSITO' AND pkg_deposito.estado(id_registro)=0")
    $invitados = [int](Invoke-Escalar -Conexion $cn -Parametros $p -Sql "SELECT COUNT(*) FROM interaccion i WHERE i.id_usuario IS NULL AND i.canal<>2 AND i.id_interaccion IN (SELECT id_registro FROM registro_simulacion WHERE id_lote=:lote AND tabla='INTERACCION')")
} finally { $cn.Close() }

$filas = @($ciu.Rows)
function Mejor($lista, [scriptblock]$orden, [switch]$Desc) { @($lista | Sort-Object -Property $orden -Descending:$Desc)[0] }
$usados = New-Object System.Collections.Generic.HashSet[string]
function Elegir($lista, [scriptblock]$filtro, [scriptblock]$orden, [switch]$Desc) {
    $c = @($lista | Where-Object $filtro | Where-Object { -not $usados.Contains([string]$_.correo) } | Sort-Object -Property $orden -Descending:$Desc)
    if ($c.Count -eq 0) { return $null }
    [void]$usados.Add([string]$c[0].correo); return $c[0]
}
$esc = [ordered]@{}
$esc['Ciudadano con más Ecopuntos']          = Elegir $filas { $true } { [int]$_.saldo } -Desc
$esc['Ciudadano con mayor actividad']        = Elegir $filas { $true } { [int]$_.depositos } -Desc
$esc['Ciudadano recién registrado']          = Elegir $filas { $true } { [int]$_.dias_cuenta }
$esc['Ciudadano sin Ecopuntos']              = Elegir $filas { [int]$_.saldo -eq 0 -and [int]$_.depositos -gt 0 } { [int]$_.depositos } -Desc
$esc['Ciudadano frecuente']                  = Elegir $filas { [int]$_.ultimos14 -ge 2 } { [int]$_.ultimos14 } -Desc
$esc['Ciudadano con depósitos de hace dos semanas'] = Elegir $filas { [int]$_.hace2sem -gt 0 } { [int]$_.hace2sem } -Desc
$esc['Ciudadano con depósitos de hace tres semanas'] = Elegir $filas { [int]$_.hace3sem -gt 0 } { [int]$_.hace3sem } -Desc
$esc['Ciudadano con depósitos pendientes']   = Elegir $filas { [int]$_.pendientes -gt 0 } { [int]$_.pendientes } -Desc
$op = @($staff.Rows | Where-Object { $_.clave -like 'op-*' })
$opVerif = Mejor $op { [int]$_.verificaciones } -Desc
$opCred = @($op | Where-Object { $_.correo -ne $opVerif.correo } | Sort-Object { [int]$_.acreditaciones } -Descending)[0]
$sup = Mejor @($staff.Rows | Where-Object { $_.clave -like 'sup-*' }) { [int]$_.verificaciones + [int]$_.rechazos + [int]$_.acreditaciones } -Desc
$adm = @($staff.Rows | Where-Object { $_.clave -like 'adm-*' } | Sort-Object clave)[0]

$sb = New-Object System.Text.StringBuilder
function L([string]$t = '') { [void]$sb.AppendLine($t) }
L 'ECOACCESS · GUÍA DE DEMOSTRACIÓN · EXPOSOFTWARE 2026'
L "Lote de datos sintéticos $Lote · generada el $((Get-Date).ToString('yyyy-MM-dd HH:mm')) con datos leídos de Oracle."
L 'Las contraseñas NO están aquí: están en ECOACCESS_CREDENCIALES_FERIA_2026.txt (equipo principal).'
L 'Todas las cuentas son ficticias (@feria.ecoaccess.test) y su actividad es sintética: no son aperturas físicas.'
L 'AVISO: el Dashboard institucional solo separa como «Demostración y simulaciones» los canales Simulador y'
L 'Telegram simulado. Mientras no se aplique la corrección pendiente (docs\feria-2026\PENDIENTES.md, P1), los'
L 'depósitos del lote con canal Telegram, Manual o Asistido aparecen en «Datos operativos». Dilo al presentarlos.'
L ''
$n = 0
$modulo = @{
  'Ciudadano con más Ecopuntos' = 'Mis ecopuntos → «Tu saldo» y «Evolución»: el saldo más alto del lote.'
  'Ciudadano con mayor actividad' = 'Mis ecopuntos → «Mis depósitos y actividad»: el historial más largo del lote.'
  'Ciudadano recién registrado' = 'Mis ecopuntos: cuenta nueva con poca o ninguna actividad.'
  'Ciudadano sin Ecopuntos' = 'Mis ecopuntos: saldo 0 aunque tiene depósitos (pendientes, rechazados o de materiales sin tarifa vigente: no generan puntos).'
  'Ciudadano frecuente' = 'Mis ecopuntos → «Esta semana»: actividad reciente frente a la semana anterior.'
  'Ciudadano con depósitos de hace dos semanas' = 'Mis depósitos y actividad: filtra o recorre el historial hasta hace 11–17 días.'
  'Ciudadano con depósitos de hace tres semanas' = 'Mis depósitos y actividad: depósitos de hace 18–24 días.'
  'Ciudadano con depósitos pendientes' = 'Mis depósitos y actividad: depósitos «pendiente de verificación» sin puntos todavía.'
}
foreach ($k in $esc.Keys) {
    $f = $esc[$k]; $n++
    L "$n. $k"
    if ($null -eq $f) { L '   (ninguna cuenta del lote cumple este criterio)'; L ''; continue }
    L "   Correo: $($f.correo)"
    L "   Abrir: $($modulo[$k])"
    L ("   Debe verse: {0} depósitos ({1} pendientes, {2} rechazados), saldo {3} Ecopuntos, cuenta creada el {4}." -f $f.depositos, $f.pendientes, $f.rechazados, $f.saldo, $f.registro)
    L ''
}
$n++; L "$n. Operario con verificaciones pendientes"
L "   Correo: $($opVerif.correo)"
L "   Abrir: Área operario → «Depósitos» / «Verificar depósitos»: hay $pend depósitos del lote pendientes."
L ("   Debe verse: su historial de {0} verificaciones y {1} rechazos." -f $opVerif.verificaciones, $opVerif.rechazos)
L ''
$n++; L "$n. Operario con acreditaciones realizadas"
L "   Correo: $($opCred.correo)"
L '   Abrir: Área operario → «Ecopuntos»: acreditaciones hechas con PKG_ECOPUNTOS / ServicioEcopuntos.'
L ("   Debe verse: {0} acreditaciones." -f $opCred.acreditaciones)
L ''
$n++; L "$n. Supervisor con actividad"
L "   Correo: $($sup.correo)"
L '   Abrir: Área supervisor → «Dashboard institucional» (alcance «Demostración y simulaciones»), «Auditar depósitos», «Usuarios y roles», «Estadísticas».'
L ("   Debe verse: {0} verificaciones, {1} rechazos y {2} acreditaciones propias." -f $sup.verificaciones, $sup.rechazos, $sup.acreditaciones)
L ''
if ($adm) {
    $n++; L "$n. Administrador"
    L "   Correo: $($adm.correo)"
    L '   Abrir: Área supervisor → «Usuarios y roles», «Tarifas y ecopuntos», «Canecas y QR». Mismo rol 3 que el supervisor.'
    L ''
}
$n++; L "$n. Flujo de invitado"
L '   Sin cuenta: abrir EcoAccess Web o el bot de Telegram como visitante y consultar un residuo.'
L "   En Oracle el lote incluye $invitados visitas de invitados sin cuenta (con depósito, sin Ecopuntos)."

New-Item -ItemType Directory -Force -Path $Destino | Out-Null
$archivo = Join-Path $Destino 'ECOACCESS_GUIA_DEMOSTRACION_FERIA.txt'
[IO.File]::WriteAllText($archivo, $sb.ToString(), (New-Object Text.UTF8Encoding($true)))
Write-Host "Guía escrita en $archivo" -ForegroundColor Green

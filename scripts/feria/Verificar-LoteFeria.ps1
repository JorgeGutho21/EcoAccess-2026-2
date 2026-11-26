<#
.SYNOPSIS
    Verifica un lote de datos de feria (por defecto SIM-FERIA-2026) en Oracle SIN ESCRIBIR NADA.

.DESCRIPTION
    Abre la conexión configurada en este equipo (ECOACCESS_ORACLE_CADENA o User Secrets), fija la transacción en
    READ ONLY y comprueba integridad, fechas, materiales por caneca, Ecopuntos, separación de cuentas reales y ausencia
    de eventos de hardware real. Escribe dos archivos sin datos personales (solo conteos):
      <Salida>\verificacion-<lote>-<fecha>.md    informe legible (OK / FALLA / AVISO por comprobación)
      <Salida>\verificacion-<lote>-<fecha>.json  los mismos resultados para comparar ejecuciones
    Termina con código 1 si alguna comprobación FALLA. Sirve en la nube y en el esquema aislado ECOACCESS_P3_PRUEBAS*.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\feria\Verificar-LoteFeria.ps1
    powershell -ExecutionPolicy Bypass -File scripts\feria\Verificar-LoteFeria.ps1 -Lote SIM-FERIA-2026 -Salida $env:TEMP\feria
#>
[CmdletBinding()]
param(
    [ValidatePattern('^SIM-[A-Z0-9-]{4,36}$')][string]$Lote = 'SIM-FERIA-2026',
    [string]$Salida,
    # Plan esperado del lote (solo para comparar; no cambia nada).
    [int]$Ciudadanos = 50, [int]$Operarios = 8, [int]$Supervisores = 4, [int]$Administradores = 2, [int]$Registros = 650,
    # Segundos entre dos lecturas para saber si otro equipo sigue escribiendo el lote (0 = no medir).
    [int]$MedirProgreso = 20
)
. (Join-Path $PSScriptRoot 'ComunOracle.ps1')
$raiz = Get-RaizRepositorio
if (-not $Salida) { $Salida = Join-Path $raiz 'docs\feria-2026\verificaciones' }
New-Item -ItemType Directory -Force -Path $Salida | Out-Null

$cn = Open-OracleSoloLectura
$p = @{ lote = $Lote }
$resultados = New-Object System.Collections.Generic.List[object]
function Add-Resultado([string]$Id, [string]$Estado, [string]$Titulo, [string]$Detalle) {
    $resultados.Add([pscustomobject]@{ Id = $Id; Estado = $Estado; Titulo = $Titulo; Detalle = $Detalle })
    $color = @{ OK = 'Green'; FALLA = 'Red'; AVISO = 'Yellow'; INFO = 'Gray' }[$Estado]
    Write-Host ("[{0,-5}] {1} {2}: {3}" -f $Estado, $Id, $Titulo, $Detalle) -ForegroundColor $color
}
function N([string]$Sql) { [int](Invoke-Escalar -Conexion $cn -Sql $Sql -Parametros $p) }
function T([string]$Sql) { Invoke-Consulta -Conexion $cn -Sql $Sql -Parametros $p }
function Cero([string]$Id, [string]$Titulo, [string]$Sql, [string]$EstadoSiNo = 'FALLA') {
    $n = N $Sql
    if ($n -eq 0) { Add-Resultado $Id 'OK' $Titulo '0 filas' } else { Add-Resultado $Id $EstadoSiNo $Titulo "$n filas" }
}
function Lista($tabla, [string]$clave, [string]$valor) { ($tabla.Rows | ForEach-Object { "$($_[$clave])=$($_[$valor])" }) -join ', ' }

# Subconsultas del lote (todas parten de REGISTRO_SIMULACION).
$LU = "SELECT id_registro FROM registro_simulacion WHERE id_lote=:lote AND tabla='USUARIO'"
$LI = "SELECT id_registro FROM registro_simulacion WHERE id_lote=:lote AND tabla='INTERACCION'"
$LD = "SELECT id_registro FROM registro_simulacion WHERE id_lote=:lote AND tabla='DEPOSITO'"
$LO = "SELECT o.id_orden FROM orden_apertura o WHERE o.id_interaccion IN ($LI)"

# ── Conexión y lote ──────────────────────────────────────────────────────────────────────────────────────────────
$id = Get-IdentidadOracle -Conexion $cn
$entorno = if ($id.EsAislado) { 'esquema aislado' } elseif ($id.EsNube) { 'Oracle Cloud (producción)' } else { 'otra base' }
Add-Resultado 'C01' 'INFO' 'Conexión (solo lectura)' "$($id.Usuario) · $entorno"
$datosLote = T "SELECT estado, ciudadanos, registros, TO_CHAR(fecha_creacion_utc,'YYYY-MM-DD HH24:MI') creado, semilla FROM lote_simulacion WHERE id_lote=:lote"
if ($datosLote.Rows.Count -eq 0) {
    Add-Resultado 'C02' 'INFO' 'Lote' "$Lote no existe en esta base"
} else {
    $f = $datosLote.Rows[0]
    Add-Resultado 'C02' ($(if ($f.estado -eq 'COMPLETO') { 'OK' } else { 'AVISO' })) 'Lote' "$Lote · estado $($f.estado) · creado $($f.creado) UTC · semilla $($f.semilla)"
}
if ($MedirProgreso -gt 0 -and $datosLote.Rows.Count -gt 0) {
    $a = N "SELECT COUNT(*) FROM registro_simulacion WHERE id_lote=:lote"
    $m1 = N "SELECT COUNT(*) FROM movimiento_ecopuntos"
    Start-Sleep -Seconds $MedirProgreso
    # Otra transacción READ ONLY para ver lo confirmado después de la espera.
    $cn.Close(); $cn = Open-OracleSoloLectura
    $b = N "SELECT COUNT(*) FROM registro_simulacion WHERE id_lote=:lote"
    $m2 = N "SELECT COUNT(*) FROM movimiento_ecopuntos"
    if ($b -eq $a -and $m2 -eq $m1) { Add-Resultado 'C03' 'OK' 'Escritura en curso' "sin cambios en $MedirProgreso s ($b filas del lote)" }
    else { Add-Resultado 'C03' 'AVISO' 'Escritura en curso' "otro proceso sigue escribiendo: filas $a→$b, movimientos $m1→$m2 en $MedirProgreso s" }
}

# ── Cuentas ───────────────────────────────────────────────────────────────────────────────────────────────────────
$pref = T "SELECT REGEXP_SUBSTR(clave,'^[a-z]+') prefijo, COUNT(*) n FROM registro_simulacion WHERE id_lote=:lote AND tabla='USUARIO' GROUP BY REGEXP_SUBSTR(clave,'^[a-z]+') ORDER BY 1"
$cuenta = @{}; foreach ($r in $pref.Rows) { $cuenta[[string]$r.prefijo] = [int]$r.n }
$esperado = [ordered]@{ ciu = $Ciudadanos; op = $Operarios; sup = $Supervisores; adm = $Administradores }
$det = ($esperado.Keys | ForEach-Object { "$_ $([int]$cuenta[$_])/$($esperado[$_])" }) -join ' · '
$completo = -not ($esperado.Keys | Where-Object { [int]$cuenta[$_] -ne $esperado[$_] })
Add-Resultado 'C04' ($(if ($completo) { 'OK' } else { 'AVISO' })) 'Cuentas del lote por tipo' $det
Cero 'C05' 'Cuentas del lote fuera del dominio de demostración' "SELECT COUNT(*) FROM usuario WHERE id_usuario IN ($LU) AND correo_electronico NOT LIKE '%@feria.ecoaccess.test'"
Cero 'C06' 'Ciudadanos del lote con un rol distinto de 1' "SELECT COUNT(*) FROM registro_simulacion r JOIN usuario_rol ur ON ur.id_usuario=r.id_registro WHERE r.id_lote=:lote AND r.tabla='USUARIO' AND r.clave LIKE 'ciu-%' AND ur.id_rol<>1"
Cero 'C07' 'Operarios del lote sin rol 2 exclusivo' "SELECT COUNT(*) FROM registro_simulacion r WHERE r.id_lote=:lote AND r.tabla='USUARIO' AND r.clave LIKE 'op-%' AND (SELECT COUNT(*) FROM usuario_rol ur WHERE ur.id_usuario=r.id_registro AND ur.id_rol=2)<>1"
Cero 'C08' 'Supervisores/administradores del lote sin rol 3' "SELECT COUNT(*) FROM registro_simulacion r WHERE r.id_lote=:lote AND r.tabla='USUARIO' AND (r.clave LIKE 'sup-%' OR r.clave LIKE 'adm-%') AND NOT EXISTS (SELECT 1 FROM usuario_rol ur WHERE ur.id_usuario=r.id_registro AND ur.id_rol=3)"
Cero 'C09' 'Cuentas del lote sin contraseña (no podrían iniciar sesión)' "SELECT COUNT(*) FROM usuario u WHERE u.id_usuario IN ($LU) AND NOT EXISTS (SELECT 1 FROM credencial_acceso c WHERE c.id_usuario=u.id_usuario)" 'AVISO'
Cero 'C10' 'Cuentas del lote bloqueadas o con intentos fallidos' "SELECT COUNT(*) FROM credencial_acceso WHERE id_usuario IN ($LU) AND (intentos_fallidos>0 OR bloqueada_hasta_utc IS NOT NULL)" 'AVISO'
Add-Resultado 'C11' 'INFO' 'Cuentas reales (fuera del lote)' ("{0} usuarios" -f (N "SELECT COUNT(*) FROM usuario WHERE id_usuario NOT IN ($LU)"))

# ── Depósitos: cantidades, canecas, materiales, fechas ───────────────────────────────────────────────────────────
$nDep = N "SELECT COUNT(*) FROM registro_simulacion WHERE id_lote=:lote AND tabla='DEPOSITO'"
Add-Resultado 'C12' ($(if ($nDep -ge $Registros) { 'OK' } else { 'AVISO' })) 'Depósitos del lote' "$nDep de $Registros planeados"
$faltan = N "SELECT COUNT(*) FROM (SELECT LEVEL n FROM dual CONNECT BY LEVEL<=$Registros) x WHERE NOT EXISTS (SELECT 1 FROM registro_simulacion r WHERE r.id_lote=:lote AND r.tabla='DEPOSITO' AND r.clave='dep-'||LPAD(x.n,3,'0'))"
Add-Resultado 'C13' 'INFO' 'Claves dep-NNN sin depósito' "$faltan (los registros que el generador decidió no crear o no pudo crear)"
$colores = T "SELECT color_caneca, COUNT(*) n, ROUND(100*RATIO_TO_REPORT(COUNT(*)) OVER (),1) pct FROM vista_depositos_estado WHERE id_lote=:lote GROUP BY color_caneca ORDER BY 2 DESC"
Add-Resultado 'C14' 'INFO' 'Reparto por caneca (plan 45 % verde · 35 % blanca · 20 % negra)' (($colores.Rows | ForEach-Object { "$($_.color_caneca) $($_.n) ($($_.pct) %)" }) -join ' · ')
Cero 'C15' 'Material declarado que esa caneca no admite' "SELECT COUNT(*) FROM deposito d JOIN orden_apertura o ON o.id_orden=d.id_orden WHERE d.id_deposito IN ($LD) AND NOT EXISTS (SELECT 1 FROM contenedor_material cm WHERE cm.id_contenedor=o.id_contenedor AND cm.id_material=d.id_material_declarado)"
Cero 'C16' 'Material verificado que esa caneca no admite' "SELECT COUNT(*) FROM deposito d JOIN orden_apertura o ON o.id_orden=d.id_orden WHERE d.id_deposito IN ($LD) AND d.id_material_verificado IS NOT NULL AND NOT EXISTS (SELECT 1 FROM contenedor_material cm WHERE cm.id_contenedor=o.id_contenedor AND cm.id_material=d.id_material_verificado)"
Cero 'C17' 'Materiales deshabilitados en el lote' "SELECT COUNT(*) FROM deposito d JOIN material m ON m.id_material=d.id_material_declarado WHERE d.id_deposito IN ($LD) AND m.habilitado<>1"
$mat = T "SELECT material, COUNT(*) n FROM vista_depositos_estado WHERE id_lote=:lote GROUP BY material ORDER BY 2 DESC, 1"
Add-Resultado 'C18' 'INFO' "Reparto por material ($($mat.Rows.Count) materiales)" (Lista $mat 'material' 'n')
$tramos = T @"
SELECT tramo, COUNT(*) n FROM (
  SELECT CASE WHEN dias<=7 THEN '01-07' WHEN dias<=14 THEN '08-14' WHEN dias<=21 THEN '15-21' WHEN dias<=30 THEN '22-30'
              WHEN dias<=60 THEN '31-60' WHEN dias<=90 THEN '61-90' ELSE '>90' END tramo
  FROM (SELECT CAST(SYS_EXTRACT_UTC(SYSTIMESTAMP) AS DATE) - CAST(d.fecha_deteccion_utc AS DATE) dias FROM deposito d WHERE d.id_deposito IN ($LD)))
GROUP BY tramo ORDER BY tramo
"@
Add-Resultado 'C19' 'INFO' 'Antigüedad en días (plan 100·110·95·95·150·100)' (Lista $tramos 'tramo' 'n')
$horas = T "SELECT TO_CHAR(fecha_deteccion_utc - INTERVAL '5' HOUR,'HH24') hora, COUNT(*) n FROM deposito WHERE id_deposito IN ($LD) GROUP BY TO_CHAR(fecha_deteccion_utc - INTERVAL '5' HOUR,'HH24') ORDER BY 1"
$maxHora = ($horas.Rows | Measure-Object -Property n -Maximum).Maximum
Add-Resultado 'C20' ($(if ($nDep -eq 0 -or $maxHora -le [math]::Ceiling($nDep * 0.2)) { 'OK' } else { 'AVISO' })) 'Reparto por hora de Colombia' ("{0} horas distintas; la más cargada tiene {1}" -f $horas.Rows.Count, $maxHora)
Cero 'C21' 'Fechas futuras (usuario, interacción, depósito)' "SELECT (SELECT COUNT(*) FROM usuario WHERE id_usuario IN ($LU) AND fecha_registro_utc>SYS_EXTRACT_UTC(SYSTIMESTAMP)) + (SELECT COUNT(*) FROM interaccion WHERE id_interaccion IN ($LI) AND fecha_inicio_utc>SYS_EXTRACT_UTC(SYSTIMESTAMP)) + (SELECT COUNT(*) FROM deposito WHERE id_deposito IN ($LD) AND fecha_deteccion_utc>SYS_EXTRACT_UTC(SYSTIMESTAMP)) FROM dual"
Cero 'C22' 'Actividad anterior a la creación de la cuenta' "SELECT COUNT(*) FROM interaccion i JOIN usuario u ON u.id_usuario=i.id_usuario WHERE i.id_interaccion IN ($LI) AND i.fecha_inicio_utc<u.fecha_registro_utc"
Cero 'C23' 'Verificación anterior a la creación del verificador' "SELECT COUNT(*) FROM deposito d JOIN usuario u ON u.id_usuario=d.id_verificador WHERE d.id_deposito IN ($LD) AND d.fecha_verificacion_utc<u.fecha_registro_utc"
Cero 'C24' 'Depósito anterior al inicio de su interacción' "SELECT COUNT(*) FROM deposito d JOIN orden_apertura o ON o.id_orden=d.id_orden JOIN interaccion i ON i.id_interaccion=o.id_interaccion WHERE d.id_deposito IN ($LD) AND d.fecha_deteccion_utc<i.fecha_inicio_utc"
Cero 'C25' 'Depósitos del lote cuya interacción no está en el lote' "SELECT COUNT(*) FROM deposito d JOIN orden_apertura o ON o.id_orden=d.id_orden WHERE d.id_deposito IN ($LD) AND o.id_interaccion NOT IN ($LI)"

# ── Estados de verificación ───────────────────────────────────────────────────────────────────────────────────────
$est = T "SELECT DECODE(pkg_deposito.estado(id_registro),0,'pendiente',1,'verificado',2,'rechazado','otro') estado, COUNT(*) n FROM registro_simulacion WHERE id_lote=:lote AND tabla='DEPOSITO' GROUP BY DECODE(pkg_deposito.estado(id_registro),0,'pendiente',1,'verificado',2,'rechazado','otro') ORDER BY 1"
Add-Resultado 'C26' 'INFO' 'Estado de los depósitos (PKG_DEPOSITO.estado)' (Lista $est 'estado' 'n')
Cero 'C27' 'Depósitos verificados por su propio titular' "SELECT COUNT(*) FROM deposito d JOIN orden_apertura o ON o.id_orden=d.id_orden JOIN interaccion i ON i.id_interaccion=o.id_interaccion WHERE d.id_deposito IN ($LD) AND d.id_verificador=i.id_usuario"
Cero 'C28' 'Depósitos del lote verificados por cuentas reales' "SELECT COUNT(*) FROM deposito WHERE id_deposito IN ($LD) AND id_verificador IS NOT NULL AND id_verificador NOT IN ($LU)" 'AVISO'
Cero 'C29' 'Depósitos reales verificados por cuentas del lote' "SELECT COUNT(*) FROM deposito WHERE id_deposito NOT IN ($LD) AND id_verificador IN ($LU)"

# ── Ecopuntos ─────────────────────────────────────────────────────────────────────────────────────────────────────
$eco = T "SELECT COUNT(*) movimientos, NVL(SUM(CASE WHEN tipo=1 THEN puntos END),0) acreditados, NVL(SUM(CASE WHEN tipo=2 THEN puntos END),0) revertidos, NVL(SUM(puntos),0) neto FROM movimiento_ecopuntos WHERE id_deposito IN ($LD)"
$e = $eco.Rows[0]
Add-Resultado 'C30' 'INFO' 'Ecopuntos del lote' "movimientos $($e.movimientos) · acreditados $($e.acreditados) · revertidos $($e.revertidos) · neto $($e.neto)"
Cero 'C31' 'Ecopuntos del lote para cuentas reales' "SELECT COUNT(*) FROM movimiento_ecopuntos WHERE id_deposito IN ($LD) AND id_usuario NOT IN ($LU)"
Cero 'C32' 'Cuentas del lote con Ecopuntos de depósitos reales' "SELECT COUNT(*) FROM movimiento_ecopuntos WHERE id_usuario IN ($LU) AND id_deposito NOT IN ($LD)"
Cero 'C33' 'Crédito vigente en depósitos no verificados o rechazados' "SELECT COUNT(*) FROM registro_simulacion r WHERE r.id_lote=:lote AND r.tabla='DEPOSITO' AND pkg_ecopuntos.credito_vigente(r.id_registro) IS NOT NULL AND pkg_deposito.estado(r.id_registro)<>1"
Cero 'C34' 'Depósitos con más de un crédito vigente' "SELECT COUNT(*) FROM (SELECT m.id_deposito FROM movimiento_ecopuntos m WHERE m.id_deposito IN ($LD) AND m.tipo=1 AND NOT EXISTS (SELECT 1 FROM movimiento_ecopuntos x WHERE x.id_credito_revertido=m.id_movimiento) GROUP BY m.id_deposito HAVING COUNT(*)>1)"
Cero 'C35' 'Crédito con tarifa de otro material' "SELECT COUNT(*) FROM movimiento_ecopuntos m JOIN regla_ecopuntos g ON g.id_regla=m.id_regla JOIN deposito d ON d.id_deposito=m.id_deposito WHERE m.id_deposito IN ($LD) AND m.tipo=1 AND g.id_material<>d.id_material_verificado"
Cero 'C36' 'Crédito que no coincide con tarifa × cantidad verificada' "SELECT COUNT(*) FROM movimiento_ecopuntos m JOIN regla_ecopuntos g ON g.id_regla=m.id_regla JOIN deposito d ON d.id_deposito=m.id_deposito WHERE m.id_deposito IN ($LD) AND m.tipo=1 AND m.puntos <> g.puntos * CASE WHEN g.por_unidad=1 THEN d.cantidad_verificada ELSE 1 END"
Cero 'C37' 'Crédito para un material sin tarifa vigente al acreditar' "SELECT COUNT(*) FROM movimiento_ecopuntos m JOIN operacion_ecopuntos op ON op.id_operacion=m.id_operacion JOIN regla_ecopuntos g ON g.id_regla=m.id_regla WHERE m.id_deposito IN ($LD) AND m.tipo=1 AND NOT (g.desde_utc<=op.fecha_utc AND (g.hasta_utc IS NULL OR g.hasta_utc>op.fecha_utc))"
$saldos = T @"
SELECT tramo, COUNT(*) n FROM (
  SELECT CASE WHEN s=0 THEN '0' WHEN s<=20 THEN '1-20' WHEN s<=100 THEN '21-100' ELSE '>100' END tramo
  FROM (SELECT r.id_registro, NVL((SELECT SUM(m.puntos) FROM movimiento_ecopuntos m WHERE m.id_usuario=r.id_registro),0) s
        FROM registro_simulacion r WHERE r.id_lote=:lote AND r.tabla='USUARIO' AND r.clave LIKE 'ciu-%'))
GROUP BY tramo ORDER BY tramo
"@
Add-Resultado 'C38' 'INFO' 'Saldo de los ciudadanos del lote (suma de movimientos = PKG_ECOPUNTOS.saldo)' (Lista $saldos 'tramo' 'n')

# ── Hardware y separación de la actividad real ───────────────────────────────────────────────────────────────────
Cero 'C39' 'Eventos de órdenes del lote que no son sintéticos (id_externo sin «sim-»)' "SELECT COUNT(*) FROM evento_dispositivo WHERE id_orden IN ($LO) AND id_externo NOT LIKE 'sim-%'"
$aper = N "SELECT COUNT(*) FROM evento_dispositivo WHERE id_orden IN ($LO) AND tipo=2"
$reales = N "SELECT COUNT(*) FROM evento_dispositivo WHERE tipo=2 AND resultado=1 AND (id_orden IS NULL OR id_orden NOT IN ($LO))"
Add-Resultado 'C40' ($(if ($aper -eq 0) { 'OK' } else { 'AVISO' })) 'Aperturas sintéticas registradas en canecas reales' "$aper del lote frente a $reales aperturas aceptadas fuera del lote (las consultas que no cruzan REGISTRO_SIMULACION las suman)"
$canal = T "SELECT i.canal, COUNT(*) n FROM interaccion i WHERE i.id_interaccion IN ($LI) GROUP BY i.canal ORDER BY 1"
$operativos = N "SELECT COUNT(*) FROM interaccion WHERE id_interaccion IN ($LI) AND canal NOT IN (3,4)"
Add-Resultado 'C41' ($(if ($operativos -eq 0) { 'OK' } else { 'AVISO' })) 'Canales del lote (1 Telegram · 2 Manual · 3 Simulador · 4 Telegram simulado · 5 Asistido)' ((Lista $canal 'canal' 'n') + $(if ($operativos) { " · $operativos con canal operativo (1, 2 o 5): los informes que solo excluyen 3 y 4 los mezclan con la actividad real" } else { '' }))
$sinDep = T "SELECT i.canal, CASE WHEN i.id_usuario IS NULL THEN 'sin cuenta' ELSE 'con cuenta' END titular, COUNT(*) n FROM interaccion i WHERE i.id_interaccion IN ($LI) AND NOT EXISTS (SELECT 1 FROM orden_apertura o JOIN deposito d ON d.id_orden=o.id_orden WHERE o.id_interaccion=i.id_interaccion) GROUP BY i.canal, CASE WHEN i.id_usuario IS NULL THEN 'sin cuenta' ELSE 'con cuenta' END ORDER BY 1,2"
Add-Resultado 'C46' 'INFO' 'Interacciones del lote sin depósito (invitados, aperturas manuales)' ((($sinDep.Rows | ForEach-Object { "canal $($_.canal) $($_.titular)=$($_.n)" }) -join ', ') + $(if ($sinDep.Rows.Count -eq 0) { 'ninguna' } else { '' }))
Cero 'C42' 'Interacciones reales atendidas por cuentas del lote' "SELECT COUNT(*) FROM interaccion WHERE id_interaccion NOT IN ($LI) AND (id_usuario IN ($LU) OR id_operario IN ($LU))"
Cero 'C43' 'Tarifas creadas por cuentas del lote' "SELECT COUNT(*) FROM regla_ecopuntos WHERE id_creador IN ($LU)"
$est2 = T "SELECT (SELECT COUNT(*) FROM punto_instalacion) estaciones, (SELECT COUNT(*) FROM contenedor) canecas, (SELECT COUNT(*) FROM registro_simulacion WHERE id_lote=:lote AND tabla IN ('PUNTO_INSTALACION','CONTENEDOR')) del_lote FROM dual"
$s = $est2.Rows[0]
Add-Resultado 'C44' ($(if ([int]$s.estaciones -eq 1 -and [int]$s.canecas -eq 3 -and [int]$s.del_lote -eq 0) { 'OK' } else { 'FALLA' })) 'Solo la Estación Oficial con tres canecas' "estaciones $($s.estaciones) · canecas $($s.canecas) · creadas por el lote $($s.del_lote)"
$opEst = N "SELECT COUNT(*) FROM operacion_estacion WHERE id_usuario IN ($LU)"
Add-Resultado 'C45' 'INFO' 'Operaciones de la estación (V030/V033) de cuentas del lote' "$opEst (no se trazan en REGISTRO_SIMULACION)"
$cn.Close()

# ── Informe ───────────────────────────────────────────────────────────────────────────────────────────────────────
$sello = (Get-Date).ToUniversalTime().ToString('yyyyMMdd-HHmm')
$commit = (git -C $raiz rev-parse --short HEAD 2>$null)
$fallas = @($resultados | Where-Object Estado -eq 'FALLA').Count
$avisos = @($resultados | Where-Object Estado -eq 'AVISO').Count
$md = New-Object System.Text.StringBuilder
[void]$md.AppendLine("# Verificación del lote $Lote")
[void]$md.AppendLine()
[void]$md.AppendLine("- **Fecha:** $((Get-Date).ToUniversalTime().ToString('yyyy-MM-dd HH:mm')) UTC · **Commit:** $commit")
[void]$md.AppendLine("- **Base:** $($id.Usuario) · $entorno · **Modo:** solo lectura (SET TRANSACTION READ ONLY)")
[void]$md.AppendLine("- **Resultado:** $fallas fallas · $avisos avisos · $($resultados.Count) comprobaciones")
[void]$md.AppendLine()
[void]$md.AppendLine('| Id | Estado | Comprobación | Detalle |')
[void]$md.AppendLine('|---|---|---|---|')
foreach ($r in $resultados) { [void]$md.AppendLine("| $($r.Id) | $($r.Estado) | $($r.Titulo) | $($r.Detalle -replace '\|','/') |") }
$base = Join-Path $Salida "verificacion-$Lote-$sello"
[IO.File]::WriteAllText("$base.md", $md.ToString(), (New-Object Text.UTF8Encoding($false)))
$json = [ordered]@{ lote = $Lote; fechaUtc = (Get-Date).ToUniversalTime().ToString('o'); commit = $commit
    base = $id.Usuario; entorno = $entorno; fallas = $fallas; avisos = $avisos; comprobaciones = $resultados }
[IO.File]::WriteAllText("$base.json", ($json | ConvertTo-Json -Depth 4), (New-Object Text.UTF8Encoding($false)))
Write-Host ''
Write-Host "Informe: $base.md" -ForegroundColor Cyan
if ($fallas -gt 0) { exit 1 }

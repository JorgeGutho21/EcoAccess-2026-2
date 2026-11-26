<#
.SYNOPSIS
    Muestra (por defecto) o aplica (-Aplicar) la reversión T004 del lote de feria SIM-FERIA-2026.

.DESCRIPTION
    · Sin -Aplicar: SOLO LECTURA (transacción READ ONLY). Muestra la base conectada, cuántas filas de cada tabla borraría T004
      y, con -ValidarSintaxis, compila el bloque PL/SQL con DBMS_SQL.PARSE (compila, no ejecuta).
    · Con -Aplicar -Confirmar SIM-FERIA-2026: ejecuta database\07_operacion\T004__reversion_lote_feria_2026.sql, que respalda
      en ECO_RESP_T004_* y borra solo lo que REGISTRO_SIMULACION atribuye al lote, en una transacción que se revierte completa
      si detecta cualquier cambio ajeno al lote.
    Requiere la cuenta dueña del esquema (ECOACCESS_P3_APP) y AUTORIZACIÓN EXPRESA del responsable de la base de datos.
    No imprime la cadena de conexión.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\feria\Revertir-LoteFeria.ps1 -ValidarSintaxis
    powershell -ExecutionPolicy Bypass -File scripts\feria\Revertir-LoteFeria.ps1 -Aplicar -Confirmar SIM-FERIA-2026
#>
[CmdletBinding()]
param(
    [switch]$Aplicar,
    [string]$Confirmar,
    [switch]$ValidarSintaxis,
    # Solo si el lote quedó EN_CURSO porque el generador se interrumpió y nadie lo está ejecutando.
    [switch]$PermitirEnCurso
)
. (Join-Path $PSScriptRoot 'ComunOracle.ps1')
$Lote = 'SIM-FERIA-2026'
$raiz = Get-RaizRepositorio
$archivo = Join-Path $raiz 'database\07_operacion\T004__reversion_lote_feria_2026.sql'
$bloque = (Get-Content -LiteralPath $archivo -Raw -Encoding UTF8) -split "`r?`n" | Where-Object { $_ -notmatch '^\s*--' }
$bloque = ($bloque -join "`n").Trim()
if ($PermitirEnCurso) { $bloque = $bloque.Replace('/*PERMITIR_EN_CURSO*/FALSE', 'TRUE') }

$cn = Open-OracleSoloLectura
try {
    $id = Get-IdentidadOracle -Conexion $cn
    Write-Host ("Conectado (solo lectura) como {0} · {1}" -f $id.Usuario, $(if ($id.EsNube) { 'Oracle Cloud' } elseif ($id.EsAislado) { 'esquema aislado' } else { 'otra base' }))
    $p = @{ lote = $Lote }
    $LU = "SELECT id_registro FROM registro_simulacion WHERE id_lote=:lote AND tabla='USUARIO'"
    $LI = "SELECT id_registro FROM registro_simulacion WHERE id_lote=:lote AND tabla='INTERACCION'"
    $LD = "SELECT id_registro FROM registro_simulacion WHERE id_lote=:lote AND tabla='DEPOSITO'"
    $plan = Invoke-Consulta -Conexion $cn -Parametros $p -Sql @"
SELECT 'lote_simulacion' tabla, (SELECT estado FROM lote_simulacion WHERE id_lote=:lote) detalle, (SELECT COUNT(*) FROM lote_simulacion WHERE id_lote=:lote) filas FROM dual
UNION ALL SELECT 'registro_simulacion', NULL, (SELECT COUNT(*) FROM registro_simulacion WHERE id_lote=:lote) FROM dual
UNION ALL SELECT 'movimiento_ecopuntos', NULL, (SELECT COUNT(*) FROM movimiento_ecopuntos WHERE id_deposito IN ($LD)) FROM dual
UNION ALL SELECT 'operacion_ecopuntos', NULL, (SELECT COUNT(*) FROM operacion_ecopuntos WHERE id_deposito IN ($LD)) FROM dual
UNION ALL SELECT 'auditoria_deposito', NULL, (SELECT COUNT(*) FROM auditoria_deposito WHERE id_deposito IN ($LD)) FROM dual
UNION ALL SELECT 'deposito', NULL, (SELECT COUNT(*) FROM deposito WHERE id_deposito IN ($LD)) FROM dual
UNION ALL SELECT 'evento_dispositivo', 'solo id_externo sim-%', (SELECT COUNT(*) FROM evento_dispositivo WHERE id_orden IN (SELECT id_orden FROM orden_apertura WHERE id_interaccion IN ($LI)) AND id_externo LIKE 'sim-%') FROM dual
UNION ALL SELECT 'orden_apertura', NULL, (SELECT COUNT(*) FROM orden_apertura WHERE id_interaccion IN ($LI)) FROM dual
UNION ALL SELECT 'interaccion', NULL, (SELECT COUNT(*) FROM interaccion WHERE id_interaccion IN ($LI)) FROM dual
UNION ALL SELECT 'credencial_acceso', NULL, (SELECT COUNT(*) FROM credencial_acceso WHERE id_usuario IN ($LU)) FROM dual
UNION ALL SELECT 'usuario_rol', NULL, (SELECT COUNT(*) FROM usuario_rol WHERE id_usuario IN ($LU)) FROM dual
UNION ALL SELECT 'usuario', '@feria.ecoaccess.test', (SELECT COUNT(*) FROM usuario WHERE id_usuario IN ($LU)) FROM dual
"@
    Write-Host "`n== Lo que T004 respaldaría (ECO_RESP_T004_*) y borraría ==" -ForegroundColor Cyan
    $plan | Format-Table -AutoSize | Out-String -Width 200 | Write-Host

    if ($ValidarSintaxis) {
        $cmd = $cn.CreateCommand()
        $cmd.CommandText = 'DECLARE c INTEGER := DBMS_SQL.OPEN_CURSOR; BEGIN DBMS_SQL.PARSE(c, :b, DBMS_SQL.NATIVE); DBMS_SQL.CLOSE_CURSOR(c); END;'
        [void]$cmd.Parameters.Add((New-Object Oracle.ManagedDataAccess.Client.OracleParameter('b', [Oracle.ManagedDataAccess.Client.OracleDbType]::Clob, $bloque, 'Input')))
        try { [void]$cmd.ExecuteNonQuery(); Write-Host 'Sintaxis de T004: el bloque compila (DBMS_SQL.PARSE, sin ejecutarlo).' -ForegroundColor Green }
        catch { Write-Host ('Sintaxis de T004: ERROR ' + $_.Exception.InnerException.Message) -ForegroundColor Red; exit 1 }
    }
} finally { $cn.Close() }

if (-not $Aplicar) {
    Write-Host 'Solo lectura: no se cambió nada. Para aplicar (con respaldo previo): -Aplicar -Confirmar SIM-FERIA-2026' -ForegroundColor Yellow
    return
}
if ($Confirmar -cne $Lote) { throw "Para borrar hay que confirmar el lote exacto: -Confirmar $Lote" }

Import-ControladorOracle
$cn = New-Object Oracle.ManagedDataAccess.Client.OracleConnection (Get-CadenaOracle)
$cn.Open()
try {
    $habilitar = $cn.CreateCommand(); $habilitar.CommandText = 'BEGIN DBMS_OUTPUT.ENABLE(NULL); END;'; [void]$habilitar.ExecuteNonQuery()
    $cmd = $cn.CreateCommand(); $cmd.CommandText = $bloque
    try { [void]$cmd.ExecuteNonQuery() }
    finally {
        $leer = $cn.CreateCommand()
        $leer.CommandText = 'BEGIN DBMS_OUTPUT.GET_LINE(:l, :s); END;'
        $l = $leer.Parameters.Add('l', [Oracle.ManagedDataAccess.Client.OracleDbType]::Varchar2, 32767); $l.Direction = 'Output'
        $s = $leer.Parameters.Add('s', [Oracle.ManagedDataAccess.Client.OracleDbType]::Int32); $s.Direction = 'Output'
        while ($true) { [void]$leer.ExecuteNonQuery(); if ([int]$s.Value.ToString() -ne 0) { break }; Write-Host $l.Value }
    }
} finally { $cn.Close() }
Write-Host "Verifica el resultado: powershell -ExecutionPolicy Bypass -File scripts\feria\Verificar-LoteFeria.ps1" -ForegroundColor Cyan

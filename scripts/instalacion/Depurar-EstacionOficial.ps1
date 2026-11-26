<#
.SYNOPSIS
    Verifica (por defecto) o aplica (-Aplicar) la depuración T003 en Oracle Cloud: solo la Estación Oficial EcoAccess con
    sus tres canecas físicas (verde, blanca y negra). Conserva el QR permanente EcoAccessCanecaPiloto1.

.DESCRIPTION
    Usa la cadena de conexión guardada en los User Secrets de este equipo (ConnectionStrings:OracleP3) y el controlador
    Oracle.ManagedDataAccess de la instalación. No imprime contraseñas ni la cadena de conexión.
    · Sin -Aplicar: solo lectura. Muestra estaciones y canecas, y lo que T003 respaldaría y borraría.
    · Con -Aplicar: ejecuta database\07_operacion\T003__depuracion_estacion_oficial_tres_canecas.sql (respalda en tablas
      ECO_RESP_T003_* antes de borrar, se detiene sin cambios ante cualquier dependencia ajena al lote) y vuelve a listar.
    Requiere la cuenta dueña del esquema; las cuentas individuales (ECO_HEYDER, ECO_SARAY) no pueden borrar.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\instalacion\Depurar-EstacionOficial.ps1
    powershell -ExecutionPolicy Bypass -File scripts\instalacion\Depurar-EstacionOficial.ps1 -Aplicar
#>
[CmdletBinding()]
param(
    [switch]$Aplicar,
    [string]$Instalacion = (Join-Path $env:LOCALAPPDATA 'Programs\EcoAccess P3')
)
$ErrorActionPreference = 'Stop'
$raiz = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$dll = Join-Path $Instalacion 'Oracle.ManagedDataAccess.dll'
if (-not (Test-Path -LiteralPath $dll)) {
    $dll = Get-ChildItem -Path (Join-Path $raiz 'src') -Recurse -Filter 'Oracle.ManagedDataAccess.dll' -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName
}
if (-not $dll) { throw 'No se encontró Oracle.ManagedDataAccess.dll: instala EcoAccess P3 o compila la solución.' }
Add-Type -Path $dll

$secretos = Join-Path $env:APPDATA 'Microsoft\UserSecrets\ecoaccess-p3-framework\secrets.json'
if (-not (Test-Path -LiteralPath $secretos)) { throw 'Este equipo no tiene la conexión a Oracle configurada (Configurar-Credenciales.ps1 o Configurar-Integrante.ps1).' }
$cadena = (Get-Content -LiteralPath $secretos -Raw -Encoding UTF8 | ConvertFrom-Json).'ConnectionStrings:OracleP3'
if (-not $cadena) { throw 'Falta ConnectionStrings:OracleP3 en los User Secrets.' }

$cn = New-Object Oracle.ManagedDataAccess.Client.OracleConnection($cadena)
$cn.Open()
function Consultar([string]$sql) {
    $cmd = $cn.CreateCommand(); $cmd.CommandText = $sql
    $t = New-Object System.Data.DataTable; $t.Load($cmd.ExecuteReader()); return ,$t
}
function Mostrar([string]$titulo) {
    Write-Host "`n== $titulo ==" -ForegroundColor Cyan
    Consultar @"
SELECT p.id_punto_instalacion AS estacion_id, p.nombre AS estacion, p.activo, c.id_contenedor AS caneca_id, c.nombre AS caneca,
       c.identificador_qr AS qr, c.estado
  FROM punto_instalacion p LEFT JOIN contenedor c ON c.id_punto_instalacion = p.id_punto_instalacion
 ORDER BY p.id_punto_instalacion, c.id_contenedor
"@ | Format-Table -AutoSize | Out-String -Width 220 | Write-Host
}
try {
    $usuario = (Consultar "SELECT USER AS u FROM dual").Rows[0]["U"]
    Write-Host "Conectado a Oracle Cloud como $usuario"
    Mostrar 'Estado actual'
    Write-Host '== Lo que T003 respaldaría y eliminaría (lote SIM-2026-11-CIERRE) ==' -ForegroundColor Cyan
    Consultar "SELECT tabla, COUNT(*) AS filas FROM registro_simulacion WHERE id_lote = 'SIM-2026-11-CIERRE' GROUP BY tabla ORDER BY tabla" |
        Format-Table -AutoSize | Out-String | Write-Host

    if (-not $Aplicar) {
        Write-Host 'Solo lectura: no se cambió nada. Para aplicar (con respaldo previo en ECO_RESP_T003_*): -Aplicar' -ForegroundColor Yellow
        return
    }

    $bloque = Get-Content -LiteralPath (Join-Path $raiz 'database\07_operacion\T003__depuracion_estacion_oficial_tres_canecas.sql') -Raw -Encoding UTF8
    $bloque = ($bloque -split "`r?`n" | Where-Object { $_ -notmatch '^\s*--' }) -join "`n"
    $habilitar = $cn.CreateCommand(); $habilitar.CommandText = 'BEGIN DBMS_OUTPUT.ENABLE(NULL); END;'; [void]$habilitar.ExecuteNonQuery()
    $cmd = $cn.CreateCommand(); $cmd.CommandText = $bloque.Trim()
    try { [void]$cmd.ExecuteNonQuery() }
    finally {
        $leer = $cn.CreateCommand()
        $leer.CommandText = 'BEGIN DBMS_OUTPUT.GET_LINE(:l, :s); END;'
        $l = $leer.Parameters.Add('l', [Oracle.ManagedDataAccess.Client.OracleDbType]::Varchar2, 32767); $l.Direction = 'Output'
        $s = $leer.Parameters.Add('s', [Oracle.ManagedDataAccess.Client.OracleDbType]::Int32); $s.Direction = 'Output'
        while ($true) { [void]$leer.ExecuteNonQuery(); if ([int]$s.Value.ToString() -ne 0) { break }; Write-Host $l.Value }
    }
    Mostrar 'Estado después de T003'
} finally { $cn.Close() }

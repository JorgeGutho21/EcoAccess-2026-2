# Funciones comunes de los scripts de feria (se cargan con «. $PSScriptRoot\ComunOracle.ps1»).
# · Detecta la raíz del repositorio sin rutas fijas de ningún equipo.
# · Carga Oracle.ManagedDataAccess desde la instalación de EcoAccess P3 o desde la compilación del repositorio.
# · Toma la cadena de conexión de la variable de entorno ECOACCESS_ORACLE_CADENA o, si no existe, de los User Secrets
#   «ecoaccess-p3-framework» (ConnectionStrings:OracleP3). NUNCA la imprime ni la escribe en disco.
# · Abrir-OracleSoloLectura deja la sesión en una transacción READ ONLY: Oracle rechaza cualquier INSERT/UPDATE/DELETE.

$ErrorActionPreference = 'Stop'

function Get-RaizRepositorio {
    $d = Get-Item -LiteralPath $PSScriptRoot
    while ($d -and -not (Test-Path -LiteralPath (Join-Path $d.FullName 'EcoAccess.P3.sln'))) { $d = $d.Parent }
    if (-not $d) { throw 'No se encontró EcoAccess.P3.sln por encima de este script.' }
    return $d.FullName
}

function Import-ControladorOracle {
    param([string]$Instalacion = (Join-Path $env:LOCALAPPDATA 'Programs\EcoAccess P3'))
    if ('Oracle.ManagedDataAccess.Client.OracleConnection' -as [type]) { return }
    $dll = Join-Path $Instalacion 'Oracle.ManagedDataAccess.dll'
    if (-not (Test-Path -LiteralPath $dll)) {
        $dll = Get-ChildItem -Path (Join-Path (Get-RaizRepositorio) 'src') -Recurse -Filter 'Oracle.ManagedDataAccess.dll' -ErrorAction SilentlyContinue |
            Select-Object -First 1 -ExpandProperty FullName
    }
    if (-not $dll) { throw 'No se encontró Oracle.ManagedDataAccess.dll: instala EcoAccess P3 o compila la solución (scripts\feria\Compilar-Y-Probar.ps1).' }
    Add-Type -Path $dll
}

function Get-CadenaOracle {
    if ($env:ECOACCESS_ORACLE_CADENA) { return $env:ECOACCESS_ORACLE_CADENA }
    $secretos = Join-Path $env:APPDATA 'Microsoft\UserSecrets\ecoaccess-p3-framework\secrets.json'
    if (-not (Test-Path -LiteralPath $secretos)) {
        throw 'Este equipo no tiene la conexión a Oracle configurada (scripts\instalacion\Configurar-Integrante.ps1) ni ECOACCESS_ORACLE_CADENA.'
    }
    $cadena = (Get-Content -LiteralPath $secretos -Raw -Encoding UTF8 | ConvertFrom-Json).'ConnectionStrings:OracleP3'
    if (-not $cadena) { throw 'Falta ConnectionStrings:OracleP3 en los User Secrets.' }
    return $cadena
}

function Open-OracleSoloLectura {
    Import-ControladorOracle
    $cn = New-Object Oracle.ManagedDataAccess.Client.OracleConnection (Get-CadenaOracle)
    $cn.Open()
    $cmd = $cn.CreateCommand(); $cmd.CommandText = 'SET TRANSACTION READ ONLY'; [void]$cmd.ExecuteNonQuery()
    return $cn
}

# Ejecuta una consulta con variables :nombre y devuelve un DataTable.
function Invoke-Consulta {
    param([Parameter(Mandatory)]$Conexion, [Parameter(Mandatory)][string]$Sql, [hashtable]$Parametros = @{})
    $cmd = $Conexion.CreateCommand()
    $cmd.BindByName = $true
    $cmd.CommandText = $Sql
    foreach ($k in $Parametros.Keys) { [void]$cmd.Parameters.Add((New-Object Oracle.ManagedDataAccess.Client.OracleParameter($k, $Parametros[$k]))) }
    $tabla = New-Object System.Data.DataTable
    $r = $cmd.ExecuteReader()
    try { $tabla.Load($r) } finally { $r.Dispose() }
    return ,$tabla
}

function Invoke-Escalar {
    param([Parameter(Mandatory)]$Conexion, [Parameter(Mandatory)][string]$Sql, [hashtable]$Parametros = @{})
    $t = Invoke-Consulta -Conexion $Conexion -Sql $Sql -Parametros $Parametros
    if ($t.Rows.Count -eq 0) { return $null }
    $v = $t.Rows[0][0]
    if ($v -is [System.DBNull]) { return $null }
    return $v
}

# Identidad de la conexión sin datos sensibles: usuario de BD, servicio y si es la nube.
function Get-IdentidadOracle {
    param([Parameter(Mandatory)]$Conexion)
    $t = Invoke-Consulta -Conexion $Conexion -Sql "SELECT USER usuario, SYS_CONTEXT('USERENV','SERVICE_NAME') servicio, SYS_CONTEXT('USERENV','CON_NAME') contenedor FROM dual"
    $f = $t.Rows[0]
    [pscustomobject]@{
        Usuario    = [string]$f.usuario
        Servicio   = [string]$f.servicio
        Contenedor = [string]$f.contenedor
        EsNube     = ([string]$f.servicio) -match 'adb\..*oraclecloud'
        EsAislado  = ([string]$f.usuario) -like 'ECOACCESS_P3_PRUEBAS*' -or ([string]$f.contenedor) -eq 'PRUEBAP3'
    }
}

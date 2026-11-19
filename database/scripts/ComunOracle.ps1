<#
  Funciones compartidas por Exportar-EsquemaOracle.ps1 y Ejecutar-PruebasBD.ps1.
  - Lee la cadena de conexión de los User Secrets de Windows (fuera del repositorio).
  - Ejecuta SQL*Plus enviando la conexión por la entrada estándar: la contraseña no queda en disco, en la lista de
    procesos ni en la salida (se reemplaza por ******** si Oracle la repitiera).
#>

function Get-EcoConexion {
    param([Parameter(Mandatory)][string]$Secreto)
    $archivo = Join-Path $env:APPDATA "Microsoft\UserSecrets\$Secreto\secrets.json"
    if (-not (Test-Path $archivo)) { throw "No existe el User Secret '$Secreto'. Siga docs/database/14-guia-companeros.md (paso 8)." }
    $json = Get-Content $archivo -Raw -Encoding UTF8 | ConvertFrom-Json
    $cadena = [string]$json.'ConnectionStrings:OracleP3'
    function Valor([string]$clave) { if ($cadena -match "(?i)(^|;)\s*$clave\s*=\s*(""[^""]*""|[^;]+)") { $Matches[2].Trim().Trim('"') } }
    $c = [pscustomobject]@{ Usuario = (Valor 'user id'); Clave = (Valor 'password'); Origen = (Valor 'data source'); TnsAdmin = $null; Local = $false }
    if (-not ($c.Usuario -and $c.Clave -and $c.Origen)) { throw "La cadena de '$Secreto' no tiene User Id, Password y Data Source." }
    if ($c.Usuario -in 'SYS', 'SYSTEM', 'ADMIN') { throw 'EcoAccess no usa cuentas administrativas para trabajar: configure la cuenta dueña del esquema.' }
    if ($c.Origen -match '^(localhost|127\.0\.0\.1)[:/]') { $c.Local = $true }
    if ($c.Origen -notmatch '[:/]') {
        $wallet = [string]$json.'EcoAccess:Oracle:CarpetaWallet'
        if (-not $wallet) { $wallet = Join-Path $env:USERPROFILE '.oracle\wallets\EcoAccess' }
        if (-not (Test-Path (Join-Path $wallet 'tnsnames.ora'))) { throw "No encuentro el wallet de Oracle Cloud en '$wallet' (ver docs/database/03-oracle-cloud.md)." }
        $c.TnsAdmin = $wallet
    }
    $c
}

function Invoke-EcoSqlPlus {
    param([Parameter(Mandatory)]$Conexion, [Parameter(Mandatory)][string]$Sql, [switch]$DetenerEnError)
    $sqlplus = (Get-Command sqlplus -ErrorAction SilentlyContinue).Source
    if (-not $sqlplus) { throw 'No se encontró SQL*Plus en el PATH (Oracle Instant Client u Oracle Database Free).' }
    $anterior = $env:TNS_ADMIN
    if ($Conexion.TnsAdmin) { $env:TNS_ADMIN = $Conexion.TnsAdmin }
    $env:NLS_LANG = 'AMERICAN_AMERICA.AL32UTF8'
    $destino = if ($Conexion.TnsAdmin) { $Conexion.Origen } else { "//$($Conexion.Origen)" }
    $modo = if ($DetenerEnError) { 'whenever sqlerror exit failure' } else { 'whenever sqlerror continue' }
    $entrada = "$modo`nconnect $($Conexion.Usuario)/`"$($Conexion.Clave)`"@$destino`nset linesize 250 pagesize 200 trimspool on`n$Sql`nexit`n"
    $codificacion = [Console]::OutputEncoding
    [Console]::OutputEncoding = New-Object Text.UTF8Encoding $false
    try { $salida = ($entrada | & $sqlplus -S -L /nolog 2>&1 | Out-String); $codigo = $LASTEXITCODE }
    finally { [Console]::OutputEncoding = $codificacion; $env:TNS_ADMIN = $anterior }
    [pscustomobject]@{ Texto = $salida.Replace($Conexion.Clave, '********'); Codigo = $codigo }
}

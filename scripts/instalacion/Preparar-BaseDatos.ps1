<#
.SYNOPSIS
    Crea el esquema de EcoAccess P3 en Oracle y aplica las migraciones pendientes en orden.
.DESCRIPTION
    Pensado para el contenedor Docker de desarrollo. Si el perfil privado aún no tiene conexión, genera una contraseña,
    crea el usuario y la guarda en los User Secrets de Windows sin mostrarla. Las versiones aplicadas se anotan en la
    tabla registro_migracion, de modo que repetir el script no vuelve a ejecutar nada. No borra objetos ni datos.
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\instalacion\Preparar-BaseDatos.ps1
#>
[CmdletBinding()]
param(
    [string] $Contenedor = 'oracle-free-p3',
    [ValidatePattern('^[A-Za-z0-9_]{1,30}$')] [string] $Servicio = 'PRUEBAP3',
    [ValidatePattern('^[A-Za-z][A-Za-z0-9_]{1,29}$')] [string] $Esquema = 'ECOACCESS_P3_APP',
    [int] $PuertoPublicado = 1522,
    [string] $IdSecretos = 'ecoaccess-p3-framework',
    [switch] $DatosDemostracion
)

$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$archivoSecretos = Join-Path $env:APPDATA "Microsoft\UserSecrets\$IdSecretos\secrets.json"

function Invocar-SqlPlus([string] $Conexion, [string[]] $Lineas) {
    # Windows PowerShell antepone un BOM a la entrada estándar; sed lo retira para que SQL*Plus lea la primera línea.
    $entrada = (@($Conexion, 'SET FEEDBACK OFF', 'SET DEFINE OFF') + $Lineas + 'EXIT') -join "`n"
    $salida = $entrada | & docker exec -i -e NLS_LANG=AMERICAN_AMERICA.AL32UTF8 $Contenedor bash -c "sed '1s/^\xEF\xBB\xBF//' | sqlplus -s /nolog" 2>&1 | Out-String
    $codigo = $LASTEXITCODE
    # SQL*Plus termina con 0 si la conexión falla antes de WHENEVER; estos mensajes también son un fallo.
    if ($salida -match 'SP2-0640|SP2-0734|SP2-0306|ORA-01017|ORA-12\d{3}') { $codigo = 1 }
    return [pscustomobject]@{ Codigo = $codigo; Salida = $salida }
}

function Leer-Secretos {
    if (Test-Path -LiteralPath $archivoSecretos) { return Get-Content -LiteralPath $archivoSecretos -Raw -Encoding UTF8 | ConvertFrom-Json }
    return New-Object psobject
}

function Guardar-Secreto([string] $Clave, [string] $Valor) {
    $secretos = Leer-Secretos
    $secretos | Add-Member -NotePropertyName $Clave -NotePropertyValue $Valor -Force
    New-Item -ItemType Directory -Force (Split-Path -Parent $archivoSecretos) | Out-Null
    [IO.File]::WriteAllText($archivoSecretos, ($secretos | ConvertTo-Json), (New-Object Text.UTF8Encoding($false)))
}

if ((& docker inspect -f '{{.State.Running}}' $Contenedor 2>$null) -ne 'true') { throw "El contenedor $Contenedor no está en ejecución." }

$secretos = Leer-Secretos
$cadena = $secretos.'ConnectionStrings:OracleP3'
if ($cadena) {
    $clave = [regex]::Match($cadena, '(?i)Password=([^;]+)').Groups[1].Value
}
else {
    $bytes = New-Object byte[] 24
    [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
    $clave = 'Eco' + (([Convert]::ToBase64String($bytes)) -replace '[^A-Za-z0-9]', '').Substring(0, 24)
    $crear = @(
        "ALTER SESSION SET CONTAINER=$Servicio;",
        'WHENEVER SQLERROR EXIT 1',
        "CREATE USER $Esquema IDENTIFIED BY `"$clave`" DEFAULT TABLESPACE users TEMPORARY TABLESPACE temp QUOTA 500M ON users;",
        "GRANT CREATE SESSION, CREATE TABLE, CREATE SEQUENCE, CREATE VIEW, CREATE PROCEDURE, CREATE TRIGGER TO $Esquema;"
    )
    $r = Invocar-SqlPlus 'CONNECT / AS SYSDBA' $crear
    if ($r.Codigo -ne 0) { throw ('No se pudo crear el usuario: ' + ($r.Salida -replace [regex]::Escape($clave), '***')) }
    $cadena = "User Id=$Esquema;Password=$clave;Data Source=localhost:$PuertoPublicado/$Servicio"
    Guardar-Secreto 'ConnectionStrings:OracleP3' $cadena
    Write-Output "Esquema $Esquema creado; conexión guardada en el perfil privado."
}

$conexion = "CONNECT $Esquema/`"$clave`"@localhost:1521/$Servicio"
$registro = Invocar-SqlPlus $conexion @(
    'WHENEVER SQLERROR CONTINUE',
    'CREATE TABLE registro_migracion (version VARCHAR2(60) CONSTRAINT pk_registro_migracion PRIMARY KEY, fecha_aplicacion TIMESTAMP DEFAULT SYSTIMESTAMP NOT NULL);',
    'SET HEADING OFF PAGESIZE 0',
    'SELECT version FROM registro_migracion;')
$aplicadas = @($registro.Salida -split "`r?`n" | ForEach-Object { $_.Trim() } | Where-Object { $_ -match '^[0-9V]' })

$pasos = @(
    'database\02_tablas\01_crear_tablas_iniciales.sql',
    'database\03_datos_prueba\01_catalogos.sql'
) + @(Get-ChildItem (Join-Path $raiz 'database\06_migraciones') -Filter 'V*.sql' | Sort-Object Name | ForEach-Object { "database\06_migraciones\$($_.Name)" }) + @(
    'database\03_datos_prueba\03_catalogo_ambiental_piloto.sql'
)
if ($DatosDemostracion) { $pasos += 'database\03_datos_prueba\02_datos_demo.sql' }

& docker exec $Contenedor mkdir -p /tmp/ecoaccess | Out-Null
$aplicados = 0
try {
    foreach ($paso in $pasos) {
        $version = [IO.Path]::GetFileNameWithoutExtension($paso)
        if ($aplicadas -contains $version) { continue }
        $destino = "/tmp/ecoaccess/$version.sql"
        & docker cp (Join-Path $raiz $paso) "${Contenedor}:$destino" 2>&1 | Out-Null
        $r = Invocar-SqlPlus $conexion @(
            'SET DEFINE ON', 'WHENEVER SQLERROR EXIT 1', "@$destino",
            'WHENEVER SQLERROR EXIT 1', "INSERT INTO registro_migracion (version) VALUES ('$version');", 'COMMIT;')
        if ($r.Codigo -ne 0) { throw ("Falló $paso. La versión no quedó registrada.`n" + (($r.Salida -replace [regex]::Escape($clave), '***') -split "`n" | Select-Object -Last 15 | Out-String)) }
        Write-Output "Aplicado $version"
        $aplicados++
    }
}
finally {
    & docker exec $Contenedor rm -rf /tmp/ecoaccess | Out-Null
}
Write-Output "Migraciones pendientes aplicadas: $aplicados; esquema $Esquema en $Servicio al día."

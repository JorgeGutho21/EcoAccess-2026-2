<#
.SYNOPSIS
    Crea, una sola vez, el esquema de pruebas ECOACCESS_P3_PRUEBAS en el Oracle Database FREE instalado en este PC
    (sin Docker). Lo ejecuta la persona dueña del equipo: es el único paso que necesita la contraseña de SYSTEM.
.DESCRIPTION
    - Solo acepta localhost / 127.0.0.1: nunca toca Oracle Cloud.
    - Pide la contraseña de SYSTEM sin mostrarla y se la entrega a SQL*Plus por la entrada estándar (no aparece en la
      lista de procesos ni en ningún archivo). No la guarda.
    - Crea ECOACCESS_P3_PRUEBAS con una contraseña aleatoria, 500 MB de cuota y los mismos privilegios que el esquema
      de la aplicación. Esa cadena de conexión queda en el perfil privado de Windows (User Secrets
      «ecoaccess-p3-pruebas»), fuera del repositorio.
    - Repetible: si el esquema y su secreto ya existen, no hace nada.
    Después, cualquier integrante instala el modelo con Instalar-Esquema-Aislado-Local.ps1, sin SYSTEM.
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\pruebas\Crear-Esquema-Aislado-Local.ps1
#>
[CmdletBinding()]
param(
    [ValidateSet('localhost', '127.0.0.1')] [string] $Servidor = 'localhost',
    [int] $Puerto = 1521,
    [ValidatePattern('^[A-Za-z0-9_]{1,30}$')] [string] $Servicio = 'freepdb1',
    [ValidatePattern('^ECOACCESS_P3_PRUEBAS[A-Z0-9_]*$')] [string] $Esquema = 'ECOACCESS_P3_PRUEBAS',
    [string] $IdSecretos = 'ecoaccess-p3-pruebas'
)
$ErrorActionPreference = 'Stop'
$archivoSecretos = Join-Path $env:APPDATA "Microsoft\UserSecrets\$IdSecretos\secrets.json"

function Buscar-SqlPlus {
    $candidatos = @()
    if ($env:ORACLE_HOME) { $candidatos += Join-Path $env:ORACLE_HOME 'bin\sqlplus.exe' }
    $candidatos += Get-ChildItem "C:\app\$env:USERNAME\product\*\dbhomeFree\bin\sqlplus.exe" -ErrorAction SilentlyContinue | ForEach-Object FullName
    $encontrado = $candidatos | Where-Object { Test-Path $_ } | Select-Object -First 1
    if (-not $encontrado) { throw 'No se encontró sqlplus.exe de Oracle FREE en este equipo.' }
    return $encontrado
}

function Leer-Secretos {
    if (Test-Path -LiteralPath $archivoSecretos) { return Get-Content -LiteralPath $archivoSecretos -Raw -Encoding UTF8 | ConvertFrom-Json }
    return New-Object psobject
}

$secretos = Leer-Secretos
$existente = $secretos.'ConnectionStrings:OracleP3'
if ($existente) {
    if ($existente -notmatch "(?i)Data Source=(localhost|127\.0\.0\.1):") { throw "El perfil $IdSecretos apunta a otro servidor; no se modifica." }
    Write-Output "Ya existe la conexión de pruebas en el perfil $IdSecretos. No se cambió nada."
    return
}

$sqlplus = Buscar-SqlPlus
$segura = Read-Host -AsSecureString "Contraseña de SYSTEM del Oracle FREE local ($Servidor`:$Puerto/$Servicio)"
$ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($segura)
try { $system = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr) } finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr) }

$bytes = New-Object byte[] 24
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
$clave = 'Eco' + (([Convert]::ToBase64String($bytes)) -replace '[^A-Za-z0-9]', '').Substring(0, 24)

$lineas = @(
    "CONNECT system/`"$system`"@$Servidor`:$Puerto/$Servicio",
    'WHENEVER SQLERROR EXIT 1',
    'SET FEEDBACK OFF',
    'SET SERVEROUTPUT ON',
    'DECLARE n NUMBER; BEGIN',
    "  SELECT COUNT(*) INTO n FROM dba_users WHERE username = '$Esquema';",
    "  IF n = 0 THEN EXECUTE IMMEDIATE 'CREATE USER $Esquema IDENTIFIED BY `"$clave`" DEFAULT TABLESPACE users TEMPORARY TABLESPACE temp QUOTA 500M ON users';",
    "  ELSE EXECUTE IMMEDIATE 'ALTER USER $Esquema IDENTIFIED BY `"$clave`" ACCOUNT UNLOCK'; END IF;",
    'END;',
    '/',
    "GRANT CREATE SESSION, CREATE TABLE, CREATE SEQUENCE, CREATE VIEW, CREATE PROCEDURE, CREATE TRIGGER TO $Esquema;",
    'EXIT'
)
if ($system.Contains('"')) { throw 'La contraseña contiene comillas dobles; SQL*Plus no puede recibirla así. No se intentó la conexión.' }
# Windows PowerShell envía texto a los programas en ASCII: una tilde o una ñ de la contraseña llegaría como «?» y
# Oracle respondería ORA-01017 aunque la contraseña sea correcta. Se envía en UTF-8 y SQL*Plus lo lee en UTF-8.
$codificacionAnterior = $OutputEncoding
$OutputEncoding = New-Object Text.UTF8Encoding($false)
$env:NLS_LANG = 'AMERICAN_AMERICA.AL32UTF8'
try { $salida = ($lineas -join "`n") | & $sqlplus -S -L /nolog 2>&1 | Out-String }
finally { $OutputEncoding = $codificacionAnterior }
$codigo = $LASTEXITCODE
$system = $null
$salida = $salida -replace [regex]::Escape($clave), '***'
if ($salida -match 'ORA-01017') {
    throw ("Oracle rechazó la contraseña de SYSTEM (ORA-01017). No se creó nada.`n" +
        'Ojo: varios intentos fallidos seguidos pueden bloquear la cuenta SYSTEM; revisa la contraseña con calma antes de reintentar.')
}
if ($salida -match 'ORA-28000') { throw 'La cuenta SYSTEM está bloqueada (ORA-28000). No se creó nada.' }
if ($codigo -ne 0 -or $salida -match 'ORA-|SP2-') {
    throw "No se pudo crear el esquema de pruebas (la contraseña no se guardó):`n$($salida.Trim())"
}

$cadena = "User Id=$Esquema;Password=$clave;Data Source=$Servidor`:$Puerto/$Servicio"
$secretos | Add-Member -NotePropertyName 'ConnectionStrings:OracleP3' -NotePropertyValue $cadena -Force
New-Item -ItemType Directory -Force (Split-Path -Parent $archivoSecretos) | Out-Null
[IO.File]::WriteAllText($archivoSecretos, ($secretos | ConvertTo-Json), (New-Object Text.UTF8Encoding($false)))
Write-Output "Esquema $Esquema listo en $Servidor`:$Puerto/$Servicio. Conexión guardada en el perfil privado $IdSecretos."

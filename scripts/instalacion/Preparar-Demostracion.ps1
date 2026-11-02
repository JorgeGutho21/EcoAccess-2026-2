<#
.SYNOPSIS
    Crea la caneca de demostración, sus tarifas ficticias y la configuración privada del servicio de dispositivos.
.DESCRIPTION
    Requiere Preparar-BaseDatos.ps1. Es repetible: conserva la caneca, la clave y el diario existentes.
#>
[CmdletBinding()]
param(
    [string] $Contenedor = 'oracle-free-p3',
    [string] $IdSecretos = 'ecoaccess-p3-framework',
    [ValidatePattern('^[A-Za-z0-9_-]{1,40}$')] [string] $Dispositivo = 'simulado-p3',
    [string] $DirectorioPrivado = (Join-Path ([Environment]::GetFolderPath('CommonApplicationData')) 'EcoAccessP3Framework\Bridge')
)
$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$archivo = Join-Path $env:APPDATA "Microsoft\UserSecrets\$IdSecretos\secrets.json"
if (-not (Test-Path $archivo)) { throw 'Primero ejecuta Preparar-BaseDatos.ps1.' }
$secretos = Get-Content $archivo -Raw -Encoding UTF8 | ConvertFrom-Json
$cadena = $secretos.'ConnectionStrings:OracleP3'
$esquema = [regex]::Match($cadena, '(?i)User Id=([^;]+)').Groups[1].Value
$clave = [regex]::Match($cadena, '(?i)Password=([^;]+)').Groups[1].Value
$servicio = [regex]::Match($cadena, '(?i)/([A-Za-z0-9_]+)\s*(;|$)').Groups[1].Value

function Ejecutar([string] $Sql) {
    $temporal = [IO.Path]::GetTempFileName()
    try {
        [IO.File]::WriteAllText($temporal, $Sql, (New-Object Text.UTF8Encoding($false)))
        & docker cp $temporal "${Contenedor}:/tmp/ecoaccess_piloto.sql" 2>&1 | Out-Null
        $entrada = "CONNECT $esquema/`"$clave`"@localhost:1521/$servicio`nSET DEFINE OFF`n@/tmp/ecoaccess_piloto.sql`nEXIT"
        $salida = $entrada | & docker exec -i -e NLS_LANG=AMERICAN_AMERICA.AL32UTF8 $Contenedor bash -c "sed '1s/^\xEF\xBB\xBF//' | sqlplus -s /nolog" 2>&1 | Out-String
        if ($LASTEXITCODE -ne 0 -or $salida -match 'SP2-0640|ORA-\d{5}') { throw ('Oracle rechazó la preparación: ' + ($salida -replace [regex]::Escape($clave), '***')) }
        return $salida
    }
    finally {
        Remove-Item $temporal -ErrorAction SilentlyContinue
        & docker exec -u root $Contenedor rm -f /tmp/ecoaccess_piloto.sql | Out-Null
    }
}

& (Join-Path $PSScriptRoot 'Preparar-BaseDatos.ps1') -Contenedor $Contenedor -IdSecretos $IdSecretos -DatosDemostracion
if ($LASTEXITCODE) { throw 'No se pudo preparar la base.' }
$qr = $secretos.'EcoAccess:QrCanecaDemo'
if (-not $qr) {
    $bytes = New-Object byte[] 16
    [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
    $qr = ([Convert]::ToBase64String($bytes)).TrimEnd('=').Replace('+', '-').Replace('/', '_')
}
$plantilla = [IO.File]::ReadAllText((Join-Path $raiz 'database\03_datos_prueba\04_piloto_simulado.sql')).Replace('<<QR_PUBLICO>>', $qr)
$salida = Ejecutar $plantilla
Ejecutar ([IO.File]::ReadAllText((Join-Path $raiz 'database\03_datos_prueba\07_usuarios_demostracion.sql'))) | Out-Null
Ejecutar ([IO.File]::ReadAllText((Join-Path $raiz 'database\03_datos_prueba\05_tarifas_piloto_simulado.sql'))) | Out-Null
$id = [regex]::Match($salida, '(?m)^PILOTO_ID=([0-9]+)').Groups[1].Value
if (-not $id) { throw 'No se pudo comprobar la caneca creada.' }

foreach ($par in @(@('EcoAccess:QrCanecaDemo', $qr), @('Telegram:QrDemo', $qr), @('Telegram:NombreBot', 'EcoAccessP3_JG_bot'), @('Vision:Ollama:ServidorSinNubeVerificado', 'true'))) {
    $secretos | Add-Member -NotePropertyName $par[0] -NotePropertyValue $par[1] -Force
}
[IO.File]::WriteAllText($archivo, ($secretos | ConvertTo-Json), (New-Object Text.UTF8Encoding($false)))

$configuracion = Join-Path $DirectorioPrivado 'bridge.local.json'
if (Test-Path $configuracion) {
    Write-Output "Caneca $id lista; se conserva la configuración de dispositivos existente."
    return
}
New-Item -ItemType Directory -Force $DirectorioPrivado | Out-Null
$acl = Get-Acl $DirectorioPrivado
$acl.SetAccessRuleProtection($true, $false)
foreach ($sid in @([Security.Principal.WindowsIdentity]::GetCurrent().User, (New-Object Security.Principal.SecurityIdentifier('S-1-5-18')), (New-Object Security.Principal.SecurityIdentifier('S-1-5-32-544')))) {
    $acl.AddAccessRule((New-Object Security.AccessControl.FileSystemAccessRule($sid, 'FullControl', 'ContainerInherit,ObjectInherit', 'None', 'Allow')))
}
Set-Acl $DirectorioPrivado $acl
$claveHmac = New-Object byte[] 32
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($claveHmac)
[IO.File]::WriteAllBytes((Join-Path $DirectorioPrivado 'clave-hmac.bin'), $claveHmac)
[Array]::Clear($claveHmac, 0, 32)
$bridge = [ordered]@{ Bridge = [ordered]@{
    IdContenedor = [int]$id; Dispositivo = $Dispositivo; VersionClave = 1
    Eventos = 'http://127.0.0.1:8282/eventos/'; Comandos = 'http://127.0.0.1:8283/comandos/'
    DirectorioEstado = $DirectorioPrivado; ArchivoClaveHmac = (Join-Path $DirectorioPrivado 'clave-hmac.bin'); EscenarioSimulado = 'Normal'; VentanaSimuladaSegundos = 20
} }
[IO.File]::WriteAllText($configuracion, ($bridge | ConvertTo-Json -Depth 4), (New-Object Text.UTF8Encoding($false)))
Write-Output "Caneca de demostración $id y configuración privada de dispositivos creadas. La clave no se muestra."

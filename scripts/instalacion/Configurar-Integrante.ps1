<#
.SYNOPSIS
    Configura EcoAccess en el computador de un integrante (Jorge, Heyder o Saray) contra la base COMPARTIDA de Oracle Cloud.
.DESCRIPTION
    Pasos, en orden. Cada uno se puede repetir sin dañar nada:
      1. Comprueba el repositorio, .NET Framework 4.8, Visual Studio y el SDK de .NET (Validar-Entorno.ps1).
      2. Restaura los paquetes NuGet y compila la solución.
      3. Pide la ruta del Wallet_*.zip y lo revisa SIN mostrar su contenido: que se pueda abrir, que traiga
         cwallet.sso y tnsnames.ora y que tenga el servicio ecoaccess_tp.
      4. Instala el wallet en %USERPROFILE%\.oracle\wallets\EcoAccess, compila la aplicación en Release, la copia a
         %LOCALAPPDATA%\Programs\EcoAccess P3 y crea el acceso directo del escritorio (Instalar-EcoAccess.ps1).
      5. Pregunta tu cuenta personal (ECO_JORGE, ECO_HEYDER o ECO_SARAY) y muestra cómo configurar SQL Developer.
      6. comprobar-cuenta: lo autorizado funciona y lo administrativo se rechaza.
      7. prueba-colaborativa: escribe una fila de prueba y muestra las de los demás (misma base).
      8. Conexión de la aplicación C# (cuenta técnica ECOACCESS_P3_APP, distinta de tu cuenta personal): su
         contraseña la escribe el propietario del proyecto en este equipo; queda solo en tu perfil de Windows.
      9. Pruebas unitarias (opcional: -SinPruebas las omite).
    No guarda secretos en el repositorio, no imprime contraseñas y no necesita la contraseña de ADMIN.
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\instalacion\Configurar-Integrante.ps1
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\instalacion\Configurar-Integrante.ps1 -Usuario ECO_HEYDER -Wallet "$HOME\Downloads\Wallet_EcoAccess.zip"
#>
[CmdletBinding()]
param(
    [ValidateSet('ECO_JORGE', 'ECO_HEYDER', 'ECO_SARAY')] [string] $Usuario,
    [string] $Wallet,
    [switch] $SinPruebas,
    [switch] $SinConexionAplicacion
)
$ErrorActionPreference = 'Stop'
$raiz = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
if (-not (Test-Path -LiteralPath (Join-Path $raiz 'EcoAccess.P3.sln'))) { throw "No encuentro EcoAccess.P3.sln en $raiz. Ejecuta este script desde el repositorio clonado." }
Set-Location -LiteralPath $raiz
$instalador = 'src\EcoAccess.P3.Instalador'
$resumen = [System.Collections.Generic.List[object]]::new()
function Anotar([string] $Paso, [string] $Resultado, [string] $Detalle) { $resumen.Add([pscustomobject]@{ Paso = $Paso; Resultado = $Resultado; Detalle = $Detalle }) }
function Titulo([string] $Texto) { Write-Host ''; Write-Host "=== $Texto ===" -ForegroundColor Cyan }
function Continuar([string] $Pregunta) { (Read-Host "$Pregunta (s/n)") -match '^[sS]' }

Titulo '1. Requisitos del equipo'
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'Validar-Entorno.ps1')
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw 'Falta el SDK de .NET: instálalo desde https://dotnet.microsoft.com/download y vuelve a ejecutar.' }
Anotar 'Requisitos' 'REVISADO' "SDK de .NET $(& dotnet --version)"

Titulo '2. Restaurar y compilar la solución'
& dotnet build EcoAccess.P3.sln -c Debug -nologo -v quiet
if ($LASTEXITCODE -ne 0) { throw 'La compilación falló. Revisa los errores de arriba (si dice que un archivo está en uso, cierra EcoAccess y el bot).' }
Anotar 'Compilación Debug' 'OK' 'EcoAccess.P3.sln'

Titulo '3. Wallet de Oracle Cloud'
if (-not $Wallet) { $Wallet = Read-Host 'Ruta del Wallet_*.zip que te entregó el propietario (arrástralo aquí)' }
$Wallet = $Wallet.Trim('"', ' ')
if (-not (Test-Path -LiteralPath $Wallet)) { throw "No existe el archivo $Wallet" }
Add-Type -AssemblyName System.IO.Compression.FileSystem
try { $zip = [IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $Wallet).Path) } catch { throw 'El archivo no es un .zip válido. Pide de nuevo el wallet.' }
try {
    $nombres = $zip.Entries | ForEach-Object { $_.FullName }
    foreach ($requerido in 'cwallet.sso', 'tnsnames.ora', 'sqlnet.ora') {
        if (-not ($nombres -contains $requerido)) { throw "El wallet no trae $requerido. Debe ser el «Instance wallet» de la base EcoAccess." }
    }
    $lector = New-Object IO.StreamReader(($zip.Entries | Where-Object FullName -eq 'tnsnames.ora').Open())
    try { $tiene = $lector.ReadToEnd() -match '(?im)^\s*ecoaccess_tp\s*=' } finally { $lector.Dispose() }
    if (-not $tiene) { throw 'El wallet no tiene el servicio ecoaccess_tp: no es el de la base EcoAccess.' }
} finally { $zip.Dispose() }
Anotar 'Wallet' 'OK' 'zip válido con cwallet.sso, tnsnames.ora y servicio ecoaccess_tp (contenido no mostrado)'

Titulo '4. Instalar el wallet y la aplicación de escritorio'
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'Instalar-EcoAccess.ps1') -Wallet $Wallet
if ($LASTEXITCODE -ne 0) { throw 'Instalar-EcoAccess.ps1 terminó con error (¿EcoAccess está abierto? ciérralo y repite).' }
Anotar 'Aplicación y acceso directo' 'OK' "$env:LOCALAPPDATA\Programs\EcoAccess P3"
# Instalar-EcoAccess.ps1 deja el wallet aquí; los comandos del instalador lo reciben explícitamente.
$carpetaWallet = Join-Path $env:LOCALAPPDATA 'EcoAccessP3\wallet'
if (-not (Test-Path -LiteralPath (Join-Path $carpetaWallet 'tnsnames.ora'))) { throw "El wallet no quedó en $carpetaWallet. Revisa la salida de Instalar-EcoAccess.ps1." }

Titulo '5. Tu cuenta personal de Oracle'
if (-not $Usuario) {
    $opcion = Read-Host 'Elige tu cuenta: 1 = ECO_JORGE, 2 = ECO_HEYDER, 3 = ECO_SARAY'
    $Usuario = @{ '1' = 'ECO_JORGE'; '2' = 'ECO_HEYDER'; '3' = 'ECO_SARAY' }[$opcion.Trim()]
    if (-not $Usuario) { throw 'Opción no válida.' }
}
Write-Host @"
En SQL Developer crea la conexión (botón +):
  Name               EcoAccess Cloud - $Usuario
  Username           $Usuario
  Password           tu contraseña (la primera vez, la temporal que te entregaron)
  Connection Type    Cloud Wallet
  Configuration File $Wallet
  Service            ecoaccess_tp
Pulsa Test: la primera vez Oracle te pide una contraseña NUEVA (12 a 30 caracteres, con mayúscula, minúscula y número).
"@
Read-Host 'Cuando SQL Developer diga «Success» con tu contraseña nueva, pulsa Enter para continuar' | Out-Null

Titulo "6. Comprobar los permisos de $Usuario (escribe TU contraseña nueva)"
& dotnet run --project $instalador -- comprobar-cuenta --usuario $Usuario --wallet $carpetaWallet
Anotar 'comprobar-cuenta' $(if ($LASTEXITCODE -eq 0) { 'OK' } else { 'REVISAR' }) "código $LASTEXITCODE"

Titulo '7. Prueba colaborativa (misma base para todos)'
if (Continuar '¿Escribir una fila de prueba en PRUEBA_COLABORATIVA? (no toca datos reales)') {
    & dotnet run --project $instalador -- prueba-colaborativa --usuario $Usuario --wallet $carpetaWallet
    Anotar 'prueba-colaborativa' $(if ($LASTEXITCODE -eq 0) { 'OK' } else { 'REVISAR' }) 'compara los id con tus compañeros'
} else { Anotar 'prueba-colaborativa' 'OMITIDA' '' }

Titulo '8. Conexión de la aplicación C# (cuenta técnica ECOACCESS_P3_APP)'
Write-Host 'La aplicación NO usa tu cuenta personal. Su contraseña la escribe el propietario del proyecto en este equipo;'
Write-Host 'se comprueba contra Oracle y se guarda solo en tu perfil de Windows (User Secrets), nunca en el repositorio.'
if (-not $SinConexionAplicacion -and (Continuar '¿Está el propietario aquí (en persona o con control remoto) para escribirla ahora?')) {
    & dotnet run --project $instalador -- guardar-conexion --wallet $carpetaWallet
    Anotar 'Conexión de la aplicación' $(if ($LASTEXITCODE -eq 0) { 'OK' } else { 'REVISAR' }) 'User Secrets ecoaccess-p3-framework'
} else { Anotar 'Conexión de la aplicación' 'PENDIENTE' "repite con: dotnet run --project src\EcoAccess.P3.Instalador -- guardar-conexion --wallet `"$carpetaWallet`"" }

if (-not $SinPruebas) {
    Titulo '9. Pruebas unitarias (no necesitan Oracle)'
    & dotnet test tests\unitarias\EcoAccess.P3.Tests -c Debug --nologo -v quiet
    Anotar 'Pruebas unitarias' $(if ($LASTEXITCODE -eq 0) { 'OK' } else { 'REVISAR' }) 'tests\unitarias\EcoAccess.P3.Tests'
}

Titulo 'Resumen'
$resumen | Format-Table -AutoSize
Write-Host 'Abre EcoAccess desde el acceso directo «EcoAccess P3» del escritorio. Guía: docs\oracle-cloud\05_ACCESO_INTEGRANTES.md' -ForegroundColor Green

<#
.SYNOPSIS
    Abre la aplicación de escritorio de EcoAccess P3 (Windows Forms) desde esta copia del repositorio.
.DESCRIPTION
    Antes de abrir, comprueba lo básico: que exista el ejecutable compilado (Release o Debug), el wallet de Oracle (TNS_ADMIN)
    y los secretos de usuario «ecoaccess-p3». Si falta algo, explica qué hacer y no abre nada a medias.
    No guarda ni pide contraseñas, y no inicia sesión por usted: la ventana de inicio de sesión queda a la espera.
    Si la aplicación ya está abierta, la trae al frente en lugar de abrir otra copia.
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\desarrollo\Iniciar-EcoAccess-P3.ps1
#>
[CmdletBinding()]
param([ValidateSet('Release', 'Debug')] [string] $Configuracion)
$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$problemas = @()

$candidatos = if ($Configuracion) { @($Configuracion) } else { @('Release', 'Debug') }
$exe = $candidatos | ForEach-Object { Join-Path $raiz "src\EcoAccess.P3.Presentacion\bin\$_\net48\EcoAccess.P3.Presentacion.exe" } |
    Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $exe) { $problemas += 'No hay ejecutable compilado. Compile con: dotnet build EcoAccess.P3.sln -c Release' }

$tns = if ($env:TNS_ADMIN) { $env:TNS_ADMIN } else { [Environment]::GetEnvironmentVariable('TNS_ADMIN', 'User') }
if (-not $tns -or -not (Test-Path (Join-Path $tns 'tnsnames.ora'))) {
    $problemas += 'Falta el wallet de Oracle Cloud: defina TNS_ADMIN con la carpeta que contiene tnsnames.ora (ver docs/instalacion).'
}
$secretos = Join-Path $env:APPDATA 'Microsoft\UserSecrets\ecoaccess-p3\secrets.json'
if (-not (Test-Path $secretos)) {
    $problemas += 'Faltan los secretos de usuario: ejecute scripts\instalacion\Configurar-Credenciales.ps1.'
}
if (-not ([Environment]::Version.Major -ge 4)) { $problemas += 'Se necesita .NET Framework 4.8.' }

if ($problemas) {
    Write-Host 'No se abrió EcoAccess P3:' -ForegroundColor Yellow
    $problemas | ForEach-Object { Write-Host " - $_" }
    exit 1
}

$abierta = Get-Process -Name 'EcoAccess.P3.Presentacion' -ErrorAction SilentlyContinue | Select-Object -First 1
if ($abierta) {
    Add-Type -Namespace Eco -Name Ventana -MemberDefinition '[DllImport("user32.dll")] public static extern bool SetForegroundWindow(System.IntPtr h);'
    [void][Eco.Ventana]::SetForegroundWindow($abierta.MainWindowHandle)
    Write-Host "EcoAccess P3 ya estaba abierta (proceso $($abierta.Id)); se trajo al frente."
    exit 0
}
$env:TNS_ADMIN = $tns
Start-Process -FilePath $exe -WorkingDirectory (Split-Path $exe)
Write-Host "EcoAccess P3 abierta desde $exe. Inicie sesión con su cuenta."

<#
.SYNOPSIS
    Instala, actualiza o desinstala EcoAccess P3 (aplicacion de escritorio) en cualquier equipo Windows
    y crea el acceso directo oficial "ECOACCESS P3" con el icono original.

.DESCRIPTION
    Sin dependencias de OneDrive ni rutas privadas:
      - Soporta perfil EstacionPrincipal (equipo con ESP32/canecas) y EstacionCompanero (simulacion + Oracle + Telegram).
      - Comprueba .NET Framework 4.8, camara, Ollama y wallet Oracle individual (sin incluir secretos ni wallets en Git).
      - Permite -Desinstalar conservando intactos el wallet y las credenciales personales de Windows.
      - Nunca abre fisicamente las canecas ni inicia bots duplicados al arrancar desde el acceso directo.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\instalacion\Instalar-EcoAccess.ps1 -Perfil EstacionCompanero -Wallet "$HOME\Downloads\Wallet_ecoaccess.zip"
#>
[CmdletBinding()]
param(
    [string]$Wallet,
    [string]$Destino = (Join-Path $env:LOCALAPPDATA 'Programs\EcoAccess P3'),
    [ValidateSet('EstacionPrincipal', 'EstacionCompanero')]
    [string]$Perfil = 'EstacionCompanero',
    [switch]$SinCompilar,
    [switch]$SinAccesoDirecto,
    [switch]$Desinstalar
)
$ErrorActionPreference = 'Stop'
$nombreExe = 'EcoAccess.P3.Presentacion.exe'
$resultados = [System.Collections.Generic.List[object]]::new()
function Anotar([string]$Prueba, [bool]$Correcta, [string]$Detalle) {
    $resultados.Add([pscustomobject]@{ Prueba = $Prueba; Resultado = $(if ($Correcta) { 'OK' } else { 'FALTA' }); Detalle = $Detalle })
}

$escritorio = [Environment]::GetFolderPath('Desktop')
$lnk = Join-Path $escritorio 'ECOACCESS P3.lnk'

if ($Desinstalar) {
    $abierta = Get-Process -Name 'EcoAccess.P3.Presentacion' -ErrorAction SilentlyContinue |
        Where-Object { $_.Path -and $_.Path.StartsWith($Destino, [StringComparison]::OrdinalIgnoreCase) }
    if ($abierta) { throw 'Cierra EcoAccess P3 antes de desinstalar.' }
    if (Test-Path -LiteralPath $Destino) { Remove-Item -LiteralPath $Destino -Recurse -Force }
    if (-not $SinAccesoDirecto -and (Test-Path -LiteralPath $lnk)) { Remove-Item -LiteralPath $lnk -Force }
    Write-Host "EcoAccess P3 desinstalado de $Destino. Se conservaron el wallet (%LOCALAPPDATA%\EcoAccessP3\wallet) y las credenciales personales." -ForegroundColor Green
    return
}

# 0. Verificar Windows y .NET Framework 4.8
$netRel = (Get-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full' -Name Release -ErrorAction SilentlyContinue).Release
Anotar '.NET Framework 4.8' ($netRel -ge 528040) $(if ($netRel -ge 528040) { "Release $netRel" } else { 'Instala .NET Framework 4.8 Runtime desde Microsoft' })

# 1. Origen de la aplicacion: paquete publicado (aplicacion\ junto al script) o repositorio.
$paquete = Join-Path $PSScriptRoot 'aplicacion'
$raiz = $null
if (Test-Path -LiteralPath (Join-Path $paquete $nombreExe)) {
    $origen = $paquete
    Write-Host "Instalando desde paquete distribuible ($Perfil)." -ForegroundColor Cyan
} else {
    $raiz = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
    $proyecto = Join-Path $raiz 'src\EcoAccess.P3.Presentacion\EcoAccess.P3.Presentacion.csproj'
    $origen = Join-Path $raiz 'src\EcoAccess.P3.Presentacion\bin\Release\net48'
    if (-not $SinCompilar) {
        if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
            throw 'Falta el SDK de .NET (https://dotnet.microsoft.com/download). Instalalo o usa el paquete distribuible .zip.'
        }
        Write-Host 'Restaurando paquetes NuGet y compilando EcoAccess (Release)...' -ForegroundColor Cyan
        Push-Location -LiteralPath $raiz
        try {
            & dotnet build $proyecto -c Release -nologo -v quiet
            if ($LASTEXITCODE -ne 0) { throw "La compilacion fallo (codigo $LASTEXITCODE)." }
        } finally { Pop-Location }
    }
}
if (-not (Test-Path -LiteralPath (Join-Path $origen $nombreExe))) { throw "No se encontro $nombreExe en $origen." }

# 2. Copia limpia a la carpeta local de instalacion (conservando configuraciones personales fuera de $Destino)
$abierta = Get-Process -Name 'EcoAccess.P3.Presentacion' -ErrorAction SilentlyContinue |
    Where-Object { $_.Path -and $_.Path.StartsWith($Destino, [StringComparison]::OrdinalIgnoreCase) }
if ($abierta) { throw 'EcoAccess esta abierto desde la carpeta de instalacion. Cierralo y vuelve a ejecutar el instalador.' }
New-Item -ItemType Directory -Force -Path $Destino | Out-Null
& robocopy.exe $origen $Destino /MIR /NFL /NDL /NJH /NJS /NP /R:2 /W:1 | Out-Null
if ($LASTEXITCODE -ge 8) { throw "No se pudo copiar la aplicacion (robocopy $LASTEXITCODE)." }
$global:LASTEXITCODE = 0   # robocopy devuelve 1-7 cuando copia bien; no debe quedar como código de salida del instalador

# Copiar tambien el verificador de equipo y el icono si existen
$verifSrc = Join-Path $PSScriptRoot 'Verificar-Equipo-EcoAccess.ps1'
if (Test-Path -LiteralPath $verifSrc) { Copy-Item -LiteralPath $verifSrc -Destination (Join-Path $Destino 'Verificar-Equipo-EcoAccess.ps1') -Force }
$iconoDst = Join-Path $Destino 'EcoAccess.ico'
if ($raiz -and (Test-Path -LiteralPath (Join-Path $raiz 'assets\identidad\EcoAccess.ico'))) {
    Copy-Item -LiteralPath (Join-Path $raiz 'assets\identidad\EcoAccess.ico') -Destination $iconoDst -Force
}
# Iconos de los cuatro accesos: del repositorio (assets\identidad) o del paquete distribuible (junto a este script).
foreach ($ico in 'EcoAccess.ico', 'EcoAccess_ESP32.ico', 'EcoAccess_DOS_CLICS.ico', 'EcoAccess_WEB.ico') {
    $origenIco = @($(if ($raiz) { Join-Path $raiz "assets\identidad\$ico" }), (Join-Path $PSScriptRoot $ico)) | Where-Object { $_ -and (Test-Path -LiteralPath $_) } | Select-Object -First 1
    if ($origenIco) { Copy-Item -LiteralPath $origenIco -Destination (Join-Path $Destino $ico) -Force }
}

$exe = Join-Path $Destino $nombreExe
Anotar 'Aplicacion instalada' (Test-Path -LiteralPath $exe) $Destino

# Guardar perfil instalado (sin secretos)
@{ Perfil = $Perfil; FechaInstalacionUtc = (Get-Date).ToUniversalTime().ToString('O'); RutaInstalacion = $Destino } |
    ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Destino 'perfil_instalacion.json') -Encoding UTF8

# 3. Wallet de Oracle Cloud (opcional): siempre fuera del repositorio.
$carpetaWallet = Join-Path $env:LOCALAPPDATA 'EcoAccessP3\wallet'
if ($Wallet) {
    if (-not (Test-Path -LiteralPath $Wallet)) { throw "No existe el wallet indicado: $Wallet" }
    New-Item -ItemType Directory -Force -Path $carpetaWallet | Out-Null
    if ((Get-Item -LiteralPath $Wallet).PSIsContainer) {
        Copy-Item -Path (Join-Path $Wallet '*') -Destination $carpetaWallet -Recurse -Force
    } else {
        Expand-Archive -LiteralPath $Wallet -DestinationPath $carpetaWallet -Force
    }
    $sqlnet = Join-Path $carpetaWallet 'sqlnet.ora'
    if (Test-Path -LiteralPath $sqlnet) {
        $texto = Get-Content -LiteralPath $sqlnet -Raw
        $corregido = [regex]::Replace($texto, 'DIRECTORY\s*=\s*"[^"]*"', 'DIRECTORY="' + $carpetaWallet + '"')
        if ($corregido -ne $texto) {
            if (-not (Test-Path -LiteralPath "$sqlnet.original")) { Copy-Item -LiteralPath $sqlnet -Destination "$sqlnet.original" }
            Set-Content -LiteralPath $sqlnet -Value $corregido -Encoding ASCII -NoNewline
        }
    }
    & icacls.exe $carpetaWallet /inheritance:r /grant:r "$($env:USERNAME):(OI)(CI)F" | Out-Null
}
$candidatas = @($env:TNS_ADMIN, [Environment]::GetEnvironmentVariable('TNS_ADMIN', 'User'), $carpetaWallet,
    (Join-Path $env:USERPROFILE '.oracle\wallets\EcoAccess')) | Where-Object { $_ }
$walletEncontrado = $candidatas | Where-Object { Test-Path -LiteralPath (Join-Path $_ 'tnsnames.ora') } | Select-Object -First 1
Anotar 'Wallet de Oracle Cloud' ([bool]$walletEncontrado) $(if ($walletEncontrado) { $walletEncontrado } else { 'pide Wallet_*.zip por canal privado y repite con -Wallet <ruta>' })
$alias = $walletEncontrado -and (Select-String -LiteralPath (Join-Path $walletEncontrado 'tnsnames.ora') -Pattern '^\s*ecoaccess_tp\s*=' -Quiet)
Anotar 'Alias ecoaccess_tp' ([bool]$alias) 'tnsnames.ora del wallet'

# 4. Accesos directos oficiales: ECOACCESS P3, ECOACCESS ESP32 (panel local o del anfitrion), ECOACCESS DOS CLICS y
#    ECOACCESS WEB. Los crea Crear-AccesosDirectos.ps1 (respalda cualquier otro de EcoAccess, sin borrarlo).
if (-not $SinAccesoDirecto) {
    $crear = Join-Path $PSScriptRoot 'Crear-AccesosDirectos.ps1'
    $argumentos = @{ Destino = $Destino; Perfil = $Perfil; Escritorio = $escritorio }
    if ($raiz) { $argumentos.Raiz = $raiz }
    foreach ($r in & $crear @argumentos) { $resultados.Add($r) }
}

# 5. Camara y Ollama (diagnostico informativo)
$cam = @(Get-PnpDevice -Class Camera, Image -Status OK -ErrorAction SilentlyContinue)
Anotar 'Camara local' ($cam.Count -gt 0) $(if ($cam.Count -gt 0) { $cam[0].FriendlyName } else { 'no detectada (opcional; carga de archivos disponible)' })
$ollamaOk = $false
try { $v = (Invoke-RestMethod -Uri 'http://127.0.0.1:11434/api/version' -TimeoutSec 2).version; $ollamaOk = [bool]$v } catch { }
Anotar 'Ollama local' $ollamaOk $(if ($ollamaOk) { 'disponible en 127.0.0.1:11434' } else { 'opcional para vision local (https://ollama.com)' })

Write-Host ''
$resultados | Format-Table -AutoSize
Write-Host 'Instalacion finalizada. Para diagnostico completo ejecuta:' -ForegroundColor Green
Write-Host "  powershell -ExecutionPolicy Bypass -File `"$(Join-Path $Destino 'Verificar-Equipo-EcoAccess.ps1')`" -Perfil $Perfil" -ForegroundColor Gray

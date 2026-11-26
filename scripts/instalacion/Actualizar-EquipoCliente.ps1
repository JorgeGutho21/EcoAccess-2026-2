<#
.SYNOPSIS
    Actualiza el computador de un integrante (CLIENTE REMOTO de la Estación Oficial EcoAccess) en un solo paso.
.DESCRIPTION
    Se ejecuta desde cualquier carpeta; el script ubica solo la raíz del repositorio (nunca trabaja en System32).
      1. git status: si hay cambios locales sin confirmar, se detiene sin tocarlos (nunca stash, reset ni force).
      2. git fetch + checkout de la rama + git pull --ff-only.
      3. Compila e instala la aplicación con el perfil EstacionPrincipal (los cuatro accesos directos).
      4. Configura el anfitrión (Configurar-Anfitrion.ps1) si aún no está configurado o si se indica -Anfitrion; la clave
         se escribe en privado cuando el configurador la pide.
      5. Marca que este equipo NO ejecuta el bot de Telegram (lo ejecuta el anfitrión) y reinicia los servicios: en un
         cliente remoto no se inicia panel, pasarela ni bot, y nunca se abre un puerto COM.
      6. Comprueba el anfitrión en modo lectura (no mueve tapas).
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\instalacion\Actualizar-EquipoCliente.ps1 -Anfitrion 192.168.1.20
.EXAMPLE
    # Actualizaciones siguientes (el anfitrión ya quedó guardado en %LOCALAPPDATA%\EcoAccessP3\anfitrion.txt):
    powershell -ExecutionPolicy Bypass -File scripts\instalacion\Actualizar-EquipoCliente.ps1
#>
[CmdletBinding()]
param(
    [string] $Anfitrion,
    [string] $Rama = 'integracion/tres-canecas-fisicas'
)
$ErrorActionPreference = 'Stop'
$raiz = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
Set-Location -LiteralPath $raiz
Write-Host "Repositorio: $raiz" -ForegroundColor Cyan

# 1-2. Código
$cambios = @(git status --porcelain --untracked-files=no)
if ($cambios.Count -gt 0) {
    Write-Host 'Hay cambios locales sin confirmar; no se actualiza para no perderlos:' -ForegroundColor Yellow
    $cambios | ForEach-Object { Write-Host "  $_" }
    throw 'Confirma (git commit) o guarda esos cambios y vuelve a ejecutar este script.'
}
git fetch origin
if ($LASTEXITCODE -ne 0) { throw 'git fetch falló: revisa la conexión a Internet.' }
git checkout $Rama
if ($LASTEXITCODE -ne 0) { throw "No se pudo cambiar a la rama $Rama." }
git pull --ff-only origin $Rama
if ($LASTEXITCODE -ne 0) { throw 'git pull --ff-only no pudo avanzar (historial divergente): pide ayuda antes de forzar nada.' }
$commit = (git rev-parse --short HEAD).Trim()
Write-Host "Código actualizado: $Rama @ $commit" -ForegroundColor Green

# 3. Instalación (compila la aplicación y crea los cuatro accesos)
& (Join-Path $raiz 'scripts\instalacion\Instalar-EcoAccess.ps1') -Perfil EstacionPrincipal

# 4. Anfitrión de red
$cfg = Join-Path $env:LOCALAPPDATA 'EcoAccessP3\anfitrion.txt'
if ($Anfitrion -or -not (Test-Path -LiteralPath $cfg)) {
    & (Join-Path $raiz 'scripts\instalacion\Configurar-Anfitrion.ps1') -Anfitrion $Anfitrion
} else {
    Write-Host "Anfitrión ya configurado en $cfg (usa -Anfitrion <IP> si cambió)."
}

# 5. Sin bot propio y servicios reiniciados
& (Join-Path $raiz 'scripts\arranque\Ubicar-BotTelegram.ps1') -EnOtroEquipo 'anfitrión'
& powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "& '$(Join-Path $raiz 'scripts\arranque\Detener-ServiciosEcoAccess.ps1')' -Confirm:`$false" | Out-Null
& (Join-Path $raiz 'scripts\desarrollo\Iniciar-EstacionExpo.ps1')

# 6. Comprobación final (solo lectura)
$url = ((Get-Content -LiteralPath $cfg | Where-Object { $_ -match '^\s*url\s*=' } | Select-Object -First 1) -replace '^\s*url\s*=\s*', '').Trim()
$clave = ((Get-Content -LiteralPath $cfg | Where-Object { $_ -match '^\s*clave\s*=' } | Select-Object -First 1) -replace '^\s*clave\s*=\s*', '').Trim()
try {
    $e = Invoke-RestMethod -Uri "$url/api/estacion/estado" -Headers @{ 'X-Clave' = $clave } -TimeoutSec 6
    Write-Host "Listo ($commit). Anfitrión $url · ESP32 $(if ($e.conectado) { "en $($e.puerto)" } else { 'desconectado' }) · $(if ($e.enServicio) { 'en servicio' } else { "en pausa: $($e.pausa)" })" -ForegroundColor Green
} catch {
    Write-Warning "Actualizado ($commit), pero el anfitrión $url no respondió. Debe estar encendido, en la misma red privada y con Habilitar-PanelEnRed.ps1 aplicado."
}

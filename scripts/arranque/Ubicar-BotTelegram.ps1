<#
.SYNOPSIS
    Decide en qué computador se ejecuta el ÚNICO bot de Telegram de la Estación Oficial EcoAccess.
.DESCRIPTION
    Telegram entrega cada mensaje a un solo consumidor por token: si dos equipos ejecutan el bot a la vez, uno recibe
    «409 Conflict» y se detiene. Este script marca el equipo actual:
      · -EnOtroEquipo "<nombre>": este equipo deja de iniciar el bot (crea %ProgramData%\EcoAccessP3Framework\Arranque\bot-en-otro-equipo)
        y detiene el bot en polling que esté corriendo aquí. No toca el panel ni el ESP32.
      · -EnEsteEquipo: quita la marca e inicia el bot aquí (si no hay ya uno).
    El equipo que ejecuta el bot debe tener Oracle (wallet y credenciales) y Ollama con el modelo visual. Si el bot corre en
    el equipo de un compañero, ese equipo necesita además Configurar-Anfitrion.ps1 para enviar las aperturas al anfitrión.
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\arranque\Ubicar-BotTelegram.ps1 -EnOtroEquipo "PC de Logos"
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\arranque\Ubicar-BotTelegram.ps1 -EnEsteEquipo
#>
[CmdletBinding(SupportsShouldProcess)]
param([string] $EnOtroEquipo, [switch] $EnEsteEquipo)
$ErrorActionPreference = 'Stop'
if (-not $EnOtroEquipo -and -not $EnEsteEquipo) { throw 'Indica -EnOtroEquipo "<nombre del equipo>" o -EnEsteEquipo.' }
$marca = Join-Path ([Environment]::GetFolderPath('CommonApplicationData')) 'EcoAccessP3Framework\Arranque\bot-en-otro-equipo'
New-Item -ItemType Directory -Force (Split-Path $marca) | Out-Null
if ($EnOtroEquipo) {
    if ($PSCmdlet.ShouldProcess($env:COMPUTERNAME, 'Dejar de ejecutar el bot de Telegram en este equipo')) {
        Set-Content -LiteralPath $marca -Value $EnOtroEquipo.Trim() -Encoding UTF8
        Get-CimInstance Win32_Process -Filter "Name='EcoAccess.P3.Bot.exe'" -ErrorAction SilentlyContinue |
            Where-Object { $_.CommandLine -match '--polling' } |
            ForEach-Object { Stop-Process -Id $_.ProcessId -ErrorAction SilentlyContinue; Write-Host "Bot local detenido (PID $($_.ProcessId))." }
        Write-Host "Listo: el bot se ejecuta en «$($EnOtroEquipo.Trim())». Este equipo sigue controlando el ESP32 si lo tiene conectado."
    }
    return
}
if (Test-Path -LiteralPath $marca) { Remove-Item -LiteralPath $marca -Force }
& (Join-Path (Split-Path -Parent $PSScriptRoot) 'desarrollo\Iniciar-EstacionExpo.ps1') -SinPasarela
Write-Host 'Listo: este equipo ejecuta el bot de Telegram. Asegúrate de que ningún otro equipo lo esté ejecutando.'

<#
.SYNOPSIS
    Registra (o quita con -Quitar) una tarea programada del usuario actual que mantiene el bot de EcoAccess P3 activo.
.DESCRIPTION
    La tarea «EcoAccess P3 - vigilante del bot» se ejecuta al iniciar sesión y luego cada 5 minutos, oculta, y llama a
    Vigilar-Bot.ps1. No necesita administrador, no abre puertos y no guarda contraseñas: usa los secretos de usuario
    ya configurados. Sigue dependiendo de que este PC esté encendido y con sesión iniciada; no es un servidor 24/7.
.EXAMPLE
    pwsh -File scripts\desarrollo\Instalar-Arranque-Automatico.ps1
    pwsh -File scripts\desarrollo\Instalar-Arranque-Automatico.ps1 -Quitar
#>
[CmdletBinding()]
param([switch] $Quitar, [ValidateRange(1, 60)] [int] $Minutos = 5)
$ErrorActionPreference = 'Stop'
$nombre = 'EcoAccess P3 - vigilante del bot'

if ($Quitar) {
    if (Get-ScheduledTask -TaskName $nombre -ErrorAction SilentlyContinue) { Unregister-ScheduledTask -TaskName $nombre -Confirm:$false; Write-Host "Tarea quitada: $nombre" }
    else { Write-Host 'No había tarea registrada.' }
    exit 0
}

$pwsh = (Get-Command pwsh).Source
$vigilante = Join-Path $PSScriptRoot 'Vigilar-Bot.ps1'
$accion = New-ScheduledTaskAction -Execute $pwsh -Argument "-NoProfile -WindowStyle Hidden -File `"$vigilante`""
$alIniciar = New-ScheduledTaskTrigger -AtLogOn -User "$env:USERDOMAIN\$env:USERNAME"
$repetir = New-ScheduledTaskTrigger -Once -At (Get-Date).AddMinutes(1) -RepetitionInterval (New-TimeSpan -Minutes $Minutos)
$ajustes = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable `
    -MultipleInstances IgnoreNew -ExecutionTimeLimit (New-TimeSpan -Minutes 10)
$principal = New-ScheduledTaskPrincipal -UserId "$env:USERDOMAIN\$env:USERNAME" -LogonType Interactive -RunLevel Limited
Register-ScheduledTask -TaskName $nombre -Action $accion -Trigger @($alIniciar, $repetir) -Settings $ajustes -Principal $principal `
    -Description 'Mantiene activo el bot de Telegram de EcoAccess P3 (un solo consumidor). Creada por Instalar-Arranque-Automatico.ps1.' -Force | Out-Null
Write-Host "Tarea registrada: $nombre (al iniciar sesión y cada $Minutos min)."
Write-Host 'Para detener el bot sin que vuelva a arrancar: crea C:\ProgramData\EcoAccessP3Framework\Desarrollo\pausa-vigilante o usa -Quitar.'

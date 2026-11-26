<#
.SYNOPSIS
    Permite que los computadores autorizados de la red local controlen la Estación Oficial EcoAccess a través del panel
    (http://<anfitrion>:8090), que sigue siendo el ÚNICO dueño del puerto serial del ESP32.
.DESCRIPTION
    Se ejecuta UNA vez, como administrador, en el equipo anfitrión (el que tiene el ESP32 conectado):
      · Reserva la URL http://+:8090/ para el usuario actual (netsh http add urlacl).
      · Abre el puerto 8090 en el firewall SOLO para redes privadas (nunca en redes públicas).
    Las peticiones desde la red exigen la clave de la estación (encabezado X-Clave), que está en
    %ProgramData%\EcoAccessP3Framework\Panel\clave.txt y se entrega a los compañeros solo por un canal privado.
    La sesión supervisada, «ESTACIÓN EN SERVICIO» y la calibración solo se activan desde el propio anfitrión.
    Después reinicia los servicios (scripts\arranque\Iniciar-ServiciosEcoAccess.ps1) para que el panel escuche en la red.
    -Quitar revierte ambos cambios.
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\arranque\Habilitar-PanelEnRed.ps1
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\arranque\Habilitar-PanelEnRed.ps1 -Quitar
#>
[CmdletBinding(SupportsShouldProcess)]
param([int] $Puerto = 8090, [switch] $Quitar)
$ErrorActionPreference = 'Stop'
$admin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $admin) { throw 'Ejecuta este script en una consola de PowerShell abierta como administrador.' }
$url = "http://+:$Puerto/"
$regla = "EcoAccess - Panel de la estacion ($Puerto, red privada)"
$usuario = "$env:USERDOMAIN\$env:USERNAME"

if ($Quitar) {
    if ($PSCmdlet.ShouldProcess($url, 'Quitar la reserva de URL')) { & netsh http delete urlacl url=$url | Out-Null }
    if ($PSCmdlet.ShouldProcess($regla, 'Quitar la regla del firewall')) { Get-NetFirewallRule -DisplayName $regla -ErrorAction SilentlyContinue | Remove-NetFirewallRule }
    Write-Host 'El panel vuelve a ser solo de este equipo (reinicia los servicios).'
    return
}
if ($PSCmdlet.ShouldProcess($url, "Reservar la URL para $usuario")) {
    & netsh http delete urlacl url=$url 2>$null | Out-Null
    & netsh http add urlacl url=$url user=$usuario | Out-Null
}
if ($PSCmdlet.ShouldProcess($regla, 'Abrir el puerto solo en redes privadas')) {
    Get-NetFirewallRule -DisplayName $regla -ErrorAction SilentlyContinue | Remove-NetFirewallRule
    New-NetFirewallRule -DisplayName $regla -Direction Inbound -Protocol TCP -LocalPort $Puerto -Action Allow -Profile Private | Out-Null
}
$ip = (Get-NetIPAddress -AddressFamily IPv4 -PrefixOrigin Dhcp, Manual -ErrorAction SilentlyContinue | Where-Object { $_.IPAddress -notlike '169.*' } | Select-Object -First 1).IPAddress
Write-Host "Listo. Reinicia los servicios y, en cada equipo autorizado, usa ECOACCESS_PANEL_URL=http://$ip`:$Puerto"

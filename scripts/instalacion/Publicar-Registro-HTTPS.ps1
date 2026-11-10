<#
.SYNOPSIS
    Publica solo la pagina de vinculacion de EcoAccess por HTTPS con Tailscale Funnel.
.DESCRIPTION
    Requiere haber iniciado sesion en Tailscale y tener Funnel permitido en la cuenta. Publica unicamente /registro/;
    Oracle, Ollama y el bridge no se exponen. No necesita administrador: el bot escucha solo en 127.0.0.1 y Funnel
    reenvia hasta alli. Depende de que este equipo este encendido. Para retirar la publicacion: tailscale funnel reset.
#>
[CmdletBinding()]
param(
    [string] $IdSecretos = 'ecoaccess-p3-framework',
    [ValidateRange(1024, 65535)] [int] $Puerto = 8081
)
$ErrorActionPreference = 'Stop'
$tailscale = Join-Path $env:ProgramFiles 'Tailscale\tailscale.exe'
if (-not (Test-Path $tailscale)) { throw 'No encuentro Tailscale. Instalalo desde https://tailscale.com/download.' }
$estado = & $tailscale status --json --peers=false | ConvertFrom-Json
if ($estado.BackendState -ne 'Running') { throw "Tailscale no tiene una sesion activa (estado: $($estado.BackendState)). Abre Tailscale, inicia sesion y repite este script." }
$dominio = $estado.Self.DNSName.TrimEnd('.')
if (-not $dominio) { throw 'Tailscale no informa el nombre publico de este equipo.' }

# Funnel entrega el tráfico en 127.0.0.1: basta escuchar en la interfaz local. Así no se necesita administrador (netsh)
# y el formulario no queda abierto a la red local, solo al túnel HTTPS.
$prefijo = "http://127.0.0.1:$Puerto/registro/"

& $tailscale funnel --bg --set-path=/registro "http://127.0.0.1:$Puerto/registro"
if ($LASTEXITCODE -ne 0) { throw 'Tailscale no publico la pagina. Si muestra un enlace, abrelo para permitir Funnel en tu cuenta y repite este script.' }

$archivo = Join-Path $env:APPDATA "Microsoft\UserSecrets\$IdSecretos\secrets.json"
if (-not (Test-Path $archivo)) { throw 'No encuentro la configuracion privada. Ejecuta primero Preparar-BaseDatos.ps1.' }
$secretos = Get-Content $archivo -Raw -Encoding UTF8 | ConvertFrom-Json
$pagina = "https://$dominio/registro/"
$secretos | Add-Member -NotePropertyName 'Telegram:PaginaRegistro' -NotePropertyValue $pagina -Force
$secretos | Add-Member -NotePropertyName 'Telegram:PrefijoRegistro' -NotePropertyValue $prefijo -Force
[IO.File]::WriteAllText($archivo, ($secretos | ConvertTo-Json -Depth 6), (New-Object Text.UTF8Encoding($false)))

Write-Output "Pagina publica configurada: $pagina"
Write-Output 'Reinicia el bot con scripts\desarrollo\Iniciar-Bot-Eco.ps1 y comprueba /vincular desde el telefono, con datos moviles.'

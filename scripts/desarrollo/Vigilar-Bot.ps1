<#
.SYNOPSIS
    Vigilante del bot de EcoAccess P3: si el bot de esta copia no está en marcha, lo inicia con Iniciar-EcoAccess-Nube.ps1.
.DESCRIPTION
    Pensado para ejecutarse cada pocos minutos desde la tarea programada que crea Instalar-Arranque-Automatico.ps1.
    - Si el bot de esta copia ya corre, no hace nada.
    - Si corre el bot de OTRA copia (por ejemplo EcoAccess V2), no lo detiene ni compite por el token: solo lo registra.
    - Si existe el archivo de pausa (Desarrollo\pausa-vigilante), no inicia nada: así Detener-Demostracion.ps1 o el
      propietario pueden detener el bot sin que el vigilante lo vuelva a levantar.
    Registro: C:\ProgramData\EcoAccessP3Framework\Desarrollo\vigilante.log (sin secretos).
#>
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$control = Join-Path ([Environment]::GetFolderPath('CommonApplicationData')) 'EcoAccessP3Framework\Desarrollo'
New-Item -ItemType Directory -Force $control | Out-Null
$registro = Join-Path $control 'vigilante.log'
function Anotar([string] $Texto) { Add-Content -Path $registro -Value ("{0:yyyy-MM-dd HH:mm:ss} {1}" -f (Get-Date), $Texto) }

if (Test-Path (Join-Path $control 'pausa-vigilante')) { exit 0 }

# Registro público (15/11/2026): tras reiniciar el PC, el servicio de Tailscale quedó en «NoState» y la página de vinculación
# dejó de responder aunque el bot seguía activo. El servicio solo terminó de conectarse al abrir la aplicación de Tailscale
# (tailscale-ipn.exe, sin administrador). El vigilante la abre si el túnel no está en «Running»; la publicación de Funnel
# se conserva y no hay que repetir Publicar-Registro-HTTPS.ps1.
$tailscale = Join-Path $env:ProgramFiles 'Tailscale\tailscale.exe'
if (Test-Path $tailscale) {
    $estadoTunel = try { (& $tailscale status --json --peers=false 2>$null | ConvertFrom-Json).BackendState } catch { 'Desconocido' }
    if ($estadoTunel -ne 'Running') {
        $aplicacion = Join-Path $env:ProgramFiles 'Tailscale\tailscale-ipn.exe'
        if ((Test-Path $aplicacion) -and -not (Get-Process tailscale-ipn -ErrorAction SilentlyContinue)) {
            Start-Process $aplicacion
            Anotar "Tailscale en estado ${estadoTunel}: se abre la aplicación para reactivar el registro público."
        } else { Anotar "Tailscale en estado ${estadoTunel} con la aplicación abierta: revisar la sesión de Tailscale." }
    }
}

# La ruta se compara con la carpeta más «\»: si no, EcoAccess-P3 tomaría como propio el bot de EcoAccess-P3-auditoria.
# Solo cuentan los procesos que consumen el token de Telegram. Una evaluación (--evaluar-objetos) o un diagnóstico usan
# el mismo ejecutable pero no compiten por el token: antes se tomaban por «otro bot» y el vigilante dejaba el bot caído.
$bots = @(Get-CimInstance Win32_Process -Filter "Name='EcoAccess.P3.Bot.exe'" | Where-Object { $_.CommandLine -match '--(polling|webhook)\b' })
$propios = @($bots | Where-Object { $_.ExecutablePath -and $_.ExecutablePath.StartsWith($raiz.TrimEnd('\') + '\',[StringComparison]::OrdinalIgnoreCase) })
if ($propios.Count -gt 0) { exit 0 }
if ($bots.Count -gt 0) { Anotar "Otro bot usa el token ($($bots[0].ExecutablePath)); no se inicia este para no competir."; exit 0 }

Anotar 'Bot detenido: se inicia.'
# El lanzador se ejecuta aparte y su salida va a un archivo: si se leyera por tubería, el bot que arranca heredaría la
# tubería y el vigilante quedaría esperando mientras el bot siga vivo. Si el inicio falla (sin red, PC recién despierto),
# el siguiente ciclo de la tarea lo vuelve a intentar.
$salida = Join-Path $control 'vigilante-ultimo-inicio.log'
Start-Process -FilePath (Get-Command pwsh).Source -WindowStyle Hidden `
    -ArgumentList @('-NoProfile', '-File', "`"$(Join-Path $PSScriptRoot 'Iniciar-EcoAccess-Nube.ps1')`"") `
    -RedirectStandardOutput $salida -RedirectStandardError "$salida.err"
Anotar "Lanzador iniciado; salida en $salida."

<#
.SYNOPSIS
    Lo que hace el acceso directo «ECOACCESS P3» en la estación anfitriona: deja lista la estación y abre la aplicación.
.DESCRIPTION
    Se ejecuta oculto (conhost --headless) y NUNCA abre una tapa:
      1. Si este equipo es la estación (bridge-*.json privados en ProgramData) y la estación no está corriendo, la inicia con
         Iniciar-Estacion-Fisica.ps1 (adaptadores, bridges y bot en modo físico supervisado). Si el ESP32 no responde,
         deja el bot de Telegram en modo orientación (--polling --sin-dispositivo) para que el celular siga funcionando.
      2. No duplica procesos: si ya hay un bot en polling, no inicia otro.
      3. Inicia Ollama si está instalado y apagado.
      4. Abre la aplicación instalada (o la compilación Release del repositorio si no está instalada).
    Registro: %ProgramData%\EcoAccessP3Framework\Sesiones\abrir-ecoaccess.log
    En equipos de compañeros (sin bridge-*.json) solo abre la aplicación: nunca inicia otro consumidor del bot.
#>
[CmdletBinding()]
param()
$ErrorActionPreference = 'Continue'
$raiz = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$base = Join-Path ([Environment]::GetFolderPath('CommonApplicationData')) 'EcoAccessP3Framework'
$log = Join-Path $base 'Sesiones\abrir-ecoaccess.log'
New-Item -ItemType Directory -Force (Split-Path $log) | Out-Null
function Anotar([string] $t) { Add-Content -LiteralPath $log -Value "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] $t" -Encoding UTF8 }

$app = Join-Path $env:LOCALAPPDATA 'Programs\EcoAccess P3\EcoAccess.P3.Presentacion.exe'
if (-not (Test-Path -LiteralPath $app)) { $app = Join-Path $raiz 'src\EcoAccess.P3.Presentacion\bin\Release\net48\EcoAccess.P3.Presentacion.exe' }
$bot = Join-Path $raiz 'src\EcoAccess.P3.Bot\bin\Release\net48\EcoAccess.P3.Bot.exe'
$esEstacion = (Test-Path (Join-Path $base 'Bridge\bridge-verde.json')) -or (Test-Path (Join-Path $base 'Panel\clave.txt'))

try {
    if ($esEstacion -and (Test-Path -LiteralPath $bot)) {
        # ExpoSoftware: Ollama + panel de canecas (único dueño del ESP32) + un solo bot en Modo Expo. Nada se mueve hasta
        # pulsar ACTIVAR CONTROL en el panel.
        $registroEstacion = Join-Path $base 'Sesiones\abrir-estacion.salida.log'
        $p = Start-Process -FilePath 'powershell.exe' -WindowStyle Hidden -PassThru `
            -ArgumentList @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', "`"$(Join-Path $PSScriptRoot 'Iniciar-EstacionExpo.ps1')`"") `
            -RedirectStandardOutput $registroEstacion -RedirectStandardError "$registroEstacion.err"
        $null = $p.Handle # sin el manejador abierto, ExitCode queda vacío al terminar
        if (-not $p.WaitForExit(120000)) { Anotar 'Iniciar-EstacionExpo no terminó en 2 minutos; se continúa.' }
        $salida = (Get-Content -LiteralPath $registroEstacion, "$registroEstacion.err" -ErrorAction SilentlyContinue) -join "`n"
        Anotar ('Estación Expo: ' + (($salida -split "`r?`n" | Where-Object { $_ -match 'Panel:|Bot|Ollama|AVISO' }) -join ' | '))
    }

    $ollama = Join-Path $env:LOCALAPPDATA 'Programs\Ollama\ollama app.exe'
    if ((Test-Path -LiteralPath $ollama) -and -not (Get-Process -Name 'ollama' -ErrorAction SilentlyContinue)) {
        Start-Process -FilePath $ollama -ArgumentList '--hide' -WindowStyle Hidden; Anotar 'Ollama iniciado.'
    }
} catch {
    Anotar "Aviso al preparar la estación: $($_.Exception.Message)"
}

$abierta = @(Get-Process -Name 'EcoAccess.P3.Presentacion' -ErrorAction SilentlyContinue)
if ($abierta.Count -gt 0) {
    Add-Type -Namespace EcoAccess -Name Ventana -MemberDefinition '[DllImport("user32.dll")] public static extern bool SetForegroundWindow(System.IntPtr h); [DllImport("user32.dll")] public static extern bool ShowWindow(System.IntPtr h, int c);'
    [void][EcoAccess.Ventana]::ShowWindow($abierta[0].MainWindowHandle, 9); [void][EcoAccess.Ventana]::SetForegroundWindow($abierta[0].MainWindowHandle)
    Anotar 'La aplicación ya estaba abierta: se trajo al frente.'
} elseif (Test-Path -LiteralPath $app) {
    Start-Process -FilePath $app -WorkingDirectory (Split-Path $app)
    Anotar "Aplicación abierta: $app"
} else {
    Anotar 'No se encontró la aplicación: instala con scripts\instalacion\Instalar-EcoAccess.ps1.'
}

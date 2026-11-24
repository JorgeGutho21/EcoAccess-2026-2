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
$esEstacion = Test-Path (Join-Path $base 'Bridge\bridge-verde.json')

try {
    if ($esEstacion -and (Test-Path -LiteralPath $bot)) {
        $enPolling = @(Get-CimInstance Win32_Process -Filter "Name='EcoAccess.P3.Bot.exe'" -ErrorAction SilentlyContinue | Where-Object { $_.CommandLine -match '--polling' })
        $bridges = @(Get-CimInstance Win32_Process -Filter "Name='EcoAccess.P3.Bot.exe'" -ErrorAction SilentlyContinue | Where-Object { $_.CommandLine -match '--bridge' })
        if ($enPolling.Count -gt 0 -and $bridges.Count -ge 3) {
            Anotar "Estación ya activa (bot PID $($enPolling[0].ProcessId), $($bridges.Count) bridges): no se duplica."
        } elseif ($enPolling.Count -gt 0 -and $enPolling[0].CommandLine -notmatch 'sin-dispositivo') {
            Anotar "Bot en polling activo (PID $($enPolling[0].ProcessId)) sin bridges completos: no se inicia otro consumidor."
        } else {
            Anotar 'Iniciando estación física (sin abrir tapas)...'
            $salida = & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'Iniciar-Estacion-Fisica.ps1') -SinCompilar 2>&1 | Out-String
            $codigo = $LASTEXITCODE
            Anotar ("Iniciar-Estacion-Fisica terminó con código $codigo. " + (($salida -split "`r?`n" | Where-Object { $_ -match 'ESP32|COM|caída|NO ESCUCHA|Detenido|bot:' }) -join ' | '))
            $vivo = @(Get-CimInstance Win32_Process -Filter "Name='EcoAccess.P3.Bot.exe'" -ErrorAction SilentlyContinue | Where-Object { $_.CommandLine -match '--polling' })
            if ($vivo.Count -eq 0) {
                # Sin ESP32 (o con la estación caída) el celular sigue orientando: modo software, que jamás mueve motores.
                $dir = Join-Path $base 'Sesiones'
                $p = Start-Process -FilePath $bot -ArgumentList '--polling --sin-dispositivo' -WorkingDirectory $raiz -WindowStyle Hidden -PassThru `
                    -RedirectStandardOutput (Join-Path $dir 'bot-orientacion.out.log') -RedirectStandardError (Join-Path $dir 'bot-orientacion.err.log')
                Anotar "Estación física no disponible: bot de Telegram en modo orientación (PID $($p.Id))."
            }
        }
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

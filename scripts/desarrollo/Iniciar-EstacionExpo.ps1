<#
.SYNOPSIS
    Inicia la estación ExpoSoftware en el equipo anfitrión (el que tiene el ESP32 por USB): Ollama con el modelo visual
    cargado, el panel local de canecas (único dueño del puerto del ESP32) y UN solo bot de Telegram en Modo Expo.
.DESCRIPTION
    No mueve ningún motor por sí mismo: la estación queda en servicio (salvo pausa manual) y cada apertura exige una orden autenticada y el ESP32 conectado.
    Orden:
      1. Ollama (127.0.0.1:11434) respondiendo; si no, intenta iniciarlo.
      2. Detiene adaptadores/bridges HMAC de una sesión anterior (abrían COM3 y competirían con el panel).
      3. Panel de canecas (http://localhost:8090) con el firmware hardware\CalibracionTresCanecas v2.
      4. Bot --polling (si no hay ya uno: una sola instancia de polling por token) con ECOACCESS_MODO_EXPO=1 y las cuentas
         de Telegram autorizadas de %ProgramData%\EcoAccessP3Framework\Bridge\telegram-autorizados.txt.
    Registro: %ProgramData%\EcoAccessP3Framework\Sesiones\expo-*.log. Para detener: Detener-Demostracion.ps1 o cerrar los procesos.
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\desarrollo\Iniciar-EstacionExpo.ps1 -AbrirPanel
.EXAMPLE
    # Tras añadir un ID a telegram-autorizados.txt: detiene el bot en polling y lo inicia de nuevo con la lista actual.
    powershell -ExecutionPolicy Bypass -File scripts\desarrollo\Iniciar-EstacionExpo.ps1 -ReiniciarBot
#>
[CmdletBinding()]
param([switch] $SinBot, [switch] $AbrirPanel, [switch] $ReiniciarBot, [switch] $SinPasarela)
$ErrorActionPreference = 'Continue'
$raiz = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$base = Join-Path ([Environment]::GetFolderPath('CommonApplicationData')) 'EcoAccessP3Framework'
$sesiones = Join-Path $base 'Sesiones'
New-Item -ItemType Directory -Force $sesiones | Out-Null
$bot = Join-Path $raiz 'src\EcoAccess.P3.Bot\bin\Release\net48\EcoAccess.P3.Bot.exe'
function Paso([string] $t) { Write-Host "== $t" }

# Anfitrión de red (equipo de un compañero): ECOACCESS_PANEL_URL o %LOCALAPPDATA%\EcoAccessP3\anfitrion.txt
# (Configurar-Anfitrion.ps1). Si apunta a otro equipo, este NO inicia panel ni pasarela: el único dueño del ESP32 es el
# anfitrión. Devuelve @{ Url; Clave } o $null si este equipo usa su propio panel.
function Get-AnfitrionRemoto {
    $url = $env:ECOACCESS_PANEL_URL; $clave = $env:ECOACCESS_PANEL_CLAVE
    $archivo = Join-Path $env:LOCALAPPDATA 'EcoAccessP3\anfitrion.txt'
    if (Test-Path -LiteralPath $archivo) {
        foreach ($l in Get-Content -LiteralPath $archivo -ErrorAction SilentlyContinue) {
            if ($l -match '^\s*#') { continue }
            if (-not $url -and $l -match '^\s*url\s*=\s*(\S+)') { $url = $Matches[1] }
            elseif (-not $clave -and $l -match '^\s*clave\s*=\s*(.+)$') { $clave = $Matches[1].Trim() }
        }
    }
    if (-not $url) { return $null }
    try { $u = [Uri]$url } catch { return $null }
    if ($u.IsLoopback -or $u.Host -eq 'localhost') { return $null }
    @{ Url = $url.TrimEnd('/'); Clave = $clave }
}
$remoto = Get-AnfitrionRemoto
# Un solo bot por token en toda la estación: el equipo que NO lo ejecuta tiene Arranque\bot-en-otro-equipo
# (scripts\arranque\Ubicar-BotTelegram.ps1). Así dos computadores nunca compiten por las actualizaciones de Telegram.
$marcaBot = Join-Path $base 'Arranque\bot-en-otro-equipo'
if (-not $SinBot -and (Test-Path -LiteralPath $marcaBot)) {
    Write-Host "== Bot de Telegram`n   Este equipo no ejecuta el bot (se ejecuta en otro computador: $((Get-Content -LiteralPath $marcaBot -TotalCount 1)))."
    $SinBot = $true
}

Paso 'Ollama y modelo visual'
$ollama = $false
try { $ollama = [bool](Invoke-RestMethod 'http://127.0.0.1:11434/api/version' -TimeoutSec 3) } catch { }
if (-not $ollama) {
    $exe = (Get-Command ollama -ErrorAction SilentlyContinue).Source
    if ($exe) {
        Start-Process $exe -ArgumentList 'serve' -WindowStyle Hidden | Out-Null
        for ($i = 0; $i -lt 20 -and -not $ollama; $i++) { Start-Sleep -Milliseconds 500; try { $ollama = [bool](Invoke-RestMethod 'http://127.0.0.1:11434/api/version' -TimeoutSec 2) } catch { } }
    }
}
Write-Host ($(if ($ollama) { '   Ollama activo (el bot carga lfm3-vl y lo mantiene en memoria).' } else { '   AVISO: Ollama no responde; las fotos no se podrán identificar (el control manual sigue funcionando).' }))

Paso 'Procesos que abrían COM3 (adaptadores y bridges HMAC)'
Get-CimInstance Win32_Process -Filter "Name='EcoAccess.P3.Bot.exe'" -ErrorAction SilentlyContinue |
    Where-Object { $_.CommandLine -match '--(bridge|dispositivo-serial|dispositivo-simulado)\b' } |
    ForEach-Object { Stop-Process -Id $_.ProcessId -ErrorAction SilentlyContinue; Write-Host "   Detenido PID $($_.ProcessId)." }

Paso 'Panel de canecas'
if ($remoto) {
    Write-Host "   Anfitrión de red: $($remoto.Url). Este equipo no abre ningún puerto COM ni inicia panel local."
    if (Get-Process -Name 'EcoAccess.P3.PanelCanecas' -ErrorAction SilentlyContinue) { Write-Host '   AVISO: hay un panel local abierto en este equipo; no lo necesita mientras use el anfitrión de red.' }
    try {
        $e = Invoke-RestMethod -Uri "$($remoto.Url)/api/estacion/estado" -Headers @{ 'X-Clave' = "$($remoto.Clave)" } -TimeoutSec 6
        Write-Host "   Anfitrión: $($e.texto) Puerto: $($e.puerto). En servicio: $($e.enServicio)."
    } catch { Write-Host "   AVISO: el anfitrión $($remoto.Url) no responde o rechazó la clave. Las aperturas quedarán pendientes hasta que responda." }
    $SinPasarela = $true
} else {
$panelExe = Join-Path $raiz 'src\EcoAccess.P3.PanelCanecas\bin\Release\net48\EcoAccess.P3.PanelCanecas.exe'
$panel = $false
try { $panel = [bool](Invoke-RestMethod 'http://localhost:8090/api/historial' -TimeoutSec 2) } catch { }
if (-not $panel -and (Test-Path -LiteralPath $panelExe)) {
    $dirPanel = Join-Path $base 'Panel'; New-Item -ItemType Directory -Force $dirPanel | Out-Null
    Start-Process -FilePath $panelExe -WorkingDirectory (Split-Path $panelExe) -WindowStyle Hidden `
        -RedirectStandardOutput (Join-Path $dirPanel 'panel.salida.log') -RedirectStandardError (Join-Path $dirPanel 'panel.error.log') | Out-Null
    for ($i = 0; $i -lt 20 -and -not $panel; $i++) { Start-Sleep -Milliseconds 500; try { $panel = [bool](Invoke-RestMethod 'http://localhost:8090/api/historial' -TimeoutSec 2) } catch { } }
}
try { $e = Invoke-RestMethod 'http://localhost:8090/api/estacion/estado' -TimeoutSec 3; Write-Host "   Panel: $($e.texto) Puerto: $($e.puerto). $($e.identificador)" }
catch { Write-Host '   AVISO: el panel no respondió.' }
}

if (-not $SinBot) {
    Paso 'Bot de Telegram (Modo ExpoSoftware)'
    $polling = @(Get-CimInstance Win32_Process -Filter "Name='EcoAccess.P3.Bot.exe'" -ErrorAction SilentlyContinue | Where-Object { $_.CommandLine -match '--polling' })
    if ($ReiniciarBot -and $polling.Count -gt 0) {
        foreach ($b in $polling) { Stop-Process -Id $b.ProcessId -ErrorAction SilentlyContinue; Write-Host "   Bot anterior detenido (PID $($b.ProcessId))." }
        Start-Sleep -Seconds 2
        $polling = @()
    }
    if ($polling.Count -gt 0) {
        Write-Host "   Ya hay un bot en polling (PID $($polling[0].ProcessId)): no se inicia otro consumidor del mismo token."
    } elseif (-not (Test-Path -LiteralPath $bot)) {
        Write-Host '   AVISO: falta el bot compilado (dotnet build src\EcoAccess.P3.Bot -c Release).'
    } else {
        $autorizados = Join-Path $base 'Bridge\telegram-autorizados.txt'
        $ids = @(if (Test-Path $autorizados) { Get-Content $autorizados | ForEach-Object { $_.Trim() } | Where-Object { $_ -match '^\d{4,19}$' } })
        $vars = @{ ECOACCESS_MODO_EXPO = '1'; ECOACCESS_TELEGRAM_CHATS_AUTORIZADOS = ($ids -join ',') }
        $anterior = @{}
        foreach ($k in $vars.Keys) { $anterior[$k] = [Environment]::GetEnvironmentVariable($k); [Environment]::SetEnvironmentVariable($k, $vars[$k]) }
        try {
            $sello = Get-Date -Format 'yyyyMMdd-HHmmss'
            $p = Start-Process $bot -ArgumentList '--polling' -WindowStyle Hidden -PassThru -WorkingDirectory $raiz `
                -RedirectStandardOutput (Join-Path $sesiones "expo-bot-$sello.salida.log") -RedirectStandardError (Join-Path $sesiones "expo-bot-$sello.error.log")
        } finally { foreach ($k in $vars.Keys) { [Environment]::SetEnvironmentVariable($k, $anterior[$k]) } }
        Write-Host "   Bot iniciado (PID $($p.Id)); cuentas autorizadas para abrir desde Telegram: $($ids.Count)."
        if ($ids.Count -eq 0) { Write-Host "   Sin $autorizados las fotos abren solo tras escanear el QR de la estación." }
    }
}
if (-not $SinPasarela) {
    Paso 'Pasarela ciudadana (GitHub Pages → túnel HTTPS → panel)'
    $pasarelaExe = Join-Path $raiz 'src\EcoAccess.P3.PasarelaCiudadana\bin\Release\net48\EcoAccess.P3.PasarelaCiudadana.exe'
    if (Get-Process -Name 'EcoAccess.P3.PasarelaCiudadana' -ErrorAction SilentlyContinue) {
        Write-Host '   Ya está en ejecución (una sola instancia).'
    } elseif (-not (Test-Path -LiteralPath $pasarelaExe)) {
        Write-Host '   AVISO: falta la pasarela compilada (dotnet build src\EcoAccess.P3.PasarelaCiudadana -c Release).'
    } else {
        $sello = Get-Date -Format 'yyyyMMdd-HHmmss'
        # Sin cloudflared queda solo local; la página pública sigue clasificando residuos en modo consulta.
        Start-Process -FilePath $pasarelaExe -WorkingDirectory (Split-Path $pasarelaExe) -WindowStyle Hidden `
            -RedirectStandardOutput (Join-Path $sesiones "pasarela-$sello.salida.log") -RedirectStandardError (Join-Path $sesiones "pasarela-$sello.error.log") | Out-Null
        Write-Host '   Pasarela iniciada: QR del día en http://localhost:8093/ (no habilita nada por sí sola).'
    }
}
if ($AbrirPanel) { Start-Process 'http://localhost:8090/' }
Write-Host 'Listo. Estación en servicio salvo pausa manual (PAUSAR ESTACIÓN en el panel). Web: http://localhost:8093/ → generar QR.'

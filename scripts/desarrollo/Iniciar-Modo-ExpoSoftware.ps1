<#
.SYNOPSIS
    Lanzador y diagnostico unificado Modo ExpoSoftware para ECOACCESS P3 (Fases 2, 4 y 10).
    Comprueba honestamente aplicacion, .NET 4.8, camara, Ollama, modelos visuales, Telegram,
    controlador ESP32 con autodescubrimiento seguro de puerto COM (solo desafio '?'), puentes HMAC
    y Oracle Cloud (solo lectura). NUNCA abre canecas fisicas al iniciar.
#>
[CmdletBinding()]
param(
    [switch]$SoloDiagnostico,
    [switch]$IniciarBotDemo,
    [switch]$HabilitarModoFisico,
    [switch]$DetenerTodo,
    [switch]$SinVentana,
    [string]$PuertoCom = 'COM3'
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$Raiz = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$BotExe = Join-Path $Raiz 'src\EcoAccess.P3.Bot\bin\Release\net48\EcoAccess.P3.Bot.exe'
$AppExe = Join-Path $Raiz 'src\EcoAccess.P3.Presentacion\bin\Release\net48\EcoAccess.P3.Presentacion.exe'

function Detener-BotsResiduales {
    param([switch]$SoloFueraDeOficial)
    $det = Join-Path $PSScriptRoot 'Detener-Demostracion.ps1'
    if (-not $SoloFueraDeOficial -and (Test-Path -LiteralPath $det)) {
        & powershell -ExecutionPolicy Bypass -File $det | Out-Null
    }
    $procs = @(Get-CimInstance Win32_Process -Filter "Name='EcoAccess.P3.Bot.exe'" -ErrorAction SilentlyContinue)
    foreach ($p in $procs) {
        $esOficial = $p.ExecutablePath -and $p.ExecutablePath.StartsWith($Raiz.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase)
        if ($SoloFueraDeOficial -and $esOficial) { continue }
        Stop-Process -Id $p.ProcessId -ErrorAction SilentlyContinue
        Write-Host "Detenido EcoAccess.P3.Bot.exe PID $($p.ProcessId) ($($p.ExecutablePath))" -ForegroundColor Yellow
    }
}

function Descubrir-PuertoEsp32 {
    param([string]$Preferido)
    $puertos = @([System.IO.Ports.SerialPort]::GetPortNames())
    if ($puertos.Count -eq 0) { return [pscustomobject]@{ Puerto = $null; Estado = 'DESCONECTADO (sin puertos COM)' } }
    $orden = @()
    if ($Preferido -and ($Preferido -in $puertos)) { $orden += $Preferido }
    $orden += @($puertos | Where-Object { $_ -ne $Preferido } | Sort-Object)
    foreach ($p in $orden) {
        $sp = New-Object System.IO.Ports.SerialPort $p, 115200
        $sp.DtrEnable = $false; $sp.RtsEnable = $false; $sp.NewLine = "`n"; $sp.ReadTimeout = 1000
        try {
            $sp.Open()
            for ($i = 0; $i -lt 3; $i++) {
                Start-Sleep -Milliseconds 400
                $sp.DiscardInBuffer(); $sp.WriteLine('?')
                try { $lin = $sp.ReadLine().Trim() } catch [TimeoutException] { $lin = '' }
                if ($lin -match '^\{"ecoaccessDesafio":"[0-9a-f]{32}"\}$') {
                    return [pscustomobject]@{ Puerto = $p; Estado = "CONECTADO ($p - firmware estacion_tres_canecas OK)" }
                }
            }
        } catch {
            # Puerto ocupado o sin respuesta; continuar con el siguiente candidato
        } finally {
            if ($sp.IsOpen) { $sp.Close() }
            $sp.Dispose()
        }
    }
    return [pscustomobject]@{ Puerto = $null; Estado = "SIN RESPUESTA ESTACION (puertos detectados: $($orden -join ', '))" }
}

if ($DetenerTodo) {
    Write-Host 'Deteniendo procesos de demostracion / estacion...' -ForegroundColor Yellow
    Detener-BotsResiduales
    return
}

Write-Host '================================================================' -ForegroundColor Cyan
Write-Host '  ECOACCESS P3 - DIAGNOSTICO REAL MODO EXPOSOFTWARE' -ForegroundColor Cyan
Write-Host '================================================================' -ForegroundColor Cyan

# 0. .NET Framework 4.8
$netRel = (Get-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full' -Name Release -ErrorAction SilentlyContinue).Release
Write-Host ("Entorno .NET 4.8   : " + $(if ($netRel -ge 528040) { "OK (Release $netRel)" } else { 'REVISAR (.NET Framework 4.8 requerido)' }))

# 1. Aplicacion principal
$appOk = (Test-Path -LiteralPath $AppExe) -and (Test-Path -LiteralPath $BotExe)
Write-Host ("Aplicacion Windows : " + $(if ($appOk) { 'LISTA (Release compilado)' } else { 'NO COMPILADA' }))

# 2. Camara
$cam = @(Get-PnpDevice -Class Camera, Image -Status OK -ErrorAction SilentlyContinue)
Write-Host ("Camara             : " + $(if ($cam.Count -gt 0) { "CONECTADA ($($cam[0].FriendlyName))" } else { 'DESCONECTADA' }))

# 3. Ollama y modelos visuales
try {
    $verOllama = (Invoke-RestMethod -Uri 'http://127.0.0.1:11434/api/version' -TimeoutSec 4).version
    $tags = (Invoke-RestMethod -Uri 'http://127.0.0.1:11434/api/tags' -TimeoutSec 4).models | ForEach-Object { $_.name }
    $vis = @($tags | Where-Object { $_ -match 'lfm3-vl|qwen3-vl' })
    Write-Host ("Ollama             : CONECTADO (v$verOllama)")
    Write-Host ("Modelo de vision   : " + $(if ($vis.Count -gt 0) { "LISTO ($($vis -join ', '))" } else { 'NO DISPONIBLE' }))
} catch {
    Write-Host 'Ollama             : DESCONECTADO'
    Write-Host 'Modelo de vision   : NO DISPONIBLE'
}

# 4. Telegram (sin imprimir jamas el token)
$secFile = Join-Path $env:APPDATA 'Microsoft\UserSecrets\ecoaccess-p3-framework\secrets.json'
$tgEstado = 'DESCONECTADO (sin token configurado)'
if (Test-Path -LiteralPath $secFile) {
    $sec = Get-Content -LiteralPath $secFile -Raw -Encoding UTF8 | ConvertFrom-Json
    $tok = $sec.'Telegram:TokenBot'
    if ($tok) {
        try {
            $me = Invoke-RestMethod -Uri "https://api.telegram.org/bot$tok/getMe" -TimeoutSec 5
            if ($me.ok) { $tgEstado = "CONECTADO (@$($me.result.username))" }
        } catch {
            $tgEstado = 'SIN CONEXION A TELEGRAM API (aplicacion local sigue operativa)'
        }
    }
}
Write-Host "Telegram           : $tgEstado"

# 5. Controlador fisico ESP32 con autodescubrimiento seguro (solo lectura '?')
$esp = Descubrir-PuertoEsp32 -Preferido $PuertoCom
Write-Host "Controlador        : $($esp.Estado)"

$privBridge = Join-Path ([Environment]::GetFolderPath('CommonApplicationData')) 'EcoAccessP3Framework\Bridge'
foreach ($par in @(@('Caneca verde  (1400)', 'bridge-verde.json'), @('Caneca blanca (1093)', 'bridge-blanca.json'), @('Caneca negra  (1401)', 'bridge-negra.json'))) {
    $ex = Test-Path -LiteralPath (Join-Path $privBridge $par[1])
    Write-Host ("$($par[0]) : " + $(if ($ex -and $esp.Puerto) { "DISPONIBLE (config HMAC lista en $($esp.Puerto))" } elseif ($ex) { 'CONFIGURADA (esperando ESP32)' } else { 'NO CONFIGURADA (modo simulacion disponible)' }))
}

# 6. Oracle Cloud (solo lectura, tolerante a desconexion temporal)
$oraTxt = 'SIN CONEXION (la aplicacion local sigue operativa)'
if (Test-Path -LiteralPath $BotExe) {
    $prevEap = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $outOra = (& $BotExe --comprobar-oracle 2>&1 | Out-String).Trim()
        if ($LASTEXITCODE -eq 0) { $oraTxt = 'CONECTADO (' + ($outOra -split "`r?`n")[0] + ')' }
    } catch {
        $oraTxt = 'SIN CONEXION (la aplicacion local sigue operativa)'
    } finally {
        $ErrorActionPreference = $prevEap
    }
}
Write-Host "Oracle             : $oraTxt"

$procsBot = @(Get-CimInstance Win32_Process -Filter "Name='EcoAccess.P3.Bot.exe'" -ErrorAction SilentlyContinue)
$procsLegado = @($procsBot | Where-Object { $_.ExecutablePath -and -not $_.ExecutablePath.StartsWith($Raiz.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase) })
if ($procsLegado.Count -gt 0) {
    Write-Host ("Procesos residuales: ALERTA ($($procsLegado.Count) instancia(s) fuera de la carpeta oficial; usa -DetenerTodo)") -ForegroundColor Yellow
} else {
    Write-Host ("Procesos residuales: LIMPIO ($($procsBot.Count) proceso(s) activos en carpeta oficial)")
}
Write-Host ("Modo fisico        : " + $(if ($HabilitarModoFisico) { 'HABILITADO (SUPERVISION PRESENCIAL REQUERIDA)' } else { 'DESHABILITADO (MODO DEMO SEGURO)' }))
Write-Host '================================================================' -ForegroundColor Cyan

if ($SoloDiagnostico) { return }

if ($procsLegado.Count -gt 0) {
    Detener-BotsResiduales -SoloFueraDeOficial
}

if ($HabilitarModoFisico) {
    Write-Host 'Iniciando estacion fisica supervisada (sin abrir canecas automaticamente)...' -ForegroundColor Yellow
    & powershell -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'Iniciar-Estacion-Fisica.ps1') -SinCompilar
} elseif ($IniciarBotDemo) {
    $yaActivo = @(Get-CimInstance Win32_Process -Filter "Name='EcoAccess.P3.Bot.exe'" -ErrorAction SilentlyContinue |
        Where-Object { $_.CommandLine -match '--polling' })
    if ($yaActivo.Count -eq 0 -and (Test-Path -LiteralPath $BotExe)) {
        $dirLog = Join-Path ([Environment]::GetFolderPath('CommonApplicationData')) 'EcoAccessP3Framework\Sesiones'
        New-Item -ItemType Directory -Force -Path $dirLog | Out-Null
        $pBot = Start-Process -FilePath $BotExe -ArgumentList '--polling --sin-dispositivo' -WorkingDirectory $Raiz -WindowStyle Hidden -PassThru `
            -RedirectStandardOutput (Join-Path $dirLog 'bot-expo-demo.out.log') `
            -RedirectStandardError (Join-Path $dirLog 'bot-expo-demo.err.log')
        Write-Host "Bot oficial Telegram iniciado en modo seguro sin hardware (PID $($pBot.Id))." -ForegroundColor Green
    } else {
        Write-Host 'Bot oficial Telegram ya se encontraba activo (no se duplico la instancia).' -ForegroundColor DarkCyan
    }
}

if (-not $SinVentana -and (Test-Path -LiteralPath $AppExe)) {
    $appYa = @(Get-Process -Name 'EcoAccess.P3.Presentacion' -ErrorAction SilentlyContinue)
    if ($appYa.Count -eq 0) {
        Start-Process -FilePath $AppExe -WorkingDirectory $Raiz
        Write-Host 'Aplicacion Windows EcoAccess P3 iniciada.' -ForegroundColor Green
    } else {
        Write-Host 'Aplicacion Windows EcoAccess P3 ya se encontraba abierta.' -ForegroundColor DarkCyan
    }
}

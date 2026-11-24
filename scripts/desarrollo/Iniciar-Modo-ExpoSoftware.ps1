<#
.SYNOPSIS
    Lanzador y diagnóstico unificado Modo ExpoSoftware para ECOACCESS P3 (Fase 11).
    Comprueba honestamente cámara, Ollama, modelos visuales, Telegram, ESP32 en COM3 (solo desafío '?'),
    puentes de canecas y Oracle Cloud. NUNCA abre canecas al iniciar.
#>
[CmdletBinding()]
param(
    [switch]$SoloDiagnostico,
    [switch]$HabilitarModoFisico,
    [switch]$DetenerTodo,
    [string]$PuertoCom = 'COM3'
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$Raiz = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$BotExe = Join-Path $Raiz 'src\EcoAccess.P3.Bot\bin\Release\net48\EcoAccess.P3.Bot.exe'
$AppExe = Join-Path $Raiz 'src\EcoAccess.P3.Presentacion\bin\Release\net48\EcoAccess.P3.Presentacion.exe'

if ($DetenerTodo) {
    Write-Host 'Deteniendo procesos de demostración / estación...' -ForegroundColor Yellow
    $det = Join-Path $PSScriptRoot 'Detener-Demostracion.ps1'
    if (Test-Path -LiteralPath $det) { & powershell -ExecutionPolicy Bypass -File $det }
    $restantes = @(Get-CimInstance Win32_Process -Filter "Name='EcoAccess.P3.Bot.exe'" -ErrorAction SilentlyContinue)
    foreach ($p in $restantes) {
        Stop-Process -Id $p.ProcessId -ErrorAction SilentlyContinue
        Write-Host "Detenido EcoAccess.P3.Bot.exe residual PID $($p.ProcessId) ($($p.ExecutablePath))" -ForegroundColor Yellow
    }
    return
}

Write-Host '================================================================' -ForegroundColor Cyan
Write-Host '  ECOACCESS P3 - DIAGNOSTICO REAL MODO EXPOSOFTWARE' -ForegroundColor Cyan
Write-Host '================================================================' -ForegroundColor Cyan

# 1. Aplicación principal
$appOk = (Test-Path -LiteralPath $AppExe) -and (Test-Path -LiteralPath $BotExe)
Write-Host ("Aplicación Windows : " + $(if ($appOk) { 'LISTA (Release compilado)' } else { 'NO COMPILADA' }))

# 2. Cámara
$cam = @(Get-PnpDevice -Class Camera, Image -Status OK -ErrorAction SilentlyContinue)
Write-Host ("Cámara             : " + $(if ($cam.Count -gt 0) { "CONECTADA ($($cam[0].FriendlyName))" } else { 'DESCONECTADA' }))

# 3. Ollama y modelos visuales
$ollamaOk = $false; $visionOk = $false; $verOllama = ''
try {
    $verOllama = (Invoke-RestMethod -Uri 'http://127.0.0.1:11434/api/version' -TimeoutSec 4).version
    $ollamaOk = $true
    $tags = (Invoke-RestMethod -Uri 'http://127.0.0.1:11434/api/tags' -TimeoutSec 4).models | ForEach-Object { $_.name }
    $vis = @($tags | Where-Object { $_ -match 'lfm3-vl|qwen3-vl' })
    $visionOk = ($vis.Count -gt 0)
    Write-Host ("Ollama             : CONECTADO (v$verOllama)")
    Write-Host ("Modelo de visión   : " + $(if ($visionOk) { "LISTO ($($vis -join ', '))" } else { 'NO DISPONIBLE' }))
} catch {
    Write-Host 'Ollama             : DESCONECTADO'
    Write-Host 'Modelo de visión   : NO DISPONIBLE'
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
            $tgEstado = 'SIN CONEXIÓN A TELEGRAM API (aplicación local sigue operativa)'
        }
    }
}
Write-Host "Telegram           : $tgEstado"

# 5. Controlador físico ESP32 en COM (solo lectura '?') y puentes de las 3 canecas
$comPresente = ($PuertoCom -in [System.IO.Ports.SerialPort]::GetPortNames())
$ctrlEstado = 'DESCONECTADO'
if ($comPresente) {
    $sp = New-Object System.IO.Ports.SerialPort $PuertoCom, 115200
    $sp.DtrEnable = $false; $sp.RtsEnable = $false; $sp.NewLine = "`n"; $sp.ReadTimeout = 1200
    try {
        $sp.Open()
        $lin = ''
        for ($i = 0; $i -lt 3; $i++) {
            Start-Sleep -Milliseconds 450
            $sp.DiscardInBuffer(); $sp.WriteLine('?')
            try { $lin = $sp.ReadLine().Trim() } catch [TimeoutException] { $lin = '' }
            if ($lin -match '^\{"ecoaccessDesafio":"[0-9a-f]{32}"\}$') { break }
        }
        if ($lin -match '^\{"ecoaccessDesafio":"[0-9a-f]{32}"\}$') { $ctrlEstado = "CONECTADO ($PuertoCom - firmware estacion_tres_canecas OK)" }
        else { $ctrlEstado = "CONECTADO ($PuertoCom - respuesta no estandar)" }
    } catch {
        $ctrlEstado = "OCUPADO O SIN RESPUESTA ($PuertoCom - $($_.Exception.Message))"
    } finally {
        if ($sp.IsOpen) { $sp.Close() }; $sp.Dispose()
    }
}
Write-Host "Controlador        : $ctrlEstado"

$privBridge = Join-Path ([Environment]::GetFolderPath('CommonApplicationData')) 'EcoAccessP3Framework\Bridge'
foreach ($par in @(@('Caneca verde  (1400)', 'bridge-verde.json'), @('Caneca blanca (1093)', 'bridge-blanca.json'), @('Caneca negra  (1401)', 'bridge-negra.json'))) {
    $ex = Test-Path -LiteralPath (Join-Path $privBridge $par[1])
    Write-Host ("$($par[0]) : " + $(if ($ex -and $comPresente) { 'DISPONIBLE (config HMAC lista)' } elseif ($ex) { 'CONFIGURADA (esperando COM)' } else { 'FALLO (falta configuración bridge)' }))
}

# 6. Oracle Cloud (solo lectura)
$oraTxt = 'SIN CONEXIÓN'
if (Test-Path -LiteralPath $BotExe) {
    $outOra = (& $BotExe --comprobar-oracle 2>&1 | Out-String).Trim()
    if ($LASTEXITCODE -eq 0) { $oraTxt = 'CONECTADO (' + ($outOra -split "`r?`n")[0] + ')' }
}
Write-Host "Oracle             : $oraTxt"
$procsBot = @(Get-CimInstance Win32_Process -Filter "Name='EcoAccess.P3.Bot.exe'" -ErrorAction SilentlyContinue)
$procsLegado = @($procsBot | Where-Object { $_.ExecutablePath -and -not $_.ExecutablePath.StartsWith($Raiz.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase) })
if ($procsLegado.Count -gt 0) {
    Write-Host ("Procesos residuales: ALERTA ($($procsLegado.Count) instancia(s) de EcoAccess.P3.Bot.exe fuera de la carpeta oficial; usa -DetenerTodo antes de activar modo fisico)") -ForegroundColor Yellow
} else {
    Write-Host ("Procesos residuales: LIMPIO ($($procsBot.Count) proceso(s) activos en carpeta oficial)")
}
Write-Host ("Modo físico        : " + $(if ($HabilitarModoFisico) { 'HABILITADO (SUPERVISIÓN PRESENCIAL REQUERIDA)' } else { 'DESHABILITADO (MODO DEMO SEGURO)' }))
Write-Host '================================================================' -ForegroundColor Cyan

if ($SoloDiagnostico) { return }

if ($HabilitarModoFisico) {
    Write-Host 'Iniciando estación física supervisada (sin abrir canecas automáticamente)...' -ForegroundColor Yellow
    & powershell -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'Iniciar-Estacion-Fisica.ps1') -SinCompilar
}
if (Test-Path -LiteralPath $AppExe) {
    Start-Process -FilePath $AppExe -WorkingDirectory $Raiz
    Write-Host 'Aplicación Windows EcoAccess P3 iniciada.' -ForegroundColor Green
}
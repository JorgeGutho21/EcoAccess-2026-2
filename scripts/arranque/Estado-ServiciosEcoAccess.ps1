<#
.SYNOPSIS
    Estado de la estación EcoAccess P3 (SOLO LECTURA): tarea programada, procesos, duplicados y servicios locales.
.DESCRIPTION
    No inicia, detiene ni reinicia nada. Solo hace peticiones GET locales:
      · http://localhost:8090/api/estacion/estado (panel de canecas)
      · http://127.0.0.1:8093/estado              (pasarela ciudadana, puerto de operador)
      · http://127.0.0.1:11434/api/version         (Ollama)
    No muestra secretos ni identificadores de Telegram. Código de salida 0 si no hay duplicados; 2 si los hay.
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\arranque\Estado-ServiciosEcoAccess.ps1
#>
[CmdletBinding()]
param()
$ErrorActionPreference = 'Continue'
$nombre = 'EcoAccess P3 - arranque de la estación'
$base = Join-Path ([Environment]::GetFolderPath('CommonApplicationData')) 'EcoAccessP3Framework'
function Get-Json([string] $url) { try { Invoke-RestMethod -Uri $url -Method Get -TimeoutSec 3 } catch { $null } }

Write-Host '== Tarea programada'
$t = Get-ScheduledTask -TaskName $nombre -ErrorAction SilentlyContinue
if ($t) {
    $i = $t | Get-ScheduledTaskInfo
    Write-Host ("   {0}: {1} · última ejecución {2} (resultado 0x{3:X}) · próxima {4}" -f $t.TaskName, $t.State, $i.LastRunTime, $i.LastTaskResult, $i.NextRunTime)
} else { Write-Host '   No registrada (instálala con Instalar-ArranqueEcoAccess.ps1).' }
if (Test-Path -LiteralPath (Join-Path $base 'Arranque\pausa')) { Write-Host '   EN PAUSA: el arranque automático no iniciará nada hasta quitar Arranque\pausa.' }

Write-Host '== Procesos'
$p = @(Get-CimInstance Win32_Process -ErrorAction SilentlyContinue)
$filas = [ordered]@{
    'Ollama (servidor)'           = @($p | Where-Object { $_.Name -eq 'ollama.exe' })
    'Ollama (aplicación)'         = @($p | Where-Object { $_.Name -eq 'ollama app.exe' })
    'Panel de canecas'            = @($p | Where-Object { $_.Name -eq 'EcoAccess.P3.PanelCanecas.exe' })
    'Bot de Telegram (polling)'   = @($p | Where-Object { $_.Name -eq 'EcoAccess.P3.Bot.exe' -and $_.CommandLine -match '--polling' })
    'Bot (otros modos)'           = @($p | Where-Object { $_.Name -eq 'EcoAccess.P3.Bot.exe' -and $_.CommandLine -notmatch '--polling' })
    'Pasarela ciudadana'          = @($p | Where-Object { $_.Name -eq 'EcoAccess.P3.PasarelaCiudadana.exe' })
    'Túnel HTTPS (cloudflared)'   = @($p | Where-Object { $_.Name -eq 'cloudflared.exe' })
    'Aplicación de escritorio'    = @($p | Where-Object { $_.Name -eq 'EcoAccess.P3.Presentacion.exe' })
}
$duplicados = 0
foreach ($k in $filas.Keys) {
    $lista = $filas[$k]
    $marca = if ($k -in 'Panel de canecas', 'Bot de Telegram (polling)', 'Pasarela ciudadana' -and $lista.Count -gt 1) { $duplicados++; '  ¡DUPLICADO!' } else { '' }
    Write-Host ("   {0,-28} {1}{2}{3}" -f $k, $lista.Count, $(if ($lista.Count) { ' · PID ' + (($lista | ForEach-Object ProcessId) -join ', ') } else { '' }), $marca)
}

Write-Host '== Servicios (GET locales)'
$e = Get-Json 'http://localhost:8090/api/estacion/estado'
if ($e) {
    Write-Host "   Panel:    $($e.estado) · $($e.texto) · puerto $($e.puerto) · $($e.identificador)"
    Write-Host ("   Sesión supervisada: {0} · ocupada: {1}" -f $(if ($e.sesionSupervisada) { "activa hasta $($e.sesionHasta)" } else { 'inactiva (motores bloqueados)' }), $e.ocupada)
    if ($e.bot) { Write-Host ("   Bot:      {0} · Oracle: {1} · pendientes {2} · pulso hace {3} s" -f $(if ($e.bot.activo) { 'activo' } else { 'sin pulso vigente' }), $(if ($null -eq $e.bot.oracle) { 'desconocido' } elseif ($e.bot.oracle) { 'conectado' } else { 'sin conexión' }), $e.bot.pendientes, $e.bot.hace) }
} else { Write-Host '   Panel:    no responde en http://localhost:8090' }
$w = Get-Json 'http://127.0.0.1:8093/estado'
Write-Host ("   Pasarela: {0}" -f $(if ($w) { 'en línea' + $(if ($w.publico) { " · túnel $($w.publico)" } else { ' · sin túnel' }) } else { 'no responde en 127.0.0.1:8093' }))
$o = Get-Json 'http://127.0.0.1:11434/api/version'
Write-Host ("   Ollama:   {0}" -f $(if ($o) { "versión $($o.version)" } else { 'no responde en 127.0.0.1:11434' }))

$autorizados = Join-Path $base 'Bridge\telegram-autorizados.txt'
$n = @(if (Test-Path -LiteralPath $autorizados) { Get-Content -LiteralPath $autorizados | Where-Object { $_.Trim() -match '^\d{4,19}$' } }).Count
Write-Host "   Cuentas de Telegram autorizadas para abrir (se aplican al iniciar el bot): $n"

$log = Join-Path $base 'Sesiones\arranque-ecoaccess.log'
if (Test-Path -LiteralPath $log) { Write-Host '== Últimas líneas del registro de arranque'; Get-Content -LiteralPath $log -Tail 8 | ForEach-Object { Write-Host "   $_" } }
exit $(if ($duplicados) { 2 } else { 0 })

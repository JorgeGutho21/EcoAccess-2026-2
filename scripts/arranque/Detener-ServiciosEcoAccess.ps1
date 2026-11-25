<#
.SYNOPSIS
    Detiene los servicios de la estación EcoAccess P3 que inicia el arranque automático: bot en polling, pasarela
    ciudadana con su túnel (cloudflared) y panel de canecas. Ollama solo con -IncluirOllama.
.DESCRIPTION
    · Pide confirmación (admite -WhatIf para ver qué haría sin detener nada).
    · No envía órdenes al ESP32. Si el panel informa una tapa en ciclo (estación ocupada) se niega a cerrarlo, salvo con
      -Forzar: espera a que termine el ciclo o pulsa DETENER TODO en el panel antes. El firmware cierra cada tapa por su
      propio temporizador aunque el panel se cierre, pero así nadie pierde de vista una tapa abierta.
    · -Pausar crea %ProgramData%\EcoAccessP3Framework\Arranque\pausa: la tarea programada no volverá a iniciar nada
      (ni al iniciar sesión) hasta ejecutar este script con -QuitarPausa.
    · Solo detiene procesos de EcoAccess (por nombre de ejecutable); cloudflared solo si su túnel apunta a la pasarela.
    Registro: %ProgramData%\EcoAccessP3Framework\Sesiones\arranque-ecoaccess.log
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\arranque\Detener-ServiciosEcoAccess.ps1 -WhatIf
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\arranque\Detener-ServiciosEcoAccess.ps1 -Pausar
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\arranque\Detener-ServiciosEcoAccess.ps1 -QuitarPausa
#>
[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
param([switch] $IncluirOllama, [switch] $Pausar, [switch] $QuitarPausa, [switch] $Forzar)
$ErrorActionPreference = 'Continue'
$base = Join-Path ([Environment]::GetFolderPath('CommonApplicationData')) 'EcoAccessP3Framework'
$pausa = Join-Path $base 'Arranque\pausa'
$log = Join-Path $base 'Sesiones\arranque-ecoaccess.log'
New-Item -ItemType Directory -Force (Split-Path $log), (Split-Path $pausa) | Out-Null
function Anotar([string] $t) { Write-Host $t; Add-Content -LiteralPath $log -Value "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] Detener: $t" -Encoding UTF8 }

if ($QuitarPausa) {
    if ((Test-Path -LiteralPath $pausa) -and $PSCmdlet.ShouldProcess($pausa, 'Quitar la pausa del arranque automático')) { Remove-Item -LiteralPath $pausa -Force; Anotar 'Pausa quitada: el próximo inicio de sesión vuelve a arrancar los servicios.' }
    else { Write-Host 'No había pausa.' }
    exit 0
}

$estado = try { Invoke-RestMethod -Uri 'http://localhost:8090/api/estacion/estado' -Method Get -TimeoutSec 3 } catch { $null }
if ($estado -and $estado.ocupada -and -not $Forzar) {
    Write-Warning 'El panel informa una tapa en ciclo o una secuencia en curso. Espera a que termine o pulsa DETENER TODO en el panel; luego repite (o usa -Forzar).'
    exit 1
}

if ($Pausar -and $PSCmdlet.ShouldProcess($pausa, 'Pausar el arranque automático')) {
    Set-Content -LiteralPath $pausa -Value "Pausado por $env:USERNAME el $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -Encoding UTF8
    Anotar 'Arranque automático en pausa.'
}

$p = @(Get-CimInstance Win32_Process -ErrorAction SilentlyContinue)
$objetivos = @(
    @{ Que = 'bot de Telegram (polling)'; Lista = @($p | Where-Object { $_.Name -eq 'EcoAccess.P3.Bot.exe' -and $_.CommandLine -match '--polling' }) }
    @{ Que = 'túnel HTTPS de la pasarela'; Lista = @($p | Where-Object { $_.Name -eq 'cloudflared.exe' -and $_.CommandLine -match '--url http://127\.0\.0\.1:8092' }) }
    @{ Que = 'pasarela ciudadana'; Lista = @($p | Where-Object { $_.Name -eq 'EcoAccess.P3.PasarelaCiudadana.exe' }) }
    @{ Que = 'panel de canecas'; Lista = @($p | Where-Object { $_.Name -eq 'EcoAccess.P3.PanelCanecas.exe' }) }
)
if ($IncluirOllama) { $objetivos += @{ Que = 'Ollama'; Lista = @($p | Where-Object { $_.Name -in 'ollama app.exe', 'ollama.exe' }) } }

foreach ($o in $objetivos) {
    if ($o.Lista.Count -eq 0) { Write-Host "   $($o.Que): no estaba en ejecución."; continue }
    foreach ($proc in $o.Lista) {
        if ($PSCmdlet.ShouldProcess("$($o.Que) (PID $($proc.ProcessId))", 'Detener')) {
            Stop-Process -Id $proc.ProcessId -Force -ErrorAction SilentlyContinue
            Anotar "Detenido: $($o.Que) (PID $($proc.ProcessId))."
        }
    }
}
if (-not $Pausar) { Write-Host 'Nota: la tarea programada volverá a iniciarlos en el próximo inicio de sesión. Para evitarlo usa -Pausar o Desinstalar-ArranqueEcoAccess.ps1.' }

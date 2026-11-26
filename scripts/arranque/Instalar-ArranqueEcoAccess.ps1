<#
.SYNOPSIS
    Registra la tarea programada «EcoAccess P3 - arranque de la estación» del usuario actual: al iniciar sesión en Windows
    (con unos 30 s de retraso) deja en marcha Ollama, el panel de canecas, UN bot de Telegram en polling (con el
    sincronizador de Oracle), la pasarela ciudadana y su túnel HTTPS, sin Claude, Visual Studio ni VS Code.
.DESCRIPTION
    · Sin privilegios elevados: tarea del usuario actual, sesión interactiva, nivel «Limitado». No pide contraseña.
    · No habilita la sesión supervisada (calibración y pruebas técnicas siguen exigiendo ACTIVAR CONTROL). La estación
      arranca en servicio salvo una pausa manual guardada en el panel.
    · No duplica procesos: el script de inicio reutiliza scripts\desarrollo\Iniciar-EstacionExpo.ps1, que comprueba el
      panel (API y mutex), el bot en --polling, la pasarela (mutex) y Ollama antes de iniciar cada uno.
    · Reinicio ante fallo acotado: si el panel no queda en línea, la tarea se repite como máximo 3 veces, cada 2 minutos.
    · La configuración privada (User Secrets) la leen los programas al arrancar; este instalador no la abre ni la muestra.
    · Copia el script de inicio a %LOCALAPPDATA%\EcoAccessP3\Arranque (carpeta solo del usuario) para que la tarea no
      dependa de una copia de trabajo temporal; los ejecutables se toman de -Raiz.
    Registro de las ejecuciones: %ProgramData%\EcoAccessP3Framework\Sesiones\arranque-ecoaccess.log
.PARAMETER Raiz
    Carpeta del repositorio con los programas compilados en Release (por defecto, la que contiene este script). Debe ser
    la carpeta oficial de la estación, no una copia de trabajo de .claude\worktrees.
.PARAMETER RetrasoSegundos
    Espera después de iniciar sesión antes de arrancar (por defecto 30 s), para que la red y OneDrive estén listos.
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\arranque\Instalar-ArranqueEcoAccess.ps1
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\arranque\Instalar-ArranqueEcoAccess.ps1 -Raiz "C:\ruta\EcoAccess-P3" -RetrasoSegundos 45
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [string] $Raiz = (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)),
    [ValidateRange(0, 600)] [int] $RetrasoSegundos = 30
)
$ErrorActionPreference = 'Stop'
$nombre = 'EcoAccess P3 - arranque de la estación'
$Raiz = (Resolve-Path -LiteralPath $Raiz).Path.TrimEnd('\')

if ($Raiz -match '\\\.claude\\worktrees\\') {
    throw "La raíz $Raiz es una copia de trabajo temporal. Indica la carpeta oficial con -Raiz."
}
$lanzador = Join-Path $Raiz 'scripts\desarrollo\Iniciar-EstacionExpo.ps1'
if (-not (Test-Path -LiteralPath $lanzador)) { throw "No existe $lanzador en la raíz indicada." }
foreach ($exe in 'src\EcoAccess.P3.PanelCanecas\bin\Release\net48\EcoAccess.P3.PanelCanecas.exe',
                 'src\EcoAccess.P3.Bot\bin\Release\net48\EcoAccess.P3.Bot.exe',
                 'src\EcoAccess.P3.PasarelaCiudadana\bin\Release\net48\EcoAccess.P3.PasarelaCiudadana.exe') {
    if (-not (Test-Path -LiteralPath (Join-Path $Raiz $exe))) { Write-Warning "Falta ${exe}: compila con «dotnet build EcoAccess.P3.sln -c Release» antes del próximo inicio de sesión." }
}

# Copia del script de inicio en una carpeta del usuario (no en ProgramData, que otros usuarios del equipo pueden escribir).
$destino = Join-Path $env:LOCALAPPDATA 'EcoAccessP3\Arranque'
$script = Join-Path $destino 'Iniciar-ServiciosEcoAccess.ps1'
if ($PSCmdlet.ShouldProcess($destino, 'Copiar el script de inicio')) {
    New-Item -ItemType Directory -Force $destino | Out-Null
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Iniciar-ServiciosEcoAccess.ps1') -Destination $script -Force
}

# conhost --headless: la consola no llega a mostrarse (con -WindowStyle Hidden aparecía un instante al iniciar sesión).
$powershell = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
$conhost = Join-Path $env:SystemRoot 'System32\conhost.exe'
$argumentos = "-NoProfile -NonInteractive -ExecutionPolicy Bypass -WindowStyle Hidden -File `"$script`" -Raiz `"$Raiz`""
$accion = if (Test-Path -LiteralPath $conhost) { New-ScheduledTaskAction -Execute $conhost -Argument "--headless `"$powershell`" $argumentos" -WorkingDirectory $destino }
          else { New-ScheduledTaskAction -Execute $powershell -Argument $argumentos -WorkingDirectory $destino }

$usuario = "$env:USERDOMAIN\$env:USERNAME"
$disparador = New-ScheduledTaskTrigger -AtLogOn -User $usuario
$disparador.Delay = 'PT{0}S' -f $RetrasoSegundos
# Vigilancia: cada 5 minutos la misma tarea comprueba que bot, panel y pasarela sigan vivos y solo reinicia lo caído.
$vigilancia = New-ScheduledTaskTrigger -Once -At (Get-Date).AddMinutes(5) -RepetitionInterval (New-TimeSpan -Minutes 5) -RepetitionDuration (New-TimeSpan -Days 3650)
$ajustes = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable `
    -MultipleInstances IgnoreNew -ExecutionTimeLimit (New-TimeSpan -Minutes 10) `
    -RestartCount 3 -RestartInterval (New-TimeSpan -Minutes 2)
$principal = New-ScheduledTaskPrincipal -UserId $usuario -LogonType Interactive -RunLevel Limited

if ($PSCmdlet.ShouldProcess($nombre, 'Registrar la tarea programada')) {
    Register-ScheduledTask -TaskName $nombre -Action $accion -Trigger @($disparador, $vigilancia) -Settings $ajustes -Principal $principal -Force `
        -Description "Inicia los servicios de EcoAccess P3 al iniciar sesión (Ollama, panel, un bot en polling, pasarela y túnel) sin duplicarlos ni habilitar movimiento. Raíz: $Raiz. Creada por scripts\arranque\Instalar-ArranqueEcoAccess.ps1." | Out-Null
    $t = Get-ScheduledTask -TaskName $nombre
    Write-Host "Tarea registrada: $($t.TaskName) · estado $($t.State) · al iniciar sesión de $usuario con $RetrasoSegundos s de retraso y vigilancia cada 5 minutos."
    Write-Host "Script de inicio: $script"
    Write-Host "Ejecutables de:   $Raiz"
    Write-Host 'Probar ahora (no duplica lo que ya corre): Start-ScheduledTask -TaskName ''EcoAccess P3 - arranque de la estación'''
    Write-Host 'Estado:  scripts\arranque\Estado-ServiciosEcoAccess.ps1   ·   Detener: scripts\arranque\Detener-ServiciosEcoAccess.ps1'
}
if (Get-ScheduledTask -TaskName 'EcoAccess P3 - vigilante del bot' -ErrorAction SilentlyContinue | Where-Object State -ne 'Disabled') {
    Write-Warning 'También está activa la tarea «EcoAccess P3 - vigilante del bot» (modo anterior). No duplica el bot, pero conviene dejar solo esta.'
}

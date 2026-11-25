<#
.SYNOPSIS
    Lo que ejecuta la tarea programada «EcoAccess P3 - arranque de la estación» al iniciar sesión: deja en marcha los
    servicios de la estación sin duplicar ninguno y sin habilitar movimiento físico.
.DESCRIPTION
    No abre ventanas, no abre el puerto COM por sí mismo, no envía órdenes al ESP32 ni peticiones POST al panel y no
    habilita la sesión supervisada: las tapas solo se mueven después de pulsar ACTIVAR CONTROL en el panel.

    Orden:
      1. Si existe el archivo de pausa (%ProgramData%\EcoAccessP3Framework\Arranque\pausa) no inicia nada.
      2. Ollama: si su API no responde y no hay un proceso de Ollama, abre «ollama app.exe --hide» y espera su API.
      3. Reutiliza el lanzador de la estación (scripts\desarrollo\Iniciar-EstacionExpo.ps1 de -Raiz), que ya deduplica:
           · panel de canecas (http://localhost:8090, único dueño del ESP32; mutex propio),
           · UN solo bot en --polling (Modo Expo; dentro corre el sincronizador con Oracle y el pulso del panel),
           · pasarela ciudadana (mutex propio), que a su vez abre el túnel HTTPS (cloudflared).
      4. Comprueba lo que quedó en marcha y lo anota (sin secretos ni identificadores de Telegram).
    La configuración privada (User Secrets del usuario) la leen los propios programas; este script nunca la abre.

    Registro: %ProgramData%\EcoAccessP3Framework\Sesiones\arranque-ecoaccess.log
    Código de salida: 0 si el panel responde; 1 si no (la tarea programada lo reintenta como máximo 3 veces).
.PARAMETER Raiz
    Carpeta del repositorio cuyos ejecutables (bin\Release\net48) se usan. La fija Instalar-ArranqueEcoAccess.ps1.
#>
[CmdletBinding()]
param([Parameter(Mandatory)] [string] $Raiz)
$ErrorActionPreference = 'Continue'
$base = Join-Path ([Environment]::GetFolderPath('CommonApplicationData')) 'EcoAccessP3Framework'
$log = Join-Path $base 'Sesiones\arranque-ecoaccess.log'
New-Item -ItemType Directory -Force (Split-Path $log) | Out-Null
function Anotar([string] $t) { Add-Content -LiteralPath $log -Value "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] $t" -Encoding UTF8 }

function Get-ConteoServicios {
    # Solo nombres de proceso y líneas de comando propias de EcoAccess (nunca argumentos con secretos: el bot no los recibe por línea de comando).
    $p = @(Get-CimInstance Win32_Process -ErrorAction SilentlyContinue)
    [ordered]@{
        ollama      = @($p | Where-Object { $_.Name -eq 'ollama.exe' }).Count
        ollamaApp   = @($p | Where-Object { $_.Name -eq 'ollama app.exe' }).Count
        panel       = @($p | Where-Object { $_.Name -eq 'EcoAccess.P3.PanelCanecas.exe' }).Count
        botPolling  = @($p | Where-Object { $_.Name -eq 'EcoAccess.P3.Bot.exe' -and $_.CommandLine -match '--polling' }).Count
        pasarela    = @($p | Where-Object { $_.Name -eq 'EcoAccess.P3.PasarelaCiudadana.exe' }).Count
        cloudflared = @($p | Where-Object { $_.Name -eq 'cloudflared.exe' }).Count
    }
}
function Format-Conteo($c) { ($c.Keys | ForEach-Object { "$_=$($c[$_])" }) -join ' ' }
function Test-Url([string] $url) { try { $null = Invoke-RestMethod -Uri $url -Method Get -TimeoutSec 3; $true } catch { $false } }

# Una sola ejecución a la vez (inicio de sesión + reintento + prueba manual no se pisan).
$candado = New-Object System.Threading.Mutex($false, 'Local\EcoAccessP3ArranqueEstacion')
if (-not $candado.WaitOne(0)) { Anotar 'Otra ejecución del arranque está en curso: esta termina sin hacer nada.'; exit 0 }
try {
    Anotar "Inicio del arranque automático (usuario $env:USERNAME, raíz $Raiz)."
    if (Test-Path -LiteralPath (Join-Path $base 'Arranque\pausa')) {
        Anotar 'Arranque en pausa (Arranque\pausa existe): no se inicia nada. Bórralo o ejecuta Detener-ServiciosEcoAccess.ps1 -QuitarPausa.'
        exit 0
    }
    $lanzador = Join-Path $Raiz 'scripts\desarrollo\Iniciar-EstacionExpo.ps1'
    if (-not (Test-Path -LiteralPath $lanzador)) { Anotar "ERROR: no existe $lanzador. Reinstala con Instalar-ArranqueEcoAccess.ps1 -Raiz <carpeta del repositorio>."; exit 1 }

    $antes = Get-ConteoServicios
    Anotar ('Antes:   ' + (Format-Conteo $antes))

    # Ollama: la aplicación de bandeja (como el acceso directo de la estación); si ya hay uno, no se abre otro.
    if (-not (Test-Url 'http://127.0.0.1:11434/api/version')) {
        $app = Join-Path $env:LOCALAPPDATA 'Programs\Ollama\ollama app.exe'
        if (($antes.ollama + $antes.ollamaApp) -eq 0 -and (Test-Path -LiteralPath $app)) {
            Start-Process -FilePath $app -ArgumentList '--hide' -WindowStyle Hidden
            Anotar 'Ollama iniciado (ollama app.exe --hide).'
        }
        for ($i = 0; $i -lt 30 -and -not (Test-Url 'http://127.0.0.1:11434/api/version'); $i++) { Start-Sleep -Seconds 1 }
    }

    # Lanzador de la estación: deduplica panel, bot en polling y pasarela, y nunca habilita la sesión supervisada.
    & $lanzador *>&1 | ForEach-Object { if ("$_".Trim()) { Anotar ('  ' + "$_".Trim()) } }

    Start-Sleep -Seconds 5
    $despues = Get-ConteoServicios
    Anotar ('Después: ' + (Format-Conteo $despues))
    foreach ($k in 'panel', 'botPolling', 'pasarela') { if ($despues[$k] -gt 1) { Anotar "AVISO: hay $($despues[$k]) procesos de $k; revisa con Estado-ServiciosEcoAccess.ps1." } }
    if ($despues.pasarela -ge 1 -and $despues.cloudflared -eq 0) { Anotar 'AVISO: la pasarela corre sin túnel (cloudflared). La web pública queda en modo consulta.' }

    $panel = Test-Url 'http://localhost:8090/api/estacion/estado'
    Anotar ($(if ($panel) { 'Fin: panel en línea. Ninguna tapa se mueve hasta ACTIVAR CONTROL en el panel.' } else { 'Fin con error: el panel no responde en http://localhost:8090.' }))
    exit $(if ($panel) { 0 } else { 1 })
} catch {
    Anotar "ERROR inesperado: $($_.Exception.Message)"
    exit 1
} finally {
    $candado.ReleaseMutex(); $candado.Dispose()
}

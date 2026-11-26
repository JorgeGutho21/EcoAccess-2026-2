<#
.SYNOPSIS
    Lo que ejecuta la tarea programada «EcoAccess P3 - arranque de la estación» al iniciar sesión: deja en marcha los
    servicios de la estación sin duplicar ninguno y sin habilitar movimiento físico.
.DESCRIPTION
    No abre ventanas, no abre el puerto COM por sí mismo, no envía órdenes al ESP32 ni peticiones POST al panel y no
    habilita la sesión supervisada. La Estación Oficial queda EN SERVICIO por defecto (salvo una pausa manual guardada):
    las aperturas ciudadanas autenticadas se ejecutan solo si el ESP32 está conectado y confirma la orden.

    Orden:
      1. Si existe el archivo de pausa (%ProgramData%\EcoAccessP3Framework\Arranque\pausa) no inicia nada.
      2. Ollama: si su API no responde y no hay un proceso de Ollama, abre «ollama app.exe --hide» (con escritorio) o
         «ollama serve» (antes de iniciar sesión, sesión 0) y espera su API.
      3. Reutiliza el lanzador de la estación (scripts\desarrollo\Iniciar-EstacionExpo.ps1 de -Raiz), que ya deduplica:
           · panel de canecas (http://localhost:8090, único dueño del ESP32; mutex propio),
           · UN solo bot en --polling (Modo Expo; dentro corre el sincronizador con Oracle y el pulso del panel),
           · pasarela ciudadana (mutex propio), que a su vez abre el túnel HTTPS (cloudflared).
      4. Comprueba lo que quedó en marcha y lo anota (sin secretos ni identificadores de Telegram).
    Arranque: con Instalar-ArranqueEcoAccess.ps1 -AntesDeIniciarSesion la tarea corre al encender Windows, sin esperar
    a que alguien inicie sesión (cuenta del usuario sin contraseña guardada, sesión 0 sin ventanas).
    Vigilancia: la misma tarea se repite cada 5 minutos. Si todo lo que este equipo debe ejecutar ya está en marcha y el
    panel responde, termina sin tocar nada ni escribir en el registro; si algo se cayó (bot, panel o pasarela), lo
    vuelve a iniciar sin duplicar lo demás. Nunca envía órdenes de apertura.
    Equipo de un compañero (anfitrion.txt apunta a otro equipo): no se esperan panel ni pasarela locales.
    Equipo sin bot (Arranque\bot-en-otro-equipo): no se espera el bot.
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

# Una sola ejecución a la vez (arranque de Windows, inicio de sesión, vigilancia y prueba manual no se pisan aunque
# corran en sesiones distintas de Windows: por eso el candado es Global\).
$candado = New-Object System.Threading.Mutex($false, 'Global\EcoAccessP3ArranqueEstacion')
if (-not $candado.WaitOne(0)) { Anotar 'Otra ejecución del arranque está en curso: esta termina sin hacer nada.'; exit 0 }
try {
    # Qué debe correr en ESTE equipo.
    $cfgAnf = Join-Path $env:LOCALAPPDATA 'EcoAccessP3\anfitrion.txt'
    $urlAnf = if (Test-Path -LiteralPath $cfgAnf) { ((Get-Content -LiteralPath $cfgAnf -ErrorAction SilentlyContinue | Where-Object { $_ -match '^\s*url\s*=' } | Select-Object -First 1) -replace '^\s*url\s*=\s*', '').Trim() } else { '' }
    $esRemoto = $urlAnf -and $urlAnf -notmatch '//(localhost|127\.)'
    $botAqui = -not (Test-Path -LiteralPath (Join-Path $base 'Arranque\bot-en-otro-equipo'))
    if (-not (Test-Path -LiteralPath (Join-Path $base 'Arranque\pausa'))) {
        $c = Get-ConteoServicios
        $completo = ($esRemoto -or ($c.panel -ge 1 -and $c.pasarela -ge 1 -and (Test-Url 'http://localhost:8090/api/estacion/estado'))) -and (-not $botAqui -or $c.botPolling -ge 1)
        if ($completo) { exit 0 }   # vigilancia silenciosa: todo en marcha
    }
    Anotar "Inicio del arranque automático (usuario $env:USERNAME, raíz $Raiz)."
    if (Test-Path -LiteralPath (Join-Path $base 'Arranque\pausa')) {
        Anotar 'Arranque en pausa (Arranque\pausa existe): no se inicia nada. Bórralo o ejecuta Detener-ServiciosEcoAccess.ps1 -QuitarPausa.'
        exit 0
    }
    $lanzador = Join-Path $Raiz 'scripts\desarrollo\Iniciar-EstacionExpo.ps1'
    if (-not (Test-Path -LiteralPath $lanzador)) { Anotar "ERROR: no existe $lanzador. Reinstala con Instalar-ArranqueEcoAccess.ps1 -Raiz <carpeta del repositorio>."; exit 1 }

    $antes = Get-ConteoServicios
    Anotar ('Antes:   ' + (Format-Conteo $antes))

    # Ollama: con escritorio, la aplicación de bandeja (como el acceso directo de la estación). Antes de iniciar sesión
    # (sesión 0, sin escritorio) solo el servidor «ollama serve». Si ya hay uno, no se abre otro.
    if (-not (Test-Url 'http://127.0.0.1:11434/api/version')) {
        $app = Join-Path $env:LOCALAPPDATA 'Programs\Ollama\ollama app.exe'
        $srv = Join-Path $env:LOCALAPPDATA 'Programs\Ollama\ollama.exe'
        if (($antes.ollama + $antes.ollamaApp) -eq 0) {
            if ((Get-Process -Id $PID).SessionId -eq 0 -and (Test-Path -LiteralPath $srv)) {
                Start-Process -FilePath $srv -ArgumentList 'serve' -WindowStyle Hidden
                Anotar 'Ollama iniciado antes del inicio de sesión (ollama serve).'
            } elseif (Test-Path -LiteralPath $app) {
                Start-Process -FilePath $app -ArgumentList '--hide' -WindowStyle Hidden
                Anotar 'Ollama iniciado (ollama app.exe --hide).'
            }
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

    # En el equipo de un compañero (anfitrion.txt apunta a otro computador) se comprueba el panel del anfitrión.
    $urlPanel = 'http://localhost:8090'; $cabeceras = @{}
    $cfg = Join-Path $env:LOCALAPPDATA 'EcoAccessP3\anfitrion.txt'
    if (Test-Path -LiteralPath $cfg) {
        $lin = Get-Content -LiteralPath $cfg -ErrorAction SilentlyContinue
        $u = ($lin | Where-Object { $_ -match '^\s*url\s*=' } | Select-Object -First 1) -replace '^\s*url\s*=\s*', ''
        $k = ($lin | Where-Object { $_ -match '^\s*clave\s*=' } | Select-Object -First 1) -replace '^\s*clave\s*=\s*', ''
        if ($u -and $u -notmatch '//(localhost|127\.)') { $urlPanel = $u.Trim().TrimEnd('/'); $cabeceras = @{ 'X-Clave' = $k.Trim() } }
    }
    $panel = try { $null = Invoke-RestMethod -Uri "$urlPanel/api/estacion/estado" -Headers $cabeceras -TimeoutSec 6; $true } catch { $false }
    if ($urlPanel -ne 'http://localhost:8090') { Anotar "Anfitrión de red: $urlPanel ($(if ($panel) { 'responde' } else { 'NO responde' }))." }
    Anotar ($(if ($panel) { 'Fin: panel en línea. Estación en servicio salvo pausa manual; sin ESP32 conectado ninguna apertura se ejecuta.' } else { "Fin con error: el panel no responde en $urlPanel." }))
    exit $(if ($panel) { 0 } else { 1 })
} catch {
    Anotar "ERROR inesperado: $($_.Exception.Message)"
    exit 1
} finally {
    $candado.ReleaseMutex(); $candado.Dispose()
}

<#
.SYNOPSIS
    Lanzador único del bot: comprueba dependencias y configuración, aplica migraciones pendientes e inicia los servicios.
.DESCRIPTION
    No reinstala nada ni borra datos. Si ya hay servicios en marcha se detiene sin iniciar otro consumidor del bot.
    Ejecútalo en tu propia ventana de PowerShell: solo ahí están tus credenciales. Para terminar: Detener-Demostracion.ps1.
.PARAMETER DispositivoSerial
    Usa el ESP32 conectado por USB en lugar de la caneca simulada. Requiere PuertoSerial en bridge.local.json y la
    alimentación y calibración verificadas según hardware\README.md.
.PARAMETER Reiniciar
    Detiene los servicios de esta sesion, espera a que terminen y los inicia otra vez. Aplica una configuracion nueva. No toca Docker ni Oracle.
.PARAMETER ConEscritorio
    Abre también la aplicación de escritorio.
#>
[CmdletBinding()]
param([switch] $Reiniciar, [switch] $DispositivoSerial, [switch] $ConEscritorio, [switch] $SinSupervisor, [ValidateSet('Debug', 'Release')] [string] $Configuracion = 'Release')
$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$bot = Join-Path $raiz "src\EcoAccess.P3.Bot\bin\$Configuracion\net48\EcoAccess.P3.Bot.exe"
function Paso([string] $Texto) { Write-Host "-> $Texto" -ForegroundColor Cyan }

Paso 'Evitando procesos duplicados'
$secretosBot = Join-Path $env:APPDATA 'Microsoft\UserSecrets\ecoaccess-p3-framework\secrets.json'
if ($Reiniciar) {
    & (Join-Path $raiz 'scripts\desarrollo\Detener-Demostracion.ps1')
    $espera = (Get-Date).AddSeconds(30)
    while ((Get-Process EcoAccess.P3.Bot -ErrorAction SilentlyContinue) -and (Get-Date) -lt $espera) { Start-Sleep -Seconds 1 }
}
$enMarcha = Get-Process EcoAccess.P3.Bot -ErrorAction SilentlyContinue
if ($enMarcha) {
    $antigua = (Test-Path $secretosBot) -and ((Get-Item $secretosBot).LastWriteTime -gt ($enMarcha | Measure-Object StartTime -Minimum).Minimum)
    if ($antigua) { throw 'El bot en marcha usa una configuracion antigua. Aplica la nueva con: Iniciar-Bot-Eco.ps1 -Reiniciar' }
    throw 'Ya hay servicios de EcoAccess en marcha. Telegram admite un solo consumidor. Para reiniciarlos: Iniciar-Bot-Eco.ps1 -Reiniciar'
}

Paso 'Comprobando Docker'
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) { throw 'Falta Docker Desktop. Consulta docs\instalacion\11_instalacion.md.' }
$ErrorActionPreference = 'Continue'
& docker info --format '{{.ServerVersion}}' 2>$null | Out-Null
$dockerOk = $LASTEXITCODE -eq 0
$ErrorActionPreference = 'Stop'
if (-not $dockerOk) { throw 'Docker no responde: abre Docker Desktop, espera a que termine de iniciar y repite.' }

if (-not (Test-Path $bot)) {
    Paso "Compilando ($Configuracion)"
    & dotnet build (Join-Path $raiz 'EcoAccess.P3.sln') -c $Configuracion -v q --nologo
    if ($LASTEXITCODE -ne 0) { throw 'La compilación falló.' }
}

Paso 'Iniciando Oracle si está detenido'
$oracle = 'oracle-free-p3'
if (-not (& docker ps -q --filter "name=^$oracle$" --filter 'status=running')) { & docker start $oracle | Out-Null }
$limite = (Get-Date).AddMinutes(5)
while ((& docker inspect -f '{{.State.Health.Status}}' $oracle) -ne 'healthy') {
    if ((Get-Date) -gt $limite) { throw "Oracle no quedó listo en 5 minutos. Revisa: docker logs $oracle" }
    Start-Sleep -Seconds 5
}

Paso 'Aplicando migraciones pendientes (no borra datos)'
& (Join-Path $raiz 'scripts\instalacion\Preparar-BaseDatos.ps1')
if ($LASTEXITCODE -ne 0) { throw 'No se pudieron aplicar las migraciones.' }

$ErrorActionPreference = 'Continue'
Paso 'Comprobando la identidad del bot'
$telegram = & $bot --comprobar-telegram 2>&1 | ForEach-Object { "$_" }
$codigoTelegram = $LASTEXITCODE
Paso 'Comprobando el motor visual'
$vision = (& $bot --estado-vision 2>$null | Out-String) | ConvertFrom-Json
$ErrorActionPreference = 'Stop'
$telegram | ForEach-Object { Write-Host "   $_" }
if ($codigoTelegram -ne 0) { throw 'Telegram no está listo. Ejecuta scripts\instalacion\Configurar-Credenciales.ps1 -Telegram y repite.' }
Write-Host "   Vision:Modo = $($vision.Modo)"
if ($vision.Modo -ne 'Groq' -and -not $vision.Local.Disponible) { Write-Warning 'Ollama no responde: inícialo si quieres análisis local. Mientras tanto el bot ofrece selección manual.' }
if ($vision.Modo -ne 'Local' -and -not $vision.Groq.ClaveConfigurada) { Write-Warning 'Falta la clave de Groq: el servicio externo queda desactivado.' }

Paso 'Iniciando los servicios'
$argumentos = @{ Configuracion = $Configuracion; ConBot = $true; SinEscritorio = (-not $ConEscritorio); DispositivoSerial = $DispositivoSerial }
& (Join-Path $raiz 'scripts\desarrollo\Iniciar-Demostracion.ps1') @argumentos

if (-not $SinSupervisor) {
    Paso 'Iniciando el supervisor'
    & $bot --supervisar-en-segundo-plano
}

Paso 'Comprobando la pagina de registro'
$secretos = if (Test-Path $secretosBot) { Get-Content $secretosBot -Raw -Encoding UTF8 | ConvertFrom-Json } else { $null }
function Probar-Registro([string] $Url, [hashtable] $Encabezados) {
    try { $r = Invoke-WebRequest -Uri $Url -Headers $Encabezados -UseBasicParsing -TimeoutSec 20; return [int] $r.StatusCode }
    catch { if ($_.Exception.Response) { return [int] $_.Exception.Response.StatusCode } return 0 }
}
$pagina = if ($secretos) { $secretos.'Telegram:PaginaRegistro' } else { $null }
if ($pagina) {
    # Sin token la pagina responde 400: demuestra que esta viva, sin crear nada.
    $limitePagina = (Get-Date).AddSeconds(45)
    do { Start-Sleep -Seconds 3; $codigoLocal = Probar-Registro "http://127.0.0.1:8081/registro/" @{ 'X-Forwarded-Proto' = 'https' } } while ($codigoLocal -eq 0 -and (Get-Date) -lt $limitePagina)
    $codigoPublico = Probar-Registro $pagina @{}
    Write-Host "   Local 8081: HTTP $codigoLocal; publica ${pagina}: HTTP $codigoPublico (400 sin token es lo esperado)"
    if ($codigoLocal -ne 400 -or $codigoPublico -ne 400) { Write-Warning 'La pagina de registro no responde como se espera: revisa el registro del bot y tailscale funnel status.' }
}
else { Write-Warning 'Sin pagina de registro: ejecuta scripts\instalacion\Publicar-Registro-HTTPS.ps1.' }

Write-Host "`nListo. Abre @EcoAccessP3_JG_bot en tu teléfono y envía /start." -ForegroundColor Green
Write-Host 'Estado:  powershell -ExecutionPolicy Bypass -File scripts\desarrollo\Verificar-EcoAccess-Telegram.ps1'
Write-Host 'Detener: powershell -ExecutionPolicy Bypass -File scripts\desarrollo\Detener-Demostracion.ps1'

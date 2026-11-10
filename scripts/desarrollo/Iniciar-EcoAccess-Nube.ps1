<#
.SYNOPSIS
    Inicia EcoAccess P3 contra Oracle Cloud en la fase de software (sin ESP32): bot de Telegram y, si se pide, la
    aplicación de escritorio. Para detener: scripts\desarrollo\Detener-Demostracion.ps1.
.DESCRIPTION
    1. Comprueba el equipo y hace una consulta real de solo lectura a Oracle (Validar-Entorno.ps1 -ProbarOracle).
    2. Garantiza un solo consumidor del token: si hay otro bot (por ejemplo EcoAccess V2) se detiene solo con
       -DetenerOtroBot; nunca se tocan sus archivos, datos ni credenciales.
    3. Comprueba la identidad del bot y el motor visual, e inicia el bot en modo polling sin dispositivo: Eco orienta el
       destino y no emite órdenes de apertura.
    4. Comprueba la página de registro local (8081) y la pública si Telegram:PaginaRegistro está configurada.
    No aplica migraciones ni borra datos. No necesita permisos de administrador.
.EXAMPLE
    pwsh -File scripts\desarrollo\Iniciar-EcoAccess-Nube.ps1 -ConEscritorio
#>
[CmdletBinding()]
param([switch] $ConEscritorio, [switch] $DetenerOtroBot, [ValidateSet('Debug', 'Release')] [string] $Configuracion = 'Release')
$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$bot = Join-Path $raiz "src\EcoAccess.P3.Bot\bin\$Configuracion\net48\EcoAccess.P3.Bot.exe"
$escritorio = Join-Path $raiz "src\EcoAccess.P3.Presentacion\bin\$Configuracion\net48\EcoAccess.P3.Presentacion.exe"
function Paso([string] $Texto) { Write-Host "-> $Texto" -ForegroundColor Cyan }
if (-not $env:TNS_ADMIN) { $env:TNS_ADMIN = [Environment]::GetEnvironmentVariable('TNS_ADMIN', 'User') }

Paso 'Un solo consumidor del bot'
# Solo compiten por el token los procesos en polling o webhook; una evaluación o un diagnóstico no cuentan.
$otros = @(Get-CimInstance Win32_Process -Filter "Name='EcoAccess.P3.Bot.exe'" | Where-Object { $_.CommandLine -match '--(polling|webhook)\b' })
foreach ($p in $otros) {
    $propio = $p.ExecutablePath -and $p.ExecutablePath.StartsWith($raiz, [StringComparison]::OrdinalIgnoreCase)
    if ($propio) { throw 'El bot de esta copia ya está en marcha. Para reiniciarlo: Detener-Demostracion.ps1 y vuelve a ejecutar este script.' }
    if (-not $DetenerOtroBot) { throw "Otro bot usa el mismo token ($($p.ExecutablePath), PID $($p.ProcessId)). Repite con -DetenerOtroBot para detenerlo; no se modifican sus datos." }
    Stop-Process -Id $p.ProcessId
    Write-Host "   Detenido el bot de otra copia (PID $($p.ProcessId)); sus archivos y datos quedan intactos."
}
Start-Sleep -Seconds 2

if (-not (Test-Path $bot) -or ($ConEscritorio -and -not (Test-Path $escritorio))) {
    Paso "Compilando ($Configuracion)"
    & dotnet build (Join-Path $raiz 'EcoAccess.P3.sln') -c $Configuracion -v q --nologo
    if ($LASTEXITCODE -ne 0) { throw 'La compilación falló.' }
}

Paso 'Comprobando el equipo y Oracle Cloud'
$validacion = & pwsh -NoProfile -File (Join-Path $raiz 'scripts\instalacion\Validar-Entorno.ps1') -ProbarOracle
$validacion | Where-Object { $_ -match '^(FALTA|OK +Consulta real)' } | ForEach-Object { Write-Host "   $_" }
if ($LASTEXITCODE -ne 0) { throw 'Faltan componentes obligatorios: ejecuta scripts\instalacion\Validar-Entorno.ps1 para ver el detalle.' }

$ErrorActionPreference = 'Continue'
Paso 'Identidad del bot y motor visual'
$telegram = & $bot --comprobar-telegram 2>&1 | ForEach-Object { "$_" }
$codigoTelegram = $LASTEXITCODE
$vision = (& $bot --estado-vision 2>$null | Out-String)
$ErrorActionPreference = 'Stop'
$telegram | ForEach-Object { Write-Host "   $_" }
if ($codigoTelegram -ne 0) { throw 'Telegram no está listo: revisa el token con scripts\instalacion\Configurar-Credenciales.ps1 -Telegram.' }
try { $v = $vision | ConvertFrom-Json; Write-Host "   Vision:Modo = $($v.Modo); local disponible = $($v.Local.Disponible)" } catch { Write-Warning 'No se pudo leer el estado del motor visual.' }

Paso 'Iniciando el bot (modo software, sin dispositivo)'
$base = Join-Path ([Environment]::GetFolderPath('CommonApplicationData')) 'EcoAccessP3Framework'
$sesion = Join-Path $base ("Sesiones\" + (Get-Date -Format 'yyyyMMdd-HHmmss'))
$control = Join-Path $base 'Desarrollo'
New-Item -ItemType Directory -Force $sesion, $control | Out-Null
$p = Start-Process $bot -ArgumentList '--polling' -WindowStyle Hidden -PassThru -WorkingDirectory $raiz `
    -RedirectStandardOutput (Join-Path $sesion 'Telegram.salida.log') -RedirectStandardError (Join-Path $sesion 'Telegram.error.log')
Start-Sleep -Seconds 8
if (-not (Get-Process -Id $p.Id -ErrorAction SilentlyContinue)) { throw "El bot terminó al iniciar. Revisa $(Join-Path $sesion 'Telegram.error.log')" }
$procesos = @([pscustomobject]@{ Componente = 'Telegram'; IdProceso = $p.Id; InicioTicks = $p.StartTime.ToUniversalTime().Ticks })
[IO.File]::WriteAllText((Join-Path $control 'sesion.json'), (@{ Directorio = $sesion; Procesos = $procesos } | ConvertTo-Json -Depth 4), (New-Object Text.UTF8Encoding($false)))
Get-Content (Join-Path $sesion 'Telegram.error.log') -TotalCount 8 | ForEach-Object { Write-Host "   $_" }

Paso 'Página de registro'
function Probar([string] $Url, [hashtable] $Encabezados) {
    try { return [int](Invoke-WebRequest -Uri $Url -Headers $Encabezados -UseBasicParsing -TimeoutSec 15).StatusCode }
    catch { if ($_.Exception.Response) { return [int]$_.Exception.Response.StatusCode } return 0 }
}
$local = Probar 'http://127.0.0.1:8081/registro/' @{ 'X-Forwarded-Proto' = 'https' }
$secretos = Get-Content (Join-Path $env:APPDATA 'Microsoft\UserSecrets\ecoaccess-p3-framework\secrets.json') -Raw -ErrorAction SilentlyContinue | ConvertFrom-Json
$publica = $secretos.'Telegram:PaginaRegistro'
if ($publica) { Write-Host "   Local 8081: HTTP $local; pública: HTTP $(Probar $publica @{}) (400 sin token es lo esperado)" }
else { Write-Warning "Local 8081: HTTP $local. Sin Telegram:PaginaRegistro: /vincular no puede ofrecer el formulario HTTPS. Publica /registro con scripts\instalacion\Publicar-Registro-HTTPS.ps1 (Tailscale Funnel)." }

if ($ConEscritorio) { Start-Process $escritorio -WorkingDirectory (Split-Path $escritorio) }
Write-Host "`nListo. Registros en $sesion" -ForegroundColor Green
Write-Host 'Detener: pwsh -File scripts\desarrollo\Detener-Demostracion.ps1'

<#
.SYNOPSIS
    Inicia el modo demostración: servicio de dispositivos, caneca simulada y aplicación de escritorio.
.DESCRIPTION
    Con -ConBot inicia además el bot oficial; úsalo en un solo equipo. Un simulador no equivale al dispositivo físico.
#>
[CmdletBinding()]
param([ValidateSet('Debug', 'Release')] [string] $Configuracion = 'Release', [switch] $ConBot, [switch] $SinEscritorio)
$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$bot = Join-Path $raiz "src\EcoAccess.P3.Bot\bin\$Configuracion\net48\EcoAccess.P3.Bot.exe"
$escritorio = Join-Path $raiz "src\EcoAccess.P3.Presentacion\bin\$Configuracion\net48\EcoAccess.P3.Presentacion.exe"
if (-not (Test-Path $bot) -or -not (Test-Path $escritorio)) { throw "Compila primero: dotnet build EcoAccess.P3.sln -c $Configuracion" }
$base = Join-Path ([Environment]::GetFolderPath('CommonApplicationData')) 'EcoAccessP3Framework'
$config = Join-Path $base 'Bridge\bridge.local.json'
if (-not (Test-Path $config)) { throw 'Falta la configuración: ejecuta scripts\instalacion\Preparar-Demostracion.ps1' }
$control = Join-Path ([Environment]::GetFolderPath('CommonApplicationData')) 'EcoAccessP3Framework\Desarrollo'
New-Item -ItemType Directory -Force $control | Out-Null
$sesion = Join-Path $base ("Sesiones\" + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Force $sesion | Out-Null
if (Get-Process EcoAccess.P3.Bot -ErrorAction SilentlyContinue) { throw 'Ya hay servicios de EcoAccess en ejecución: detén esa sesión primero.' }

# Docker deja el contenedor detenido al reiniciarse: sin Oracle, los servicios terminarían al iniciar.
$oracle = 'oracle-free-p3'
if (-not (& docker ps -q --filter "name=^$oracle$" --filter 'status=running')) { & docker start $oracle | Out-Null }
$limite = (Get-Date).AddMinutes(5)
while ((& docker inspect -f '{{.State.Health.Status}}' $oracle) -ne 'healthy') {
    if ((Get-Date) -gt $limite) { throw "Oracle no quedó listo en 5 minutos. Revisa: docker logs $oracle" }
    Start-Sleep -Seconds 5
}

$env:ECOACCESS_ARCHIVO_BRIDGE = $config
function Iniciar([string] $Componente, [string] $Modo) {
    $p = Start-Process $bot -ArgumentList $Modo -WindowStyle Hidden -PassThru -WorkingDirectory $raiz `
        -RedirectStandardOutput (Join-Path $sesion "$Componente.salida.log") -RedirectStandardError (Join-Path $sesion "$Componente.error.log")
    return [pscustomobject]@{ Componente = $Componente; IdProceso = $p.Id; InicioTicks = $p.StartTime.ToUniversalTime().Ticks }
}
$procesos = @((Iniciar 'Bridge' '--bridge'))
Start-Sleep -Seconds 2
$procesos += Iniciar 'Simulador' '--dispositivo-simulado'
if ($ConBot) { $procesos += Iniciar 'Telegram' '--polling' }
Start-Sleep -Seconds 4
foreach ($r in $procesos) {
    if (-not (Get-Process -Id $r.IdProceso -ErrorAction SilentlyContinue)) {
        # Sin sesión registrada, Detener-Demostracion.ps1 no podría detener los que siguen activos.
        $procesos | ForEach-Object { Stop-Process -Id $_.IdProceso -ErrorAction SilentlyContinue }
        throw "$($r.Componente) terminó al iniciar. Revisa $(Join-Path $sesion ($r.Componente + '.error.log'))"
    }
}
[IO.File]::WriteAllText((Join-Path $control 'sesion.json'), (@{ Directorio = $sesion; Procesos = $procesos } | ConvertTo-Json -Depth 4), (New-Object Text.UTF8Encoding($false)))
Write-Output "Servicios activos: $(($procesos | ForEach-Object Componente) -join ', '). Registros en $sesion"
if (-not $SinEscritorio) { Start-Process $escritorio }

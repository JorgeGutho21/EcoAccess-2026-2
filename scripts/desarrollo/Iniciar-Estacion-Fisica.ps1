<#
.SYNOPSIS
    Inicia la estación física de tres canecas contra Oracle Cloud: un adaptador USB y un bridge por caneca (los tres
    comparten COM3, que se abre en exclusiva por orden) y el bot de Telegram en modo físico.
.DESCRIPTION
    Requiere scripts\instalacion\Preparar-Estacion-Fisica.ps1 (claves y JSON privados) y el firmware
    estacion_tres_canecas cargado con las tapas calibradas. Comprueba COM3 libre, detiene SOLO el bot de esta copia
    (comprobando ruta y modo), compila Release y arranca en este orden: adaptadores --dispositivo-serial, bridges
    --bridge y el bot --polling con Bridge:Canecas:<id>:Eventos/Comandos para que cada orden compruebe el bridge de su
    caneca. Registra los procesos en EcoAccessP3Framework\Desarrollo\sesion.json: para detener todo,
    scripts\desarrollo\Detener-Demostracion.ps1.
    Las canecas fuera de servicio en Oracle siguen sin recibir órdenes: el bot orienta, pero no abre.
.EXAMPLE
    pwsh -File scripts\desarrollo\Iniciar-Estacion-Fisica.ps1
#>
[CmdletBinding()]
param(
    [ValidateSet('blanca', 'verde', 'negra')] [string[]] $Canecas = @('blanca', 'negra', 'verde'),
    [switch] $SinBot,
    [switch] $SinCompilar
)
$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$base = Join-Path ([Environment]::GetFolderPath('CommonApplicationData')) 'EcoAccessP3Framework'
$privado = Join-Path $base 'Bridge'
$bot = Join-Path $raiz 'src\EcoAccess.P3.Bot\bin\Release\net48\EcoAccess.P3.Bot.exe'
function Paso([string] $t) { Write-Host "-> $t" -ForegroundColor Cyan }
if (-not $env:TNS_ADMIN) { $env:TNS_ADMIN = [Environment]::GetEnvironmentVariable('TNS_ADMIN', 'User') }

Paso 'Configuración privada de las canecas'
$config = @{}
foreach ($c in $Canecas) {
    $archivo = Join-Path $privado "bridge-$c.json"
    if (-not (Test-Path $archivo)) { throw "Falta ${archivo}: ejecuta scripts\instalacion\Preparar-Estacion-Fisica.ps1." }
    $config[$c] = (Get-Content $archivo -Raw | ConvertFrom-Json).Bridge
    Write-Host "   ${c}: caneca $($config[$c].IdContenedor), $($config[$c].PuertoSerial), eventos $($config[$c].Eventos)"
}
$puerto = $config[$Canecas[0]].PuertoSerial
if (@($Canecas | Where-Object { $config[$_].PuertoSerial -ne $puerto }).Count) { throw 'Las canecas de la estación deben compartir el mismo COM.' }

Paso "Puerto $puerto presente y libre"
if ($puerto -notin [IO.Ports.SerialPort]::GetPortNames()) { throw "$puerto no aparece: conecta el ESP32 por USB." }
$sp = New-Object IO.Ports.SerialPort $puerto, 115200
try { $sp.Open(); $sp.Close() } catch { throw "$puerto está ocupado: cierra el Monitor Serie de Arduino IDE y cualquier prueba en curso." }

Paso 'Procesos anteriores de esta copia'
$propios = @(Get-CimInstance Win32_Process -Filter "Name='EcoAccess.P3.Bot.exe'" | Where-Object {
    $_.ExecutablePath -and $_.ExecutablePath.StartsWith($raiz.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase) -and
    $_.CommandLine -match '--(polling|bridge|dispositivo-serial|dispositivo-simulado|supervisar)\b' })
if (Test-Path (Join-Path $base 'Desarrollo\sesion.json')) { & (Join-Path $PSScriptRoot 'Detener-Demostracion.ps1') | ForEach-Object { Write-Host "   $_" } }
foreach ($p in $propios) {
    if (Get-Process -Id $p.ProcessId -ErrorAction SilentlyContinue) { Stop-Process -Id $p.ProcessId; Write-Host "   Detenido PID $($p.ProcessId) ($([regex]::Match($p.CommandLine, '--[a-z-]+').Value))." }
}
Start-Sleep -Seconds 2

if (-not $SinCompilar) {
    Paso 'Compilando Release'
    # Solo el bot: la aplicación de escritorio puede estar abierta (un administrador habilitando canecas) y bloquear sus DLL.
    & dotnet build (Join-Path $raiz 'src\EcoAccess.P3.Bot\EcoAccess.P3.Bot.csproj') -c Release -v q --nologo | Select-Object -Last 3
    if ($LASTEXITCODE -ne 0) { throw 'La compilación falló.' }
}

$sesion = Join-Path $base ('Sesiones\estacion-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Force $sesion, (Join-Path $base 'Desarrollo') | Out-Null
$procesos = New-Object System.Collections.Generic.List[object]
function Lanzar([string] $componente, [string] $modo, [string] $archivoBridge, [hashtable] $extra = @{}) {
    $anterior = @{}
    $vars = @{ ECOACCESS_ARCHIVO_BRIDGE = $archivoBridge } + $extra
    foreach ($k in $vars.Keys) { $anterior[$k] = [Environment]::GetEnvironmentVariable($k); [Environment]::SetEnvironmentVariable($k, $vars[$k]) }
    try {
        $p = Start-Process $bot -ArgumentList $modo -WindowStyle Hidden -PassThru -WorkingDirectory $raiz `
            -RedirectStandardOutput (Join-Path $sesion "$componente.salida.log") -RedirectStandardError (Join-Path $sesion "$componente.error.log")
    } finally { foreach ($k in $vars.Keys) { [Environment]::SetEnvironmentVariable($k, $anterior[$k]) } }
    $procesos.Add([pscustomobject]@{ Componente = $componente; IdProceso = $p.Id; InicioTicks = $p.StartTime.ToUniversalTime().Ticks })
    Write-Host "   $componente (PID $($p.Id))"
}

Paso 'Adaptadores USB (uno por caneca, mismo COM)'
foreach ($c in $Canecas) { Lanzar "Dispositivo-$c" '--dispositivo-serial' (Join-Path $privado "bridge-$c.json") }
Start-Sleep -Seconds 4
Paso 'Bridges con Oracle (uno por caneca)'
foreach ($c in $Canecas) { Lanzar "Bridge-$c" '--bridge' (Join-Path $privado "bridge-$c.json") }
Start-Sleep -Seconds 6

if (-not $SinBot) {
    Paso 'Bot de Telegram en modo físico'
    $enlaces = @{}
    foreach ($c in $Canecas) {
        $id = $config[$c].IdContenedor
        $enlaces["ECOACCESS_Bridge__Canecas__${id}__Eventos"] = $config[$c].Eventos
        $enlaces["ECOACCESS_Bridge__Canecas__${id}__Comandos"] = $config[$c].Comandos
    }
    # El archivo de la primera caneca da los enlaces predeterminados y saca al bot del modo software.
    Lanzar 'Telegram' '--polling' (Join-Path $privado "bridge-$($Canecas[0]).json") $enlaces
    Start-Sleep -Seconds 10
}

[IO.File]::WriteAllText((Join-Path $base 'Desarrollo\sesion.json'), (@{ Directorio = $sesion; Procesos = $procesos } | ConvertTo-Json -Depth 4), (New-Object Text.UTF8Encoding($false)))

Paso 'Comprobación'
$caidos = 0
foreach ($r in $procesos) {
    $vivo = [bool](Get-Process -Id $r.IdProceso -ErrorAction SilentlyContinue)
    if (-not $vivo) { $caidos++; Write-Warning "$($r.Componente) terminó: $(Get-Content (Join-Path $sesion "$($r.Componente).error.log") -Tail 3 -ErrorAction SilentlyContinue)" }
}
foreach ($c in $Canecas) {
    foreach ($u in @($config[$c].Eventos, $config[$c].Comandos)) {
        $uri = [Uri]$u; $t = New-Object Net.Sockets.TcpClient
        $ok = $t.ConnectAsync($uri.Host, $uri.Port).Wait(800) -and $t.Connected; $t.Dispose()
        Write-Host ("   {0,-7} {1,-34} {2}" -f $c, $u, $(if ($ok) { 'escucha' } else { 'NO ESCUCHA' }))
        if (-not $ok) { $caidos++ }
    }
}
if (-not $SinBot) { Get-Content (Join-Path $sesion 'Telegram.error.log') -TotalCount 6 -ErrorAction SilentlyContinue | ForEach-Object { Write-Host "   bot: $_" } }
Write-Host "`nRegistros: $sesion" -ForegroundColor Green
Write-Host 'Detener todo: pwsh -File scripts\desarrollo\Detener-Demostracion.ps1'
if ($caidos) { Write-Warning "$caidos comprobaciones fallaron."; exit 1 }

<#
.SYNOPSIS
    Inicia, detiene o consulta la estación de las tres canecas (blanca 1093, verde 1400, negra 1401) contra Oracle Cloud,
    en modo DEMOSTRACIÓN (canecas simuladas) o FÍSICO (ESP32 por un puerto COM).

.DESCRIPTION
    Por cada caneca se ejecutan dos procesos de EcoAccess.P3.Bot:
      * --bridge: lee de Oracle las órdenes de apertura de SU caneca y registra los eventos (acuse, apertura, cierre).
      * --dispositivo-simulado (Demo) o --dispositivo-serial (Fisica): la caneca. Habla con su bridge por HTTP local y
        firma cada mensaje con una clave HMAC propia de esa caneca.
    La primera vez crea la configuración privada en %ProgramData%\EcoAccessP3Framework\Bridge (fuera de Git): un JSON,
    una clave HMAC aleatoria de 32 bytes y una carpeta de estado por caneca y por modo. Nunca muestra las claves.
    Registra la sesión en %ProgramData%\EcoAccessP3Framework\Desarrollo\estacion.json: la aplicación de escritorio la lee
    y muestra «Estación simulada (demo)» o «Bridge físico activo».

    Modo Demo: la tapa se «abre» en software durante 10 s y se cierra; los eventos quedan en Oracle como cualquier
    caneca, pero la solicitud se hace por el canal Simulador y el depósito queda SIN verificar: no da ecopuntos reales
    hasta que un Operario o Supervisor lo verifique.
    Modo Fisica: mismo flujo con el ESP32 de tres tapas (hardware/estacion_tres_canecas). Reutiliza la configuración
    física existente (bridge-<color>.json y clave-<color>.bin, las claves del firmware); si no existe, la crea y hay que
    copiar cada clave en configuracion_privada.h del firmware (ver docs/hardware/GUIA_ESTACION_DEMO_Y_FISICA.md).

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\estacion\EcoAccess-Estacion.ps1 -Accion Iniciar -Modo Demo
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\estacion\EcoAccess-Estacion.ps1 -Accion Iniciar -Modo Fisica -PuertoSerial COM5
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\estacion\EcoAccess-Estacion.ps1 -Accion Estado
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\estacion\EcoAccess-Estacion.ps1 -Accion Detener
#>
[CmdletBinding()]
param(
    [ValidateSet('Iniciar', 'Detener', 'Estado')] [string] $Accion = 'Iniciar',
    [ValidateSet('Demo', 'Fisica')] [string] $Modo = 'Demo',
    [ValidateSet('blanca', 'verde', 'negra')] [string[]] $Canecas = @('blanca', 'verde', 'negra'),
    [ValidatePattern('^COM[0-9]{1,3}$')] [string] $PuertoSerial,
    [switch] $CrearAccesosDirectos
)
$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$base = Join-Path ([Environment]::GetFolderPath('CommonApplicationData')) 'EcoAccessP3Framework'
$carpetaBridge = Join-Path $base 'Bridge'
$control = Join-Path $base 'Desarrollo'
# Manifiesto propio: sesion.json lo reescribe el vigilante del bot de Telegram (Vigilar-Bot.ps1).
$manifiesto = Join-Path $control 'estacion.json'

# Identidad fija de cada caneca de la estación (Oracle Cloud) y sus puertos locales (eventos, comandos).
$estacion = @{
    blanca = @{ Id = 1093; Eventos = 8182; Comandos = 8183 }
    verde  = @{ Id = 1400; Eventos = 8184; Comandos = 8185 }
    negra  = @{ Id = 1401; Eventos = 8186; Comandos = 8187 }
}

function Obtener-Sesion {
    if (-not (Test-Path $manifiesto)) { return $null }
    try { return Get-Content $manifiesto -Raw | ConvertFrom-Json } catch { return $null }
}

function Proceso-Vigente($registro) {
    $p = Get-Process -Id $registro.IdProceso -ErrorAction SilentlyContinue
    return [bool]($p -and $p.StartTime.ToUniversalTime().Ticks -eq [long]$registro.InicioTicks)
}

function Mostrar-Estado {
    $sesion = Obtener-Sesion
    if (-not $sesion) { Write-Output 'Estación detenida: no hay una sesión registrada.'; return }
    $modoSesion = if ($sesion.Modo) { $sesion.Modo } else { 'desconocido' }
    Write-Output "Sesión en modo $modoSesion. Registros: $($sesion.Directorio)"
    foreach ($r in $sesion.Procesos) {
        $estado = if (Proceso-Vigente $r) { "activo (proceso $($r.IdProceso))" } else { 'DETENIDO' }
        Write-Output ("  {0,-20} {1}" -f $r.Componente, $estado)
        if (-not (Proceso-Vigente $r)) {
            $registroError = Join-Path $sesion.Directorio "$($r.Componente).error.log"
            if (Test-Path $registroError) { Get-Content $registroError -Tail 3 | ForEach-Object { Write-Output "      $_" } }
        }
    }
}

function Detener-Estacion {
    $sesion = Obtener-Sesion
    if (-not $sesion) { Write-Output 'No hay una sesión registrada.'; return }
    foreach ($r in $sesion.Procesos) {
        if (Proceso-Vigente $r) { Stop-Process -Id $r.IdProceso; Write-Output "Detenido $($r.Componente)." }
    }
    Remove-Item $manifiesto -ErrorAction SilentlyContinue
    Write-Output 'Estación detenida. Las órdenes que quedaron sin atender vencen solas a los 2 minutos.'
}

function Proteger-Archivo([string] $ruta) {
    # Solo el usuario actual, SYSTEM y Administradores pueden leer la clave.
    & icacls.exe $ruta /inheritance:r /grant:r "$($env:USERNAME):F" 'SYSTEM:F' '*S-1-5-32-544:F' | Out-Null
}

function Preparar-Caneca([string] $color) {
    $datos = $estacion[$color]
    # Física usa los nombres de la estación ya configurada (bridge-blanca.json, clave-blanca.bin, estado-blanca): sus
    # claves son las que tiene el firmware del ESP32 y NO se regeneran. Demo usa archivos propios (…-demo-…).
    $sufijo = if ($Modo -eq 'Demo') { "demo-$color" } else { $color }
    $dispositivo = if ($Modo -eq 'Demo') { "simulado-$color" } else { "esp32-piloto-$color" }
    $json = Join-Path $carpetaBridge "bridge-$sufijo.json"
    if ($Modo -eq 'Fisica' -and (Test-Path $json)) {
        $existente = Get-Content $json -Raw | ConvertFrom-Json
        if ($PuertoSerial -and $existente.Bridge.PuertoSerial -ne $PuertoSerial) {
            $existente.Bridge.PuertoSerial = $PuertoSerial
            [IO.File]::WriteAllText($json, ($existente | ConvertTo-Json -Depth 3), (New-Object Text.UTF8Encoding($false)))
            Write-Host "Caneca ${color}: puerto cambiado a $PuertoSerial (se conservan dispositivo y clave)."
        }
        if (-not $existente.Bridge.PuertoSerial) { throw "La caneca $color no tiene PuertoSerial: indica -PuertoSerial COMx." }
        if (-not (Test-Path $existente.Bridge.ArchivoClaveHmac)) { throw "Falta la clave de la caneca $color ($($existente.Bridge.ArchivoClaveHmac)). No se genera otra: debe ser la del firmware." }
        return $json
    }
    $clave = Join-Path $carpetaBridge "clave-$sufijo.bin"
    $estado = Join-Path $carpetaBridge "estado-$sufijo"
    New-Item -ItemType Directory -Force $carpetaBridge, $estado | Out-Null
    if (-not (Test-Path $clave)) {
        $bytes = New-Object byte[] 32
        [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
        [IO.File]::WriteAllBytes($clave, $bytes)
        [Array]::Clear($bytes, 0, $bytes.Length)
        Proteger-Archivo $clave
        Write-Host "Clave HMAC nueva para la caneca $color ($Modo)."
    }
    $config = [ordered]@{
        IdContenedor     = "$($datos.Id)"
        Dispositivo      = $dispositivo
        VersionClave     = '1'
        ArchivoClaveHmac = $clave
        Eventos          = "http://127.0.0.1:$($datos.Eventos)/eventos/"
        Comandos         = "http://127.0.0.1:$($datos.Comandos)/comandos/"
        DirectorioEstado = $estado
    }
    if ($Modo -eq 'Fisica') { $config.PuertoSerial = $PuertoSerial }
    else { $config.EscenarioSimulado = 'Normal'; $config.VentanaSimuladaSegundos = '10' }
    [IO.File]::WriteAllText($json, (@{ Bridge = $config } | ConvertTo-Json -Depth 3), (New-Object Text.UTF8Encoding($false)))
    return $json
}

function Buscar-Bot {
    $exe = Join-Path $raiz 'src\EcoAccess.P3.Bot\bin\Release\net48\EcoAccess.P3.Bot.exe'
    if (-not (Test-Path $exe)) {
        Write-Host 'Compilando el servicio de la estación (Release)…'
        & dotnet build (Join-Path $raiz 'src\EcoAccess.P3.Bot\EcoAccess.P3.Bot.csproj') -c Release -nologo -v quiet
        if ($LASTEXITCODE -ne 0 -or -not (Test-Path $exe)) { throw 'No se pudo compilar EcoAccess.P3.Bot. Ejecuta: dotnet build EcoAccess.P3.sln -c Release' }
    }
    return $exe
}

function Crear-AccesosDirectos {
    $escritorio = [Environment]::GetFolderPath('Desktop')
    $shell = New-Object -ComObject WScript.Shell
    $script = Join-Path $PSScriptRoot 'EcoAccess-Estacion.ps1'
    $icono = Join-Path $raiz 'assets\identidad\EcoAccess.ico'
    foreach ($a in @(
            @{ Nombre = 'EcoAccess - Iniciar canecas DEMO'; Argumentos = '-Accion Iniciar -Modo Demo' },
            @{ Nombre = 'EcoAccess - Detener canecas'; Argumentos = '-Accion Detener' })) {
        $lnk = $shell.CreateShortcut((Join-Path $escritorio "$($a.Nombre).lnk"))
        $lnk.TargetPath = 'powershell.exe'
        $lnk.Arguments = "-NoProfile -ExecutionPolicy Bypass -NoExit -File `"$script`" $($a.Argumentos)"
        $lnk.WorkingDirectory = $raiz
        if (Test-Path $icono) { $lnk.IconLocation = $icono }
        $lnk.Save()
        Write-Output "Acceso directo: $($a.Nombre)"
    }
}

switch ($Accion) {
    'Estado' { Mostrar-Estado; return }
    'Detener' { Detener-Estacion; return }
}

if ($CrearAccesosDirectos) { Crear-AccesosDirectos }
if ($Modo -eq 'Fisica' -and -not $PuertoSerial -and @($Canecas | Where-Object { -not (Test-Path (Join-Path $carpetaBridge "bridge-$_.json")) }).Count -gt 0) {
    throw 'El modo Fisica necesita -PuertoSerial (el COM del ESP32 en el Administrador de dispositivos) para crear la configuración.'
}
$previa = Obtener-Sesion
if ($previa -and @($previa.Procesos | Where-Object { Proceso-Vigente $_ }).Count -gt 0) {
    throw "Ya hay una estación en ejecución (modo $($previa.Modo)). Detenla primero con -Accion Detener."
}
$secretos = Join-Path $env:APPDATA 'Microsoft\UserSecrets\ecoaccess-p3-framework\secrets.json'
if (-not (Test-Path $secretos)) { throw 'Este equipo no tiene la conexión de Oracle. Ejecuta primero scripts\instalacion\Configurar-Integrante.ps1.' }

$bot = Buscar-Bot
New-Item -ItemType Directory -Force $control | Out-Null
$sesionDir = Join-Path $base ("Sesiones\" + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Force $sesionDir | Out-Null

function Iniciar([string] $componente, [string] $argumento, [string] $json) {
    $env:ECOACCESS_ARCHIVO_BRIDGE = $json
    $p = Start-Process $bot -ArgumentList $argumento -WindowStyle Hidden -PassThru -WorkingDirectory $raiz `
        -RedirectStandardOutput (Join-Path $sesionDir "$componente.salida.log") -RedirectStandardError (Join-Path $sesionDir "$componente.error.log")
    return [pscustomobject]@{ Componente = $componente; IdProceso = $p.Id; InicioTicks = $p.StartTime.ToUniversalTime().Ticks }
}

$procesos = @()
$nombreDispositivo = if ($Modo -eq 'Demo') { 'Simulador' } else { 'Dispositivo' }
$argumentoDispositivo = if ($Modo -eq 'Demo') { '--dispositivo-simulado' } else { '--dispositivo-serial' }
try {
    foreach ($color in $Canecas) {
        $json = Preparar-Caneca $color
        # La caneca (simulada o física) primero: el bridge le entrega las órdenes por su puerto local.
        $procesos += Iniciar "$nombreDispositivo-$color" $argumentoDispositivo $json
        $procesos += Iniciar "Bridge-$color" '--bridge' $json
    }
}
finally { Remove-Item Env:\ECOACCESS_ARCHIVO_BRIDGE -ErrorAction SilentlyContinue }

Start-Sleep -Seconds 6
$caidos = @($procesos | Where-Object { -not (Proceso-Vigente $_) })
[IO.File]::WriteAllText($manifiesto, (@{ Directorio = $sesionDir; Modo = $Modo; Procesos = $procesos } | ConvertTo-Json -Depth 4), (New-Object Text.UTF8Encoding($false)))
if ($caidos.Count -gt 0) {
    Mostrar-Estado
    Detener-Estacion | Out-Null
    throw "No se pudo iniciar: $(($caidos | ForEach-Object Componente) -join ', '). Revisa los .error.log en $sesionDir"
}
$etiqueta = if ($Modo -eq 'Demo') { 'DEMOSTRACIÓN (canecas simuladas: no hay apertura física)' } else { "FÍSICA (ESP32 en $PuertoSerial)" }
Write-Output "Estación en modo ${etiqueta}: $($Canecas -join ', ')."
Write-Output 'Abre EcoAccess y deposita un residuo: la caneca acusa, abre 10 s y cierra. Para parar: -Accion Detener.'

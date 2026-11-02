<#
.SYNOPSIS
    Diagnóstico integral: comprueba cada componente de verdad e informa un estado distinguible, sin imprimir secretos.
.DESCRIPTION
    OPERATIVO             se ejecutó una comprobación real y respondió bien.
    NO CONFIGURADO        faltan parámetros o credenciales.
    NO DISPONIBLE         el componente no responde.
    PENDIENTE DE PRUEBA REAL  necesita un teléfono, hardware u otra intervención que este script no puede hacer.
    ERROR                 hay un fallo que corregir.
    Con -Imagen ejecuta además una inferencia real con el motor local. No consume updates de Telegram ni envía fotos a Groq.
#>
[CmdletBinding()]
param([string] $Imagen, [ValidateSet('Debug', 'Release')] [string] $Configuracion = 'Release', [string] $Contenedor = 'oracle-free-p3')
$raiz = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$bot = Join-Path $raiz "src\EcoAccess.P3.Bot\bin\$Configuracion\net48\EcoAccess.P3.Bot.exe"
$base = Join-Path ([Environment]::GetFolderPath('CommonApplicationData')) 'EcoAccessP3Framework'
$errores = 0

function Informar([string] $Componente, [string] $Estado, [string] $Detalle) {
    if ($Estado -eq 'ERROR') { $script:errores++ }
    $color = switch ($Estado) { 'OPERATIVO' { 'Green' } 'ERROR' { 'Red' } 'NO DISPONIBLE' { 'Red' } 'NO CONFIGURADO' { 'Yellow' } default { 'Cyan' } }
    Write-Host ('{0,-26} {1,-26} {2}' -f $Componente, $Estado, $Detalle) -ForegroundColor $color
}

function Ejecutar([string[]] $Argumentos) {
    # El bot escribe avisos en stderr; aquí solo cuentan su salida y su código.
    $salida = & $bot @Argumentos 2>&1 | ForEach-Object { "$_" }
    return [pscustomobject]@{ Codigo = $LASTEXITCODE; Texto = ($salida -join "`n") }
}

if (-not (Test-Path $bot)) { Informar 'Compilación' 'NO CONFIGURADO' "Falta compilar: dotnet build EcoAccess.P3.sln -c $Configuracion"; exit 1 }
$compilado = (Get-Item $bot).LastWriteTime
$masNuevo = Get-ChildItem (Join-Path $raiz 'src') -Recurse -Filter *.cs -File | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' -and $_.LastWriteTime -gt $compilado } | Select-Object -First 1
if ($masNuevo) { Informar 'Compilación' 'ERROR' "Hay código más nuevo que el ejecutable ($($masNuevo.Name)): vuelve a compilar." }
else { Informar 'Compilación' 'OPERATIVO' "ejecutable del $($compilado.ToString('yyyy-MM-dd HH:mm'))" }

$dockerOk = $false
if (Get-Command docker -ErrorAction SilentlyContinue) {
    & docker info --format '{{.ServerVersion}}' 2>$null | Out-Null
    $dockerOk = $LASTEXITCODE -eq 0
}
if ($dockerOk) { Informar 'Docker' 'OPERATIVO' 'el servicio responde' } else { Informar 'Docker' 'NO DISPONIBLE' 'abre Docker Desktop' }

$oracle = Ejecutar '--comprobar-oracle'
if ($oracle.Codigo -eq 0) { Informar 'Oracle' 'OPERATIVO' ($oracle.Texto -replace ' No se modificaron.*$', '') }
elseif ($oracle.Texto -match 'Falta ConnectionStrings|no es válida') { Informar 'Oracle' 'NO CONFIGURADO' 'ejecuta scripts\instalacion\Preparar-BaseDatos.ps1' }
else {
    $salud = if ($dockerOk) { & docker inspect -f '{{.State.Status}}/{{.State.Health.Status}}' $Contenedor 2>$null } else { $null }
    Informar 'Oracle' 'NO DISPONIBLE' "sin respuesta; contenedor ${Contenedor}: $(if ($salud) { $salud } else { 'no encontrado' })"
}

$telegram = Ejecutar '--comprobar-telegram'
switch ($telegram.Codigo) {
    0 { Informar 'Telegram: identidad' 'OPERATIVO' (($telegram.Texto -split "`n")[0]) }
    3 { Informar 'Telegram: identidad' 'ERROR' 'hay un webhook activo: es incompatible con polling' }
    4 { Informar 'Telegram: identidad' 'ERROR' 'Telegram rechazó el token: ejecuta Configurar-Credenciales.ps1 -Telegram' }
    default {
        if ($telegram.Texto -match 'Falta configurar') { Informar 'Telegram: identidad' 'NO CONFIGURADO' 'ejecuta scripts\instalacion\Configurar-Credenciales.ps1 -Telegram' }
        else { Informar 'Telegram: identidad' 'NO DISPONIBLE' 'sin conexión con Telegram' }
    }
}
$consumidor = @(Get-Process EcoAccess.P3.Bot -ErrorAction SilentlyContinue).Count
$sesion = Join-Path $base 'Desarrollo\sesion.json'
$activos = if (Test-Path $sesion) { @((Get-Content $sesion -Raw | ConvertFrom-Json).Procesos | Where-Object { Get-Process -Id $_.IdProceso -ErrorAction SilentlyContinue }) } else { @() }
$polling = @($activos | Where-Object Componente -eq 'Telegram').Count
if ($polling -eq 1) { Informar 'Telegram: consumidor' 'OPERATIVO' 'un único proceso en polling' }
else { Informar 'Telegram: consumidor' 'NO DISPONIBLE' 'el bot no está en marcha: scripts\desarrollo\Iniciar-EcoAccess-Telegram.ps1' }
Informar 'Telegram: teléfono' 'PENDIENTE DE PRUEBA REAL' 'envía /start y una foto a @EcoAccessP3_JG_bot desde tu teléfono'

$estadoVision = Ejecutar '--estado-vision'
$vision = $null
try { $vision = $estadoVision.Texto | ConvertFrom-Json } catch { Informar 'Visión' 'ERROR' 'no se pudo leer el estado' }
if ($vision) {
    Informar 'Motor visual elegido' 'OPERATIVO' "Vision:Modo = $($vision.Modo)"
    if (-not $vision.Local.Disponible) { Informar 'Ollama' 'NO DISPONIBLE' 'inicia Ollama; la selección manual sigue disponible' }
    else {
        Informar 'Ollama' 'OPERATIVO' "versión $($vision.Local.Version)"
        if ($vision.Local.ModeloInstalado) { Informar 'LFM3-VL' 'OPERATIVO' "$($vision.Local.Modelo) instalado" } else { Informar 'LFM3-VL' 'NO CONFIGURADO' 'ollama pull lfm3-vl' }
    }
    $u = $vision.Local.UltimaInferencia
    if ($u) { Informar 'Última inferencia local' $(if ($u.Exitosa) { 'OPERATIVO' } else { 'ERROR' }) "$($u.FechaUtc) UTC, $($u.DuracionMs) ms, origen $($u.Origen) $($u.Error)" }
    else { Informar 'Última inferencia local' 'PENDIENTE DE PRUEBA REAL' 'usa -Imagen <foto>' }
}
if ($Imagen) {
    $inferencia = Ejecutar @('--comprobar-vision-local', $Imagen)
    if ($inferencia.Codigo -eq 0) { Informar 'Inferencia real local' 'OPERATIVO' $inferencia.Texto } else { Informar 'Inferencia real local' 'ERROR' $inferencia.Texto }
}

$groq = Ejecutar '--comprobar-groq'
$saludGroq = $null
try { $saludGroq = $groq.Texto | ConvertFrom-Json } catch { }
switch ($saludGroq.Estado) {
    'Operativo' { Informar 'Groq: clave y modelo' 'OPERATIVO' "$($saludGroq.Modelo) disponible" }
    'NoConfigurado' { Informar 'Groq: clave y modelo' 'NO CONFIGURADO' 'opcional: Configurar-Credenciales.ps1 -Groq' }
    'NoDisponible' { Informar 'Groq: clave y modelo' 'NO DISPONIBLE' $saludGroq.Mensaje }
    default { Informar 'Groq: clave y modelo' 'ERROR' $(if ($saludGroq) { $saludGroq.Mensaje } else { 'sin respuesta legible' }) }
}
if ($saludGroq.Estado -eq 'Operativo') {
    $g = $vision.Groq.UltimaInferencia
    if ($g) { Informar 'Última inferencia Groq' $(if ($g.Exitosa) { 'OPERATIVO' } else { 'ERROR' }) "$($g.FechaUtc) UTC, $($g.DuracionMs) ms $($g.Error)" }
    else { Informar 'Última inferencia Groq' 'PENDIENTE DE PRUEBA REAL' 'usa: EcoAccess.P3.Bot.exe --comprobar-vision-groq <foto>' }
    if ($vision.Groq.UsoHoy) { Write-Host ('{0,-26} {1,-26} {2}' -f 'Uso de Groq hoy', '', "$($vision.Groq.UsoHoy.Solicitudes) solicitudes, costo estimado USD $($vision.Groq.UsoHoy.CostoUsd)") }
}
Informar 'Autorización de fotos' 'PENDIENTE DE PRUEBA REAL' 'cada persona autoriza en Telegram; se guarda en Oracle y se retira con /revocarfotos'

$configuracionBridge = Join-Path $base 'Bridge\bridge.local.json'
if (-not (Test-Path $configuracionBridge)) { Informar 'Bridge' 'NO CONFIGURADO' 'ejecuta scripts\instalacion\Preparar-Demostracion.ps1' }
else {
    $ajustes = (Get-Content $configuracionBridge -Raw | ConvertFrom-Json).Bridge
    if (@($activos | Where-Object Componente -eq 'Bridge').Count -ne 1) { Informar 'Bridge' 'NO DISPONIBLE' 'no está en marcha' }
    else {
        $abierto = $false
        try { $uri = [Uri]$ajustes.Comandos; $c = New-Object Net.Sockets.TcpClient; $c.Connect($uri.Host, $uri.Port); $abierto = $c.Connected; $c.Close() } catch { }
        if ($abierto) { Informar 'Bridge' 'OPERATIVO' "escucha en $($uri.Host):$($uri.Port)" } else { Informar 'Bridge' 'ERROR' 'el proceso existe pero no escucha' }
    }
    if (@($activos | Where-Object Componente -eq 'Simulador').Count -eq 1) { Informar 'Caneca simulada' 'OPERATIVO' "ventana de $($ajustes.VentanaSimuladaSegundos) s; un simulador no equivale al dispositivo físico" }
}
$puertos = [System.IO.Ports.SerialPort]::GetPortNames()
Informar 'Puerto serial' $(if ($puertos.Count) { 'OPERATIVO' } else { 'NO DISPONIBLE' }) $(if ($puertos.Count) { $puertos -join ', ' } else { 'ningún puerto COM en este equipo' })
Informar 'ESP32 físico' 'PENDIENTE DE PRUEBA REAL' 'carga, calibración, fuente y 20 s medidos: hardware\README.md'

$ultimaSesion = Get-ChildItem (Join-Path $base 'Sesiones') -Directory -ErrorAction SilentlyContinue | Sort-Object Name -Descending | Select-Object -First 1
if ($ultimaSesion) {
    $fallos = Get-ChildItem $ultimaSesion.FullName -Filter *.error.log | ForEach-Object { Get-Content $_.FullName -Tail 2 -ErrorAction SilentlyContinue | ForEach-Object { "$($_)" } } | Where-Object { $_ -match 'falló|No se|rechaz|Conflict|Sin conexión' }
    Write-Host "`nÚltimos fallos registrados ($($ultimaSesion.Name)): $(if ($fallos) { '' } else { 'ninguno' })"
    $fallos | Select-Object -Last 5 | ForEach-Object { Write-Host "  $_" }
}
if ($errores -gt 0) { exit 1 }

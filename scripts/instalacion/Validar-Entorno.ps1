<#
.SYNOPSIS
    Comprueba qué necesita EcoAccess P3 en este equipo e informa con claridad lo que falta.
.DESCRIPTION
    Solo consulta: no instala, no modifica ni muestra credenciales. Termina con código 1 si falta algo obligatorio.
    Distingue dos modos de base de datos: Oracle Cloud con wallet (TNS_ADMIN) u Oracle local en Docker. Con
    -ProbarOracle ejecuta una consulta real de solo lectura (catálogo público) con el bot ya compilado.
    No necesita permisos de administrador.
#>
[CmdletBinding()]
param([string] $IdSecretos = 'ecoaccess-p3-framework', [string] $Contenedor = 'oracle-free-p3', [switch] $ProbarOracle)

$faltan = 0
function Informar([string] $Nombre, [bool] $Correcto, [string] $Detalle, [switch] $Opcional) {
    $marca = if ($Correcto) { 'OK     ' } elseif ($Opcional) { 'OPCIONAL' } else { 'FALTA  ' }
    Write-Output ("{0} {1}: {2}" -f $marca, $Nombre, $Detalle)
    if (-not $Correcto -and -not $Opcional) { $script:faltan++ }
}
$raiz = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent

$version = (Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full' -ErrorAction SilentlyContinue)
Informar '.NET Framework 4.8' ($version -and $version.Release -ge 528040) $(if ($version) { "versión $($version.Version)" } else { 'instálalo desde Microsoft (requiere administrador)' })
$paquete = "${env:ProgramFiles(x86)}\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8"
Informar 'Developer Pack 4.8' (Test-Path $paquete) 'necesario para compilar (requiere administrador)'

# vswhere no siempre está; se buscan también las instalaciones de Visual Studio 2022 (17) y posteriores.
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
$vs = if (Test-Path $vswhere) { & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -property displayName } else { $null }
if (-not $vs) {
    $devenv = Get-ChildItem "$env:ProgramFiles\Microsoft Visual Studio\*\*\Common7\IDE\devenv.exe" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($devenv) { $vs = "Visual Studio $($devenv.VersionInfo.ProductVersion) ($($devenv.Directory.Parent.Parent.Name))" }
}
Informar 'Visual Studio' ([bool]$vs) $(if ($vs) { $vs } else { 'instala Visual Studio 2022 o posterior con «Desarrollo de escritorio de .NET»' })
$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
Informar '.NET SDK' ([bool]$dotnet) $(if ($dotnet) { (& dotnet --version) } else { 'necesario para restaurar y ejecutar pruebas desde consola' })

# Modo de base de datos: Oracle Cloud (wallet en TNS_ADMIN) u Oracle local en Docker.
# Si la variable se creó con la consola ya abierta, solo existe en el perfil del usuario.
$tns = if ($env:TNS_ADMIN) { $env:TNS_ADMIN } else { [Environment]::GetEnvironmentVariable('TNS_ADMIN', 'User') }
$nube = $tns -and (Test-Path (Join-Path $tns 'tnsnames.ora'))
if ($nube) {
    Informar 'Wallet Oracle Cloud' ((Test-Path (Join-Path $tns 'cwallet.sso')) -and (Test-Path (Join-Path $tns 'sqlnet.ora'))) "TNS_ADMIN = carpeta del wallet (fuera del repositorio)"
    $sqlnet = Get-Content (Join-Path $tns 'sqlnet.ora') -Raw -ErrorAction SilentlyContinue
    Informar 'sqlnet.ora del wallet' (-not ($sqlnet -match '\?/network/admin')) 'DIRECTORY debe ser la ruta real de la carpeta del wallet (Power BI y ODP.NET sin Oracle Home)' -Opcional
    Informar 'Docker' $true 'no se necesita: se usa Oracle Cloud'
} else {
    $docker = Get-Command docker -ErrorAction SilentlyContinue
    Informar 'Docker u Oracle Cloud' ([bool]$docker) 'define TNS_ADMIN con la carpeta del wallet de Oracle Cloud, o instala Docker para la base local'
    if ($docker) {
        $activo = (& docker inspect -f '{{.State.Running}}' $Contenedor 2>$null) -eq 'true'
        Informar "Contenedor $Contenedor" $activo 'Oracle Database Free'
    }
}

$archivo = Join-Path $env:APPDATA "Microsoft\UserSecrets\$IdSecretos\secrets.json"
$secretos = if (Test-Path $archivo) { Get-Content $archivo -Raw -Encoding UTF8 | ConvertFrom-Json } else { $null }
$cadena = [bool]($secretos -and $secretos.'ConnectionStrings:OracleP3')
Informar 'Cadena Oracle privada' $cadena $(if ($cadena) { 'en User Secrets de este perfil (no se muestra)' } else { 'la aplicación la pide al iniciar; no la pegues en archivos del repositorio' }) -Opcional
Informar 'App.config' (Test-Path (Join-Path $raiz 'src\EcoAccess.P3.Presentacion\App.config')) 'parte pública de la conexión (sin contraseña)'

if ($ProbarOracle) {
    $bot = @('Release', 'Debug') | ForEach-Object { Join-Path $raiz "src\EcoAccess.P3.Bot\bin\$_\net48\EcoAccess.P3.Bot.exe" } | Where-Object { Test-Path $_ } | Select-Object -First 1
    if ($bot) {
        $env:TNS_ADMIN = $tns
        $salida = & $bot --comprobar-oracle 2>&1
        Informar 'Consulta real a Oracle' ($LASTEXITCODE -eq 0) (($salida | Select-Object -Last 1) -as [string])
    } else { Informar 'Consulta real a Oracle' $false 'compila la solución primero (dotnet build EcoAccess.P3.sln -c Release)' }
}

$ollama = Get-Command ollama -ErrorAction SilentlyContinue
Informar 'Ollama' ([bool]$ollama) 'reconocimiento visual local; sin él queda la selección manual' -Opcional
if ($ollama) { Informar 'Modelo visual' ([bool]((& ollama list 2>$null) -match 'lfm3-vl')) 'ollama pull lfm3-vl' -Opcional }
Informar 'Token de Telegram' ([bool]($secretos -and $secretos.'Telegram:TokenBot')) 'solo en el equipo que atiende el bot oficial' -Opcional
# Un solo consumidor por token: Telegram entrega cada mensaje a quien haga getUpdates; dos bots se los reparten.
$bots = @(Get-CimInstance Win32_Process -Filter "Name='EcoAccess.P3.Bot.exe'" |
    Where-Object { $_.CommandLine -match '--(polling|webhook)\b' })
Informar 'Bot en este equipo' ($bots.Count -le 1) $(if ($bots.Count) { "$($bots.Count) consumidor(es) activo(s): no inicies otro con el mismo token" } else { 'ninguno activo; solo el equipo central lo inicia' }) -Opcional
$puertos = [System.IO.Ports.SerialPort]::GetPortNames()
Informar 'Puerto USB del ESP32' ($puertos.Count -gt 0) $(if ($puertos.Count) { $puertos -join ', ' } else { 'sin puertos; conecta el ESP32 por USB o usa el simulador' }) -Opcional

if ($faltan -gt 0) { Write-Output "Faltan $faltan componentes obligatorios."; exit 1 }
Write-Output 'Componentes obligatorios disponibles.'

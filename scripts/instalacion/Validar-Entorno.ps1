<#
.SYNOPSIS
    Comprueba qué necesita EcoAccess P3 en este equipo e informa con claridad lo que falta.
.DESCRIPTION
    Solo consulta: no instala, no modifica ni muestra credenciales. Termina con código 1 si falta algo obligatorio.
#>
[CmdletBinding()]
param([string] $IdSecretos = 'ecoaccess-p3-framework', [string] $Contenedor = 'oracle-free-p3')

$faltan = 0
function Informar([string] $Nombre, [bool] $Correcto, [string] $Detalle, [switch] $Opcional) {
    $marca = if ($Correcto) { 'OK     ' } elseif ($Opcional) { 'OPCIONAL' } else { 'FALTA  ' }
    Write-Output ("{0} {1}: {2}" -f $marca, $Nombre, $Detalle)
    if (-not $Correcto -and -not $Opcional) { $script:faltan++ }
}

$version = (Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full' -ErrorAction SilentlyContinue)
Informar '.NET Framework 4.8' ($version -and $version.Release -ge 528040) $(if ($version) { "versión $($version.Version)" } else { 'instálalo desde Microsoft' })
$paquete = "${env:ProgramFiles(x86)}\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8"
Informar 'Developer Pack 4.8' (Test-Path $paquete) 'necesario para compilar'
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
$vs = if (Test-Path $vswhere) { & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -property displayName } else { $null }
Informar 'Visual Studio' ([bool]$vs) $(if ($vs) { $vs } else { 'instala la carga de trabajo de escritorio .NET' })
$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
Informar '.NET SDK' ([bool]$dotnet) $(if ($dotnet) { (& dotnet --version) } else { 'necesario para restaurar y ejecutar pruebas desde consola' })
$docker = Get-Command docker -ErrorAction SilentlyContinue
Informar 'Docker' ([bool]$docker) 'aloja Oracle en el modo de desarrollo'
if ($docker) {
    $activo = (& docker inspect -f '{{.State.Running}}' $Contenedor 2>$null) -eq 'true'
    Informar "Contenedor $Contenedor" $activo 'Oracle Database Free'
}
$archivo = Join-Path $env:APPDATA "Microsoft\UserSecrets\$IdSecretos\secrets.json"
$secretos = if (Test-Path $archivo) { Get-Content $archivo -Raw -Encoding UTF8 | ConvertFrom-Json } else { $null }
Informar 'Conexión Oracle' ([bool]($secretos -and $secretos.'ConnectionStrings:OracleP3')) 'ejecuta scripts\instalacion\Preparar-BaseDatos.ps1'
$ollama = Get-Command ollama -ErrorAction SilentlyContinue
Informar 'Ollama' ([bool]$ollama) 'reconocimiento visual local; sin él queda la selección manual' -Opcional
if ($ollama) { Informar 'Modelo visual' ([bool]((& ollama list 2>$null) -match 'lfm3-vl')) 'ollama pull lfm3-vl' -Opcional }
Informar 'Token de Telegram' ([bool]($secretos -and $secretos.'Telegram:TokenBot')) 'solo en el equipo que atiende el bot oficial' -Opcional
$puertos = [System.IO.Ports.SerialPort]::GetPortNames()
Informar 'Puerto USB del ESP32' ($puertos.Count -gt 0) $(if ($puertos.Count) { $puertos -join ', ' } else { 'sin puertos; el modo demostración usa el simulador' }) -Opcional

if ($faltan -gt 0) { Write-Output "Faltan $faltan componentes obligatorios."; exit 1 }
Write-Output 'Componentes obligatorios disponibles.'

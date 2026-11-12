<#
.SYNOPSIS
    Prepara la estación física de tres canecas (un ESP32, un COM): una clave HMAC, un JSON de bridge y una carpeta de
    estado por caneca, y la cabecera privada del firmware con las mismas claves.
.DESCRIPTION
    - Las claves se generan con un generador criptográfico, se guardan solo en la carpeta privada (fuera de Git) y en
      hardware/estacion_tres_canecas/configuracion_privada.h (excluido por .gitignore). Nunca se imprimen.
    - Es repetible: conserva cualquier clave, JSON o cabecera que ya exista. Para rehacer la cabecera (por ejemplo, tras
      medir pines y ángulos) usa -RehacerCabecera; las claves se toman otra vez de los archivos .bin, así que siguen
      coincidiendo con los bridges.
    - Los pines quedan en SinPin y calibracionVerificada en false hasta que se pasen -Pines y -Calibradas: sin ellos el
      firmware deja cada tapa fuera de servicio y no mueve nada.
.EXAMPLE
    .\Preparar-Estacion-Fisica.ps1 -Puerto COM3
.EXAMPLE
    .\Preparar-Estacion-Fisica.ps1 -Puerto COM3 -RehacerCabecera -Pines @{ blanca = '18,19' } -Angulos @{ blanca = '0,0;90,90' } -Calibradas blanca
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [ValidatePattern('^COM[1-9][0-9]{0,3}$')] [string] $Puerto,
    [string] $DirectorioPrivado = (Join-Path ([Environment]::GetFolderPath('CommonApplicationData')) 'EcoAccessP3Framework\Bridge'),
    [hashtable] $Pines = @{},
    [hashtable] $Angulos = @{},
    [ValidateSet('blanca', 'verde', 'negra')] [string[]] $Calibradas = @(),
    [switch] $RehacerCabecera
)
$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$utf8 = New-Object Text.UTF8Encoding($false)

# Canecas registradas en Oracle Cloud (QR compartido EcoAccessCanecaPiloto1). No se inventan otras.
$canecas = @(
    [ordered]@{ Color = 'blanca'; Id = 1093; Eventos = 8182; Comandos = 8183 },
    [ordered]@{ Color = 'verde';  Id = 1400; Eventos = 8184; Comandos = 8185 },
    [ordered]@{ Color = 'negra';  Id = 1401; Eventos = 8186; Comandos = 8187 }
)
$permitidos = 16, 17, 18, 19, 21, 23

if ($DirectorioPrivado -match '^\\\\' -or (Test-Path (Join-Path $DirectorioPrivado '.git'))) { throw 'La carpeta privada no puede ser de red ni de Git.' }
New-Item -ItemType Directory -Force $DirectorioPrivado | Out-Null

foreach ($c in $canecas) {
    $color = $c.Color
    $archivoClave = Join-Path $DirectorioPrivado "clave-$color.bin"
    if (-not (Test-Path $archivoClave)) {
        $bytes = New-Object byte[] 32
        $rng = [Security.Cryptography.RandomNumberGenerator]::Create(); $rng.GetBytes($bytes); $rng.Dispose()
        [IO.File]::WriteAllBytes($archivoClave, $bytes); [Array]::Clear($bytes, 0, 32)
        Write-Host "Clave nueva para la caneca $color (32 bytes)."
    } elseif ((Get-Item $archivoClave).Length -notin 32..128) { throw "La clave de la caneca $color tiene una longitud inválida; no se modifica." }
    else { Write-Host "Se conserva la clave existente de la caneca $color." }

    $json = Join-Path $DirectorioPrivado "bridge-$color.json"
    if (-not (Test-Path $json)) {
        $bridge = [ordered]@{ Bridge = [ordered]@{
            IdContenedor = "$($c.Id)"; Dispositivo = "esp32-piloto-$color"; VersionClave = '1'
            ArchivoClaveHmac = $archivoClave
            Eventos = "http://127.0.0.1:$($c.Eventos)/eventos/"; Comandos = "http://127.0.0.1:$($c.Comandos)/comandos/"
            DirectorioEstado = (Join-Path $DirectorioPrivado "estado-$color"); PuertoSerial = $Puerto } }
        [IO.File]::WriteAllText($json, ($bridge | ConvertTo-Json -Depth 4), $utf8)
        Write-Host "Configuración creada: bridge-$color.json"
    } else {
        $existente = Get-Content $json -Raw -Encoding UTF8 | ConvertFrom-Json
        if ($existente.Bridge.PuertoSerial -ne $Puerto) { Write-Warning "bridge-$color.json usa $($existente.Bridge.PuertoSerial); no se cambia a $Puerto." }
        Write-Host "Se conserva bridge-$color.json."
    }
}

function Par([string] $texto, [string] $que, [int] $min, [int] $max) {
    $v = @($texto -split ',' | ForEach-Object { [int]$_.Trim() })
    if ($v.Count -ne 2 -or ($v | Where-Object { $_ -lt $min -or $_ -gt $max })) { throw "$que inválido: '$texto'." }
    return $v
}

$cabecera = Join-Path $raiz 'hardware\estacion_tres_canecas\configuracion_privada.h'
if ((Test-Path $cabecera) -and -not $RehacerCabecera) {
    Write-Host 'Se conserva configuracion_privada.h (usa -RehacerCabecera para generarla otra vez).'
    return
}
$filas = foreach ($c in $canecas) {
    $color = $c.Color
    $bytes = [IO.File]::ReadAllBytes((Join-Path $DirectorioPrivado "clave-$color.bin"))
    $b64 = [Convert]::ToBase64String($bytes); [Array]::Clear($bytes, 0, $bytes.Length)
    $pin = 'ecoaccess::SinPin, ecoaccess::SinPin'
    if ($Pines.ContainsKey($color)) {
        $p = Par $Pines[$color] "Pines de $color" 0 39
        if ($p | Where-Object { $_ -notin $permitidos }) { throw "Pines de $color fuera de 16, 17, 18, 19, 21, 23." }
        $pin = "$($p[0]), $($p[1])"
    }
    $cerrado = '0, 0'; $abierto = '90, 90'
    if ($Angulos.ContainsKey($color)) {
        $partes = $Angulos[$color] -split ';'
        if ($partes.Count -ne 2) { throw "Ángulos de ${color}: usa 'cerrado1,cerrado2;abierto1,abierto2'." }
        $cerrado = (Par $partes[0] "Cerrado de $color" 0 180) -join ', '
        $abierto = (Par $partes[1] "Abierto de $color" 0 180) -join ', '
    }
    $calibrada = if ($color -in $Calibradas) { if (-not $Pines.ContainsKey($color)) { throw "No se puede marcar $color como calibrada sin pines." }; 'true' } else { 'false' }
    "  {`"esp32-piloto-$color`", $($c.Id), 1, `"$b64`", {$pin}, {$cerrado}, {$abierto}, $calibrada},"
}
$contenido = @"
#pragma once
// GENERADO por scripts/instalacion/Preparar-Estacion-Fisica.ps1. PRIVADO: contiene las claves HMAC de las tres
// canecas. Está excluido de Git (.gitignore); no lo copies, no lo publiques y no lo pegues en un chat.
// dispositivo, caneca, versión, clave, pines, cerrado, abierto, calibrada
static const ecoaccess::Tapa TAPAS[] = {
$($filas -join "`n")
};
static const unsigned ABIERTO_MS = 10000; // Ventana de depósito: 10 segundos desde la apertura aplicada.
static const bool RECUPERACION_MANUAL_VERIFICADA[] = {false, false, false};
"@
[IO.File]::WriteAllText($cabecera, $contenido, $utf8)
& git -C $raiz check-ignore -q $cabecera
if ($LASTEXITCODE -ne 0) { Remove-Item $cabecera; throw 'configuracion_privada.h no está excluida de Git; se eliminó por seguridad.' }
Write-Host "Cabecera privada escrita (excluida de Git). Tapas calibradas: $(if ($Calibradas) { $Calibradas -join ', ' } else { 'ninguna' })."

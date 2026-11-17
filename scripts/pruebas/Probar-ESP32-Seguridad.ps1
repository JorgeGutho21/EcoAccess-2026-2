<#
.SYNOPSIS
    Pruebas de seguridad contra el ESP32 REAL con estacion_tres_canecas cargado. No mueve motores.
.DESCRIPTION
    Habla por USB serie (115200) con la placa y comprueba que rechaza todo lo que no debe ejecutar:
    texto basura, firma falsa, identidad desconocida, desafío ajeno o repetido, línea excesiva y una orden bien firmada
    para una tapa que sigue fuera de servicio (sin pines o sin calibrar). Cada desafío es nuevo y de 32 hex.
    Úsalo SOLO mientras ninguna tapa esté calibrada: con una tapa habilitada, la prueba firmada podría moverla.
    La clave se lee del archivo privado del bridge y nunca se imprime.
.EXAMPLE
    .\Probar-ESP32-Seguridad.ps1 -Puerto COM3
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [ValidatePattern('^COM[1-9][0-9]{0,3}$')] [string] $Puerto,
    [string] $DirectorioPrivado = (Join-Path ([Environment]::GetFolderPath('CommonApplicationData')) 'EcoAccessP3Framework\Bridge'),
    [string] $Salida
)
$ErrorActionPreference = 'Stop'
$cabecera = Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) 'hardware\estacion_tres_canecas\configuracion_privada.h'
if ((Test-Path $cabecera) -and (Select-String -Path $cabecera -Pattern '\btrue\}' -Quiet)) {
    throw 'Hay al menos una tapa calibrada en configuracion_privada.h: esta prueba podría moverla. Úsala solo sin tapas habilitadas.'
}

$resultados = New-Object System.Collections.Generic.List[object]
function Anotar([string] $prueba, [bool] $ok, [string] $detalle) {
    $resultados.Add([pscustomobject]@{ Prueba = $prueba; Resultado = $(if ($ok) { 'SUPERADA' } else { 'FALLIDA' }); Detalle = $detalle })
}

$p = New-Object System.IO.Ports.SerialPort $Puerto, 115200
$p.DtrEnable = $false; $p.RtsEnable = $false; $p.NewLine = "`n"; $p.ReadTimeout = 3000; $p.WriteTimeout = 2000
$p.Open()
try {
    Start-Sleep -Milliseconds 400; $p.DiscardInBuffer()
    function Intercambiar([string] $linea) { $p.WriteLine($linea); return $p.ReadLine().TrimEnd("`r") }
    function Desafio { $r = Intercambiar '?'; $m = [regex]::Match($r, '^\{"ecoaccessDesafio":"([0-9a-f]{32})"\}$'); if ($m.Success) { $m.Groups[1].Value } else { $null } }

    $d1 = Desafio; $d2 = Desafio
    Anotar 'Desafío con formato correcto' ($null -ne $d1) 'Respuesta {"ecoaccessDesafio":"<32 hex>"}'
    Anotar 'Cada desafío es nuevo' ($d1 -and $d2 -and $d1 -ne $d2) 'Dos peticiones seguidas dan valores distintos'

    $r = Intercambiar 'abrir blanca'
    Anotar 'Texto basura rechazado' ($r -eq '{"error":"rechazado"}') $r

    $r = Intercambiar '{"version":1,"dispositivo":"esp32-piloto-blanca"}'
    Anotar 'Sobre incompleto rechazado' ($r -eq '{"error":"rechazado"}') $r

    function Sobre([string] $dispositivo, [byte[]] $clave, [int] $caneca, [string] $nonce, [int] $orden) {
        $cuerpo = [ordered]@{ comando = [ordered]@{ idOrden = $orden; idContenedor = $caneca; claveIdempotencia = "prueba-seguridad-$orden"; venceUtc = (Get-Date).ToUniversalTime().AddSeconds(5).ToString('o') }; desafio = $nonce; validezMs = 3000 }
        $datos = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes(($cuerpo | ConvertTo-Json -Compress -Depth 4)))
        $h = New-Object System.Security.Cryptography.HMACSHA256 (, $clave)
        $firma = [Convert]::ToBase64String($h.ComputeHash([Text.Encoding]::UTF8.GetBytes("ecoaccess-p3/v1`n$dispositivo`n1`nactuacion-serial`n$datos"))); $h.Dispose()
        return ([ordered]@{ version = 1; dispositivo = $dispositivo; versionClave = 1; tipo = 'actuacion-serial'; datos = $datos; firma = $firma } | ConvertTo-Json -Compress)
    }
    $orden = [int]((Get-Date).ToUniversalTime() - [datetime]'2026-01-01').TotalSeconds

    $falsa = New-Object byte[] 32; [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($falsa)
    $r = Intercambiar (Sobre 'esp32-piloto-blanca' $falsa 1093 (Desafio) ($orden + 1))
    Anotar 'Firma con clave falsa rechazada' ($r -eq '{"error":"rechazado"}') $r

    $r = Intercambiar (Sobre 'esp32-intruso' $falsa 1093 (Desafio) ($orden + 2))
    Anotar 'Identidad desconocida rechazada' ($r -eq '{"error":"rechazado"}') $r

    $real = [IO.File]::ReadAllBytes((Join-Path $DirectorioPrivado 'clave-blanca.bin'))
    try {
        $r = Intercambiar (Sobre 'esp32-piloto-blanca' $real 1093 ('0' * 32) ($orden + 3))
        Anotar 'Desafío inventado rechazado (clave real)' ($r -eq '{"error":"rechazado"}') $r

        $viejo = Desafio; [void](Desafio)
        $r = Intercambiar (Sobre 'esp32-piloto-blanca' $real 1093 $viejo ($orden + 4))
        Anotar 'Desafío anterior ya no vale (clave real)' ($r -eq '{"error":"rechazado"}') $r

        $r = Intercambiar (Sobre 'esp32-piloto-blanca' $real 1400 (Desafio) ($orden + 5))
        Anotar 'Orden de la verde firmada por la blanca rechazada' ($r -eq '{"error":"rechazado"}') $r

        $r = Intercambiar (Sobre 'esp32-piloto-blanca' $real 1093 (Desafio) ($orden + 6))
        Anotar 'Orden correcta para tapa fuera de servicio: no se ejecuta' ($r -eq '{"error":"rechazado"}') $r
    } finally { [Array]::Clear($real, 0, $real.Length) }

    $p.Write(('x' * 5000) + "`n"); Start-Sleep -Milliseconds 300
    $basura = $p.ReadExisting()
    $sigue = Desafio
    Anotar 'Línea de 5000 bytes descartada sin bloquear la placa' ([string]::IsNullOrEmpty($basura) -and $null -ne $sigue) "respuesta='$basura', desafío posterior=$([bool]$sigue)"
}
finally { $p.Close() }

$resultados | Format-Table -AutoSize | Out-String -Width 200 | Write-Host
$fallidas = @($resultados | Where-Object Resultado -eq 'FALLIDA').Count
Write-Host "Superadas: $($resultados.Count - $fallidas) de $($resultados.Count)"
if ($Salida) { $resultados | ConvertTo-Json | Set-Content -Path $Salida -Encoding UTF8 }
exit $fallidas

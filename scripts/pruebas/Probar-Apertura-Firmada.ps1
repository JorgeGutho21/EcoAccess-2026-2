<#
.SYNOPSIS
    Prueba FÍSICA supervisada del firmware estacion_tres_canecas: una orden firmada con HMAC por caneca, recibos
    autenticados de apertura y cierre, cronometría de la ventana y rechazo de la orden repetida.
.DESCRIPTION
    Hace el papel del bridge (mismo sobre ecoaccess-p3/v1 que ActuacionSerial): pide desafío, firma la orden con la clave
    privada de esa caneca, valida la firma de cada recibo y mide el tiempo entre «pwm-apertura-aplicada» y
    «pwm-completado-sin-sensor». No pasa por Oracle ni por Telegram: prueba el último tramo PC → ESP32 → servo.
    Los identificadores de orden empiezan en 2 000 000 000 para no chocar con los de Oracle (la reserva del ESP32 es
    permanente). La clave nunca se imprime. Una persona debe mirar las tapas: el recibo confirma PWM, no la posición física.
.EXAMPLE
    .\Probar-Apertura-Firmada.ps1 -Puerto COM3 -Canecas blanca,negra,verde -Salida evidencia.json
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [ValidatePattern('^COM[1-9][0-9]{0,3}$')] [string] $Puerto,
    [ValidateSet('blanca', 'verde', 'negra')] [string[]] $Canecas = @('blanca'),
    [string] $DirectorioPrivado = (Join-Path ([Environment]::GetFolderPath('CommonApplicationData')) 'EcoAccessP3Framework\Bridge'),
    [ValidateRange(0, 30)] [int] $PausaS = 5,
    [switch] $ProbarDuplicado,
    [string] $Salida
)
$ErrorActionPreference = 'Stop'
$ids = @{ blanca = 1093; verde = 1400; negra = 1401 }
$utf8 = [Text.Encoding]::UTF8
$resultados = New-Object System.Collections.Generic.List[object]

function Firma([byte[]] $clave, [string] $dispositivo, [string] $tipo, [string] $datos) {
    $h = New-Object System.Security.Cryptography.HMACSHA256 (, $clave)
    try { return $h.ComputeHash($utf8.GetBytes("ecoaccess-p3/v1`n$dispositivo`n1`n$tipo`n$datos")) } finally { $h.Dispose() }
}

$p = New-Object System.IO.Ports.SerialPort $Puerto, 115200
$p.DtrEnable = $false; $p.RtsEnable = $false; $p.NewLine = "`n"; $p.ReadTimeout = 3000
try { $p.Open() } catch { throw "No se pudo abrir ${Puerto}: cierra el Monitor Serie y los bridges." }
try {
    function Desafio {
        for ($i = 0; $i -lt 5; $i++) {
            $p.DiscardInBuffer(); $p.WriteLine('?')
            try { $r = $p.ReadLine().TrimEnd("`r") } catch [TimeoutException] { continue }
            $m = [regex]::Match($r, '^\{"ecoaccessDesafio":"([0-9a-f]{32})"\}$')
            if ($m.Success) { return $m.Groups[1].Value }
            Start-Sleep -Milliseconds 500
        }
        throw 'La placa no entrega desafíos: ¿está cargado estacion_tres_canecas?'
    }
    function Recibo([string] $linea, [byte[]] $clave, [string] $dispositivo, [string] $nonce, [byte[]] $paquete) {
        if ($linea -eq '{"error":"rechazado"}') { return [pscustomobject]@{ Valido = $true; Aceptado = $false; Estado = 'rechazado-sin-autenticar' } }
        $s = $linea | ConvertFrom-Json
        $esperada = Firma $clave $dispositivo 'recepcion-serial' $s.datos
        $ok = $s.dispositivo -eq $dispositivo -and $s.tipo -eq 'recepcion-serial' -and [Convert]::ToBase64String($esperada) -eq $s.firma
        $c = $utf8.GetString([Convert]::FromBase64String($s.datos)) | ConvertFrom-Json
        $sha = -join ([Security.Cryptography.SHA256]::Create().ComputeHash($paquete) | ForEach-Object { $_.ToString('X2') })
        $ok = $ok -and $c.desafio -eq $nonce -and $c.sha256Paquete -eq $sha
        return [pscustomobject]@{ Valido = $ok; Aceptado = [bool]$c.aceptado; Estado = $c.estado }
    }
    function Enviar([string] $color, [int] $orden, [byte[]] $clave) {
        $dispositivo = "esp32-piloto-$color"
        $nonce = Desafio
        $cuerpo = [ordered]@{ comando = [ordered]@{ idOrden = $orden; idContenedor = $ids[$color]; claveIdempotencia = "prueba-fisica-$orden"; venceUtc = (Get-Date).ToUniversalTime().AddSeconds(60).ToString('o') }; desafio = $nonce; validezMs = 4000 }
        $datos = [Convert]::ToBase64String($utf8.GetBytes(($cuerpo | ConvertTo-Json -Compress -Depth 4)))
        $sobre = [ordered]@{ version = 1; dispositivo = $dispositivo; versionClave = 1; tipo = 'actuacion-serial'; datos = $datos; firma = [Convert]::ToBase64String((Firma $clave $dispositivo 'actuacion-serial' $datos)) } | ConvertTo-Json -Compress
        $paquete = $utf8.GetBytes($sobre)
        $reloj = [Diagnostics.Stopwatch]::StartNew()
        $p.WriteLine($sobre)
        $p.ReadTimeout = 5000
        $r1 = Recibo ($p.ReadLine().TrimEnd("`r")) $clave $dispositivo $nonce $paquete
        $tApertura = $reloj.Elapsed.TotalSeconds
        $r2 = $null; $tCierre = $null
        if ($r1.Aceptado) {
            $p.ReadTimeout = 20000
            $r2 = Recibo ($p.ReadLine().TrimEnd("`r")) $clave $dispositivo $nonce $paquete
            $tCierre = $reloj.Elapsed.TotalSeconds
        }
        $p.ReadTimeout = 3000
        return [pscustomobject]@{ Apertura = $r1; Cierre = $r2; TApertura = $tApertura; TCierre = $tCierre }
    }

    $base = 2000000000 + [int](((Get-Date).ToUniversalTime() - [datetime]'2026-11-11').TotalSeconds) * 4
    $n = 0
    foreach ($color in $Canecas) {
        $n++
        $clave = [IO.File]::ReadAllBytes((Join-Path $DirectorioPrivado "clave-$color.bin"))
        try {
            $orden = $base + $n
            $hora = Get-Date -Format 'HH:mm:ss'
            Write-Host "[$hora] $color (caneca $($ids[$color])): enviando orden firmada $orden…"
            $e = Enviar $color $orden $clave
            $ventana = if ($e.TCierre) { [math]::Round($e.TCierre - $e.TApertura, 2) } else { $null }
            $ok = $e.Apertura.Valido -and $e.Apertura.Aceptado -and $e.Apertura.Estado -eq 'pwm-apertura-aplicada' `
                -and $e.Cierre.Valido -and $e.Cierre.Aceptado -and $e.Cierre.Estado -eq 'pwm-completado-sin-sensor'
            Write-Host ("    apertura: {0} (firma {1}) a {2:N2} s · cierre: {3} (firma {4}) · ventana medida: {5} s" -f $e.Apertura.Estado, $(if ($e.Apertura.Valido) { 'válida' } else { 'INVÁLIDA' }), $e.TApertura, $e.Cierre.Estado, $(if ($e.Cierre.Valido) { 'válida' } else { 'INVÁLIDA' }), $ventana)
            $resultados.Add([pscustomobject]@{ Hora = $hora; Caneca = $color; IdContenedor = $ids[$color]; Orden = $orden; Prueba = 'orden firmada'; Resultado = $(if ($ok) { 'SUPERADA' } else { 'FALLIDA' }); EstadoApertura = $e.Apertura.Estado; EstadoCierre = $e.Cierre.Estado; VentanaSegundos = $ventana })
            if ($ProbarDuplicado) {
                Start-Sleep -Seconds 2
                $d = Enviar $color $orden $clave
                $okd = $d.Apertura.Valido -and -not $d.Apertura.Aceptado -and $null -eq $d.Cierre
                Write-Host "    misma orden otra vez: $($d.Apertura.Estado) → $(if ($okd) { 'rechazada, no abre' } else { '¡FALLO!' })"
                $resultados.Add([pscustomobject]@{ Hora = (Get-Date -Format 'HH:mm:ss'); Caneca = $color; IdContenedor = $ids[$color]; Orden = $orden; Prueba = 'orden duplicada'; Resultado = $(if ($okd) { 'SUPERADA' } else { 'FALLIDA' }); EstadoApertura = $d.Apertura.Estado; EstadoCierre = $null; VentanaSegundos = $null })
            }
        } finally { [Array]::Clear($clave, 0, $clave.Length) }
        if ($n -lt $Canecas.Count) { Start-Sleep -Seconds $PausaS }
    }
}
finally { $p.Close() }

if ($Salida) {
    $previos = if (Test-Path $Salida) { @(Get-Content $Salida -Raw | ConvertFrom-Json) } else { @() }
    @($previos) + $resultados | ConvertTo-Json -Depth 3 | Set-Content $Salida -Encoding UTF8
}
$fallidas = @($resultados | Where-Object Resultado -eq 'FALLIDA').Count
Write-Host "Superadas: $($resultados.Count - $fallidas) de $($resultados.Count). Recuerda: el recibo confirma PWM; la posición de la tapa la confirma quien observa."
exit $fallidas

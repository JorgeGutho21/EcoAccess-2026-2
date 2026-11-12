<#
.SYNOPSIS
    Mueve UN servo con el banco hardware/calibracion_seis_servos cargado en el ESP32 y deja constancia en un registro.
.DESCRIPTION
    Uso supervisado: tapas desacopladas, fuente externa con tierra común y alguien mirando el servo.
    - Solo un pin por llamada; al terminar envía PARAR (sin PWM en ningún pin).
    - Con -Barrido recorre Desde → ángulo → Desde (30 por defecto; 0 reproduce el código original del compañero: 0 → 60 → 0).
    - -Todos recorre 17, 16, 21, 23, 18 y 19 en orden, uno a la vez, con 3 s de pausa: sirve para descubrir el mapeo.
    - Detecta un reinicio de la placa (caída de tensión) si vuelve a aparecer el saludo del banco.
    El registro (CSV) solo dice qué ORDENÓ el PC y qué CONFIRMÓ el firmware; lo que se movió físicamente lo anota la persona
    con -Observacion o después en el documento de evidencias.
.EXAMPLE
    .\Mover-Servo.ps1 -Puerto COM3 -Pin 17 -Angulo 90
.EXAMPLE
    .\Mover-Servo.ps1 -Puerto COM3 -Todos -Barrido -Angulo 90
#>
[CmdletBinding(DefaultParameterSetName = 'Uno')]
param(
    [Parameter(Mandatory)] [ValidatePattern('^COM[1-9][0-9]{0,3}$')] [string] $Puerto,
    [Parameter(Mandatory, ParameterSetName = 'Uno')] [ValidateSet(16, 17, 18, 19, 21, 23)] [int] $Pin,
    [Parameter(Mandatory, ParameterSetName = 'Todos')] [switch] $Todos,
    [ValidateRange(0, 120)] [int] $Angulo = 90,
    [ValidateRange(200, 5000)] [int] $SostenerMs = 2000,
    [switch] $Barrido,
    [ValidateRange(1, 30)] [int] $PausaS = 3,
    [ValidateRange(0, 120)] [int] $Desde = 30,
    [string] $Observacion = '',
    [string] $Registro = (Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) 'docs\evidencias\esp32_fisico\movimientos_servos.csv')
)
$ErrorActionPreference = 'Stop'
$pines = if ($Todos) { 17, 16, 21, 23, 18, 19 } else { , $Pin }

$p = New-Object System.IO.Ports.SerialPort $Puerto, 115200
$p.DtrEnable = $false; $p.RtsEnable = $false; $p.NewLine = "`n"
try { $p.Open() } catch { throw "No se pudo abrir $Puerto. Cierra el Monitor Serie del IDE y los bridges: el puerto admite un solo dueño." }
try {
    # Abrir el puerto puede reiniciar la placa (auto-reset del CP210x): reintentar el saludo mientras arranca.
    $listo = $false
    for ($i = 0; $i -lt 4 -and -not $listo; $i++) {
        Start-Sleep -Milliseconds 800; $p.DiscardInBuffer()
        $p.WriteLine('ESTADO'); Start-Sleep -Milliseconds 500
        $listo = $p.ReadExisting() -match 'armado='
    }
    if (-not $listo) { throw 'La placa no responde como banco de calibración. Carga hardware/calibracion_seis_servos primero.' }
    if (-not (Test-Path $Registro)) { 'fecha,pin,angulo,barrido,confirmado_firmware,reinicio_detectado,observacion' | Set-Content $Registro -Encoding UTF8 }
    $n = 0
    foreach ($g in $pines) {
        $n++
        $p.WriteLine('ARMAR'); Start-Sleep -Milliseconds 200
        $angulos = if ($Barrido) { $Desde, $Angulo, $Desde } else { , $Angulo }
        foreach ($a in $angulos) { $p.WriteLine("S $g $a"); Start-Sleep -Milliseconds $SostenerMs }
        $p.WriteLine('PARAR'); Start-Sleep -Milliseconds 400
        $salida = $p.ReadExisting()
        $confirmados = ([regex]::Matches($salida, "MOVIDO: GPIO $g ")).Count
        $reinicio = $salida -match 'Banco de calibracion|rst:|brownout'
        $hora = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
        Write-Host ("#{0} {1} GPIO {2} -> {3}°: confirmados {4}/{5}{6}" -f $n, $hora, $g, $Angulo, $confirmados, $angulos.Count, $(if ($reinicio) { ' ¡REINICIO DE LA PLACA: revisa la fuente!' } else { '' }))
        ('{0},{1},{2},{3},{4},{5},"{6}"' -f $hora, $g, $Angulo, [bool]$Barrido, "$confirmados/$($angulos.Count)", $reinicio, ($Observacion -replace '"', "'")) | Add-Content $Registro -Encoding UTF8
        if ($reinicio) { break }
        if ($Todos) { Start-Sleep -Seconds $PausaS }
    }
}
finally {
    if ($p.IsOpen) { try { $p.WriteLine('PARAR') } catch { } ; $p.Close() }
}

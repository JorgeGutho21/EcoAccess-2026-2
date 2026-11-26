# EcoAccess P3 · Inicia la pasarela ciudadana en el equipo anfitrión (el que tiene el ESP32 y el panel de canecas).
# Requisitos: panel de canecas en ejecución (http://localhost:8090) y cloudflared instalado para el túnel HTTPS.
# Uso:  powershell -ExecutionPolicy Bypass -File scripts\estacion\Iniciar-Estacion-Ciudadana.ps1 [-SinTunel]
param([switch]$SinTunel)
$ErrorActionPreference = 'Stop'
$raiz = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$exe = Join-Path $raiz 'src\EcoAccess.P3.PasarelaCiudadana\bin\Release\net48\EcoAccess.P3.PasarelaCiudadana.exe'
if (-not (Test-Path $exe)) {
    Write-Host 'Compilando la pasarela ciudadana…'
    dotnet build (Join-Path $raiz 'src\EcoAccess.P3.PasarelaCiudadana') -c Release -v q --nologo | Out-Null
}
try { Invoke-RestMethod http://127.0.0.1:8090/api/estacion/estado -TimeoutSec 3 | Out-Null }
catch { Write-Warning 'El panel de canecas (localhost:8090) no responde: la página mostrará la estación como desconectada.' }
$argumentos = @()
if ($SinTunel) { $argumentos += '--sin-tunel' }
Start-Process -FilePath $exe -ArgumentList $argumentos
Start-Sleep -Seconds 2
Start-Process 'http://localhost:8093/'
Write-Host 'Pasarela iniciada. En la página del operador pulsa «Habilitar 2 horas y generar QR».'

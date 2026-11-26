<#
.SYNOPSIS
    Verificador autonomo de entorno ECOACCESS P3 para computadores de companeros y estacion principal (Fase 6).
    No depende de OneDrive ni de rutas propias de ningun equipo. No modifica datos ni abre canecas.
#>
[CmdletBinding()]
param(
    [ValidateSet('EstacionCompanero', 'EstacionPrincipal')]
    [string]$Perfil = 'EstacionCompanero',
    [string]$CarpetaInstalacion = (Join-Path $env:LOCALAPPDATA 'Programs\EcoAccess P3')
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Write-Host '================================================================' -ForegroundColor Cyan
Write-Host "  ECOACCESS P3 - DIAGNOSTICO DE EQUIPO ($Perfil)" -ForegroundColor Cyan
Write-Host '================================================================' -ForegroundColor Cyan

$os = [System.Environment]::OSVersion.VersionString
Write-Host "Sistema operativo  : $os"

$netRel = (Get-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full' -Name Release -ErrorAction SilentlyContinue).Release
$netOk = ($netRel -ge 528040)
Write-Host (".NET Framework 4.8 : " + $(if ($netOk) { "OK (Release $netRel)" } else { 'FALTA (descargar .NET Framework 4.8 Runtime de Microsoft)' }))

$exe = Join-Path $CarpetaInstalacion 'EcoAccess.P3.Presentacion.exe'
Write-Host ("Instalacion local  : " + $(if (Test-Path -LiteralPath $exe) { "OK ($CarpetaInstalacion)" } else { "NO INSTALADA en $CarpetaInstalacion" }))

$escritorio = [Environment]::GetFolderPath('Desktop')
$esperados = @('ECOACCESS P3.lnk', 'ECOACCESS ESP32.lnk', 'ECOACCESS DOS CLICS.lnk', 'ECOACCESS WEB.lnk')
$presentes = @(Get-ChildItem -LiteralPath $escritorio -Filter '*.lnk' -ErrorAction SilentlyContinue | Where-Object { $_.BaseName -match '(?i)eco\s*access' } | ForEach-Object Name)
$faltan = @($esperados | Where-Object { $presentes -cnotcontains $_ })
$sobran = @($presentes | Where-Object { $esperados -cnotcontains $_ })
$estadoAccesos = if (-not $faltan -and -not $sobran) { "OK ($($esperados.Count): $($esperados -join ', '))" } else { "REVISAR (faltan: $($faltan -join ', '); sobran: $($sobran -join ', ')) - ejecuta Crear-AccesosDirectos.ps1" }
Write-Host ("Accesos directos   : " + $estadoAccesos)

$cam = @(Get-PnpDevice -Class Camera, Image -Status OK -ErrorAction SilentlyContinue)
Write-Host ("Camara             : " + $(if ($cam.Count -gt 0) { "DISPONIBLE ($($cam[0].FriendlyName))" } else { 'NO DETECTADA (puedes cargar fotos desde archivo)' }))

try {
    $ver = (Invoke-RestMethod -Uri 'http://127.0.0.1:11434/api/version' -TimeoutSec 3).version
    $tags = (Invoke-RestMethod -Uri 'http://127.0.0.1:11434/api/tags' -TimeoutSec 3).models | ForEach-Object { $_.name }
    $vis = @($tags | Where-Object { $_ -match 'lfm3-vl|qwen3-vl' })
    Write-Host "Ollama local       : CONECTADO (v$ver)"
    Write-Host ("Modelos visuales   : " + $(if ($vis.Count -gt 0) { $vis -join ', ' } else { 'NINGUNO (ejecuta: ollama pull lfm3-vl)' }))
} catch {
    Write-Host 'Ollama local       : NO INSTALADO O DETENIDO (opcional; la app funciona en modo catalogo/manual/simulacion)'
}

$candidatasWallet = @(
    $env:TNS_ADMIN,
    [Environment]::GetEnvironmentVariable('TNS_ADMIN', 'User'),
    (Join-Path $env:LOCALAPPDATA 'EcoAccessP3\wallet'),
    (Join-Path $env:USERPROFILE '.oracle\wallets\EcoAccess')
) | Where-Object { $_ }
$wallet = $candidatasWallet | Where-Object { Test-Path -LiteralPath (Join-Path $_ 'tnsnames.ora') } | Select-Object -First 1
Write-Host ("Wallet Oracle      : " + $(if ($wallet) { "CONFIGURADO ($wallet)" } else { 'PENDIENTE (solicita Wallet_*.zip por canal privado y ejecuta Instalar-EcoAccess.ps1 -Wallet <ruta>)' }))

if ($Perfil -eq 'EstacionCompanero') {
    Write-Host 'Hardware COM       : NO APLICA (este equipo nunca abre el puerto del ESP32; habla con el panel del anfitrion)'
    $cfgAnfitrion = Join-Path $env:LOCALAPPDATA 'EcoAccessP3\anfitrion.txt'
    $lineas = if (Test-Path -LiteralPath $cfgAnfitrion) { Get-Content -LiteralPath $cfgAnfitrion } else { @() }
    $urlA = ($lineas | Where-Object { $_ -match '^\s*url\s*=' } | Select-Object -First 1) -replace '^\s*url\s*=\s*', ''
    $claveA = ($lineas | Where-Object { $_ -match '^\s*clave\s*=' } | Select-Object -First 1) -replace '^\s*clave\s*=\s*', ''
    if (-not $urlA) {
        Write-Host 'Anfitrion ESP32    : SIN CONFIGURAR (la app usa simulacion declarada; ejecuta Configurar-Anfitrion.ps1 -Anfitrion <IP>)'
    } else {
        $e = try { Invoke-RestMethod -Uri "$urlA/api/estacion/estado" -Headers @{ 'X-Clave' = $claveA } -TimeoutSec 5 } catch { $null }
        Write-Host ("Anfitrion ESP32    : " + $(if ($e) { "OK ($urlA; ESP32 $(if ($e.conectado) { 'conectado' } else { 'desconectado' }); $(if ($e.enServicio) { 'en servicio' } else { 'en pausa' }))" } else { "SIN RESPUESTA ($urlA)" }))
    }
} else {
    $puertos = @([System.IO.Ports.SerialPort]::GetPortNames())
    Write-Host ("Puertos seriales   : " + $(if ($puertos.Count -gt 0) { $puertos -join ', ' } else { 'NINGUNO DETECTADO' }))
}
Write-Host 'Bot de Telegram    : https://t.me/EcoAccessP3_JG_bot (usar desde celular/Telegram sin ejecutar otro --polling local)'
Write-Host '================================================================' -ForegroundColor Cyan

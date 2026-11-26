<#
.SYNOPSIS
    Conecta este computador con el equipo ANFITRIÓN de la Estación Oficial EcoAccess (el que tiene el ESP32 por USB).

.DESCRIPTION
    Solo hay un servicio central de hardware: el panel ECOACCESS ESP32 (EcoAccess.P3.PanelCanecas.exe, puerto 8090) del
    anfitrión, único dueño del puerto COM. Los demás equipos NUNCA abren un puerto COM: la app C#, ECOACCESS ESP32 y las
    pruebas de este equipo envían las órdenes a ese panel por la red local, con la clave de la estación.

    Guarda la dirección y la clave en %LOCALAPPDATA%\EcoAccessP3\anfitrion.txt (solo tu usuario de Windows; nunca en
    GitHub) y comprueba la conexión en modo lectura (no mueve ninguna tapa).
    · La clave está en el anfitrión: %ProgramData%\EcoAccessP3Framework\Panel\clave.txt (pídesela al anfitrión por un
      canal privado).
    · En el anfitrión debe haberse ejecutado una vez, como administrador, scripts\arranque\Habilitar-PanelEnRed.ps1.
    · Cambiar de anfitrión (el ESP32 pasa a otro computador) = volver a ejecutar este script con la nueva dirección.
    · -Quitar: este equipo deja de usar el anfitrión de red.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\instalacion\Configurar-Anfitrion.ps1 -Anfitrion 192.168.1.20
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\instalacion\Configurar-Anfitrion.ps1 -Quitar
#>
[CmdletBinding()]
param(
    [string]$Anfitrion,
    [int]$Puerto = 8090,
    [string]$Clave,
    [switch]$Quitar
)
$ErrorActionPreference = 'Stop'
$archivo = Join-Path $env:LOCALAPPDATA 'EcoAccessP3\anfitrion.txt'

if ($Quitar) {
    if (Test-Path -LiteralPath $archivo) { Remove-Item -LiteralPath $archivo -Force }
    Write-Host 'Este equipo ya no usa un anfitrión de red (la app vuelve a buscar el panel local o usa simulación declarada).'
    return
}
if (-not $Anfitrion) { $Anfitrion = Read-Host 'Dirección IP o nombre del equipo anfitrión (el que tiene el ESP32)' }
$Anfitrion = $Anfitrion.Trim() -replace '^https?://', '' -replace '[:/].*$', ''
if (-not $Anfitrion) { throw 'Falta la dirección del anfitrión.' }
if (-not $Clave) {
    $segura = Read-Host 'Clave de la estación (clave.txt del anfitrión)' -AsSecureString
    $Clave = [Runtime.InteropServices.Marshal]::PtrToStringBSTR([Runtime.InteropServices.Marshal]::SecureStringToBSTR($segura))
}
$Clave = $Clave.Trim()
if ($Clave.Length -lt 8) { throw 'La clave parece incompleta.' }
$url = "http://${Anfitrion}:$Puerto"

# Comprobación de solo lectura: estado de la estación con la clave (no mueve motores).
try {
    $estado = Invoke-RestMethod -Uri "$url/api/estacion/estado" -Headers @{ 'X-Clave' = $Clave } -TimeoutSec 6
} catch {
    $codigo = try { [int]$_.Exception.Response.StatusCode } catch { 0 }
    if ($codigo -eq 401) { throw "El anfitrión $url rechazó la clave. Verifica clave.txt en el anfitrión." }
    throw "No hay respuesta de $url. En el anfitrión: ejecuta Habilitar-PanelEnRed.ps1 (administrador), deja abierto ECOACCESS ESP32 y verifica que ambos equipos estén en la misma red privada."
}

New-Item -ItemType Directory -Force -Path (Split-Path $archivo) | Out-Null
$contenido = "# Anfitrión de la Estación Oficial EcoAccess (generado por Configurar-Anfitrion.ps1 el $(Get-Date -Format 'yyyy-MM-dd HH:mm')).`r`nurl=$url`r`nclave=$Clave`r`n"
[IO.File]::WriteAllText($archivo, $contenido, (New-Object Text.UTF8Encoding($false)))
# Solo el usuario actual puede leer la clave.
try {
    $acl = Get-Acl -LiteralPath $archivo
    $acl.SetAccessRuleProtection($true, $false)
    $acl.Access | ForEach-Object { [void]$acl.RemoveAccessRule($_) }
    $acl.AddAccessRule((New-Object Security.AccessControl.FileSystemAccessRule([Security.Principal.WindowsIdentity]::GetCurrent().Name, 'FullControl', 'Allow')))
    Set-Acl -LiteralPath $archivo -AclObject $acl
} catch { Write-Warning "No se pudieron restringir los permisos de $archivo ($($_.Exception.Message))." }

$servicio = if ($estado.enServicio) { 'en servicio' } elseif ($null -ne $estado.enServicio) { "en pausa ($($estado.pausa))" } else { 'sin dato de servicio (panel anterior)' }
Write-Host "Listo: este equipo usa el anfitrión $url" -ForegroundColor Green
Write-Host "  ESP32: $(if ($estado.conectado) { "conectado en $($estado.puerto)" } else { $estado.texto })"
Write-Host "  Estación: $servicio"
Write-Host '  ECOACCESS ESP32 de este equipo abrirá el panel del anfitrión (la primera vez pide esta misma clave).'
Write-Host '  Vuelve a crear los accesos: scripts\instalacion\Crear-AccesosDirectos.ps1'

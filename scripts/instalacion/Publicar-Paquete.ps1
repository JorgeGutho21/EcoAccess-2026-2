<#
.SYNOPSIS
    Genera el paquete distribuible para companeros: dist\EcoAccess-P3-<version>-<commit>.zip (Fases 6 y 12).
    Incluye aplicacion Release, Instalar-EcoAccess.ps1, Verificar-Equipo-EcoAccess.ps1, guia de instalacion,
    VERSION.txt y LEEME.txt. Nunca incluye wallets ni secretos.
#>
[CmdletBinding()]
param(
    [string]$Version = '3.1.0-exposoftware'
)
$ErrorActionPreference = 'Stop'
$raiz = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
Push-Location -LiteralPath $raiz
try {
    & dotnet build 'src\EcoAccess.P3.Presentacion\EcoAccess.P3.Presentacion.csproj' -c Release -nologo -v quiet
    if ($LASTEXITCODE -ne 0) { throw "La compilacion fallo (codigo $LASTEXITCODE)." }
    $commit = (& git rev-parse --short HEAD).Trim()
    $rama = (& git rev-parse --abbrev-ref HEAD).Trim()
    $nombre = "EcoAccess-P3-$Version-$commit"
    $carpeta = Join-Path $raiz "dist\$nombre"
    if (Test-Path -LiteralPath $carpeta) { Remove-Item -LiteralPath $carpeta -Recurse -Force }
    New-Item -ItemType Directory -Force -Path (Join-Path $carpeta 'aplicacion') | Out-Null
    & robocopy.exe 'src\EcoAccess.P3.Presentacion\bin\Release\net48' (Join-Path $carpeta 'aplicacion') /E /NFL /NDL /NJH /NJS /NP /XF *.pdb | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "No se pudo copiar la aplicacion (robocopy $LASTEXITCODE)." }

    Copy-Item 'scripts\instalacion\Instalar-EcoAccess.ps1' $carpeta
    Copy-Item 'scripts\instalacion\Verificar-Equipo-EcoAccess.ps1' $carpeta
    if (Test-Path 'assets\identidad\EcoAccess.ico') { Copy-Item 'assets\identidad\EcoAccess.ico' (Join-Path $carpeta 'aplicacion') }
    # Iconos de los cuatro accesos directos (ECOACCESS P3, ESP32, WEB y CHALLENGE): el instalador los busca junto a si mismo.
    foreach ($ico in 'EcoAccess.ico', 'EcoAccess_ESP32.ico', 'EcoAccess_WEB.ico', 'EcoAccess_CHALLENGE.ico') { if (Test-Path "assets\identidad\$ico") { Copy-Item "assets\identidad\$ico" $carpeta } }
    if (Test-Path 'docs\instalacion\GUIA_INSTALACION_COMPANEROS_EXPOSOFTWARE.md') {
        Copy-Item 'docs\instalacion\GUIA_INSTALACION_COMPANEROS_EXPOSOFTWARE.md' $carpeta
    }

    @"
VERSION=$Version
COMMIT=$commit
RAMA=$rama
FECHA_UTC=$((Get-Date).ToUniversalTime().ToString('O'))
REPOSITORIO=https://github.com/JorgeGutho21/EcoAccess-2026-2
"@ | Set-Content -LiteralPath (Join-Path $carpeta 'VERSION.txt') -Encoding UTF8

    @"
ECOACCESS P3 - Paquete Distribuible ($Version / commit $commit)

PERFIL RECOMENDADO PARA COMPANEROS: EstacionCompanero

1. Descomprime este .zip en cualquier carpeta local (sin depender de OneDrive).
2. Solicita al responsable del proyecto el archivo Wallet_*.zip de Oracle Cloud por canal privado (nunca en GitHub).
3. Ejecuta en PowerShell:
   powershell -ExecutionPolicy Bypass -File .\Instalar-EcoAccess.ps1 -Perfil EstacionCompanero -Wallet "<ruta del Wallet_*.zip>"
4. Verifica el equipo:
   powershell -ExecutionPolicy Bypass -File .\Verificar-Equipo-EcoAccess.ps1 -Perfil EstacionCompanero
5. Abre "ECOACCESS P3" desde tu Escritorio. La aplicacion pedira tu contrasena de esquema la primera vez
   y la guardara de forma privada en tu perfil de Windows.
6. Para usar el chatbot de Telegram desde tu celular o PC abre https://t.me/EcoAccessP3_JG_bot
   (NO ejecutes otra instancia de EcoAccess.P3.Bot --polling con el mismo token).
"@ | Set-Content -LiteralPath (Join-Path $carpeta 'LEEME.txt') -Encoding UTF8

    $zip = Join-Path $raiz "dist\$nombre.zip"
    if (Test-Path -LiteralPath $zip) { Remove-Item -LiteralPath $zip -Force }
    Compress-Archive -Path (Join-Path $carpeta '*') -DestinationPath $zip
    Write-Host "Paquete listo: $zip" -ForegroundColor Green
} finally { Pop-Location }

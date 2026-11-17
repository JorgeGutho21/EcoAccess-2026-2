<#
.SYNOPSIS
    Genera el paquete para compañeros que solo usan EcoAccess (no programan): dist\EcoAccess-P3-<commit>.zip.

.DESCRIPTION
    Compila la aplicación de escritorio en Release y arma un .zip con:
      aplicacion\            ejecutable y bibliotecas (sin secretos: la contraseña vive en el perfil de cada usuario)
      Instalar-EcoAccess.ps1 el mismo instalador del repositorio (detecta la carpeta «aplicacion» y no compila)
      LEEME.txt              pasos de instalación
    El paquete no incluye el wallet de Oracle Cloud: se entrega aparte, por un canal privado, al compañero autorizado.
    La carpeta dist\ está en .gitignore; el .zip se comparte como adjunto o como «Release» privada de GitHub.
#>
$ErrorActionPreference = 'Stop'
$raiz = Resolve-Path (Join-Path $PSScriptRoot '..\..')
Push-Location -LiteralPath $raiz
try {
    & dotnet build 'src\EcoAccess.P3.Presentacion\EcoAccess.P3.Presentacion.csproj' -c Release -nologo -v quiet
    if ($LASTEXITCODE -ne 0) { throw "La compilación falló (código $LASTEXITCODE)." }
    $commit = (& git rev-parse --short HEAD).Trim()
    $nombre = "EcoAccess-P3-$commit"
    $carpeta = Join-Path $raiz "dist\$nombre"
    if (Test-Path -LiteralPath $carpeta) { Remove-Item -LiteralPath $carpeta -Recurse -Force }
    New-Item -ItemType Directory -Force -Path (Join-Path $carpeta 'aplicacion') | Out-Null
    & robocopy.exe 'src\EcoAccess.P3.Presentacion\bin\Release\net48' (Join-Path $carpeta 'aplicacion') /E /NFL /NDL /NJH /NJS /NP /XF *.pdb | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "No se pudo copiar la aplicación (robocopy $LASTEXITCODE)." }
    Copy-Item 'scripts\instalacion\Instalar-EcoAccess.ps1' $carpeta
    @"
EcoAccess P3 - paquete de instalación ($commit)

1. Descomprime este .zip en cualquier carpeta.
2. Pide al responsable del proyecto el archivo Wallet_*.zip de Oracle Cloud (canal privado; nunca por GitHub).
3. Clic derecho en Instalar-EcoAccess.ps1 > «Ejecutar con PowerShell», o en una terminal:
   powershell -ExecutionPolicy Bypass -File .\Instalar-EcoAccess.ps1 -Wallet "<ruta del Wallet_*.zip>"
4. Abre «EcoAccess P3» desde el escritorio. La primera vez escribe la contraseña del esquema que te entregó el
   responsable: se comprueba con Oracle y queda guardada solo en tu perfil de Windows.
Docker no es necesario.
"@ | Set-Content -LiteralPath (Join-Path $carpeta 'LEEME.txt') -Encoding UTF8
    $zip = Join-Path $raiz "dist\$nombre.zip"
    if (Test-Path -LiteralPath $zip) { Remove-Item -LiteralPath $zip -Force }
    Compress-Archive -Path (Join-Path $carpeta '*') -DestinationPath $zip
    Write-Host "Paquete listo: $zip" -ForegroundColor Green
} finally { Pop-Location }

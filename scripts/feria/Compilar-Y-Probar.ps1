<#
.SYNOPSIS
    Restaura paquetes, compila EcoAccess.P3.sln en Debug y Release y ejecuta las pruebas que NO tocan Oracle.

.DESCRIPTION
    Detecta la raíz del repositorio y usa el SDK que fija global.json. Excluye la categoría IntegracionOracle (esas pruebas
    solo corren contra el esquema aislado ECOACCESS_P3_PRUEBAS* y se lanzan aparte). No instala ni reinicia servicios, no
    toca la instalación de EcoAccess P3 ni los accesos directos: compila en las carpetas bin\ del repositorio.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\feria\Compilar-Y-Probar.ps1
#>
[CmdletBinding()]
param([switch]$SoloRelease)
. (Join-Path $PSScriptRoot 'ComunOracle.ps1')
$raiz = Get-RaizRepositorio
$sln = Join-Path $raiz 'EcoAccess.P3.sln'
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw 'Falta el SDK de .NET (ver global.json).' }
Write-Host "SDK: $(dotnet --version) · solución: $sln"
dotnet restore $sln -v q; if ($LASTEXITCODE) { throw 'Falló la restauración de paquetes NuGet.' }
$configuraciones = if ($SoloRelease) { @('Release') } else { @('Debug', 'Release') }
foreach ($c in $configuraciones) {
    Write-Host "== Compilar $c" -ForegroundColor Cyan
    dotnet build $sln -c $c --no-restore -v q -nologo; if ($LASTEXITCODE) { throw "Falló la compilación $c." }
}
Write-Host '== Pruebas (Release, sin IntegracionOracle)' -ForegroundColor Cyan
dotnet test $sln -c Release --no-build --filter 'TestCategory!=IntegracionOracle' -v q -nologo
if ($LASTEXITCODE) { throw 'Hay pruebas con error.' }

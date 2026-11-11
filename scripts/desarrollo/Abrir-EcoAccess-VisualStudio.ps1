<#
.SYNOPSIS
    Abre en Visual Studio la solución EcoAccess.P3.sln de ESTA copia del repositorio (no la de auditoría ni la de V2).
.DESCRIPTION
    Busca Visual Studio con vswhere y, si no lo encuentra, en la ruta de App Paths de Windows. No compila ni ejecuta nada.
    El proyecto de inicio para la aplicación de escritorio es EcoAccess.P3.Presentacion (clic derecho > Establecer como
    proyecto de inicio, una sola vez: Visual Studio lo recuerda en la carpeta .vs).
#>
$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$sln = Join-Path $raiz 'EcoAccess.P3.sln'
if (-not (Test-Path $sln)) { throw "No se encontró $sln" }
$devenv = $null
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
if (Test-Path $vswhere) { $devenv = & $vswhere -latest -prerelease -property productPath 2>$null | Select-Object -First 1 }
if (-not $devenv) {
    $devenv = (Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\devenv.exe' -ErrorAction SilentlyContinue).'(default)'
}
if (-not $devenv -or -not (Test-Path $devenv)) {
    # Visual Studio 18 no siempre aparece en vswhere ni en App Paths: se busca en su carpeta de instalación.
    $devenv = Get-ChildItem (Join-Path $env:ProgramFiles 'Microsoft Visual Studio\*\*\Common7\IDE\devenv.exe') -ErrorAction SilentlyContinue |
        Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
}
if (-not $devenv -or -not (Test-Path $devenv)) { throw 'No se encontró Visual Studio. Instale Visual Studio Community con el componente de escritorio .NET.' }
Start-Process -FilePath $devenv -ArgumentList "`"$sln`""
Write-Host "Visual Studio abriendo $sln"

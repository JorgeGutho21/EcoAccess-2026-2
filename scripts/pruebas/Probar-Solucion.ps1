<#
.SYNOPSIS
    Compila la solución y ejecuta las pruebas unitarias y, si hay conexión privada, las de integración con Oracle.
#>
[CmdletBinding()]
param([ValidateSet('Debug', 'Release')] [string] $Configuracion = 'Release', [switch] $SinOracle, [string] $IdSecretos = 'ecoaccess-p3-framework')
$raiz = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Set-Location $raiz
& dotnet build EcoAccess.P3.sln -c $Configuracion -v q --nologo
if ($LASTEXITCODE -ne 0) { exit 1 }
& dotnet test tests\unitarias\EcoAccess.P3.Tests -c $Configuracion --no-build --nologo
$codigo = $LASTEXITCODE
if (-not $SinOracle) {
    $archivo = Join-Path $env:APPDATA "Microsoft\UserSecrets\$IdSecretos\secrets.json"
    if (-not (Test-Path $archivo)) { Write-Output 'Sin conexión privada: integración Oracle no ejecutada.' }
    else {
        $env:ECOACCESS_ConnectionStrings__OracleP3 = (Get-Content $archivo -Raw -Encoding UTF8 | ConvertFrom-Json).'ConnectionStrings:OracleP3'
        & dotnet test tests\integracion\EcoAccess.P3.Datos.Tests -c $Configuracion --no-build --nologo
        if ($LASTEXITCODE -ne 0) { $codigo = 1 }
        Remove-Item Env:ECOACCESS_ConnectionStrings__OracleP3
    }
}
exit $codigo

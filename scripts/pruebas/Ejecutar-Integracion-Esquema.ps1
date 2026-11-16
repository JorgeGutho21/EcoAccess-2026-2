<#
.SYNOPSIS
    Pruebas de integracion y de carga en un esquema aparte de la instancia Oracle existente, sin crear otro contenedor.
.DESCRIPTION
    Crea, si falta, el esquema ECOACCESS_P3_PRUEBAS en oracle-free-p3 con 500 MB de cuota. No toca ECOACCESS_P3_APP. Las
    pruebas usan la instancia real, asi que evita ejecutarlas durante una demostracion. Compila antes con el bot detenido:
    dotnet build EcoAccess.P3.sln -c Release
#>
[CmdletBinding()]
param(
    [string] $Salida = (Join-Path $env:ProgramData 'EcoAccessP3Framework\Pruebas'),
    [string] $Filtro,
    [switch] $ConModeloReal
)
$raiz = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Set-Location $raiz
$id = 'ecoaccess-p3-pruebas'
$esquema = 'ECOACCESS_P3_PRUEBAS'
$privado = Join-Path $Salida 'bridge-pruebas'
New-Item -ItemType Directory -Force $Salida | Out-Null
$instalacion = Join-Path $raiz 'scripts\instalacion'
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $instalacion 'Preparar-BaseDatos.ps1') -Esquema $esquema -IdSecretos $id -DatosDemostracion | Select-Object -Last 2
if ($LASTEXITCODE -ne 0) { throw 'No se pudo preparar el esquema de pruebas.' }
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $instalacion 'Preparar-Demostracion.ps1') -EsquemaOracle $esquema -IdSecretos $id -DirectorioPrivado $privado | Select-Object -Last 1
$secretos = Join-Path $env:APPDATA "Microsoft\UserSecrets\$id\secrets.json"
$env:ECOACCESS_ConnectionStrings__OracleP3 = (Get-Content -LiteralPath $secretos -Raw -Encoding UTF8 | ConvertFrom-Json).'ConnectionStrings:OracleP3'
$env:ECOACCESS_CARGA_SALIDA = $Salida
if ($ConModeloReal) { $env:ECOACCESS_PROBAR_VISION_INTEGRADA = '1'; $env:ECOACCESS_PROBAR_VISION_REAL = '1' }
$informe = Join-Path $Salida 'integracion.txt'

# Muestra las sesiones Oracle del esquema mientras corren las pruebas.
$muestreo = Start-Job -ScriptBlock {
    param($esquema, $archivo)
    while ($true) {
        $sql = "ALTER SESSION SET CONTAINER=PRUEBAP3;`nSET HEADING OFF FEEDBACK OFF`nSELECT COUNT(*) FROM v`$session WHERE username='$esquema';`nEXIT"
        $n = ($sql | docker exec -i oracle-free-p3 sqlplus -s / as sysdba | Where-Object { $_ -match '^\s*\d+\s*$' } | Select-Object -Last 1)
        if ($n) { Add-Content -Path $archivo -Value $n.Trim() }
        Start-Sleep -Seconds 3
    }
} -ArgumentList $esquema, (Join-Path $Salida 'sesiones_oracle.txt')
try {
    $argumentos = @('test', 'tests\integracion\EcoAccess.P3.Datos.Tests', '-c', 'Release', '--no-build', '--nologo', '--logger', 'console;verbosity=normal')
    if ($Filtro) { $argumentos += @('--filter', $Filtro) }
    & dotnet @argumentos 2>&1 | Out-File -Encoding utf8 $informe
    "Integracion: codigo $LASTEXITCODE"
    Select-String -Path $informe -Pattern 'Passed!|Failed!|^\s+Failed ' | Select-Object -First 40 | ForEach-Object { $_.Line }
}
finally {
    Stop-Job $muestreo -ErrorAction SilentlyContinue
    Remove-Job $muestreo -Force -ErrorAction SilentlyContinue
    Remove-Item Env:ECOACCESS_ConnectionStrings__OracleP3, Env:ECOACCESS_CARGA_SALIDA, Env:ECOACCESS_PROBAR_VISION_INTEGRADA, Env:ECOACCESS_PROBAR_VISION_REAL -ErrorAction SilentlyContinue
}
'FIN-INTEGRACION'

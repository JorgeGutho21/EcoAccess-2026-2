<#
.SYNOPSIS
    Instala el modelo completo de EcoAccess P3 en el esquema de pruebas local (Oracle FREE sin Docker) y ensaya una
    migración nueva dos veces. No necesita SYSTEM: usa la conexión que dejó Crear-Esquema-Aislado-Local.ps1.
.DESCRIPTION
    Orden: tablas iniciales, catálogos, migraciones V*.sql de P3 y las indicadas en -MigracionesExternas (por ejemplo
    V021 y V022 de EcoAccess V2) ordenadas por nombre, y después el catálogo ambiental. Cada paso queda en
    REGISTRO_MIGRACION y no se repite. Con -RepetirMigracion ejecuta ese archivo otra vez al final para demostrar que es
    repetible (debe terminar sin error y sin crear nada).
    Se niega a ejecutarse si la conexión no es localhost/127.0.0.1 o si el esquema no se llama ECOACCESS_P3_PRUEBAS*.
#>
[CmdletBinding()]
param(
    [string] $IdSecretos = 'ecoaccess-p3-pruebas',
    [string[]] $MigracionesExternas = @(),
    [string] $RepetirMigracion,
    [switch] $DatosPiloto
)
$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$archivoSecretos = Join-Path $env:APPDATA "Microsoft\UserSecrets\$IdSecretos\secrets.json"
if (-not (Test-Path -LiteralPath $archivoSecretos)) { throw "Falta el perfil ${IdSecretos}: ejecuta antes Crear-Esquema-Aislado-Local.ps1." }
$cadena = (Get-Content -LiteralPath $archivoSecretos -Raw -Encoding UTF8 | ConvertFrom-Json).'ConnectionStrings:OracleP3'
$usuario = [regex]::Match($cadena, '(?i)User Id=([^;]+)').Groups[1].Value
$clave = [regex]::Match($cadena, '(?i)Password=([^;]+)').Groups[1].Value
$destino = [regex]::Match($cadena, '(?i)Data Source=([^;]+)').Groups[1].Value
if ($destino -notmatch '^(localhost|127\.0\.0\.1):\d+/\w+$') { throw "Solo se instala en un Oracle local; el perfil apunta a otro destino." }
if ($usuario -notmatch '^(?i)ECOACCESS_P3_PRUEBAS\w*$') { throw "Solo se instala en un esquema ECOACCESS_P3_PRUEBAS*." }

$sqlplus = @($env:ORACLE_HOME | Where-Object { $_ } | ForEach-Object { Join-Path $_ 'bin\sqlplus.exe' }) +
    @(Get-ChildItem "C:\app\$env:USERNAME\product\*\dbhomeFree\bin\sqlplus.exe" -ErrorAction SilentlyContinue | ForEach-Object FullName) |
    Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $sqlplus) { throw 'No se encontró sqlplus.exe.' }
$env:NLS_LANG = 'AMERICAN_AMERICA.AL32UTF8'
# La ruta del proyecto tiene tildes («Programación»): en ASCII llegaría como «?» y SQL*Plus no hallaría los archivos.
$OutputEncoding = New-Object Text.UTF8Encoding($false)
[Console]::OutputEncoding = New-Object Text.UTF8Encoding($false)

function Invocar([string[]] $Lineas) {
    # La contraseña va por la entrada estándar: no aparece en la línea de comandos del proceso.
    $entrada = (@("CONNECT $usuario/`"$clave`"@$destino", 'SET FEEDBACK OFF', 'SET DEFINE OFF', 'SET SERVEROUTPUT ON') + $Lineas + 'EXIT') -join "`n"
    $salida = $entrada | & $sqlplus -S -L /nolog 2>&1 | Out-String
    $codigo = $LASTEXITCODE
    if ($salida -match 'SP2-0640|SP2-0734|SP2-0306|ORA-01017|ORA-12\d{3}') { $codigo = 1 }
    return [pscustomobject]@{ Codigo = $codigo; Salida = ($salida -replace [regex]::Escape($clave), '***') }
}

$registro = Invocar @(
    'WHENEVER SQLERROR CONTINUE',
    'CREATE TABLE registro_migracion (version VARCHAR2(60) CONSTRAINT pk_registro_migracion PRIMARY KEY, fecha_aplicacion TIMESTAMP DEFAULT SYSTIMESTAMP NOT NULL);',
    'SET HEADING OFF PAGESIZE 0',
    'SELECT version FROM registro_migracion;')
if ($registro.Salida -match 'ORA-01017|ORA-12\d{3}') { throw "No se pudo conectar al esquema de pruebas:`n$($registro.Salida)" }
$aplicadas = @($registro.Salida -split "`r?`n" | ForEach-Object { $_.Trim() } | Where-Object { $_ -match '^[0-9V]' })

$migraciones = @(Get-ChildItem (Join-Path $raiz 'database\06_migraciones') -Filter 'V*.sql' | ForEach-Object FullName) + $MigracionesExternas |
    Sort-Object { [IO.Path]::GetFileName($_) }
$pasos = @(
    (Join-Path $raiz 'database\02_tablas\01_crear_tablas_iniciales.sql'),
    (Join-Path $raiz 'database\03_datos_prueba\01_catalogos.sql')
) + $migraciones + @(
    (Join-Path $raiz 'database\03_datos_prueba\03_catalogo_ambiental_piloto.sql'),
    (Join-Path $raiz 'database\03_datos_prueba\06_catalogo_ampliado.sql')
)
if ($DatosPiloto) {
    $pasos += (Join-Path $raiz 'database\03_datos_prueba\02_datos_demo.sql')
    $pasos += (Join-Path $raiz 'database\03_datos_prueba\04_piloto_simulado.sql')
    $pasos += (Join-Path $raiz 'database\03_datos_prueba\05_tarifas_piloto_simulado.sql')
}

$aplicados = 0
foreach ($paso in $pasos) {
    $version = [IO.Path]::GetFileNameWithoutExtension($paso)
    if ($aplicadas -contains $version) { continue }
    $reloj = [Diagnostics.Stopwatch]::StartNew()
    $r = Invocar @('SET DEFINE ON', 'WHENEVER SQLERROR EXIT 1', "@`"$paso`"", 'WHENEVER SQLERROR EXIT 1',
        "INSERT INTO registro_migracion (version) VALUES ('$version');", 'COMMIT;')
    if ($r.Codigo -ne 0) { throw ("Falló $paso. La versión no quedó registrada.`n" + (($r.Salida -split "`n" | Select-Object -Last 15) -join "`n")) }
    Write-Output ("Aplicado {0} en {1} ms" -f $version, $reloj.ElapsedMilliseconds)
    $aplicados++
}
Write-Output "Pasos nuevos: $aplicados; esquema $usuario en $destino al día."

if ($RepetirMigracion) {
    $r = Invocar @('WHENEVER SQLERROR EXIT 1', "@`"$RepetirMigracion`"")
    Write-Output "Segunda ejecución de $([IO.Path]::GetFileName($RepetirMigracion)): código $($r.Codigo)"
    Write-Output $r.Salida.Trim()
    if ($r.Codigo -ne 0) { throw 'La segunda ejecución falló: la migración no es repetible.' }
}

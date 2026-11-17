<#
  EcoAccess BD · INSTALAR Y PROBAR TODO (un solo comando)

  Qué hace, en orden:
    1. Revisa que Oracle esté encendido y que las tres cuentas del proyecto puedan conectarse.
       (Con -CrearUsuarios crea las cuentas la primera vez, entrando como administrador local.)
    2. Si el esquema ECOACCESS_BD está vacío, lo instala (scripts 02 → 11 y 15).
       Con -Reinstalar lo borra y lo vuelve a crear desde cero (pide escribir BORRAR_ECOACCESS_BD).
    3. Instala o actualiza las prácticas de clase (P00 → P05).
    4. Ejecuta TODAS las pruebas y guarda las evidencias:
         163 pruebas del esquema, 19 de prácticas, 10 de la cuenta lectora, 22 consultas,
         la prueba de concurrencia de tarifas (dos sesiones),
         309 ejercicios de la guía, la prueba de C# y la carga de datos de Power BI.
    5. Extrae el DDL FINAL desde Oracle y lo compara con los scripts y con el diccionario.
    6. Escribe 16_EVIDENCIAS_Y_PRUEBAS\RESUMEN_ULTIMA_EJECUCION.md con un PASA/FALLA por cada comprobación.

  Nunca toca P3, Oracle Cloud, HR (solo lo lee) ni otros esquemas. Nunca usa DROP USER ... CASCADE.
  Las contraseñas no se escriben en ningún archivo del proyecto (ver 18_ENTREGA_Y_RESPALDOS\herramientas\comun.ps1).

  Uso típico (PowerShell 7, desde esta carpeta):
    pwsh -File .\INSTALAR_Y_PROBAR.ps1                       # instala si falta y prueba todo
    pwsh -File .\INSTALAR_Y_PROBAR.ps1 -Reinstalar           # borra y reinstala ECOACCESS_BD desde cero
    pwsh -File .\INSTALAR_Y_PROBAR.ps1 -CrearUsuarios        # primera vez en un PC nuevo
    pwsh -File .\INSTALAR_Y_PROBAR.ps1 -SinEjercicios -SinCSharp   # versión rápida
    pwsh -File .\INSTALAR_Y_PROBAR.ps1 -CapturasCSharp       # además repite las capturas de la ventana C# (N03)
#>
param(
    [switch]$Reinstalar,
    [string]$Confirmacion,
    [switch]$CrearUsuarios,
    [switch]$SinEjercicios,
    [switch]$SinCSharp,
    [switch]$SinPowerBI,
    [switch]$CapturasCSharp   # repite la demostración de la ventana C# (N03); cada vez inserta 1 empleado de demostración
)
$ErrorActionPreference = 'Stop'
$raiz = $PSScriptRoot
. (Join-Path $raiz '18_ENTREGA_Y_RESPALDOS\herramientas\comun.ps1')
$utf8 = New-Object Text.UTF8Encoding $false
$inicio = Get-Date
$evid = Join-Path $raiz '16_EVIDENCIAS_Y_PRUEBAS\salidas\ultima_ejecucion'
New-Item -ItemType Directory -Force $evid | Out-Null
$resultados = New-Object Collections.Generic.List[object]

function Anotar([string]$Id, [string]$Que, [bool]$Paso, [string]$Detalle) {
    $resultados.Add([pscustomobject]@{ Id = $Id; Que = $Que; Estado = $(if ($Paso) { 'PASA' } else { 'FALLA' }); Detalle = $Detalle })
    $color = if ($Paso) { 'Green' } else { 'Red' }
    Write-Host ("{0,-5} {1,-4} {2}  {3}" -f $(if ($Paso) { 'PASA' } else { 'FALLA' }), $Id, $Que, $Detalle) -ForegroundColor $color
}
function Paso([string]$t) { Write-Host "`n=== $t ===" -ForegroundColor Cyan }
function Sql([string]$Usuario, [string[]]$Scripts, [string]$Archivo, [string[]]$Respuestas = @(), [string[]]$Sentencias = @(), [switch]$ComoSysdba) {
    $r = Invoke-EcoSql -Usuario $Usuario -Scripts $Scripts -Respuestas $Respuestas -Sentencias $Sentencias -ComoSysdba:$ComoSysdba -Salida $(if ($Archivo) { Join-Path $evid $Archivo })
    $r
}
function SinErrores([string]$texto) { -not ($texto -match 'ERROR en l[ií]nea|PLS-\d{5}|SP2-\d{4}|con errores de compilaci[oó]n|Advertencia:') }
function SinArchivoPerdido([string]$texto) { -not ($texto -match 'SP2-0310') }
function Total([string]$texto, [string]$etiqueta) {
    $m = [regex]::Match($texto, "$etiqueta\s*:?\s*(\d+) pruebas · PASAN: (\d+) · FALLAN: (\d+)")
    if ($m.Success) { [pscustomobject]@{ Total = [int]$m.Groups[1].Value; Pasan = [int]$m.Groups[2].Value; Fallan = [int]$m.Groups[3].Value } }
}

# ------------------------------------------------------------------ 1. ENTORNO
Paso '1. Entorno y conexiones'
$sqlplus = Initialize-EcoOracle
Anotar 'E01' 'SQL*Plus encontrado' $true $sqlplus
$lsn = & (Join-Path (Split-Path $sqlplus) 'lsnrctl.exe') status 2>&1 | Out-String
$svc = ((Get-EcoServicio) -split '/')[-1]
Anotar 'E02' "El listener publica el servicio $svc" ($lsn -match [regex]::Escape($svc)) "TNS_ADMIN=$env:TNS_ADMIN"

if ($CrearUsuarios) {
    $env_archivo = Join-Path $raiz '_LOCAL_NO_PUBLICAR\credenciales_locales.env'
    if (-not (Test-Path $env_archivo)) {
        New-Item -ItemType Directory -Force (Split-Path $env_archivo) | Out-Null
        function NuevaClave { 'Eco' + (-join ((48..57) + (65..90) + (97..122) | Get-Random -Count 14 | ForEach-Object { [char]$_ })) + '9' }
        @('# NO PUBLICAR. Contraseñas de PRUEBA para los esquemas académicos LOCALES.', "ECOACCESS_BD=$(NuevaClave)", "ECOACCESS_PRACTICAS=$(NuevaClave)", "ECOACCESS_BD_LECTOR=$(NuevaClave)") | Set-Content $env_archivo -Encoding UTF8
        '*' | Set-Content (Join-Path (Split-Path $env_archivo) '.gitignore')
        Write-Host "Se generaron contraseñas nuevas en $env_archivo (no lo subas a Git)."
    }
    $resp = @('ECOACCESS_BD', 'ECOACCESS_PRACTICAS', 'ECOACCESS_BD_LECTOR' | ForEach-Object { Get-EcoClave $_ -Obligatoria })
    $r = Sql 'SYS' @("$raiz\04_SCRIPTS_ORACLE\01_esquema_practicas.sql") '01_usuarios.txt' -Respuestas $resp -ComoSysdba
    Anotar 'E03' 'Cuentas del proyecto creadas o ya existentes (01)' ($r.Codigo -eq 0) "código $($r.Codigo)"
}
foreach ($u in 'ECOACCESS_BD', 'ECOACCESS_PRACTICAS', 'ECOACCESS_BD_LECTOR') {
    $r = Sql $u @() $null -Sentencias @("SELECT 'CONEXION_' || 'OK' AS estado FROM dual;")
    Anotar 'E04' "Conexión como $u" ($r.Texto -match 'CONEXION_OK') $(if ($r.Texto -match '(ORA-\d{5}[^\r\n]*)') { $matches[1] } else { "//$(Get-EcoServicio)" })
}
if ($resultados | Where-Object { $_.Id -eq 'E04' -and $_.Estado -eq 'FALLA' }) {
    Write-Host "`nNo se pudo conectar con alguna cuenta. Si es la primera vez en este PC usa -CrearUsuarios; si es ORA-01017 revisa la contraseña (paso a paso, sección «Cuando algo falla»)." -ForegroundColor Yellow
    exit 1
}

# ------------------------------------------------------------------ 2. INSTALACIÓN DEL ESQUEMA
Paso '2. Esquema ECOACCESS_BD'
$cuenta = Sql 'ECOACCESS_BD' @() $null -Sentencias @("SELECT 'TABLAS=' || COUNT(*) FROM user_tables;")
$tablas = [int]([regex]::Match($cuenta.Texto, 'TABLAS=(\d+)').Groups[1].Value)
if ($Reinstalar -and $tablas -gt 0) {
    if (-not $Confirmacion) { $Confirmacion = Read-Host 'Esto BORRA y vuelve a crear ECOACCESS_BD (datos sintéticos). Escribe BORRAR_ECOACCESS_BD para seguir' }
    if ($Confirmacion -ne 'BORRAR_ECOACCESS_BD') { Write-Host 'Confirmación incorrecta: no se borró nada.' -ForegroundColor Yellow; exit 1 }
    $r = Sql 'ECOACCESS_BD' @("$raiz\04_SCRIPTS_ORACLE\99_reversion_esquema_academico.sql") '99_reversion.txt' -Respuestas @($Confirmacion)
    $quedan = [regex]::Match($r.Texto, 'OBJETOS_RESTANTES\s*-+\s*(\d+)').Groups[1].Value
    Anotar 'I01' 'Reversión del esquema académico (99)' ($r.Codigo -eq 0 -and $quedan -eq '0') "objetos restantes: $quedan"
    $tablas = 0
}
if ($tablas -eq 0) {
    # El script maestro instala 00 → 15 en orden y al final corre las 163 pruebas y la verificación.
    $r = Sql 'ECOACCESS_BD' @("$raiz\04_SCRIPTS_ORACLE\00_INSTALAR_TODO.sql") '00_instalar_todo.txt'
    $t = Total $r.Texto 'TOTAL'
    $antesDePruebas = $r.Texto -replace '(?s)----- Paso 14.*', ''
    Anotar 'I02' 'Script maestro 00_INSTALAR_TODO.sql sin errores' ($r.Codigo -eq 0 -and (SinErrores $antesDePruebas) -and (SinArchivoPerdido $r.Texto) -and $t -and $t.Fallan -eq 0) "código $($r.Codigo) · pruebas internas $(if ($t) { "$($t.Pasan)/$($t.Total)" } else { 'sin total' })"
} elseif ($tablas -eq 19) {
    Anotar 'I02' 'Esquema ya instalado (19 tablas); no se reinstaló' $true 'usa -Reinstalar para empezar de cero'
} else {
    Anotar 'I02' 'Esquema a medias' $false "$tablas tablas: ejecuta con -Reinstalar"
}
Paso '3. Prácticas de clase (ECOACCESS_PRACTICAS)'
$r = Sql 'ECOACCESS_PRACTICAS' @('P00_tablas_de_clase', 'P01_secuencia_pk', 'P02_precio_final', 'P03_triggers_libres', 'P04_hr_plsql', 'P05_crud_sin_delete' | ForEach-Object { "$raiz\05_SQL_Y_PLSQL\00_PRACTICAS_VERIFICADAS\$_.sql" }) 'P00_a_P05_practicas.txt'
Anotar 'I03' 'Prácticas P00 → P05 ejecutadas (sus demos de error son intencionales)' ($r.Codigo -eq 0 -and (SinArchivoPerdido $r.Texto) -and -not ($r.Texto -match 'PLS-\d{5}|con errores de compilaci')) "código $($r.Codigo)"

# ------------------------------------------------------------------ 3. PRUEBAS
Paso '4. Pruebas'
$r = Sql 'ECOACCESS_BD' @("$raiz\04_SCRIPTS_ORACLE\12_pruebas.sql") '12_pruebas.txt'
$t = Total $r.Texto 'TOTAL'
Anotar 'T01' 'Pruebas del esquema (12_pruebas.sql)' ($t -and $t.Fallan -eq 0 -and $t.Total -eq $t.Pasan) $(if ($t) { "$($t.Pasan)/$($t.Total)" } else { 'sin total' })
$r = Sql 'ECOACCESS_PRACTICAS' @("$raiz\05_SQL_Y_PLSQL\00_PRACTICAS_VERIFICADAS\P99_pruebas_practicas.sql") 'P99_pruebas_practicas.txt'
$t = Total $r.Texto 'TOTAL PRÁCTICAS'
Anotar 'T02' 'Pruebas de las prácticas (P99)' ($t -and $t.Fallan -eq 0) $(if ($t) { "$($t.Pasan)/$($t.Total)" } else { 'sin total' })
$r = Sql 'ECOACCESS_BD_LECTOR' @("$raiz\04_SCRIPTS_ORACLE\16_prueba_lector.sql") '16_prueba_lector.txt'
$t = Total $r.Texto 'TOTAL LECTOR'
Anotar 'T03' 'Mínimo privilegio de la cuenta lectora (16)' ($t -and $t.Fallan -eq 0) $(if ($t) { "$($t.Pasan)/$($t.Total)" } else { 'sin total' })
$r = Sql 'ECOACCESS_BD' @("$raiz\04_SCRIPTS_ORACLE\13_consultas_exposicion.sql") '13_consultas_exposicion.txt'
Anotar 'T04' '22 consultas de exposición sin errores (13)' (SinErrores $r.Texto) "$(([regex]::Matches($r.Texto, '===== Q\d\d')).Count) consultas ejecutadas"

$r = Sql 'ECOACCESS_BD' @("$raiz\07_DISPARADORES_TRIGGERS\demo_cuatro_triggers.sql") 'demo_cuatro_triggers.txt'
$okDemo = ($r.Texto -match 'ORA-20120') -and ($r.Texto -match 'ORA-20101') -and ($r.Texto -match 'ORA-20103') -and ($r.Texto -match 'fila de auditoría: VERIFICACION') -and ($r.Texto -match 'historial sigue en') -and ($r.Texto -match 'ROLLBACK  todo se deshizo')
Anotar 'T05' 'Los 4 triggers: antes → sentencia → después y su rechazo (demo)' $okDemo 'ORA-20120, ORA-20101, ORA-20103, auditoría :OLD/:NEW y ROLLBACK'

# T06: dos sesiones reales registran a la vez la primera tarifa de un material (revisión del PR #12).
$sal = & pwsh -NoProfile -File (Join-Path $raiz '18_ENTREGA_Y_RESPALDOS\herramientas\prueba_concurrencia_tarifas.ps1') -Salida (Join-Path $evid '20_concurrencia_tarifas.txt') 2>&1 | Out-String
Anotar 'T06' 'Concurrencia: dos sesiones no crean dos tarifas abiertas (registrar_regla bloquea MATERIAL)' ($LASTEXITCODE -eq 0) $(if ($sal -match '(?m)^(PASA|FALLA)\s+(.+)$') { $matches[2].Trim() } else { 'sin resultado' })

# ------------------------------------------------------------------ 4. DDL FINAL
Paso '5. DDL final contra scripts y diccionario'
$corto = Join-Path ([IO.Path]::GetTempPath()) 'ecoaccess_salida'; Remove-Item $corto -Recurse -Force -ErrorAction SilentlyContinue; New-Item -ItemType Directory $corto | Out-Null
$r = Invoke-EcoSql -Usuario 'ECOACCESS_BD' -Scripts @("$raiz\04_SCRIPTS_ORACLE\17_extraer_ddl_final.sql") -Directorio $corto -Salida (Join-Path $evid '17_extraer_ddl.txt')
if (Test-Path (Join-Path $corto 'DDL_FINAL_EXTRAIDO.sql')) { Copy-Item (Join-Path $corto 'DDL_FINAL_EXTRAIDO.sql') (Join-Path $raiz '04_SCRIPTS_ORACLE\DDL_FINAL_EXTRAIDO.sql') -Force }
$ddl = Join-Path $raiz '04_SCRIPTS_ORACLE\DDL_FINAL_EXTRAIDO.sql'
$ddlTexto = if ((Test-Path $ddl) -and (Get-Item $ddl).LastWriteTime -ge $inicio) { [IO.File]::ReadAllText($ddl, $utf8) } else { '' }
$cuentaDdl = @{
    'CREATE TABLE' = ([regex]::Matches($ddlTexto, '(?m)^\s*CREATE TABLE')).Count
    'VIEW'         = ([regex]::Matches($ddlTexto, '(?m)^\s*CREATE OR REPLACE FORCE (EDITIONABLE |NONEDITIONABLE )?VIEW')).Count
    'PACKAGE BODY' = ([regex]::Matches($ddlTexto, '(?m)^\s*CREATE OR REPLACE (EDITIONABLE |NONEDITIONABLE )?PACKAGE BODY')).Count
    'TRIGGER'      = ([regex]::Matches($ddlTexto, '(?m)^\s*CREATE OR REPLACE (EDITIONABLE |NONEDITIONABLE )?TRIGGER')).Count
}
Anotar 'D01' 'DDL final extraído (04_SCRIPTS_ORACLE\DDL_FINAL_EXTRAIDO.sql)' ($cuentaDdl['CREATE TABLE'] -eq 19 -and $cuentaDdl['VIEW'] -eq 9 -and $cuentaDdl['PACKAGE BODY'] -eq 4 -and $cuentaDdl['TRIGGER'] -eq 4) ("tablas {0}, vistas {1}, paquetes {2}, triggers {3}, {4} líneas" -f $cuentaDdl['CREATE TABLE'], $cuentaDdl['VIEW'], $cuentaDdl['PACKAGE BODY'], $cuentaDdl['TRIGGER'], ($ddlTexto -split "`n").Count)
$inv = (Sql 'ECOACCESS_BD' @("$raiz\04_SCRIPTS_ORACLE\18_inventario_objetos.sql") '18_inventario.txt').Texto -split "`r?`n"
$bd = @{}
foreach ($l in $inv) { if ($l -match '^(OBJ|COL|CON)\|([^|]+)\|([^|]+)') { $k = $matches[1]; if (-not $bd[$k]) { $bd[$k] = New-Object Collections.Generic.List[string] }; $bd[$k].Add("$($matches[2])|$($matches[3])".Trim()) } }
$invalidos = @($inv | Where-Object { $_ -match '^OBJ\|.*\|INVALID$' }).Count
Anotar 'D02' '0 objetos inválidos' ($invalidos -eq 0) "$invalidos inválidos"
function NombresEn([string]$archivo, [string]$patron) { [regex]::Matches([IO.File]::ReadAllText((Join-Path $raiz "04_SCRIPTS_ORACLE\$archivo"), $utf8), $patron, 'IgnoreCase') | ForEach-Object { $_.Groups['n'].Value.ToUpper() } | Sort-Object -Unique }
function Comparar([string]$Id, [string]$Que, [string[]]$enScripts, [string[]]$enBd) {
    $faltan = @($enScripts | Where-Object { $_ -notin $enBd }); $sobran = @($enBd | Where-Object { $_ -notin $enScripts })
    Anotar $Id $Que ($faltan.Count -eq 0 -and $sobran.Count -eq 0) ("scripts {0} = Oracle {1}{2}{3}" -f $enScripts.Count, $enBd.Count, $(if ($faltan) { " · faltan en Oracle: $($faltan -join ', ')" }), $(if ($sobran) { " · no están en scripts: $($sobran -join ', ')" }))
}
$objBd = @($bd['OBJ'])
Comparar 'D03' 'Tablas: 02_tablas.sql = Oracle' (NombresEn '02_tablas.sql' '(?m)^\s*CREATE TABLE\s+(?<n>\w+)') @($objBd | Where-Object { $_ -like 'TABLE|*' } | ForEach-Object { ($_ -split '\|')[1] })
Comparar 'D04' 'Vistas: 06_vistas.sql = Oracle' (NombresEn '06_vistas.sql' 'CREATE OR REPLACE VIEW\s+(?<n>\w+)') @($objBd | Where-Object { $_ -like 'VIEW|*' } | ForEach-Object { ($_ -split '\|')[1] })
Comparar 'D05' 'Paquetes, funciones y procedimientos: 07 = Oracle' (NombresEn '07_procedimientos_funciones_paquetes.sql' 'CREATE OR REPLACE (PACKAGE|FUNCTION|PROCEDURE)\s+(?!BODY)(?<n>\w+)') @($objBd | Where-Object { $_ -match '^(PACKAGE|FUNCTION|PROCEDURE)\|' } | ForEach-Object { ($_ -split '\|')[1] })
Comparar 'D06' 'Disparadores: 08_triggers.sql = Oracle' (NombresEn '08_triggers.sql' 'CREATE OR REPLACE TRIGGER\s+(?<n>\w+)') @($objBd | Where-Object { $_ -like 'TRIGGER|*' } | ForEach-Object { ($_ -split '\|')[1] })
$idxScripts = NombresEn '04_indices.sql' 'CREATE\s+(UNIQUE\s+)?INDEX\s+(?<n>\w+)'
$idxFalt = @($idxScripts | Where-Object { "INDEX|$_" -notin $objBd })
Anotar 'D07' 'Índices de 04_indices.sql existen en Oracle' ($idxFalt.Count -eq 0) ("{0} índices del script, {1} índices en total en Oracle{2}" -f $idxScripts.Count, @($objBd | Where-Object { $_ -like 'INDEX|*' }).Count, $(if ($idxFalt) { " · faltan: $($idxFalt -join ', ')" }))
$conScripts = @(NombresEn '02_tablas.sql' 'CONSTRAINT\s+(?<n>\w+)') + @(NombresEn '03_restricciones.sql' 'CONSTRAINT\s+(?<n>\w+)') | Sort-Object -Unique
$conBd = @($bd['CON'] | ForEach-Object { ($_ -split '\|')[1] })
Comparar 'D08' 'Restricciones con nombre: 02 + 03 = Oracle' $conScripts $conBd
$dic = Import-Csv (Join-Path $raiz '03_DICCIONARIO_DE_DATOS\diccionario.csv') -Delimiter ';' | ForEach-Object { "$($_.Tabla)|$($_.Columna)" }
Comparar 'D09' 'Columnas: diccionario (173) = Oracle' @($dic) @($bd['COL'])
$cmt = [regex]::Match(($inv -join "`n"), 'CMT\|(\d+)').Groups[1].Value
Anotar 'D10' 'Columnas con comentario del diccionario dentro de Oracle' ($cmt -eq "$(@($dic).Count)") "$cmt de $(@($dic).Count)"

# ------------------------------------------------------------------ 5. GUÍA, C# Y POWER BI
if (-not $SinEjercicios) {
    Paso '6. 309 ejercicios de la guía'
    $sal = & pwsh -NoProfile -File (Join-Path $raiz '08_CONSULTAS_Y_EJERCICIOS\generar_banco.ps1') -Ejecutor (Join-Path $raiz '18_ENTREGA_Y_RESPALDOS\herramientas\ejecutar_sql.ps1') 2>&1 | Out-String
    [IO.File]::WriteAllText((Join-Path $evid 'ejercicios_resumen.txt'), $sal, $utf8)
    $m = [regex]::Match($sal, 'TOTAL: (\d+) ejercicios · OK: (\d+) · OK con error esperado: (\d+) · FALLAN: (\d+)')
    Anotar 'G01' 'Banco de ejercicios ejecutado en Oracle' ($m.Success -and $m.Groups[4].Value -eq '0') $(if ($m.Success) { "$($m.Groups[1].Value) ejercicios · $($m.Groups[2].Value) OK · $($m.Groups[3].Value) con error intencional · $($m.Groups[4].Value) fallan" } else { 'sin resumen' })
}
if (-not $SinCSharp) {
    Paso '7. Conexión desde C# (ODP.NET)'
    if (Get-Command dotnet -ErrorAction SilentlyContinue) {
        $cs = Join-Path $env:TEMP 'ecoaccess_csharp'
        Remove-Item $cs -Recurse -Force -ErrorAction SilentlyContinue
        Copy-Item (Join-Path $raiz '10_CSHARP_VISUAL_STUDIO\SOLUCION_CSHARP') $cs -Recurse -Exclude 'bin', 'obj'
        $env:LECTOR_USUARIO = 'ECOACCESS_BD_LECTOR'; $env:LECTOR_CLAVE = Get-EcoClave 'ECOACCESS_BD_LECTOR'
        $env:PRACTICAS_USUARIO = 'ECOACCESS_PRACTICAS'; $env:PRACTICAS_CLAVE = Get-EcoClave 'ECOACCESS_PRACTICAS'
        $env:LECTOR_DATASOURCE = Get-EcoServicio; $env:PRACTICAS_DATASOURCE = Get-EcoServicio
        $sal = & dotnet run --project (Join-Path $cs 'EcoAccessBD.Consola\EcoAccessBD.Consola.csproj') 2>&1 | Out-String
        foreach ($s in @($env:LECTOR_CLAVE, $env:PRACTICAS_CLAVE)) { if ($s) { $sal = $sal.Replace($s, '********') } }
        [IO.File]::WriteAllText((Join-Path $evid 'csharp_consola.txt'), $sal, $utf8)
        Anotar 'N01' 'Prueba de integración C# (lector + CRUD sin DELETE)' ($sal -match 'TODO PASA') "$(([regex]::Matches($sal, '(?m)^PASA')).Count) casos PASA"
        $b = & dotnet build (Join-Path $cs 'EmpleadosCrud.WinForms\EmpleadosCrud.WinForms.csproj') -nologo -v q 2>&1 | Out-String
        Anotar 'N02' 'La ventana WinForms del CRUD compila' ($b -match '0 Errores|0 Error\(s\)') ''
        # N03: la ventana real pulsa sus propios botones (PerformClick) fuera de la pantalla, sin tomar el foco,
        # y guarda una imagen tras cada paso. Cada ejecución inserta 1 empleado de demostración (el CRUD no borra).
        $exe = Get-ChildItem (Join-Path $cs 'EmpleadosCrud.WinForms\bin') -Recurse -Filter 'EmpleadosCrud.WinForms.exe' -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName
        if (-not $CapturasCSharp) {
            Write-Host '   N03 omitida: las capturas de la ventana ya existen; para repetirlas (inserta 1 empleado de demostración) use -CapturasCSharp' -ForegroundColor DarkGray
        } elseif ($exe) {
            $capt = Join-Path ([IO.Path]::GetTempPath()) 'ecoaccess_capturas_cs'
            Remove-Item $capt -Recurse -Force -ErrorAction SilentlyContinue
            $p = Start-Process $exe -ArgumentList '--demo', $capt -Wait -PassThru
            $dest = Join-Path $raiz '16_EVIDENCIAS_Y_PRUEBAS\capturas_csharp'
            New-Item -ItemType Directory -Force $dest | Out-Null
            Copy-Item (Join-Path $capt '*') $dest -Force -ErrorAction SilentlyContinue
            $reg = Get-Content (Join-Path $capt 'registro_demo.txt') -ErrorAction SilentlyContinue
            Anotar 'N03' 'La ventana C# consulta, inserta, actualiza y muestra ORA-20002 (4 capturas)' ($p.ExitCode -eq 0) "$(@($reg).Count - 1) pasos · capturas en 16_EVIDENCIAS_Y_PRUEBAS\capturas_csharp"
        } else { Anotar 'N03' 'Demostración de la ventana C#' $false 'no se encontró el ejecutable' }
    } else { Anotar 'N01' 'Prueba C#' $false 'no está instalado el SDK de .NET' }
}
if (-not $SinPowerBI) {
    Paso '8. Datos de Power BI'
    New-Item -ItemType Directory -Force (Join-Path $raiz '09_POWER_BI\DATOS_Y_CONSULTAS\datos_csv') | Out-Null
    $corto = Join-Path ([IO.Path]::GetTempPath()) 'ecoaccess_csv'; Remove-Item $corto -Recurse -Force -ErrorAction SilentlyContinue; New-Item -ItemType Directory $corto | Out-Null
    $r = Invoke-EcoSql -Usuario 'ECOACCESS_BD_LECTOR' -Scripts @("$raiz\09_POWER_BI\DATOS_Y_CONSULTAS\exportar_csv.sql") -Directorio $corto -Salida (Join-Path $evid 'powerbi_exportar_csv.txt')
    Copy-Item (Join-Path $corto '*.csv') (Join-Path $raiz '09_POWER_BI\DATOS_Y_CONSULTAS\datos_csv') -Force
    $filas = Get-ChildItem (Join-Path $raiz '09_POWER_BI\DATOS_Y_CONSULTAS\datos_csv\*.csv') | Where-Object LastWriteTime -ge $inicio | ForEach-Object { "$($_.BaseName)=$(@(Get-Content $_.FullName).Count - 1)" }
    $dep = [int](($filas | Where-Object { $_ -like 'vw_bi_depositos=*' }) -replace '.*=', '')
    Anotar 'B01' 'CSV exportados con la cuenta lectora' ($r.Codigo -eq 0 -and (SinArchivoPerdido $r.Texto) -and $dep -gt 0 -and @($filas).Count -eq 6) ($filas -join ', ')
    # B02: NO se regenera el PBIP (generar_pbip.ps1 borraría el formato PBIR, el glosario y el origen Oracle directo
    # agregados después). Solo se comprueba su estructura.
    $pb = Join-Path $raiz '09_POWER_BI\PROYECTO_PBIP\EcoAccess_BD'
    $bim = Get-Content (Join-Path $pb 'EcoAccess_BD.SemanticModel\model.bim') -Raw | ConvertFrom-Json -Depth 100
    $tabDatos = @($bim.model.tables | Where-Object { $_.name -notmatch '^(DateTableTemplate|LocalDateTable)' })
    $medidas = @($tabDatos | ForEach-Object { @($_.measures) } | Where-Object { $_ }).Count
    $oracle = @($tabDatos | Where-Object { ($_.partitions[0].source.expression -join ' ') -match 'Oracle\.Database' }).Count
    $paginas = @(Get-ChildItem (Join-Path $pb 'EcoAccess_BD.Report\definition\pages') -Directory).Count
    $okPb = $tabDatos.Count -eq 6 -and $medidas -ge 15 -and $oracle -eq 5 -and $paginas -eq 5 -and (Test-Path (Join-Path $raiz '09_POWER_BI\ARCHIVO_PBIX\EcoAccess_BD.pbix'))
    Anotar 'B02' 'Proyecto Power BI íntegro (PBIP + PBIX; origen Oracle directo con respaldo CSV)' $okPb "$($tabDatos.Count) tablas · $medidas medidas · $oracle tablas leen Oracle · $paginas páginas"
}

# ------------------------------------------------------------------ 6. RESUMEN
$fallan = [int]@($resultados | Where-Object Estado -eq 'FALLA').Count
$total = [int]$resultados.Count
$pasan = $total - $fallan
$md = New-Object Text.StringBuilder
[void]$md.AppendLine('# Resumen de la última ejecución de INSTALAR_Y_PROBAR.ps1'); [void]$md.AppendLine('')
[void]$md.AppendLine("- **Fecha:** $(Get-Date -Format 'yyyy-MM-dd HH:mm') (hora del PC) · duración $([int]((Get-Date) - $inicio).TotalMinutes) min")
[void]$md.AppendLine("- **Equipo:** $env:COMPUTERNAME · Oracle: ``$(Get-EcoServicio)`` · SQL*Plus: ``$sqlplus``")
[void]$md.AppendLine("- **Opciones:** $(if ($Reinstalar) { 'reinstalación desde cero' } else { 'sin reinstalar' })$(if ($SinEjercicios) { ', sin ejercicios' })$(if ($SinCSharp) { ', sin C#' })$(if ($SinPowerBI) { ', sin Power BI' })")
[void]$md.AppendLine("- **Resultado:** $pasan de $total comprobaciones PASAN$(if ($fallan) { " · **$fallan FALLAN**" } else { ' · **todo funciona**' })")
[void]$md.AppendLine('- **Evidencias completas:** `16_EVIDENCIAS_Y_PRUEBAS/salidas/ultima_ejecucion/` (una salida de SQL*Plus por paso, sin contraseñas).'); [void]$md.AppendLine('')
[void]$md.AppendLine('| Estado | Id | Qué se comprobó | Detalle |'); [void]$md.AppendLine('|---|---|---|---|')
foreach ($x in $resultados) { [void]$md.AppendLine("| $(if ($x.Estado -eq 'PASA') { '✅ PASA' } else { '❌ FALLA' }) | $($x.Id) | $($x.Que) | $($x.Detalle -replace '\|', '/') |") }
[IO.File]::WriteAllText((Join-Path $raiz '16_EVIDENCIAS_Y_PRUEBAS\RESUMEN_ULTIMA_EJECUCION.md'), $md.ToString(), $utf8)
Write-Host ("`n{0} de {1} comprobaciones PASAN. Resumen: 16_EVIDENCIAS_Y_PRUEBAS\RESUMEN_ULTIMA_EJECUCION.md" -f $pasan, $total) -ForegroundColor $(if ($fallan) { 'Red' } else { 'Green' })
exit $(if ($fallan) { 1 } else { 0 })

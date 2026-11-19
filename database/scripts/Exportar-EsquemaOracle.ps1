<#
.SYNOPSIS
  Extrae de Oracle el DDL real del esquema EcoAccess (un archivo por objeto) e inventario de objetos.

.DESCRIPTION
  Solo lectura: usa DBMS_METADATA y las vistas USER_*; no crea, cambia ni borra nada en Oracle.
  Escribe:
    database/schema/<tipo>/<objeto>.sql      DDL normalizado (sin esquema, sin almacenamiento ni tablespace)
    database/schema/00_instalar_esquema_completo.sql   orden de instalación para un esquema VACÍO
    database/schema/INVENTARIO.md            objeto, tipo, estado, dependencias, archivo y documentación

  La cadena de conexión se lee de los User Secrets de Windows (la misma que usa la aplicación); la contraseña
  se envía a SQL*Plus por la entrada estándar y nunca se escribe en disco, en la pantalla ni en los archivos.

.PARAMETER Secreto
  Id de User Secrets que contiene ConnectionStrings:OracleP3. Por defecto, el de la aplicación (Oracle Cloud).
  Use ecoaccess-p3-pruebas para el esquema aislado local.

.EXAMPLE
  pwsh -File database\scripts\Exportar-EsquemaOracle.ps1
.EXAMPLE
  pwsh -File database\scripts\Exportar-EsquemaOracle.ps1 -Secreto ecoaccess-p3-pruebas -Destino $env:TEMP\esquema_pruebas
#>
param(
    [string]$Secreto = 'ecoaccess-p3-framework',
    [string]$Destino
)
$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if (-not $Destino) { $Destino = Join-Path $raiz 'database\schema' }
$utf8 = New-Object Text.UTF8Encoding $false

# --- 1. Conexión desde User Secrets (sin mostrarla) -------------------------------------------------
. (Join-Path $PSScriptRoot 'ComunOracle.ps1')
$conexion = Get-EcoConexion -Secreto $Secreto

# --- 2. Extracción con DBMS_METADATA -----------------------------------------------------------------
$tmp = Join-Path ([IO.Path]::GetTempPath()) ("ecoaccess_ddl_" + [guid]::NewGuid().ToString('N'))
$tipos = 'tables', 'constraints', 'indexes', 'comments', 'views', 'packages', 'procedures', 'functions', 'triggers'
foreach ($t in $tipos) { New-Item -ItemType Directory -Force (Join-Path $tmp $t) | Out-Null }
$generador = @"
set heading off pagesize 0 linesize 32767 trimspool on feedback off verify off termout off
spool "$tmp\extraer.sql"
SELECT 'set heading off pagesize 0 linesize 32767 long 2000000 longchunksize 32767 trimspool on feedback off termout off' FROM dual;
SELECT 'exec DBMS_METADATA.SET_TRANSFORM_PARAM(DBMS_METADATA.SESSION_TRANSFORM,'''||p||''','||v||')' FROM (
  SELECT 'SQLTERMINATOR' p, 'TRUE' v FROM dual UNION ALL SELECT 'PRETTY','TRUE' FROM dual UNION ALL
  SELECT 'SEGMENT_ATTRIBUTES','FALSE' FROM dual UNION ALL SELECT 'STORAGE','FALSE' FROM dual UNION ALL
  SELECT 'REF_CONSTRAINTS','FALSE' FROM dual UNION ALL SELECT 'EMIT_SCHEMA','FALSE' FROM dual);
SELECT 'spool "$tmp\'||d||'\'||LOWER(n)||'.sql"'||CHR(10)||q||CHR(10)||'spool off' FROM (
  SELECT 'tables' d, table_name n, 'SELECT DBMS_METADATA.GET_DDL(''TABLE'','''||table_name||''') FROM dual;' q FROM user_tables
  UNION ALL SELECT DISTINCT 'constraints', table_name, 'SELECT DBMS_METADATA.GET_DEPENDENT_DDL(''REF_CONSTRAINT'','''||table_name||''') FROM dual;' FROM user_constraints WHERE constraint_type = 'R'
  UNION ALL SELECT 'indexes', index_name, 'SELECT DBMS_METADATA.GET_DDL(''INDEX'','''||index_name||''') FROM dual;' FROM user_indexes i
            WHERE index_type <> 'LOB' AND NOT EXISTS (SELECT 1 FROM user_constraints c WHERE c.index_name = i.index_name)
  UNION ALL SELECT DISTINCT 'comments', table_name, 'SELECT DBMS_METADATA.GET_DEPENDENT_DDL(''COMMENT'','''||table_name||''') FROM dual;'
            FROM user_tab_comments WHERE table_type = 'TABLE' AND comments IS NOT NULL
            UNION SELECT DISTINCT 'comments', c.table_name, 'SELECT DBMS_METADATA.GET_DEPENDENT_DDL(''COMMENT'','''||c.table_name||''') FROM dual;'
            FROM user_col_comments c JOIN user_tables t ON t.table_name = c.table_name WHERE c.comments IS NOT NULL
  UNION ALL SELECT 'views', view_name, 'SELECT DBMS_METADATA.GET_DDL(''VIEW'','''||view_name||''') FROM dual;' FROM user_views
  UNION ALL SELECT 'packages', object_name||'.pks', 'SELECT DBMS_METADATA.GET_DDL(''PACKAGE_SPEC'','''||object_name||''') FROM dual;' FROM user_objects WHERE object_type = 'PACKAGE'
  UNION ALL SELECT 'packages', object_name||'.pkb', 'SELECT DBMS_METADATA.GET_DDL(''PACKAGE_BODY'','''||object_name||''') FROM dual;' FROM user_objects WHERE object_type = 'PACKAGE BODY'
  UNION ALL SELECT 'procedures', object_name, 'SELECT DBMS_METADATA.GET_DDL(''PROCEDURE'','''||object_name||''') FROM dual;' FROM user_objects WHERE object_type = 'PROCEDURE'
  UNION ALL SELECT 'functions', object_name, 'SELECT DBMS_METADATA.GET_DDL(''FUNCTION'','''||object_name||''') FROM dual;' FROM user_objects WHERE object_type = 'FUNCTION'
  UNION ALL SELECT 'triggers', trigger_name, 'SELECT DBMS_METADATA.GET_DDL(''TRIGGER'','''||trigger_name||''') FROM dual;' FROM user_triggers
) ORDER BY d, n;
spool off
spool "$tmp\inventario.txt"
SELECT 'O|'||o.object_type||'|'||o.object_name||'|'||o.status||'|'||
       (SELECT LISTAGG(DISTINCT d.referenced_name, ', ') WITHIN GROUP (ORDER BY d.referenced_name) FROM user_dependencies d
        WHERE d.name = o.object_name AND d.type = o.object_type AND d.referenced_owner = USER AND d.referenced_name <> o.object_name)
FROM user_objects o WHERE o.object_type IN ('TABLE','VIEW','PACKAGE','PACKAGE BODY','PROCEDURE','FUNCTION','TRIGGER','SEQUENCE','INDEX')
ORDER BY 1;
SELECT 'F|'||c.table_name||'|'||LISTAGG(DISTINCT p.table_name, ', ') WITHIN GROUP (ORDER BY p.table_name)
FROM user_constraints c JOIN user_constraints p ON p.constraint_name = c.r_constraint_name
WHERE c.constraint_type = 'R' AND p.table_name <> c.table_name GROUP BY c.table_name ORDER BY 1;
SELECT 'S|'||s.sequence_name||'|'||i.table_name||'.'||i.column_name FROM user_sequences s
LEFT JOIN user_tab_identity_cols i ON i.sequence_name = s.sequence_name ORDER BY 1;
SELECT 'E|'||COUNT(*) FROM user_errors;
SELECT 'Q|'||SYS_CONTEXT('USERENV','CURRENT_SCHEMA')||'|'||(SELECT MAX(version) FROM product_component_version) FROM dual;
spool off
@"$tmp\extraer.sql"
"@
$r = Invoke-EcoSqlPlus -Conexion $conexion -Sql $generador -DetenerEnError
if ($r.Codigo -ne 0 -or $r.Texto -match 'ORA-\d{5}|SP2-\d{4}') { Remove-Item -Recurse -Force $tmp; throw "SQL*Plus falló:`n$($r.Texto)" }
$conexion.Clave = $null

# --- 3. Normalización: sin espacios finales, LF, cabecera común ------------------------------------
$lineasInv = Get-Content (Join-Path $tmp 'inventario.txt') -Encoding UTF8 | Where-Object { $_ -match '^[OFSEQ]\|' }
$esquema, $version = (($lineasInv | Where-Object { $_ -like 'Q|*' }) -split '\|')[1, 2]
$fecha = (Get-Date).ToString('yyyy-MM-dd')
if (Test-Path $Destino) { foreach ($t in $tipos) { Remove-Item -Recurse -Force (Join-Path $Destino $t) -ErrorAction SilentlyContinue } }
$archivos = @{}
foreach ($t in $tipos) {
    New-Item -ItemType Directory -Force (Join-Path $Destino $t) | Out-Null
    foreach ($f in Get-ChildItem (Join-Path $tmp $t) -Filter *.sql) {
        $texto = [IO.File]::ReadAllText($f.FullName, $utf8) -replace "`r", ''
        $lineas = ($texto -split "`n" | ForEach-Object { $_.TrimEnd() })
        $cuerpo = (($lineas -join "`n").Trim("`n")) -replace "`n{3,}", "`n`n"
        if (-not $cuerpo) { continue }
        $cabecera = "-- EcoAccess · $t/$($f.Name)`n-- DDL real extraído con DBMS_METADATA de $esquema (Oracle $version) el $fecha.`n" +
                    "-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.`n`n"
        [IO.File]::WriteAllText((Join-Path $Destino "$t\$($f.Name)"), $cabecera + $cuerpo + "`n", $utf8)
        $archivos["$t/$($f.Name)"] = $true
    }
}

# --- 4. Script maestro de instalación (orden de dependencias) --------------------------------------
$orden = @(
    @('tables', 'Tablas con PK, UNIQUE, CHECK y columnas IDENTITY (las secuencias ISEQ$$ las crea Oracle)', '*.sql'),
    @('constraints', 'Claves foráneas (después de todas las tablas)', '*.sql'),
    @('indexes', 'Índices que no respaldan una restricción (incluye el índice único de crédito inicial)', '*.sql'),
    @('comments', 'Comentarios del diccionario', '*.sql'),
    @('packages', 'Especificaciones de paquetes (las vistas usan PKG_DEPOSITO.ESTADO)', '*.pks.sql'),
    @('views', 'Vistas (FORCE: se compilan aunque falte un objeto que se crea después)', '*.sql'),
    @('functions', 'Funciones independientes', '*.sql'),
    @('procedures', 'Procedimientos independientes', '*.sql'),
    @('packages', 'Cuerpos de paquetes', '*.pkb.sql'),
    @('triggers', 'Disparadores (TRG_DEPOSITO_COHERENCIA va después de TRG_DEPOSITO_BLOQUEO_CERRADO: FOLLOWS)', '*.sql')
)
$m = New-Object Text.StringBuilder
[void]$m.Append(@"
-- =====================================================================
-- EcoAccess · instalación COMPLETA del esquema en un esquema de desarrollo VACÍO
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1 desde $esquema el $fecha.
--
-- NO ejecutar en el esquema compartido de Oracle Cloud (ECOACCESS_P3_APP): allí los objetos ya existen.
-- Ejecutar conectado como el DUEÑO del esquema nuevo (nunca SYS, SYSTEM ni ADMIN):
--     sql usuario@//localhost:1521/FREEPDB1 @database/schema/00_instalar_esquema_completo.sql
-- Se detiene en el primer error. Las sentencias DDL confirman solas (COMMIT implícito): si falla a mitad,
-- revise el error, corríjalo y reinstale en un esquema vacío nuevo; un ROLLBACK no deshace DDL.
-- =====================================================================
SET SERVEROUTPUT ON
SET DEFINE OFF
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK
DECLARE
    n NUMBER;
BEGIN
    IF SYS_CONTEXT('USERENV', 'SESSION_USER') IN ('SYS', 'SYSTEM', 'ADMIN') THEN
        RAISE_APPLICATION_ERROR(-20990, 'Instale con la cuenta dueña del esquema de desarrollo, no con una cuenta administrativa.');
    END IF;
    SELECT COUNT(*) INTO n FROM user_objects WHERE object_type IN ('TABLE', 'VIEW', 'PACKAGE', 'PROCEDURE', 'FUNCTION', 'TRIGGER');
    IF n > 0 THEN
        RAISE_APPLICATION_ERROR(-20991, 'El esquema ' || USER || ' no está vacío (' || n || ' objetos). Esta instalación es solo para un esquema nuevo.');
    END IF;
END;
/

"@)
foreach ($paso in $orden) {
    [void]$m.Append("PROMPT === $($paso[1])`n")
    foreach ($f in Get-ChildItem (Join-Path $Destino $paso[0]) -Filter $paso[2] | Sort-Object Name) { [void]$m.Append("@@$($paso[0])/$($f.Name)`n") }
    [void]$m.Append("`n")
}
[void]$m.Append(@"
PROMPT === Recompilar y comprobar
EXEC DBMS_UTILITY.COMPILE_SCHEMA(schema => USER, compile_all => FALSE)
DECLARE
    n NUMBER;
BEGIN
    SELECT COUNT(*) INTO n FROM user_objects WHERE status <> 'VALID';
    IF n > 0 THEN
        RAISE_APPLICATION_ERROR(-20992, n || ' objetos inválidos: SELECT name, type, line, text FROM user_errors;');
    END IF;
    DBMS_OUTPUT.PUT_LINE('Esquema instalado sin objetos inválidos. Siguiente paso: catálogos en database/03_datos_prueba (ver docs/database/04-instalacion.md).');
END;
/
"@)
[IO.File]::WriteAllText((Join-Path $Destino '00_instalar_esquema_completo.sql'), ($m.ToString() -replace "`r", ''), $utf8)

# --- 5. Inventario ----------------------------------------------------------------------------------
$docs = @{ 'TABLE' = '../../docs/database/05-tablas-relaciones.md'; 'VIEW' = '../../docs/database/06-vistas.md'; 'PACKAGE' = '../../docs/database/07-paquetes.md'
           'PACKAGE BODY' = '../../docs/database/07-paquetes.md'; 'PROCEDURE' = '../../docs/database/08-procedimientos.md'; 'FUNCTION' = '../../docs/database/09-funciones.md'
           'TRIGGER' = '../../docs/database/10-triggers-secuencias.md'; 'SEQUENCE' = '../../docs/database/10-triggers-secuencias.md'; 'INDEX' = '../../docs/database/05-tablas-relaciones.md' }
$carpeta = @{ 'TABLE' = 'tables'; 'VIEW' = 'views'; 'PROCEDURE' = 'procedures'; 'FUNCTION' = 'functions'; 'TRIGGER' = 'triggers'; 'INDEX' = 'indexes' }
$padres = @{}; foreach ($l in $lineasInv | Where-Object { $_ -like 'F|*' }) { $p = $l -split '\|'; $padres[$p[1]] = $p[2] }
$secuencias = @{}; foreach ($l in $lineasInv | Where-Object { $_ -like 'S|*' }) { $p = $l -split '\|'; $secuencias[$p[1]] = $p[2] }
$errores = (($lineasInv | Where-Object { $_ -like 'E|*' }) -split '\|')[1]
$inv = New-Object Text.StringBuilder
$objetos = $lineasInv | Where-Object { $_ -like 'O|*' } | ForEach-Object { $p = $_ -split '\|'; [pscustomobject]@{ Tipo = $p[1]; Nombre = $p[2]; Estado = $p[3]; Dep = $p[4] } }
[void]$inv.Append("# Inventario del esquema $esquema`n`nGenerado por ``database/scripts/Exportar-EsquemaOracle.ps1`` el $fecha desde Oracle $version, con las vistas ``USER_OBJECTS``, ``USER_DEPENDENCIES``, ``USER_CONSTRAINTS``, ``USER_SEQUENCES`` y ``USER_ERRORS``. No editar a mano.`n`n")
[void]$inv.Append("| Tipo | Cantidad | Inválidos |`n|---|---:|---:|`n")
foreach ($g in $objetos | Group-Object Tipo | Sort-Object Name) { [void]$inv.Append("| $($g.Name) | $($g.Count) | $(@($g.Group | Where-Object Estado -ne 'VALID').Count) |`n") }
[void]$inv.Append("`nErrores de compilación pendientes (``USER_ERRORS``): **$errores**. Sinónimos: los que haya no son parte de EcoAccess (el esquema no usa sinónimos).`n`n")
[void]$inv.Append("## Objetos`n`n| Objeto | Tipo | Estado | Dependencias (mismo esquema) | Archivo de origen | Documentación |`n|---|---|---|---|---|---|`n")
foreach ($o in $objetos | Where-Object { $_.Tipo -notin 'INDEX', 'SEQUENCE' } | Sort-Object Tipo, Nombre) {
    $dep = if ($o.Tipo -eq 'TABLE') { if ($padres[$o.Nombre]) { "FK → $($padres[$o.Nombre])" } else { '—' } } elseif ($o.Dep) { $o.Dep } else { '—' }
    $arch = switch ($o.Tipo) { 'PACKAGE' { "packages/$($o.Nombre.ToLower()).pks.sql" } 'PACKAGE BODY' { "packages/$($o.Nombre.ToLower()).pkb.sql" } default { "$($carpeta[$o.Tipo])/$($o.Nombre.ToLower()).sql" } }
    $enlace = if ($archivos[$arch]) { "[$arch]($arch)" } else { '—' }
    [void]$inv.Append("| ``$($o.Nombre)`` | $($o.Tipo) | $($o.Estado) | $dep | $enlace | [ver]($($docs[$o.Tipo])) |`n")
}
[void]$inv.Append("`n## Secuencias`n`nTodas son secuencias de sistema ``ISEQ`$`$_*`` que Oracle crea para las columnas ``GENERATED ALWAYS AS IDENTITY``. No se crean a mano ni se exportan: las vuelve a crear la definición de la tabla.`n`n| Secuencia | Columna identidad |`n|---|---|`n")
foreach ($k in $secuencias.Keys | Sort-Object) { [void]$inv.Append("| ``$k`` | ``$($secuencias[$k])`` |`n") }
$indices = @($objetos | Where-Object Tipo -eq 'INDEX')
[void]$inv.Append("`n## Índices`n`n$($indices.Count) índices en total. Los que respaldan una PK o UNIQUE se crean con la tabla; los demás están en [indexes/](indexes/).`n")
[IO.File]::WriteAllText((Join-Path $Destino 'INVENTARIO.md'), ($inv.ToString() -replace "`r", ''), $utf8)
Remove-Item -Recurse -Force $tmp

$total = (Get-ChildItem $Destino -Recurse -Filter *.sql).Count
Write-Host "Esquema $esquema exportado: $total archivos .sql en $Destino ($($objetos.Count) objetos, $errores errores de compilación)."

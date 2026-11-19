-- =====================================================================
-- EcoAccess P3 - migracion V029: repara el texto con doble codificacion UTF-8 que dejaron V026-V028
-- Archivo: database/06_migraciones/V029__corregir_codificacion_v026_v028.sql
-- Hallazgo: docs/database/16-hallazgos.md, H-18.
--
-- Problema: V026, V027 y V028 se aplicaron con un cliente que no estaba en UTF-8. Cada letra con tilde quedo
-- guardada como dos caracteres (por ejemplo U+00E1 se guardo como U+00C3 U+00A1). Afecta a filas de
-- PUNTO_DISPOSICION y PESO_REFERENCIAL, a la descripcion de esas versiones en ECO_MIGRACION_APLICADA, al codigo
-- fuente de paquetes, procedimientos y disparadores, y a textos que devuelven algunas vistas.
--
-- Reparacion: texto_bueno = RAW_TO_CHAR(STRING_TO_RAW(texto_danado, 'WE8MSWIN1252'), 'AL32UTF8').
-- Seguridad:
--   * Este archivo es solo ASCII: no se puede volver a danar aunque se ejecute con otra codificacion.
--   * FASE 1 calcula TODAS las reparaciones y exige que cada una sea exactamente reversible (al volver a danar
--     el texto reparado se obtiene el original). Si una sola falla, se detiene ANTES de cambiar nada.
--   * Solo toca lineas o valores que contienen las secuencias danadas (U+00C3, U+00C2 o U+00E2 U+20AC).
--   * Objetos: se regeneran con DBMS_METADATA y CREATE OR REPLACE (conserva permisos). Datos: UPDATE por ROWID
--     solo en PUNTO_DISPOSICION, PESO_REFERENCIAL y ECO_MIGRACION_APLICADA.
--   * Repetible: si no queda nada danado, no hace nada.
-- Aplicar con el bot detenido: al reemplazar PKG_CONTEXTO_APP y PKG_DEPOSITO, una sesion abierta que ya los
-- usaba recibe una vez ORA-04068 (estado del paquete descartado).
-- Reversion: no se revierte a texto danado. El DDL anterior queda en el historial de Git de database/schema.
-- =====================================================================
SET SERVEROUTPUT ON
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK

DECLARE
    c_a_tilde CONSTANT VARCHAR2(8) := UNISTR('\00C3');
    c_a_circ  CONSTANT VARCHAR2(8) := UNISTR('\00C2');
    c_euro    CONSTANT VARCHAR2(8) := UNISTR('\00E2\20AC');

    TYPE t_objeto IS RECORD (nombre VARCHAR2(128), tipo VARCHAR2(30), ddl CLOB);
    TYPE t_objetos IS TABLE OF t_objeto;
    TYPE t_fila IS RECORD (tabla VARCHAR2(128), columna VARCHAR2(128), fila ROWID, valor VARCHAR2(4000));
    TYPE t_filas IS TABLE OF t_fila;
    v_objetos t_objetos := t_objetos();
    v_filas   t_filas := t_filas();
    v_problemas PLS_INTEGER := 0;
    v_quedan    NUMBER;
    v_ddl       CLOB;
    v_cursor    SYS_REFCURSOR;
    v_rowid     ROWID;
    v_texto     VARCHAR2(4000);
    v_reparado  VARCHAR2(4000);
    v_pos       PLS_INTEGER;

    FUNCTION danada(p VARCHAR2) RETURN BOOLEAN IS
    BEGIN
        RETURN INSTR(p, c_a_tilde) > 0 OR INSTR(p, c_a_circ) > 0 OR INSTR(p, c_euro) > 0;
    END danada;

    -- Devuelve el texto reparado, el mismo texto si no estaba danado, o NULL si la reparacion no es reversible.
    FUNCTION reparar(p VARCHAR2) RETURN VARCHAR2 IS
        v VARCHAR2(32767);
    BEGIN
        IF p IS NULL OR NOT danada(p) THEN
            RETURN p;
        END IF;
        v := UTL_I18N.RAW_TO_CHAR(UTL_I18N.STRING_TO_RAW(p, 'WE8MSWIN1252'), 'AL32UTF8');
        IF v IS NULL OR danada(v)
           OR UTL_I18N.RAW_TO_CHAR(UTL_I18N.STRING_TO_RAW(v, 'AL32UTF8'), 'WE8MSWIN1252') <> p THEN
            RETURN NULL;
        END IF;
        RETURN v;
    END reparar;

    -- Repara un CLOB linea por linea. NULL si alguna linea no se puede reparar con seguridad.
    FUNCTION reparar_clob(p CLOB) RETURN CLOB IS
        r      CLOB;
        largo  PLS_INTEGER := DBMS_LOB.GETLENGTH(p);
        ini    PLS_INTEGER := 1;
        fin    PLS_INTEGER;
        linea  VARCHAR2(32767);
        buena  VARCHAR2(32767);
    BEGIN
        DBMS_LOB.CREATETEMPORARY(r, TRUE);
        WHILE ini <= largo LOOP
            fin := DBMS_LOB.INSTR(p, CHR(10), ini);
            IF fin = 0 THEN
                fin := largo + 1;
            END IF;
            linea := CASE WHEN fin > ini THEN DBMS_LOB.SUBSTR(p, fin - ini, ini) END;
            buena := reparar(linea);
            IF linea IS NOT NULL AND buena IS NULL THEN
                RETURN NULL;
            END IF;
            IF buena IS NOT NULL THEN
                DBMS_LOB.WRITEAPPEND(r, LENGTH(buena), buena);
            END IF;
            IF fin <= largo THEN
                DBMS_LOB.WRITEAPPEND(r, 1, CHR(10));
            END IF;
            ini := fin + 1;
        END LOOP;
        RETURN r;
    END reparar_clob;

    PROCEDURE preparar_objeto(p_nombre VARCHAR2, p_tipo VARCHAR2) IS
        v_bueno CLOB;
    BEGIN
        v_ddl := DBMS_METADATA.GET_DDL(CASE p_tipo WHEN 'PACKAGE' THEN 'PACKAGE_SPEC' WHEN 'PACKAGE BODY' THEN 'PACKAGE_BODY' ELSE p_tipo END, p_nombre);
        IF p_tipo = 'TRIGGER' THEN
            -- DBMS_METADATA agrega ALTER TRIGGER ... ENABLE: el disparador recreado ya queda habilitado.
            v_pos := DBMS_LOB.INSTR(v_ddl, 'ALTER TRIGGER');
            IF v_pos > 0 THEN
                v_ddl := RTRIM(DBMS_LOB.SUBSTR(v_ddl, v_pos - 1, 1), ' ' || CHR(9) || CHR(10) || CHR(13));
            END IF;
        END IF;
        IF DBMS_LOB.INSTR(v_ddl, c_a_tilde) = 0 AND DBMS_LOB.INSTR(v_ddl, c_a_circ) = 0 AND DBMS_LOB.INSTR(v_ddl, c_euro) = 0 THEN
            RETURN;
        END IF;
        v_bueno := reparar_clob(v_ddl);
        IF v_bueno IS NULL THEN
            v_problemas := v_problemas + 1;
            DBMS_OUTPUT.PUT_LINE('NO REVERSIBLE: ' || p_tipo || ' ' || p_nombre);
            RETURN;
        END IF;
        v_objetos.EXTEND;
        v_objetos(v_objetos.LAST).nombre := p_nombre;
        v_objetos(v_objetos.LAST).tipo := p_tipo;
        v_objetos(v_objetos.LAST).ddl := v_bueno;
    END preparar_objeto;
BEGIN
    IF SYS_CONTEXT('USERENV', 'SESSION_USER') <> SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA')
       OR SYS_CONTEXT('USERENV', 'SESSION_USER') IN ('SYS', 'SYSTEM', 'ADMIN') THEN
        RAISE_APPLICATION_ERROR(-20929, 'V029 se ejecuta conectado como el dueno del esquema.');
    END IF;
    DBMS_METADATA.SET_TRANSFORM_PARAM(DBMS_METADATA.SESSION_TRANSFORM, 'SQLTERMINATOR', FALSE);

    -- FASE 1. Calcular todo y validar, sin cambiar nada.
    FOR o IN (SELECT DISTINCT name, type FROM user_source
              WHERE INSTR(text, c_a_tilde) > 0 OR INSTR(text, c_a_circ) > 0 OR INSTR(text, c_euro) > 0
              ORDER BY DECODE(type, 'PACKAGE', 1, 'PACKAGE BODY', 2, 'PROCEDURE', 3, 'FUNCTION', 4, 'TRIGGER', 5, 9), name) LOOP
        preparar_objeto(o.name, o.type);
    END LOOP;
    FOR v IN (SELECT view_name FROM user_views ORDER BY view_name) LOOP
        preparar_objeto(v.view_name, 'VIEW');
    END LOOP;

    FOR c IN (SELECT table_name, column_name FROM user_tab_columns
              WHERE table_name IN ('PUNTO_DISPOSICION', 'PESO_REFERENCIAL', 'ECO_MIGRACION_APLICADA')
              AND data_type IN ('VARCHAR2', 'CHAR') ORDER BY table_name, column_id) LOOP
        OPEN v_cursor FOR 'SELECT ROWID, ' || DBMS_ASSERT.SIMPLE_SQL_NAME(c.column_name) || ' FROM ' || DBMS_ASSERT.SIMPLE_SQL_NAME(c.table_name)
            || ' WHERE INSTR(' || c.column_name || ', :a) > 0 OR INSTR(' || c.column_name || ', :b) > 0 OR INSTR(' || c.column_name || ', :c) > 0'
            USING c_a_tilde, c_a_circ, c_euro;
        LOOP
            FETCH v_cursor INTO v_rowid, v_texto;
            EXIT WHEN v_cursor%NOTFOUND;
            v_reparado := reparar(v_texto);
            IF v_reparado IS NULL THEN
                v_problemas := v_problemas + 1;
                DBMS_OUTPUT.PUT_LINE('NO REVERSIBLE: ' || c.table_name || '.' || c.column_name || ' fila ' || v_rowid);
            ELSE
                v_filas.EXTEND;
                v_filas(v_filas.LAST).tabla := c.table_name;
                v_filas(v_filas.LAST).columna := c.column_name;
                v_filas(v_filas.LAST).fila := v_rowid;
                v_filas(v_filas.LAST).valor := v_reparado;
            END IF;
        END LOOP;
        CLOSE v_cursor;
    END LOOP;

    DBMS_OUTPUT.PUT_LINE('V029 fase 1: ' || v_objetos.COUNT || ' objetos y ' || v_filas.COUNT || ' valores por reparar; '
        || v_problemas || ' no reversibles.');
    IF v_problemas > 0 THEN
        RAISE_APPLICATION_ERROR(-20930, 'V029 no cambio nada: ' || v_problemas || ' textos no se pueden reparar con seguridad.');
    END IF;

    -- FASE 2. Datos (una transaccion) y despues objetos (cada CREATE OR REPLACE confirma solo).
    FOR i IN 1 .. v_filas.COUNT LOOP
        EXECUTE IMMEDIATE 'UPDATE ' || DBMS_ASSERT.SIMPLE_SQL_NAME(v_filas(i).tabla) || ' SET '
            || DBMS_ASSERT.SIMPLE_SQL_NAME(v_filas(i).columna) || ' = :v WHERE ROWID = :r'
            USING v_filas(i).valor, v_filas(i).fila;
    END LOOP;
    COMMIT;
    FOR i IN 1 .. v_objetos.COUNT LOOP
        EXECUTE IMMEDIATE v_objetos(i).ddl;
        DBMS_OUTPUT.PUT_LINE('   recreado ' || v_objetos(i).tipo || ' ' || v_objetos(i).nombre);
    END LOOP;

    -- FASE 3. Recompilar y comprobar.
    DBMS_UTILITY.COMPILE_SCHEMA(schema => USER, compile_all => FALSE);
    SELECT COUNT(*) INTO v_quedan FROM user_objects WHERE status <> 'VALID';
    IF v_quedan > 0 THEN
        RAISE_APPLICATION_ERROR(-20931, 'V029: quedaron ' || v_quedan || ' objetos invalidos (ver USER_ERRORS).');
    END IF;
    SELECT COUNT(*) INTO v_quedan FROM user_source WHERE INSTR(text, c_a_tilde) > 0 OR INSTR(text, c_a_circ) > 0 OR INSTR(text, c_euro) > 0;
    SELECT v_quedan + COUNT(*) INTO v_quedan FROM punto_disposicion
    WHERE INSTR(nombre || recibe || rutas || fuente || NVL(direccion, '-') || NVL(horario, '-'), c_a_tilde) > 0;
    SELECT v_quedan + COUNT(*) INTO v_quedan FROM peso_referencial WHERE INSTR(descripcion_objeto || fuente, c_a_tilde) > 0;
    IF v_quedan > 0 THEN
        RAISE_APPLICATION_ERROR(-20932, 'V029: quedan ' || v_quedan || ' textos danados.');
    END IF;
    DBMS_OUTPUT.PUT_LINE('V029 aplicada: ' || v_objetos.COUNT || ' objetos recreados, ' || v_filas.COUNT
        || ' valores reparados, 0 objetos invalidos, 0 textos danados.');
END;
/

-- =====================================================================
-- Extrae los metadatos REALES de ECOACCESS_BD (solo lectura) en formato
-- de texto separado por «|», una línea por columna. Lo usa
-- generar_diccionario.ps1 para construir el diccionario de datos.
-- Formato: COL|tabla|columna|posición|tipo|nulo|default|identidad|pk|uk|fk|checks
-- =====================================================================
SET LINESIZE 32767
SET TRIMSPOOL ON
SET SERVEROUTPUT ON SIZE UNLIMITED FORMAT TRUNCATED
SET FEEDBACK OFF
DECLARE
    v_default VARCHAR2(4000);
    v_tipo    VARCHAR2(100);
    v_pk      VARCHAR2(10);
    v_uk      VARCHAR2(400);
    v_fk      VARCHAR2(400);
    v_ck      VARCHAR2(4000);
    v_ident   VARCHAR2(40);
BEGIN
    FOR c IN (SELECT tc.table_name, tc.column_name, tc.column_id, tc.data_type, tc.data_length, tc.char_length,
                     tc.char_used, tc.data_precision, tc.data_scale, tc.nullable
              FROM   user_tab_columns tc JOIN user_tables t ON t.table_name = tc.table_name
              ORDER  BY tc.table_name, tc.column_id) LOOP
        SELECT data_default INTO v_default FROM user_tab_columns
        WHERE  table_name = c.table_name AND column_name = c.column_name;
        v_default := TRIM(REPLACE(REPLACE(v_default, CHR(10), ' '), CHR(13), ''));
        v_tipo := c.data_type ||
            CASE WHEN c.data_type = 'VARCHAR2' THEN '(' || c.char_length || CASE c.char_used WHEN 'C' THEN ' CHAR' ELSE ' BYTE' END || ')'
                 WHEN c.data_type = 'RAW' THEN '(' || c.data_length || ')'
                 WHEN c.data_type = 'NUMBER' AND c.data_precision IS NOT NULL
                      THEN '(' || c.data_precision || CASE WHEN c.data_scale > 0 THEN ',' || c.data_scale END || ')'
                 ELSE '' END;
        BEGIN
            SELECT generation_type INTO v_ident FROM user_tab_identity_cols
            WHERE table_name = c.table_name AND column_name = c.column_name;
            v_ident := 'IDENTITY ' || v_ident;
            v_default := NULL;     -- el default de una IDENTITY es la secuencia interna
        EXCEPTION WHEN NO_DATA_FOUND THEN v_ident := NULL;
        END;
        SELECT MAX(CASE WHEN k.constraint_type = 'P' THEN 'PK' END) INTO v_pk
        FROM user_cons_columns cc JOIN user_constraints k ON k.constraint_name = cc.constraint_name
        WHERE cc.table_name = c.table_name AND cc.column_name = c.column_name;
        SELECT LISTAGG(k.constraint_name, ', ') WITHIN GROUP (ORDER BY k.constraint_name) INTO v_uk
        FROM user_cons_columns cc JOIN user_constraints k ON k.constraint_name = cc.constraint_name
        WHERE cc.table_name = c.table_name AND cc.column_name = c.column_name AND k.constraint_type = 'U';
        SELECT LISTAGG(k.constraint_name || '->' || r.table_name, ', ') WITHIN GROUP (ORDER BY k.constraint_name) INTO v_fk
        FROM user_cons_columns cc JOIN user_constraints k ON k.constraint_name = cc.constraint_name
        JOIN user_constraints r ON r.constraint_name = k.r_constraint_name
        WHERE cc.table_name = c.table_name AND cc.column_name = c.column_name AND k.constraint_type = 'R';
        SELECT LISTAGG(k.constraint_name, ', ') WITHIN GROUP (ORDER BY k.constraint_name) INTO v_ck
        FROM user_cons_columns cc JOIN user_constraints k ON k.constraint_name = cc.constraint_name
        WHERE cc.table_name = c.table_name AND cc.column_name = c.column_name AND k.constraint_type = 'C'
          AND k.constraint_name NOT LIKE 'SYS_C%';
        DBMS_OUTPUT.PUT_LINE('COL|' || c.table_name || '|' || c.column_name || '|' || c.column_id || '|' || v_tipo || '|'
            || c.nullable || '|' || v_default || '|' || v_ident || '|' || v_pk || '|' || v_uk || '|' || v_fk || '|' || v_ck);
    END LOOP;
    FOR k IN (SELECT constraint_name, table_name, search_condition_vc FROM user_constraints
              WHERE constraint_type = 'C' AND constraint_name NOT LIKE 'SYS_C%' ORDER BY table_name, constraint_name) LOOP
        DBMS_OUTPUT.PUT_LINE('CK|' || k.table_name || '|' || k.constraint_name || '|'
            || REGEXP_REPLACE(k.search_condition_vc, '\s+', ' '));
    END LOOP;
END;
/

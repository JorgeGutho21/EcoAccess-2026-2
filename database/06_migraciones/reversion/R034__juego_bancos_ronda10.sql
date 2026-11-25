-- Reversión de V034 (bancos de preguntas y ronda de 10). Ejecutar DESPUÉS de R035 (paquete de V032 restaurado).
-- Retira SOLO lo que creó V034: disparadores, competencias del banco de 10 (las de prueba con sus datos; la oficial
-- preparada solo si no tiene participantes), preguntas 101..110 y sus opciones, la columna BANCO_ID, las tablas de bancos
-- y los CHECK ampliados (restaura los de 7 preguntas).
-- Se DETIENE sin borrar nada si una competencia OFICIAL del banco de 10 ya tiene participantes: exporta primero sus
-- resultados (panel → Exportar CSV) y decide; las competencias de 7 preguntas y sus datos nunca se tocan.
WHENEVER SQLERROR EXIT SQL.SQLCODE
SET SERVEROUTPUT ON
DECLARE
    n NUMBER;
    PROCEDURE ddl_si(p_condicion NUMBER, p_ddl VARCHAR2, p_texto VARCHAR2) IS
    BEGIN
        IF p_condicion > 0 THEN EXECUTE IMMEDIATE p_ddl; DBMS_OUTPUT.PUT_LINE('R034: ' || p_texto); END IF;
    END;
    FUNCTION existe(p_tipo VARCHAR2, p_nombre VARCHAR2) RETURN NUMBER IS
        r NUMBER;
    BEGIN
        SELECT COUNT(*) INTO r FROM user_objects WHERE object_type = p_tipo AND object_name = p_nombre;
        RETURN r;
    END;
    FUNCTION restriccion(p_nombre VARCHAR2) RETURN NUMBER IS
        r NUMBER;
    BEGIN
        SELECT COUNT(*) INTO r FROM user_constraints WHERE table_name = 'ECO_JUEGO_INTENTOS' AND constraint_name = p_nombre;
        RETURN r;
    END;
BEGIN
    EXECUTE IMMEDIATE 'ALTER SESSION SET ddl_lock_timeout = 30';
    SELECT COUNT(*) INTO n FROM user_tab_columns WHERE table_name = 'ECO_JUEGO_COMPETENCIAS' AND column_name = 'BANCO_ID';
    IF n = 1 THEN
        EXECUTE IMMEDIATE q'[SELECT COUNT(*) FROM eco_juego_participantes p JOIN eco_juego_competencias c ON c.id = p.competencia_id
                              WHERE NVL(c.banco_id, 1) <> 1 AND c.es_prueba = 'N']' INTO n;
        IF n > 0 THEN
            RAISE_APPLICATION_ERROR(-20434, 'R034 detenida: hay ' || n || ' participantes en competencias oficiales del banco de 10. Exporta sus resultados antes de revertir.');
        END IF;
        ddl_si(existe('TRIGGER', 'TR_ECO_JUEGO_COMP_BANCO'), 'DROP TRIGGER tr_eco_juego_comp_banco', 'disparador TR_ECO_JUEGO_COMP_BANCO retirado');
        ddl_si(existe('TRIGGER', 'TR_ECO_JUEGO_BANCO_INMUTABLE'), 'DROP TRIGGER tr_eco_juego_banco_inmutable', 'disparador TR_ECO_JUEGO_BANCO_INMUTABLE retirado');
        -- Competencias del banco de 10 (de prueba, o la oficial preparada sin participantes) y sus datos.
        EXECUTE IMMEDIATE q'[DELETE FROM eco_juego_respuestas WHERE intento_id IN (SELECT i.id FROM eco_juego_intentos i
                               JOIN eco_juego_competencias c ON c.id = i.competencia_id WHERE NVL(c.banco_id, 1) <> 1)]';
        EXECUTE IMMEDIATE q'[DELETE FROM eco_juego_intentos WHERE competencia_id IN (SELECT id FROM eco_juego_competencias WHERE NVL(banco_id, 1) <> 1)]';
        EXECUTE IMMEDIATE q'[DELETE FROM eco_juego_participantes WHERE competencia_id IN (SELECT id FROM eco_juego_competencias WHERE NVL(banco_id, 1) <> 1)]';
        EXECUTE IMMEDIATE q'[DELETE FROM eco_juego_competencias WHERE NVL(banco_id, 1) <> 1]';
        DBMS_OUTPUT.PUT_LINE('R034: ' || SQL%ROWCOUNT || ' competencias del banco de 10 retiradas');
        SELECT COUNT(*) INTO n FROM user_constraints WHERE table_name = 'ECO_JUEGO_COMPETENCIAS' AND constraint_name = 'FK_ECO_JUEGO_COMP_BANCO';
        ddl_si(n, 'ALTER TABLE eco_juego_competencias DROP CONSTRAINT fk_eco_juego_comp_banco', 'FK_ECO_JUEGO_COMP_BANCO retirada');
        ddl_si(existe('INDEX', 'IX_ECO_JUEGO_COMP_BANCO'), 'DROP INDEX ix_eco_juego_comp_banco', 'índice IX_ECO_JUEGO_COMP_BANCO retirado');
        ddl_si(1, 'ALTER TABLE eco_juego_competencias DROP COLUMN banco_id', 'columna BANCO_ID retirada');
    END IF;
    ddl_si(existe('TABLE', 'ECO_JUEGO_BANCO_PREGUNTAS'), 'DROP TABLE eco_juego_banco_preguntas PURGE', 'tabla ECO_JUEGO_BANCO_PREGUNTAS borrada');
    ddl_si(existe('TABLE', 'ECO_JUEGO_BANCOS'), 'DROP TABLE eco_juego_bancos PURGE', 'tabla ECO_JUEGO_BANCOS borrada');
    -- Preguntas nuevas (101..110) y sus opciones; ninguna respuesta las referencia ya.
    DELETE FROM eco_juego_respuestas WHERE pregunta_id BETWEEN 101 AND 110;
    DELETE FROM eco_juego_opciones WHERE pregunta_id BETWEEN 101 AND 110;
    DELETE FROM eco_juego_preguntas WHERE id BETWEEN 101 AND 110;
    COMMIT;
    -- Restricciones de 7 preguntas: se crean antes de retirar las ampliadas.
    ddl_si(1 - restriccion('CK_ECO_JUEGO_INT_PUNTAJE'), 'ALTER TABLE eco_juego_intentos ADD CONSTRAINT ck_eco_juego_int_puntaje CHECK (puntaje BETWEEN 0 AND 700 AND MOD(puntaje, 100) = 0)', 'CK_ECO_JUEGO_INT_PUNTAJE restaurada');
    ddl_si(1 - restriccion('CK_ECO_JUEGO_INT_CONTEO'), 'ALTER TABLE eco_juego_intentos ADD CONSTRAINT ck_eco_juego_int_conteo CHECK (aciertos + errores <= 7 AND aciertos >= 0 AND errores >= 0)', 'CK_ECO_JUEGO_INT_CONTEO restaurada');
    ddl_si(1 - restriccion('CK_ECO_JUEGO_INT_PREGUNTA'), 'ALTER TABLE eco_juego_intentos ADD CONSTRAINT ck_eco_juego_int_pregunta CHECK (pregunta_actual BETWEEN 1 AND 8)', 'CK_ECO_JUEGO_INT_PREGUNTA restaurada');
    ddl_si(restriccion('CK_ECO_JUEGO_INT_PUNTAJE10'), 'ALTER TABLE eco_juego_intentos DROP CONSTRAINT ck_eco_juego_int_puntaje10', 'CK_ECO_JUEGO_INT_PUNTAJE10 retirada');
    ddl_si(restriccion('CK_ECO_JUEGO_INT_CONTEO10'), 'ALTER TABLE eco_juego_intentos DROP CONSTRAINT ck_eco_juego_int_conteo10', 'CK_ECO_JUEGO_INT_CONTEO10 retirada');
    ddl_si(restriccion('CK_ECO_JUEGO_INT_PREGUNTA10'), 'ALTER TABLE eco_juego_intentos DROP CONSTRAINT ck_eco_juego_int_pregunta10', 'CK_ECO_JUEGO_INT_PREGUNTA10 retirada');
END;
/
PROMPT R034_JUEGO_BANCOS_RONDA10_REVERTIDA

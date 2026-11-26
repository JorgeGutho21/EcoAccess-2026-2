-- Reversión de T003: devuelve el lote SIM-2026-11-CIERRE desde las tablas ECO_RESP_T003_* (padres primero).
-- No toca los nombres de las tres canecas físicas ni la Estación Oficial. Manual: ningún instalador la ejecuta.
-- Se detiene sin cambios si el lote ya existe o si falta algún respaldo.
DECLARE
    n NUMBER;
BEGIN
    SELECT COUNT(*) INTO n FROM user_tables WHERE table_name LIKE 'ECO\_RESP\_T003\_%' ESCAPE '\';
    IF n < 14 THEN RAISE_APPLICATION_ERROR(-20365, 'R_T003: faltan tablas de respaldo ECO_RESP_T003_* (' || n || ').'); END IF;
    SELECT COUNT(*) INTO n FROM lote_simulacion WHERE id_lote = 'SIM-2026-11-CIERRE';
    IF n > 0 THEN RAISE_APPLICATION_ERROR(-20366, 'R_T003: el lote ya existe; no se restaura.'); END IF;
    EXECUTE IMMEDIATE 'INSERT INTO lote_simulacion SELECT * FROM eco_resp_t003_lote';
    EXECUTE IMMEDIATE 'INSERT INTO punto_instalacion SELECT * FROM eco_resp_t003_punto';
    EXECUTE IMMEDIATE 'INSERT INTO usuario SELECT * FROM eco_resp_t003_usuario';
    EXECUTE IMMEDIATE 'INSERT INTO usuario_rol SELECT * FROM eco_resp_t003_usuario_rol';
    EXECUTE IMMEDIATE 'INSERT INTO contenedor SELECT * FROM eco_resp_t003_contenedor';
    EXECUTE IMMEDIATE 'INSERT INTO contenedor_material SELECT * FROM eco_resp_t003_cont_mat';
    EXECUTE IMMEDIATE 'INSERT INTO configuracion_caneca SELECT * FROM eco_resp_t003_config';
    EXECUTE IMMEDIATE 'INSERT INTO historial_estado_contenedor SELECT * FROM eco_resp_t003_historial';
    EXECUTE IMMEDIATE 'INSERT INTO interaccion SELECT * FROM eco_resp_t003_interaccion';
    EXECUTE IMMEDIATE 'INSERT INTO orden_apertura SELECT * FROM eco_resp_t003_orden';
    EXECUTE IMMEDIATE 'INSERT INTO evento_dispositivo SELECT * FROM eco_resp_t003_evento';
    EXECUTE IMMEDIATE 'INSERT INTO deposito SELECT * FROM eco_resp_t003_deposito';
    EXECUTE IMMEDIATE 'INSERT INTO auditoria_deposito SELECT * FROM eco_resp_t003_auditoria';
    EXECUTE IMMEDIATE 'INSERT INTO registro_simulacion SELECT * FROM eco_resp_t003_registro';
    COMMIT;
    DBMS_OUTPUT.PUT_LINE('R_T003: lote SIM-2026-11-CIERRE restaurado desde los respaldos.');
EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK;
        RAISE;
END;

-- =====================================================================
-- EcoAccess BD — 20 · Prueba de concurrencia de pkg_ecopuntos.registrar_regla
-- Ejecutar como ECOACCESS_BD. Es UNA de las dos sesiones de la prueba:
-- 18_ENTREGA_Y_RESPALDOS\herramientas\prueba_concurrencia_tarifas.ps1 abre la sesión A,
-- espera dos segundos y abre la sesión B, y luego compara las horas que imprime cada una.
--
-- Qué se prueba: las dos sesiones registran a la vez la PRIMERA tarifa de PLASTICO_RIGIDO,
-- un material que no tiene ninguna regla. La sesión A registra y retiene su transacción
-- unos segundos sin confirmarla. Si registrar_regla bloquea la fila padre de MATERIAL,
-- la sesión B tiene que esperar a que A termine; si no la bloquea, B registra al mismo
-- tiempo y quedan dos tarifas abiertas para el mismo material.
--
-- Las dos sesiones terminan con ROLLBACK: la prueba no deja datos nuevos.
-- =====================================================================
SET SERVEROUTPUT ON SIZE UNLIMITED
SET FEEDBACK OFF
SET VERIFY OFF
ACCEPT papel   CHAR   PROMPT 'Sesión (A o B): '
ACCEPT retener NUMBER PROMPT 'Segundos que la sesión A retiene la transacción: '

DECLARE
    v_adm    NUMBER;
    v_mat    NUMBER;
    v_regla  NUMBER;
    v_vistas NUMBER;
    FUNCTION ahora RETURN VARCHAR2 IS
    BEGIN
        RETURN TO_CHAR(SYSTIMESTAMP, 'HH24:MI:SS.FF3');
    END;
BEGIN
    SELECT id_usuario  INTO v_adm FROM usuario  WHERE correo_electronico = 'administradora@ejemplo.test';
    SELECT id_material INTO v_mat FROM material WHERE codigo = 'PLASTICO_RIGIDO';
    SELECT COUNT(*) INTO v_vistas FROM regla_ecopuntos WHERE id_material = v_mat;
    DBMS_OUTPUT.PUT_LINE('REGLAS_PREVIAS_&papel=' || v_vistas);
    DBMS_OUTPUT.PUT_LINE('INICIO_&papel=' || ahora);

    pkg_ecopuntos.registrar_regla(v_mat, 7, 1, 'Prueba de concurrencia (sesión &papel)',
                                  SYS_EXTRACT_UTC(SYSTIMESTAMP), v_adm, 1, v_regla);
    DBMS_OUTPUT.PUT_LINE('REGISTRO_&papel=' || ahora);

    SELECT COUNT(*) INTO v_vistas FROM regla_ecopuntos WHERE id_material = v_mat AND hasta_utc IS NULL;
    DBMS_OUTPUT.PUT_LINE('ABIERTAS_VISTAS_&papel=' || v_vistas);

    IF '&papel' = 'A' THEN
        DBMS_SESSION.SLEEP(&retener);       -- A retiene la transacción sin confirmarla
    END IF;
    ROLLBACK;
    DBMS_OUTPUT.PUT_LINE('FIN_&papel=' || ahora);
EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK;
        DBMS_OUTPUT.PUT_LINE('ERROR_&papel=' || SQLERRM);
END;
/
SET FEEDBACK ON

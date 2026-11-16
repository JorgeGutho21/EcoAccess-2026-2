-- =====================================================================
-- EcoAccess P3 — T001: matriz de ecopuntos propuesta (misión de cierre, versión 2026.11.10-1)
-- Archivo: database/07_operacion/T001__tarifas_ecopuntos_propuestas.sql
--
-- Crea una regla por unidad (por_unidad = 1) para cada material con puntos:
--   lata 5, PET 4, cartón 3, vidrio 3, acero (METAL) 3, papel 2, multicapa 2.
-- Los materiales con 0 (flexibles, icopor, orgánicos, no aprovechables, sanitarios y
-- peligrosos) NO reciben regla: sin regla vigente el sistema no acredita nada.
-- Misma matriz que src/EcoAccess.P3.Logica/Servicios/MatrizEcopuntosPropuesta.cs.
--
-- NO es una migración y ningún instalador la ejecuta. La ejecuta un administrador
-- REAL, que queda como creador de cada regla: en una copia, reemplace <<ID_ADMINISTRADOR>> por su id.
-- Se niega si ese id no es una cuenta activa con rol Administrador (3).
-- Idempotente: no duplica una regla vigente del mismo material.
-- =====================================================================
SET SERVEROUTPUT ON
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK
DECLARE
    n NUMBER;
    v_admin NUMBER;
BEGIN
    SELECT TO_NUMBER('<<ID_ADMINISTRADOR>>' DEFAULT NULL ON CONVERSION ERROR) INTO v_admin FROM dual;
    SELECT COUNT(*) INTO n FROM usuario u JOIN usuario_rol r ON r.id_usuario = u.id_usuario JOIN rol ro ON ro.id_rol = r.id_rol
     WHERE u.id_usuario = v_admin AND u.activo = 1 AND ro.codigo = 3;
    IF v_admin IS NULL OR n <> 1 THEN RAISE_APPLICATION_ERROR(-20301, 'T001 requiere un administrador activo real como creador.'); END IF;
    FOR t IN (SELECT 'LATA_ALUMINIO' codigo, 5 puntos FROM dual UNION ALL SELECT 'PET', 4 FROM dual
              UNION ALL SELECT 'CARTON', 3 FROM dual UNION ALL SELECT 'VIDRIO', 3 FROM dual
              UNION ALL SELECT 'METAL', 3 FROM dual UNION ALL SELECT 'PAPEL', 2 FROM dual
              UNION ALL SELECT 'MULTICAPA', 2 FROM dual) LOOP
        INSERT INTO regla_ecopuntos (id_material, puntos, descripcion, desde_utc, hasta_utc, id_creador, por_unidad)
        SELECT m.id_material, t.puntos, 'Matriz propuesta 2026.11.10-1: ' || t.puntos || ' por unidad',
               SYS_EXTRACT_UTC(SYSTIMESTAMP), NULL, v_admin, 1
        FROM material m
        WHERE m.codigo = t.codigo AND m.habilitado = 1 AND m.categoria = 1
          AND NOT EXISTS (SELECT 1 FROM regla_ecopuntos r WHERE r.id_material = m.id_material
                          AND (r.hasta_utc IS NULL OR r.hasta_utc > SYS_EXTRACT_UTC(SYSTIMESTAMP)));
        DBMS_OUTPUT.PUT_LINE(t.codigo || ': ' || SQL%ROWCOUNT || ' regla(s) nueva(s)');
    END LOOP;
    COMMIT;
END;
/

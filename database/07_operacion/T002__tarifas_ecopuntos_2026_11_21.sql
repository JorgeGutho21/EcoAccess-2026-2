-- =====================================================================
-- EcoAccess P3 — T002: matriz de ecopuntos 2026.11.21-1 (APROBADA el 21/11/2026 por el responsable del proyecto)
-- Archivo: database/07_operacion/T002__tarifas_ecopuntos_2026_11_21.sql
--
-- Sin peso: puntos por unidad, solo tras un depósito confirmado y verificado (las reglas no cambian eso).
--   Caneca blanca: LATA_ALUMINIO 10, PET 8, CARTON 7, VIDRIO 7, PAPEL 5
--                  (METAL 3 y MULTICAPA 2 se conservan de T001 hasta que se reevalúen).
--   Caneca verde : ORGANICO 2. Oracle tiene un único código orgánico: distinguir frutas (4), café (3) y
--                  hojas (2) exige dividir ese material en una migración aparte; mientras tanto se usa el menor.
--   Caneca negra : sin regla = 0 puntos (la tabla no admite reglas de 0).
-- Misma matriz que src/EcoAccess.P3.Logica/Servicios/MatrizEcopuntosPropuesta.cs.
--
-- NO es una migración y ningún instalador la ejecuta. La ejecuta un administrador REAL (la matriz ya está aprobada):
-- en una copia, reemplace <<ID_ADMINISTRADOR>> por su id. Se niega si no es una cuenta activa con rol Administrador.
-- Las reglas son versiones: cierra la vigente (hasta_utc = ahora) y crea la nueva. Los movimientos ya acreditados
-- guardan sus puntos y su regla, así que el historial no cambia. Idempotente: si la vigente ya tiene el valor, no hace nada.
-- Reversión: cerrar las reglas creadas por este script y volver a ejecutar T001.
-- =====================================================================
SET SERVEROUTPUT ON
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK
DECLARE
    n NUMBER;
    v_admin NUMBER;
    v_ahora TIMESTAMP := SYS_EXTRACT_UTC(SYSTIMESTAMP);
BEGIN
    SELECT TO_NUMBER('<<ID_ADMINISTRADOR>>' DEFAULT NULL ON CONVERSION ERROR) INTO v_admin FROM dual;
    SELECT COUNT(*) INTO n FROM usuario u JOIN usuario_rol r ON r.id_usuario = u.id_usuario JOIN rol ro ON ro.id_rol = r.id_rol
     WHERE u.id_usuario = v_admin AND u.activo = 1 AND ro.codigo = 3;
    IF v_admin IS NULL OR n <> 1 THEN RAISE_APPLICATION_ERROR(-20302, 'T002 requiere un administrador activo real como creador.'); END IF;
    FOR t IN (SELECT 'LATA_ALUMINIO' codigo, 10 puntos FROM dual UNION ALL SELECT 'PET', 8 FROM dual
              UNION ALL SELECT 'CARTON', 7 FROM dual UNION ALL SELECT 'VIDRIO', 7 FROM dual
              UNION ALL SELECT 'PAPEL', 5 FROM dual UNION ALL SELECT 'METAL', 3 FROM dual
              UNION ALL SELECT 'MULTICAPA', 2 FROM dual UNION ALL SELECT 'ORGANICO', 2 FROM dual) LOOP
        -- Cierra la vigente solo si tiene otro valor.
        UPDATE regla_ecopuntos r SET r.hasta_utc = v_ahora
         WHERE r.id_material IN (SELECT m.id_material FROM material m WHERE m.codigo = t.codigo AND m.categoria IN (1, 2))
           AND (r.hasta_utc IS NULL OR r.hasta_utc > v_ahora) AND r.desde_utc < v_ahora AND r.puntos <> t.puntos;
        INSERT INTO regla_ecopuntos (id_material, puntos, descripcion, desde_utc, hasta_utc, id_creador, por_unidad)
        SELECT m.id_material, t.puntos, 'Matriz propuesta 2026.11.21-1: ' || t.puntos || ' por unidad', v_ahora, NULL, v_admin, 1
        FROM material m
        WHERE m.codigo = t.codigo AND m.habilitado = 1 AND m.categoria IN (1, 2)
          AND NOT EXISTS (SELECT 1 FROM regla_ecopuntos r WHERE r.id_material = m.id_material
                          AND (r.hasta_utc IS NULL OR r.hasta_utc > v_ahora));
        DBMS_OUTPUT.PUT_LINE(t.codigo || ': ' || SQL%ROWCOUNT || ' regla(s) nueva(s)');
    END LOOP;
    COMMIT;
END;
/

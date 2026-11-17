-- Ensayo de T001 (solo esquema aislado): cierra las reglas vigentes DENTRO de la transacción, ejecuta el bloque de T001
-- dos veces sin COMMIT, prueba un id inválido y hace ROLLBACK. Ejercita la rama que inserta reglas sin borrar historia.
WHENEVER SQLERROR EXIT 1 ROLLBACK
SELECT COUNT(*) reglas_antes, SUM(CASE WHEN hasta_utc IS NULL OR hasta_utc > SYS_EXTRACT_UTC(SYSTIMESTAMP) THEN 1 ELSE 0 END) vigentes_antes FROM regla_ecopuntos;
COLUMN admin NEW_VALUE adm
SELECT (SELECT MIN(u.id_usuario) FROM usuario u JOIN usuario_rol r ON r.id_usuario=u.id_usuario JOIN rol ro ON ro.id_rol=r.id_rol WHERE u.activo=1 AND ro.codigo=3) admin FROM dual;
-- Ensayo: se cierran las reglas vigentes DENTRO de la transacción para que la base quede sin reglas vigentes.
UPDATE regla_ecopuntos SET hasta_utc = SYS_EXTRACT_UTC(SYSTIMESTAMP) - INTERVAL '1' SECOND WHERE hasta_utc IS NULL OR hasta_utc > SYS_EXTRACT_UTC(SYSTIMESTAMP);
DECLARE
    n NUMBER;
    v_admin NUMBER;
BEGIN
    SELECT TO_NUMBER('&adm' DEFAULT NULL ON CONVERSION ERROR) INTO v_admin FROM dual;
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
    NULL; -- (ensayo: sin COMMIT)
END;
/
PROMPT == primera ejecucion terminada; segunda ejecucion (idempotencia):
DECLARE
    n NUMBER;
    v_admin NUMBER;
BEGIN
    SELECT TO_NUMBER('&adm' DEFAULT NULL ON CONVERSION ERROR) INTO v_admin FROM dual;
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
    NULL;
END;
/
SELECT m.codigo, r.puntos, r.por_unidad, r.id_creador, CASE WHEN r.hasta_utc IS NULL THEN 'VIGENTE' END vig FROM regla_ecopuntos r JOIN material m ON m.id_material=r.id_material WHERE r.hasta_utc IS NULL ORDER BY r.puntos DESC, m.codigo;
PROMPT == prueba negativa: id de un ciudadano debe rechazarse con ORA-20301
WHENEVER SQLERROR CONTINUE
DECLARE
    n NUMBER;
    v_admin NUMBER;
BEGIN
    SELECT TO_NUMBER('0' DEFAULT NULL ON CONVERSION ERROR) INTO v_admin FROM dual;
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
    NULL;
END;
/
ROLLBACK;
SELECT COUNT(*) reglas_despues, SUM(CASE WHEN hasta_utc IS NULL OR hasta_utc > SYS_EXTRACT_UTC(SYSTIMESTAMP) THEN 1 ELSE 0 END) vigentes_despues FROM regla_ecopuntos;

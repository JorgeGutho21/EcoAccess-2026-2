-- EcoAccess P3: borra los fixtures que deja en PRUEBAP3 una ejecución interrumpida de EcopuntosOracleTests
-- (correos ep.*@example.invalid, «Caneca EP», «Punto EP», «Material ficticio EP»). Si la prueba se corta antes de su
-- limpieza, sus administradores ficticios dejan «no aplicable» la prueba del último administrador.
-- Se niega a ejecutarse fuera de PRUEBAP3. Uso:
-- scripts/Ejecutar-SqlP3.ps1 -Servicio PRUEBAP3 -IdSecretos ecoaccess-p3-prueba database/05_pruebas/04_limpiar_restos_ecopuntos.sql
WHENEVER SQLERROR EXIT FAILURE ROLLBACK
SET SERVEROUTPUT ON
DECLARE
    v_pdb VARCHAR2(128) := SYS_CONTEXT('USERENV', 'CON_NAME');
    c_canecas CONSTANT VARCHAR2(200) := '(SELECT id_contenedor FROM contenedor WHERE nombre LIKE ''Caneca EP %'')';
BEGIN
    IF v_pdb <> 'PRUEBAP3' THEN
        RAISE_APPLICATION_ERROR(-20090, 'Esta limpieza solo se ejecuta en PRUEBAP3, no en ' || v_pdb || '.');
    END IF;

    DELETE FROM movimiento_ecopuntos WHERE id_usuario IN
        (SELECT id_usuario FROM usuario WHERE correo_electronico LIKE 'ep.%@example.invalid');
    DELETE FROM operacion_ecopuntos WHERE id_deposito IN (SELECT d.id_deposito FROM deposito d
        JOIN orden_apertura o ON o.id_orden = d.id_orden WHERE o.id_contenedor IN
        (SELECT id_contenedor FROM contenedor WHERE nombre LIKE 'Caneca EP %'));
    DELETE FROM regla_ecopuntos WHERE id_material IN
        (SELECT id_material FROM material WHERE nombre = 'Material ficticio EP');
    EXECUTE IMMEDIATE 'DELETE FROM deposito WHERE id_orden IN (SELECT id_orden FROM orden_apertura WHERE id_contenedor IN '
        || c_canecas || ')';
    FOR t IN (SELECT column_value AS tabla FROM TABLE(sys.odcivarchar2list(
        'evento_dispositivo', 'orden_apertura', 'interaccion', 'contenedor_material')))
    LOOP
        EXECUTE IMMEDIATE 'DELETE FROM ' || t.tabla || ' WHERE id_contenedor IN ' || c_canecas;
    END LOOP;
    DELETE FROM contenedor WHERE nombre LIKE 'Caneca EP %';
    DELETE FROM punto_instalacion WHERE nombre LIKE 'Punto EP %';
    DELETE FROM material WHERE nombre = 'Material ficticio EP';
    DELETE FROM credencial_acceso WHERE id_usuario IN
        (SELECT id_usuario FROM usuario WHERE correo_electronico LIKE 'ep.%@example.invalid');
    DELETE FROM usuario_rol WHERE id_usuario IN
        (SELECT id_usuario FROM usuario WHERE correo_electronico LIKE 'ep.%@example.invalid');
    DELETE FROM usuario WHERE correo_electronico LIKE 'ep.%@example.invalid';
    COMMIT;
    DBMS_OUTPUT.PUT_LINE('Restos de ecopuntos borrados de ' || v_pdb || '.');
END;
/

-- EcoAccess P3: borra los datos ficticios que deja scripts/Demostrar-RecorridoIntegral.ps1 en el PDB aislado PRUEBAP3.
-- Solo toca filas de los fixtures del bridge (correos br.*@example.invalid, canecas «Caneca ficticia bridge»,
-- materiales BR_/BN_, puntos «Punto ficticio bridge»). Se niega a ejecutarse en cualquier otro PDB.
-- Para restos de una ejecución interrumpida de EcopuntosOracleTests, usar 04_limpiar_restos_ecopuntos.sql.
-- Uso: scripts/Ejecutar-SqlP3.ps1 -Servicio PRUEBAP3 -IdSecretos ecoaccess-p3-prueba database/05_pruebas/03_limpiar_demo_aislada.sql
WHENEVER SQLERROR EXIT FAILURE ROLLBACK
SET SERVEROUTPUT ON
DECLARE
    v_pdb VARCHAR2(128) := SYS_CONTEXT('USERENV', 'CON_NAME');
BEGIN
    IF v_pdb <> 'PRUEBAP3' THEN
        RAISE_APPLICATION_ERROR(-20090, 'Esta limpieza solo se ejecuta en PRUEBAP3, no en ' || v_pdb || '.');
    END IF;

    DELETE FROM movimiento_ecopuntos WHERE id_usuario IN
        (SELECT id_usuario FROM usuario WHERE correo_electronico LIKE 'br.%@example.invalid');
    DELETE FROM operacion_ecopuntos WHERE id_deposito IN (SELECT d.id_deposito FROM deposito d
        JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN contenedor c ON c.id_contenedor = o.id_contenedor
        WHERE c.nombre LIKE 'Caneca ficticia bridge %');
    DELETE FROM regla_ecopuntos WHERE id_material IN
        (SELECT id_material FROM material WHERE codigo LIKE 'BR\_%' ESCAPE '\' OR codigo LIKE 'BN\_%' ESCAPE '\');
    DELETE FROM conversacion_telegram WHERE id_contenedor IN
        (SELECT id_contenedor FROM contenedor WHERE nombre LIKE 'Caneca ficticia bridge %');
    DELETE FROM vinculo_telegram WHERE id_usuario IN
        (SELECT id_usuario FROM usuario WHERE correo_electronico LIKE 'br.%@example.invalid');
    -- La demostración con vinculación web deja pendientes y tokens que apuntan a esas cuentas.
    DELETE FROM vinculacion_pendiente WHERE id_usuario IN
        (SELECT id_usuario FROM usuario WHERE correo_electronico LIKE 'br.%@example.invalid');
    DELETE FROM token_vinculacion WHERE id_usuario_registrado IN
        (SELECT id_usuario FROM usuario WHERE correo_electronico LIKE 'br.%@example.invalid');
    DELETE FROM deposito WHERE id_orden IN (SELECT o.id_orden FROM orden_apertura o
        JOIN contenedor c ON c.id_contenedor = o.id_contenedor WHERE c.nombre LIKE 'Caneca ficticia bridge %');
    FOR t IN (SELECT column_value AS tabla FROM TABLE(sys.odcivarchar2list(
        'evento_dispositivo', 'orden_apertura', 'interaccion', 'contenedor_material')))
    LOOP
        EXECUTE IMMEDIATE 'DELETE FROM ' || t.tabla || ' WHERE id_contenedor IN '
            || '(SELECT id_contenedor FROM contenedor WHERE nombre LIKE ''Caneca ficticia bridge %'')';
    END LOOP;
    DELETE FROM contenedor WHERE nombre LIKE 'Caneca ficticia bridge %';
    DELETE FROM punto_instalacion WHERE nombre LIKE 'Punto ficticio bridge %';
    DELETE FROM material WHERE codigo LIKE 'BR\_%' ESCAPE '\' OR codigo LIKE 'BN\_%' ESCAPE '\';
    -- Las cuentas creadas desde la interfaz en la demostración tienen contraseña.
    DELETE FROM credencial_acceso WHERE id_usuario IN
        (SELECT id_usuario FROM usuario WHERE correo_electronico LIKE 'br.%@example.invalid');
    DELETE FROM usuario_rol WHERE id_usuario IN
        (SELECT id_usuario FROM usuario WHERE correo_electronico LIKE 'br.%@example.invalid');
    DELETE FROM usuario WHERE correo_electronico LIKE 'br.%@example.invalid';
    COMMIT;
    DBMS_OUTPUT.PUT_LINE('Datos de demostración borrados de ' || v_pdb || '.');
END;
/

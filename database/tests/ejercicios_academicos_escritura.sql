-- =====================================================================
-- EcoAccess · Ejercicios E11–E14: INSERT, UPDATE, procedimiento y disparador
-- ESCRIBE DATOS y termina con ROLLBACK. Ejecutar SOLO en el esquema aislado (ECOACCESS_P3_PRUEBAS) o en un esquema
-- de desarrollo propio. NUNCA en Oracle Cloud. El bloque se niega a correr en ECOACCESS_P3_APP.
-- =====================================================================
SET SERVEROUTPUT ON SIZE UNLIMITED
DECLARE
    v_material NUMBER; v_caneca NUMBER; v_admin NUMBER; v_estado NUMBER; n NUMBER;
BEGIN
    IF USER = 'ECOACCESS_P3_APP' THEN
        RAISE_APPLICATION_ERROR(-20999, 'Estos ejercicios escriben: ejecútelos en el esquema aislado, no en la base oficial.');
    END IF;

    -- E11 · INSERT con RETURNING: un material de práctica (la PK es IDENTITY, no se escribe).
    INSERT INTO material (codigo, nombre, categoria, habilitado)
    VALUES ('PRUEBA_EJERCICIO', 'Material de ejercicio', 1, 1)
    RETURNING id_material INTO v_material;
    DBMS_OUTPUT.PUT_LINE('E11 material creado con id ' || v_material);

    -- E12 · UPDATE con condición, y una restricción CHECK que lo impide.
    UPDATE material SET nombre = 'Material de ejercicio (editado)' WHERE id_material = v_material;
    DBMS_OUTPUT.PUT_LINE('E12 filas actualizadas: ' || SQL%ROWCOUNT);
    BEGIN
        UPDATE material SET categoria = 9 WHERE id_material = v_material;
    EXCEPTION WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('E12 CK_MATERIAL_CATEGORIA rechazó la categoría 9: ' || SQLCODE);
    END;

    -- E13 · Procedimiento + disparador: cambiar el estado de una caneca deja historia sola.
    SELECT MIN(id_contenedor) INTO v_caneca FROM contenedor WHERE estado = 1;
    SELECT MIN(u.id_usuario) INTO v_admin FROM usuario u JOIN usuario_rol ur ON ur.id_usuario = u.id_usuario
           JOIN rol r ON r.id_rol = ur.id_rol WHERE r.codigo = 3 AND u.activo = 1;
    IF v_caneca IS NULL OR v_admin IS NULL THEN
        DBMS_OUTPUT.PUT_LINE('E13 omitido: el esquema no tiene una caneca en servicio o un administrador.');
    ELSE
        pr_cambiar_estado_contenedor(v_caneca, 2, v_admin, 'Ejercicio académico');
        SELECT estado INTO v_estado FROM contenedor WHERE id_contenedor = v_caneca;
        SELECT COUNT(*) INTO n FROM historial_estado_contenedor WHERE id_contenedor = v_caneca AND motivo = 'Ejercicio académico';
        DBMS_OUTPUT.PUT_LINE('E13 caneca ' || v_caneca || ' quedó en estado ' || v_estado || '; filas de historia creadas por TRG_CONTENEDOR_AUD_ESTADO: ' || n);
    END IF;

    -- E14 · Disparador que protege la evidencia: no se cambia la fecha de un depósito.
    SELECT MIN(id_deposito) INTO n FROM deposito;
    IF n IS NOT NULL THEN
        BEGIN
            UPDATE deposito SET fecha_deteccion_utc = fecha_deteccion_utc - 1 WHERE id_deposito = n;
            DBMS_OUTPUT.PUT_LINE('E14 (inesperado) se cambió la fecha');
        EXCEPTION WHEN OTHERS THEN
            DBMS_OUTPUT.PUT_LINE('E14 el disparador rechazó el cambio: ' || SQLERRM);
        END;
    END IF;

    ROLLBACK;   -- deshace E11–E13: el esquema queda como estaba
    DBMS_OUTPUT.PUT_LINE('ROLLBACK hecho: no quedó ninguna fila del ejercicio.');
END;
/

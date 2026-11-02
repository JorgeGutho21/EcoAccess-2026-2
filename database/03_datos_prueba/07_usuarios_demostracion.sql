-- Personas ficticias para el modo demostración, solo en PRUEBAP3 y sin contraseña: el administrador la define con la
-- configuración inicial de la aplicación. Repetir el script no duplica filas.
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK
DECLARE
    PROCEDURE agregar(p_nombre VARCHAR2, p_correo VARCHAR2, p_rol NUMBER) IS
        id_cuenta NUMBER;
        n NUMBER;
    BEGIN
        SELECT COUNT(*) INTO n FROM usuario WHERE correo_electronico = p_correo;
        IF n = 0 THEN
            INSERT INTO usuario (nombre, correo_electronico, fecha_registro_utc, activo) VALUES (p_nombre, p_correo, SYS_EXTRACT_UTC(SYSTIMESTAMP), 1);
        END IF;
        SELECT id_usuario INTO id_cuenta FROM usuario WHERE correo_electronico = p_correo;
        SELECT COUNT(*) INTO n FROM usuario_rol ur JOIN rol r ON r.id_rol = ur.id_rol WHERE ur.id_usuario = id_cuenta AND r.codigo = p_rol;
        IF n = 0 THEN
            INSERT INTO usuario_rol (id_usuario, id_rol) SELECT id_cuenta, id_rol FROM rol WHERE codigo = p_rol;
        END IF;
    END;
BEGIN
    IF SYS_CONTEXT('USERENV', 'CON_NAME') <> 'PRUEBAP3' THEN
        RAISE_APPLICATION_ERROR(-20160, 'Los usuarios ficticios solo se preparan en la base de pruebas.');
    END IF;
    agregar('Administración de demostración', 'admin.demo@ejemplo.com', 3);
    agregar('Operación de demostración', 'operador.demo@ejemplo.com', 2);
    COMMIT;
END;
/
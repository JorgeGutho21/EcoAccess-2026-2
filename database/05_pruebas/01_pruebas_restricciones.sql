-- =====================================================================
-- EcoAccess P3 — pruebas de restricciones de las tablas iniciales
-- Archivo: database/05_pruebas/01_pruebas_restricciones.sql
--
-- Cada caso intenta guardar un dato inválido y comprueba que Oracle lo rechace
-- con el error esperado. También hay casos válidos que deben aceptarse.
-- Al final se hace ROLLBACK: la prueba no deja datos en la base, ni siquiera si
-- un error inesperado interrumpe el bloque (el manejador final deshace y relanza).
-- Requiere las tablas (02_tablas) y los catálogos (03_datos_prueba/01_catalogos.sql).
--
-- Errores de Oracle que se esperan:
--   ORA-00001 valor duplicado en una restricción UNIQUE o PRIMARY KEY
--   ORA-01400 NULL en una columna NOT NULL
--   ORA-02290 incumple una restricción CHECK
--   ORA-02291 clave foránea a un registro padre que no existe
--   ORA-02292 borrar un padre que todavía tiene hijos
--   ORA-12899 texto más largo que la columna
--   ORA-32795 valor explícito en una columna GENERATED ALWAYS AS IDENTITY
-- =====================================================================

SET SERVEROUTPUT ON SIZE UNLIMITED
WHENEVER SQLERROR EXIT SQL.SQLCODE

DECLARE
    v_pasan   PLS_INTEGER := 0;
    v_fallan  PLS_INTEGER := 0;
    v_id_rol_ciudadano  NUMBER;
    v_id_material_pet   NUMBER;
    v_id_usuario        NUMBER;
    v_id_punto          NUMBER;
    v_id_contenedor     NUMBER;

    PROCEDURE registrar(p_pasa BOOLEAN, p_caso VARCHAR2, p_detalle VARCHAR2 := NULL) IS
    BEGIN
        IF p_pasa THEN
            v_pasan := v_pasan + 1;
            DBMS_OUTPUT.PUT_LINE('PASA   ' || p_caso);
        ELSE
            v_fallan := v_fallan + 1;
            DBMS_OUTPUT.PUT_LINE('FALLA  ' || p_caso || ' -> ' || p_detalle);
        END IF;
    END;

    -- Ejecuta una sentencia que DEBE fallar con el código indicado.
    PROCEDURE esperar_error(p_caso VARCHAR2, p_sql VARCHAR2, p_codigo NUMBER) IS
    BEGIN
        EXECUTE IMMEDIATE p_sql;
        registrar(FALSE, p_caso, 'Oracle aceptó un dato inválido');
    EXCEPTION
        WHEN OTHERS THEN
            registrar(SQLCODE = p_codigo, p_caso, 'error inesperado: ' || SQLERRM);
    END;

    -- Ejecuta una sentencia que DEBE aceptarse.
    PROCEDURE esperar_exito(p_caso VARCHAR2, p_sql VARCHAR2) IS
    BEGIN
        EXECUTE IMMEDIATE p_sql;
        registrar(TRUE, p_caso);
    EXCEPTION
        WHEN OTHERS THEN
            registrar(FALSE, p_caso, SQLERRM);
    END;
BEGIN
    SELECT id_rol INTO v_id_rol_ciudadano FROM rol WHERE codigo = 1;
    SELECT id_material INTO v_id_material_pet FROM material WHERE codigo = 'PET';

    -- ---------- USUARIO ----------
    INSERT INTO usuario (nombre, correo_electronico, fecha_registro_utc, version_consentimiento, fecha_consentimiento_utc)
    VALUES ('Prueba Ficticia', 'prueba.ficticia@example.com', SYS_EXTRACT_UTC(SYSTIMESTAMP), '2026-10', SYS_EXTRACT_UTC(SYSTIMESTAMP))
    RETURNING id_usuario INTO v_id_usuario;
    registrar(v_id_usuario > 0, 'usuario válido con consentimiento se guarda y recibe id de la identidad');

    esperar_error('correo duplicado',
        q'[INSERT INTO usuario (nombre, correo_electronico, fecha_registro_utc)
           VALUES ('Otra Persona', 'prueba.ficticia@example.com', SYSTIMESTAMP)]', -1);
    esperar_error('correo con mayúsculas',
        q'[INSERT INTO usuario (nombre, correo_electronico, fecha_registro_utc)
           VALUES ('Otra Persona', 'Otra@Example.com', SYSTIMESTAMP)]', -2290);
    esperar_error('nombre de una letra',
        q'[INSERT INTO usuario (nombre, correo_electronico, fecha_registro_utc)
           VALUES ('A', 'a@example.com', SYSTIMESTAMP)]', -2290);
    esperar_error('nombre solo de espacios',
        q'[INSERT INTO usuario (nombre, correo_electronico, fecha_registro_utc)
           VALUES ('   ', 'a2@example.com', SYSTIMESTAMP)]', -2290);
    esperar_error('nombre con espacios sobrantes',
        q'[INSERT INTO usuario (nombre, correo_electronico, fecha_registro_utc)
           VALUES (' Ana ', 'a3@example.com', SYSTIMESTAMP)]', -2290);
    esperar_error('nombre nulo',
        q'[INSERT INTO usuario (nombre, correo_electronico, fecha_registro_utc)
           VALUES (NULL, 'b@example.com', SYSTIMESTAMP)]', -1400);
    esperar_error('activo distinto de 0 o 1',
        q'[INSERT INTO usuario (nombre, correo_electronico, fecha_registro_utc, activo)
           VALUES ('Otra Persona', 'c@example.com', SYSTIMESTAMP, 2)]', -2290);
    esperar_error('consentimiento con versión y sin fecha',
        q'[INSERT INTO usuario (nombre, correo_electronico, fecha_registro_utc, version_consentimiento)
           VALUES ('Otra Persona', 'd@example.com', SYSTIMESTAMP, '2026-10')]', -2290);
    esperar_error('id explícito en identidad ALWAYS',
        q'[INSERT INTO usuario (id_usuario, nombre, correo_electronico, fecha_registro_utc)
           VALUES (999, 'Otra Persona', 'e@example.com', SYSTIMESTAMP)]', -32795);
    esperar_error('nombre de 101 caracteres',
        q'[INSERT INTO usuario (nombre, correo_electronico, fecha_registro_utc)
           VALUES (RPAD('n', 101, 'n'), 'f@example.com', SYSTIMESTAMP)]', -12899);
    esperar_exito('nombre de 100 caracteres con tildes cabe (VARCHAR2 en CHAR, no en bytes)',
        q'[INSERT INTO usuario (nombre, correo_electronico, fecha_registro_utc)
           VALUES (RPAD('ñ', 100, 'á'), 'g@example.com', SYSTIMESTAMP)]');

    -- ---------- USUARIO_ROL ----------
    esperar_exito('asignar rol ciudadano',
        'INSERT INTO usuario_rol (id_usuario, id_rol) VALUES (' || v_id_usuario || ', ' || v_id_rol_ciudadano || ')');
    esperar_error('asignar el mismo rol dos veces',
        'INSERT INTO usuario_rol (id_usuario, id_rol) VALUES (' || v_id_usuario || ', ' || v_id_rol_ciudadano || ')', -1);
    esperar_error('asignar rol a usuario inexistente',
        'INSERT INTO usuario_rol (id_usuario, id_rol) VALUES (-1, ' || v_id_rol_ciudadano || ')', -2291);
    esperar_error('borrar un usuario que tiene roles',
        'DELETE FROM usuario WHERE id_usuario = ' || v_id_usuario, -2292);

    -- ---------- ROL ----------
    esperar_error('rol con código fuera de los tres aprobados',
        q'[INSERT INTO rol (codigo, descripcion) VALUES (4, 'Supervisor')]', -2290);
    esperar_error('rol con código repetido',
        q'[INSERT INTO rol (codigo, descripcion) VALUES (1, 'Otro ciudadano')]', -1);
    esperar_error('rol con descripción solo de espacios',
        q'[INSERT INTO rol (codigo, descripcion) VALUES (2, '   ')]', -2290);

    -- ---------- MATERIAL ----------
    esperar_error('material con código repetido',
        q'[INSERT INTO material (codigo, nombre, categoria) VALUES ('PET', 'Otra botella', 1)]', -1);
    esperar_error('material con código en minúsculas',
        q'[INSERT INTO material (codigo, nombre, categoria) VALUES ('pet2', 'Botella', 1)]', -2290);
    esperar_error('material con tilde en el código',
        q'[INSERT INTO material (codigo, nombre, categoria) VALUES ('CARTÓN2', 'Cartón', 1)]', -2290);
    esperar_error('material con código que empieza por número',
        q'[INSERT INTO material (codigo, nombre, categoria) VALUES ('1PET', 'Botella', 1)]', -2290);
    esperar_error('material con guion en el código',
        q'[INSERT INTO material (codigo, nombre, categoria) VALUES ('PET-2', 'Botella', 1)]', -2290);
    esperar_error('material con código de una letra',
        q'[INSERT INTO material (codigo, nombre, categoria) VALUES ('P', 'Botella', 1)]', -2290);
    esperar_error('material con Ñ al inicio del código',
        q'[INSERT INTO material (codigo, nombre, categoria) VALUES ('ÑANDU', 'Prueba', 1)]', -2290);
    esperar_error('material con Á al inicio del código',
        q'[INSERT INTO material (codigo, nombre, categoria) VALUES ('ÁRBOL', 'Prueba', 1)]', -2290);
    esperar_error('material con nombre solo de espacios',
        q'[INSERT INTO material (codigo, nombre, categoria) VALUES ('VACIO', '    ', 1)]', -2290);
    esperar_error('material con habilitado 2',
        q'[INSERT INTO material (codigo, nombre, categoria, habilitado) VALUES ('HAB', 'Prueba', 1, 2)]', -2290);
    esperar_error('material con categoría 0',
        q'[INSERT INTO material (codigo, nombre, categoria) VALUES ('CERO', 'Prueba', 0)]', -2290);
    esperar_error('material con categoría 5',
        q'[INSERT INTO material (codigo, nombre, categoria) VALUES ('NUEVO', 'Nuevo', 5)]', -2290);
    esperar_exito('material válido con guion bajo',
        q'[INSERT INTO material (codigo, nombre, categoria) VALUES ('PRUEBA_TEMPORAL_1', 'Material de prueba', 1)]');

    -- ---------- PUNTO_INSTALACION y CONTENEDOR ----------
    esperar_error('punto con nombre solo de espacios',
        q'[INSERT INTO punto_instalacion (nombre) VALUES ('   ')]', -2290);
    esperar_error('punto con activo 2',
        q'[INSERT INTO punto_instalacion (nombre, activo) VALUES ('PRUEBA - otro punto', 2)]', -2290);
    INSERT INTO punto_instalacion (nombre) VALUES ('PRUEBA - punto temporal')
    RETURNING id_punto_instalacion INTO v_id_punto;

    INSERT INTO contenedor (id_punto_instalacion, nombre, identificador_qr, fecha_alta_utc)
    VALUES (v_id_punto, 'PRUEBA - caneca temporal', 'PRUEBA_qr_temporal_01', SYS_EXTRACT_UTC(SYSTIMESTAMP))
    RETURNING id_contenedor INTO v_id_contenedor;

    DECLARE
        v_estado NUMBER;
    BEGIN
        SELECT estado INTO v_estado FROM contenedor WHERE id_contenedor = v_id_contenedor;
        registrar(v_estado = 2, 'una caneca nueva queda fuera de servicio por defecto (estado 2)',
                  'estado = ' || v_estado);
    END;

    esperar_error('dos canecas con el mismo QR',
        'INSERT INTO contenedor (id_punto_instalacion, nombre, identificador_qr, fecha_alta_utc) VALUES ('
        || v_id_punto || q'[, 'Otra', 'PRUEBA_qr_temporal_01', SYSTIMESTAMP)]', -1);
    esperar_error('QR de 15 caracteres',
        'INSERT INTO contenedor (id_punto_instalacion, nombre, identificador_qr, fecha_alta_utc) VALUES ('
        || v_id_punto || q'[, 'Otra', 'abcdefghijklmno', SYSTIMESTAMP)]', -2290);
    esperar_error('QR con signo + (base64 estándar)',
        'INSERT INTO contenedor (id_punto_instalacion, nombre, identificador_qr, fecha_alta_utc) VALUES ('
        || v_id_punto || q'[, 'Otra', 'abcdefgh+ijklmnop', SYSTIMESTAMP)]', -2290);
    esperar_error('QR con espacio',
        'INSERT INTO contenedor (id_punto_instalacion, nombre, identificador_qr, fecha_alta_utc) VALUES ('
        || v_id_punto || q'[, 'Otra', 'abcdefgh ijklmnop', SYSTIMESTAMP)]', -2290);
    esperar_error('QR de 65 caracteres',
        'INSERT INTO contenedor (id_punto_instalacion, nombre, identificador_qr, fecha_alta_utc) VALUES ('
        || v_id_punto || q'[, 'Otra', RPAD('a', 65, 'a'), SYSTIMESTAMP)]', -12899);
    esperar_error('caneca en un punto inexistente',
        q'[INSERT INTO contenedor (id_punto_instalacion, nombre, identificador_qr, fecha_alta_utc)
           VALUES (-1, 'Otra', 'PRUEBA_qr_temporal_02', SYSTIMESTAMP)]', -2291);
    esperar_error('caneca con nombre solo de espacios',
        'INSERT INTO contenedor (id_punto_instalacion, nombre, identificador_qr, fecha_alta_utc) VALUES ('
        || v_id_punto || q'[, '   ', 'PRUEBA_qr_temporal_04', SYSTIMESTAMP)]', -2290);
    esperar_error('caneca con estado 0',
        'INSERT INTO contenedor (id_punto_instalacion, nombre, identificador_qr, fecha_alta_utc, estado) VALUES ('
        || v_id_punto || q'[, 'Otra', 'PRUEBA_qr_temporal_05', SYSTIMESTAMP, 0)]', -2290);
    esperar_error('caneca con estado 4',
        'INSERT INTO contenedor (id_punto_instalacion, nombre, identificador_qr, fecha_alta_utc, estado) VALUES ('
        || v_id_punto || q'[, 'Otra', 'PRUEBA_qr_temporal_03', SYSTIMESTAMP, 4)]', -2290);

    -- ---------- CONTENEDOR_MATERIAL ----------
    esperar_exito('admitir PET en la caneca de prueba',
        'INSERT INTO contenedor_material (id_contenedor, id_material) VALUES ('
        || v_id_contenedor || ', ' || v_id_material_pet || ')');
    esperar_error('admitir el mismo material dos veces',
        'INSERT INTO contenedor_material (id_contenedor, id_material) VALUES ('
        || v_id_contenedor || ', ' || v_id_material_pet || ')', -1);
    esperar_error('admitir un material inexistente',
        'INSERT INTO contenedor_material (id_contenedor, id_material) VALUES (' || v_id_contenedor || ', -1)', -2291);
    esperar_error('borrar un material que una caneca admite',
        'DELETE FROM material WHERE id_material = ' || v_id_material_pet, -2292);
    esperar_error('borrar un punto que tiene canecas',
        'DELETE FROM punto_instalacion WHERE id_punto_instalacion = ' || v_id_punto, -2292);

    ROLLBACK;

    DBMS_OUTPUT.PUT_LINE('-----');
    DBMS_OUTPUT.PUT_LINE('Resultado: ' || v_pasan || ' pasan, ' || v_fallan || ' fallan. Se hizo ROLLBACK de todos los datos de prueba.');

    IF v_fallan > 0 THEN
        RAISE_APPLICATION_ERROR(-20020, v_fallan || ' pruebas de restricciones fallaron.');
    END IF;
EXCEPTION
    WHEN OTHERS THEN
        -- Un error no previsto (por ejemplo, faltan los catálogos) no debe dejar filas de prueba.
        ROLLBACK;
        DBMS_OUTPUT.PUT_LINE('Prueba interrumpida; se hizo ROLLBACK. ' || SQLERRM);
        RAISE;
END;
/

WHENEVER SQLERROR CONTINUE

-- =====================================================================
-- Práctica P99 · Pruebas automáticas de las prácticas de clase
-- Conexión: ECOACCESS_PRACTICAS (F5). Se puede repetir: termina en ROLLBACK.
-- Comprueba con PASA/FALLA lo que pide cada tarea del docente:
--   precio final (1000 → 1200, 1500 → 1800), secuencia + trigger para la PK,
--   triggers libres (auditoría y límite del 20 %), ejemplos 32–34
--   (stock y nota inválida ORA-20001) y el CRUD de empleado SIN DELETE.
-- =====================================================================
SET SERVEROUTPUT ON SIZE UNLIMITED
SET FEEDBACK OFF
DECLARE
    n_ok  PLS_INTEGER := 0;
    n_mal PLS_INTEGER := 0;
    v     NUMBER;
    v2    NUMBER;
    v3    NUMBER;
    e     NUMBER;
    PROCEDURE ok(p_nombre VARCHAR2, p_cond BOOLEAN, p_detalle VARCHAR2 DEFAULT NULL) IS
    BEGIN
        IF p_cond THEN n_ok := n_ok + 1; DBMS_OUTPUT.PUT_LINE('PASA  ' || p_nombre || ' ' || p_detalle);
        ELSE n_mal := n_mal + 1; DBMS_OUTPUT.PUT_LINE('FALLA ' || p_nombre || ' ' || p_detalle); END IF;
    END;
BEGIN
    -- 1. Precio final (trigger BEFORE INSERT OR UPDATE OF precio_unitario, utilidad)
    INSERT INTO producto_practica (nombre, precio_unitario, utilidad) VALUES ('P99 A', 1000, 20) RETURNING precio_final INTO v;
    ok('PR01 1000 + 20 % = 1200', v = 1200, '(' || v || ')');
    INSERT INTO producto_practica (nombre, precio_unitario, utilidad) VALUES ('P99 B', 1500, 20) RETURNING precio_final INTO v;
    ok('PR02 1500 + 20 % = 1800', v = 1800, '(' || v || ')');
    UPDATE producto_practica SET utilidad = 30 WHERE nombre = 'P99 B' RETURNING precio_final INTO v;
    ok('PR03 Cambiar solo la utilidad recalcula (1950)', v = 1950, '(' || v || ')');
    UPDATE producto_practica SET precio_unitario = 2000 WHERE nombre = 'P99 B' RETURNING precio_final INTO v;
    ok('PR04 Cambiar solo el precio recalcula (2600)', v = 2600, '(' || v || ')');
    UPDATE producto_practica SET precio_final = 1 WHERE nombre = 'P99 B' RETURNING precio_final INTO v;
    ok('PR05 Un precio final falso se recalcula', v = 2600, '(' || v || ')');
    BEGIN
        INSERT INTO producto_practica (nombre, precio_unitario, utilidad) VALUES ('P99 C', -5, 20); e := 0;
    EXCEPTION WHEN OTHERS THEN e := SQLCODE; END;
    ok('PR06 Precio negativo rechazado (ORA-20301/20302)', e IN (-20301, -20302), '(SQLCODE ' || e || ')');

    -- 2. Secuencia + trigger para la PK subrogada
    INSERT INTO bd_evento_academico (nombre, fecha_evento) VALUES ('P99 uno', SYSDATE) RETURNING id_evento INTO v;
    INSERT INTO bd_evento_academico (nombre, fecha_evento) VALUES ('P99 dos', SYSDATE) RETURNING id_evento INTO v2;
    ok('SQ01 El trigger asigna el id con la secuencia', v IS NOT NULL AND v2 > v, '(' || v || ', ' || v2 || ')');

    -- 3. Triggers libres sobre EMPLEADOS
    SELECT MIN(id_empleado) INTO v3 FROM empleados;
    SELECT salario INTO v FROM empleados WHERE id_empleado = v3;
    SELECT COUNT(*) INTO e FROM auditoria_salarios;
    UPDATE empleados SET salario = salario * 3 WHERE id_empleado = v3;
    SELECT salario INTO v2 FROM empleados WHERE id_empleado = v3;
    ok('TL01 Límite del 20 %: el aumento se recorta', v2 = v * 1.2, '(' || v || ' -> ' || v2 || ')');
    SELECT COUNT(*) INTO v FROM auditoria_salarios;
    ok('TL02 La auditoría de salarios registró el cambio', v = e + 1, '(' || e || ' -> ' || v || ')');

    -- 4. Ejemplos 32–34
    BEGIN INSERT INTO notas VALUES (99001, 1, 7); e := 0; EXCEPTION WHEN OTHERS THEN e := SQLCODE; END;
    ok('EJ34 Nota 7 rechazada (ORA-20001)', e = -20001, '(SQLCODE ' || e || ')');
    BEGIN INSERT INTO notas VALUES (99002, 1, 4.5); e := 0; EXCEPTION WHEN OTHERS THEN e := SQLCODE; END;
    ok('EJ34 Nota 4,5 aceptada', e = 0, '(SQLCODE ' || e || ')');
    SELECT MIN(id_producto) INTO v3 FROM productos;
    SELECT stock INTO v FROM productos WHERE id_producto = v3;
    BEGIN INSERT INTO ventas VALUES (99001, v3, v + 100); e := 0; EXCEPTION WHEN OTHERS THEN e := SQLCODE; END;
    ok('EJ32 Vender más que el stock (ORA-20003)', e = -20003, '(SQLCODE ' || e || ')');
    INSERT INTO ventas VALUES (99002, v3, 1);
    SELECT stock INTO v2 FROM productos WHERE id_producto = v3;
    ok('EJ32 Una venta válida descuenta el stock', v2 = v - 1, '(' || v || ' -> ' || v2 || ')');

    -- 5. CRUD de empleado SIN DELETE
    pkg_empleado_crud.insertar('P99 Empleado', 'P99MAIL', 2500, 60, v);
    ok('CR01 Insertar: id desde la secuencia (>= 1000)', v >= 1000, '(' || v || ')');
    pkg_empleado_crud.actualizar(v, 'P99 Empleado', 2600, 60);
    SELECT salario INTO v2 FROM empleado_practica WHERE id_empleado = v;
    ok('CR02 Actualizar: el salario quedó en 2600', v2 = 2600, '(' || v2 || ')');
    BEGIN pkg_empleado_crud.actualizar(999999, 'Nadie', 1, 60); e := 0; EXCEPTION WHEN OTHERS THEN e := SQLCODE; END;
    ok('CR03 Actualizar un id inexistente (ORA-20002)', e = -20002, '(SQLCODE ' || e || ')');
    SELECT COUNT(*) INTO v FROM user_procedures WHERE object_name = 'PKG_EMPLEADO_CRUD' AND procedure_name IN ('ELIMINAR', 'BORRAR', 'DELETE_EMPLEADO');
    ok('CR04 El paquete NO tiene operación de borrado', v = 0, '(' || v || ')');

    -- 6. Salud del esquema
    SELECT COUNT(*) INTO v FROM user_objects WHERE status = 'INVALID';
    ok('ES01 0 objetos inválidos en ECOACCESS_PRACTICAS', v = 0, '(' || v || ')');
    SELECT COUNT(*) INTO v FROM user_triggers WHERE status = 'ENABLED';
    ok('ES02 Los 7 triggers de práctica están habilitados', v = 7, '(' || v || ')');

    ROLLBACK;
    DBMS_OUTPUT.PUT_LINE('TOTAL PRÁCTICAS: ' || (n_ok + n_mal) || ' pruebas · PASAN: ' || n_ok || ' · FALLAN: ' || n_mal);
    DBMS_OUTPUT.PUT_LINE('Se hizo ROLLBACK: las tablas de práctica quedaron igual.');
END;
/

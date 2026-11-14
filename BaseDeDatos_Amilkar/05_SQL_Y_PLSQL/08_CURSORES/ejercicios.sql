-- 08_CURSORES — 14 ejercicios del banco verificado (conexión: ECOACCESS_BD)
-- Generado por 05_SQL_Y_PLSQL/generar_carpetas_por_tema.ps1 desde 08_CONSULTAS_Y_EJERCICIOS. No editar a mano.
-- Selecciona un ejercicio y pulsa Ctrl+Enter (consulta) o F5 (bloque PL/SQL). Los que modifican datos terminan en ROLLBACK.
SET SERVEROUTPUT ON SIZE UNLIMITED

-- ===== E234 · 15. Cursores · Cursor implícito SQL%ROWCOUNT
-- Objetivo: Cuántas filas cambió un UPDATE.
-- Explicación: SQL% describe la última sentencia.
BEGIN
    UPDATE material SET habilitado = habilitado WHERE categoria = 1;
    DBMS_OUTPUT.PUT_LINE('Filas tocadas: ' || SQL%ROWCOUNT);
    ROLLBACK;
END;
/

-- ===== E235 · 15. Cursores · Cursor FOR (implícito)
-- Objetivo: Recorrer filas fácilmente.
-- Explicación: Oracle abre, lee y cierra solo.
BEGIN
    FOR r IN (SELECT codigo, nombre FROM material WHERE categoria = 2 ORDER BY codigo) LOOP
        DBMS_OUTPUT.PUT_LINE(r.codigo || ': ' || r.nombre);
    END LOOP;
END;
/

-- ===== E236 · 15. Cursores · Cursor explícito
-- Objetivo: OPEN, FETCH, CLOSE paso a paso.
-- Explicación: %NOTFOUND indica que no hay más filas.
DECLARE
    CURSOR c IS SELECT nombre FROM contenedor ORDER BY id_contenedor;
    v_nombre contenedor.nombre%TYPE;
BEGIN
    OPEN c;
    LOOP
        FETCH c INTO v_nombre;
        EXIT WHEN c%NOTFOUND;
        DBMS_OUTPUT.PUT_LINE(c%ROWCOUNT || '. ' || v_nombre);
    END LOOP;
    CLOSE c;
END;
/

-- ===== E237 · 15. Cursores · Cursor con parámetro
-- Objetivo: El mismo cursor para distintos colores.
-- Explicación: Los parámetros hacen reutilizable el cursor.
DECLARE
    CURSOR c (p_cat NUMBER) IS SELECT codigo FROM material WHERE categoria = p_cat ORDER BY codigo;
BEGIN
    FOR r IN c(3) LOOP
        DBMS_OUTPUT.PUT_LINE('Negra: ' || r.codigo);
    END LOOP;
END;
/

-- ===== E238 · 15. Cursores · Cursor con %ROWTYPE
-- Objetivo: Leer filas completas del cursor.
-- Explicación: cursor%ROWTYPE toma la forma del SELECT.
DECLARE
    CURSOR c IS SELECT id_deposito, estado_texto, color FROM vw_deposito_detalle WHERE estado_verificacion = 2;
    r c%ROWTYPE;
BEGIN
    OPEN c;
    FETCH c INTO r;
    DBMS_OUTPUT.PUT_LINE('Primer rechazado: ' || r.id_deposito || ' en caneca ' || r.color);
    CLOSE c;
END;
/

-- ===== E239 · 15. Cursores · SYS_REFCURSOR
-- Objetivo: Cursor que se devuelve a otro programa (C#).
-- Explicación: Lo usa fn_depositos_ciudadano.
VARIABLE rc REFCURSOR
EXEC :rc := fn_depositos_ciudadano(6)
PRINT rc

-- ===== E240 · 15. Cursores · Acumular con cursor
-- Objetivo: Sumar kilos verificados con un cursor.
-- Explicación: Ejemplo de cálculo fila por fila (en SQL sería SUM).
DECLARE
    v_total NUMBER := 0;
BEGIN
    FOR r IN (SELECT peso_estimado_kg FROM deposito WHERE estado_verificacion = 1 AND peso_estimado_kg IS NOT NULL) LOOP
        v_total := v_total + r.peso_estimado_kg;
    END LOOP;
    DBMS_OUTPUT.PUT_LINE('Kg estimados verificados: ' || ROUND(v_total, 3));
END;
/

-- ===== E241 · 15. Cursores · Cursor FOR UPDATE
-- Objetivo: Bloquear filas para modificarlas (con ROLLBACK).
-- Explicación: WHERE CURRENT OF actualiza la fila del cursor.
DECLARE
    CURSOR c IS SELECT id_material, descripcion FROM material WHERE categoria = 4 FOR UPDATE OF descripcion;
BEGIN
    FOR r IN c LOOP
        UPDATE material SET descripcion = descripcion || ' (peligroso)' WHERE CURRENT OF c;
    END LOOP;
    DBMS_OUTPUT.PUT_LINE('Actualizados y deshechos.');
    ROLLBACK;
END;
/

-- ===== E242 · 15. Cursores · %FOUND y %NOTFOUND
-- Objetivo: Recorrer un cursor manualmente.
-- Explicación: %NOTFOUND se vuelve TRUE cuando ya no hay filas.
DECLARE
    CURSOR c IS SELECT nombre FROM contenedor ORDER BY id_contenedor;
    v contenedor.nombre%TYPE;
BEGIN
    OPEN c;
    LOOP
        FETCH c INTO v;
        EXIT WHEN c%NOTFOUND;
        DBMS_OUTPUT.PUT_LINE(c%ROWCOUNT || '. ' || v);
    END LOOP;
    CLOSE c;
END;
/

-- ===== E243 · 15. Cursores · %ISOPEN
-- Objetivo: Preguntar si un cursor está abierto.
-- Explicación: Evita el error de cerrar un cursor ya cerrado.
DECLARE
    CURSOR c IS SELECT 1 FROM dual;
BEGIN
    DBMS_OUTPUT.PUT_LINE('Antes: ' || CASE WHEN c%ISOPEN THEN 'abierto' ELSE 'cerrado' END);
    OPEN c;
    DBMS_OUTPUT.PUT_LINE('Después: ' || CASE WHEN c%ISOPEN THEN 'abierto' ELSE 'cerrado' END);
    CLOSE c;
END;
/

-- ===== E244 · 15. Cursores · Cursores anidados
-- Objetivo: Cada caneca con sus materiales.
-- Explicación: Un cursor FOR dentro de otro, usando un parámetro.
DECLARE
    CURSOR c_mat(p_cont NUMBER) IS SELECT m.codigo FROM contenedor_material cm JOIN material m ON m.id_material = cm.id_material WHERE cm.id_contenedor = p_cont ORDER BY m.codigo;
BEGIN
    FOR c IN (SELECT id_contenedor, nombre FROM contenedor WHERE id_punto_instalacion = 1 ORDER BY id_contenedor) LOOP
        DBMS_OUTPUT.PUT(c.nombre || ': ');
        FOR m IN c_mat(c.id_contenedor) LOOP DBMS_OUTPUT.PUT(m.codigo || ' '); END LOOP;
        DBMS_OUTPUT.NEW_LINE;
    END LOOP;
END;
/

-- ===== E245 · 15. Cursores · BULK COLLECT con LIMIT
-- Objetivo: Traer filas por lotes.
-- Explicación: LIMIT evita cargar millones de filas en memoria.
DECLARE
    CURSOR c IS SELECT id_consulta FROM consulta_vision;
    TYPE t_ids IS TABLE OF consulta_vision.id_consulta%TYPE;
    v t_ids;
    lotes NUMBER := 0;
BEGIN
    OPEN c;
    LOOP
        FETCH c BULK COLLECT INTO v LIMIT 50;
        EXIT WHEN v.COUNT = 0;
        lotes := lotes + 1;
        DBMS_OUTPUT.PUT_LINE('Lote ' || lotes || ': ' || v.COUNT || ' fotos');
    END LOOP;
    CLOSE c;
END;
/

-- ===== E246 · 15. Cursores · Cursor dinámico
-- Objetivo: OPEN FOR con un texto SQL.
-- Explicación: Útil cuando la consulta se arma en tiempo de ejecución; el parámetro va con USING.
DECLARE
    rc SYS_REFCURSOR;
    v_n NUMBER;
BEGIN
    OPEN rc FOR 'SELECT COUNT(*) FROM material WHERE categoria = :c' USING 2;
    FETCH rc INTO v_n;
    CLOSE rc;
    DBMS_OUTPUT.PUT_LINE('Materiales de la caneca verde: ' || v_n);
END;
/

-- ===== E247 · 15. Cursores · Recorrer el cursor de un paquete
-- Objetivo: Leer el ranking que devuelve PKG_ESTADISTICAS.
-- Explicación: Un SYS_REFCURSOR se recorre con FETCH igual que un cursor normal.
DECLARE
    rc SYS_REFCURSOR;
    v_id NUMBER; v_nom VARCHAR2(100); v_dep NUMBER; v_uni NUMBER; v_est NUMBER; v_med NUMBER; v_pue NUMBER;
BEGIN
    rc := pkg_estadisticas.ranking_verificados(SYSDATE - 30, SYSDATE, 3);
    LOOP
        FETCH rc INTO v_id, v_nom, v_dep, v_uni, v_est, v_med, v_pue;
        EXIT WHEN rc%NOTFOUND;
        DBMS_OUTPUT.PUT_LINE(v_pue || '. ' || v_nom || ' - ' || v_dep || ' verificados');
    END LOOP;
    CLOSE rc;
END;
/

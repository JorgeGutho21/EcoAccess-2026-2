-- EcoAccess · Base de Datos (profesor Amílkar) · Carpeta 21 · 05 Cursores
-- SOLO LECTURA. Conexión: EcoAccess BD LOCAL. Ejecuta con F5.
SET SERVEROUTPUT ON

PROMPT ===== A. Cursor implícito: ciclo FOR sobre un SELECT (materiales orgánicos) =====
BEGIN
    FOR r IN (SELECT codigo, nombre FROM material WHERE categoria = 2 ORDER BY codigo) LOOP
        DBMS_OUTPUT.PUT_LINE('Orgánico: ' || r.codigo || ' - ' || r.nombre);
    END LOOP;
END;
/

PROMPT ===== B. Cursor explícito con OPEN, FETCH, EXIT WHEN y CLOSE =====
DECLARE
    CURSOR c_canecas IS
        SELECT c.nombre AS caneca, p.nombre AS estacion
        FROM   contenedor c
        JOIN   punto_instalacion p ON p.id_punto_instalacion = c.id_punto_instalacion
        WHERE  c.estado = 1
        ORDER  BY c.id_contenedor;
    v_fila c_canecas%ROWTYPE;
    v_n    PLS_INTEGER := 0;
BEGIN
    OPEN c_canecas;
    LOOP
        FETCH c_canecas INTO v_fila;
        EXIT WHEN c_canecas%NOTFOUND;
        v_n := v_n + 1;
        IF v_n <= 6 THEN
            DBMS_OUTPUT.PUT_LINE(v_n || '. ' || v_fila.caneca || ' (' || v_fila.estacion || ')');
        END IF;
    END LOOP;
    DBMS_OUTPUT.PUT_LINE('Canecas en servicio: ' || c_canecas%ROWCOUNT);
    CLOSE c_canecas;
END;
/

PROMPT ===== C. EcoAccess: cursor con parámetro (depósitos de un estado) =====
DECLARE
    CURSOR c_dep (p_estado NUMBER) IS
        SELECT d.id_deposito, m.codigo
        FROM   deposito d
        JOIN   material m ON m.id_material = d.id_material_declarado
        WHERE  d.estado_verificacion = p_estado
        ORDER  BY d.id_deposito;
    v_n PLS_INTEGER := 0;
BEGIN
    FOR r IN c_dep(0) LOOP              -- 0 = PENDIENTE
        v_n := v_n + 1;
        DBMS_OUTPUT.PUT_LINE('Pendiente ' || r.id_deposito || ' · ' || r.codigo);
    END LOOP;
    DBMS_OUTPUT.PUT_LINE('Total pendientes: ' || v_n);
END;
/

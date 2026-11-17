-- EcoAccess · Base de Datos (profesor Amílkar) · Carpeta 21 · 04 Bloques anónimos y variables
-- SOLO LECTURA (los bloques solo leen y escriben en pantalla). Conexión: EcoAccess BD LOCAL. Ejecuta con F5.
SET SERVEROUTPUT ON

PROMPT ===== A. Ejemplo básico: el bloque más pequeño =====
BEGIN
    DBMS_OUTPUT.PUT_LINE('Hola desde PL/SQL');
END;
/

PROMPT ===== B. Variables, constantes y asignación =====
DECLARE
    v_mensaje   VARCHAR2(40) := 'Depósitos sintéticos';
    c_iva       CONSTANT NUMBER := 0.19;           -- una constante no puede cambiar
    v_total     NUMBER;
BEGIN
    v_total := 100 * (1 + c_iva);
    DBMS_OUTPUT.PUT_LINE(v_mensaje || ' · ejemplo de cálculo: ' || v_total);
END;
/

PROMPT ===== C. EcoAccess: SELECT INTO con %TYPE e IF =====
DECLARE
    v_total       NUMBER;
    v_verificados NUMBER;
    v_estado      deposito.estado_verificacion%TYPE := 1;   -- %TYPE copia el tipo de la columna
BEGIN
    SELECT COUNT(*) INTO v_total FROM deposito;
    SELECT COUNT(*) INTO v_verificados FROM deposito WHERE estado_verificacion = v_estado;

    DBMS_OUTPUT.PUT_LINE('Depósitos registrados: ' || v_total);
    DBMS_OUTPUT.PUT_LINE('Depósitos verificados: ' || v_verificados);

    IF v_verificados = 0 THEN
        DBMS_OUTPUT.PUT_LINE('Todavía no hay depósitos verificados.');
    ELSIF v_verificados < v_total THEN
        DBMS_OUTPUT.PUT_LINE('Hay depósitos pendientes o rechazados: '
                             || (v_total - v_verificados));
    ELSE
        DBMS_OUTPUT.PUT_LINE('Todos los depósitos están verificados.');
    END IF;
END;
/

PROMPT ===== D. EcoAccess: %ROWTYPE (una fila completa) y manejo de NO_DATA_FOUND =====
DECLARE
    v_material material%ROWTYPE;                      -- una variable con todas las columnas de MATERIAL
BEGIN
    SELECT * INTO v_material FROM material WHERE codigo = 'PET';
    DBMS_OUTPUT.PUT_LINE('PET es el material ' || v_material.id_material
                         || ', categoría ' || v_material.categoria);

    SELECT * INTO v_material FROM material WHERE codigo = 'NO_EXISTE';
EXCEPTION
    WHEN NO_DATA_FOUND THEN
        DBMS_OUTPUT.PUT_LINE('No existe ese material: NO_DATA_FOUND se atrapa y el bloque termina bien.');
END;
/

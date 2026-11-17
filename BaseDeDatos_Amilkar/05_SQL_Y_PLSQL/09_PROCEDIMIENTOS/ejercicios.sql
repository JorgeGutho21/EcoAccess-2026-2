-- 09_PROCEDIMIENTOS — 2 ejercicios del banco verificado (conexión: ECOACCESS_BD)
-- Generado por 05_SQL_Y_PLSQL/generar_carpetas_por_tema.ps1 desde 08_CONSULTAS_Y_EJERCICIOS. No editar a mano.
-- Selecciona un ejercicio y pulsa Ctrl+Enter (consulta) o F5 (bloque PL/SQL). Los que modifican datos terminan en ROLLBACK.
SET SERVEROUTPUT ON SIZE UNLIMITED

-- ===== E270 · 17. Procedimientos, funciones y paquetes · Fuente de un procedimiento
-- Objetivo: Leer el código guardado en Oracle.
-- Explicación: USER_SOURCE guarda el código fuente línea por línea.
SELECT line, text FROM user_source WHERE name = 'FN_PESO_ESTIMADO_KG' ORDER BY line FETCH FIRST 8 ROWS ONLY;

-- ===== E276 · 17. Procedimientos, funciones y paquetes · Procedimiento local
-- Objetivo: Un procedimiento que solo existe dentro del bloque.
-- Explicación: Sirve para no repetir código en un script.
DECLARE
    v NUMBER;   -- las variables van ANTES de los subprogramas locales (si no: PLS-00103)
    PROCEDURE mostrar(p_titulo VARCHAR2, p_valor NUMBER) IS
    BEGIN
        DBMS_OUTPUT.PUT_LINE(RPAD(p_titulo, 22, '.') || p_valor);
    END;
BEGIN
    SELECT COUNT(*) INTO v FROM deposito; mostrar('Depósitos', v);
    SELECT COUNT(*) INTO v FROM consulta_vision; mostrar('Fotos', v);
END;
/

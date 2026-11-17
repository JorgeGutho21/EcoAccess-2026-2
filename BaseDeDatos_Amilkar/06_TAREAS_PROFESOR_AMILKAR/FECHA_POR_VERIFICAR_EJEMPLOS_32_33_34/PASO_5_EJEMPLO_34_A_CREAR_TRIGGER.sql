-- ===========================================================================
-- DIAPOSITIVA 34: validacion de notas: crear trigger BEFORE
-- CONEXION: CNX_HR (o cualquier conexion al MISMO usuario donde se instalo)
-- ABRIR SOLO ESTE ARCHIVO Y PRESIONAR F5. Se puede repetir.
-- Datos y resultados coinciden con PLSQL_y_Disparadores.pdf.
-- ===========================================================================
SET SERVEROUTPUT ON
SET LINESIZE 150
SET PAGESIZE 80
PROMPT
PROMPT =================== DIAPOSITIVA 34: validacion de notas: crear trigger BEFORE ===================

-- 1 de 3 pasos de la DIAP. 34. Ejecutar primero este archivo.
CREATE OR REPLACE TRIGGER trg_validar_nota
BEFORE INSERT OR UPDATE ON notas
FOR EACH ROW
BEGIN
    IF :NEW.nota < 0 OR :NEW.nota > 5 THEN
        RAISE_APPLICATION_ERROR(-20001,
            'La nota debe estar entre 0 y 5');
    END IF;
END;
/
SHOW ERRORS
PROMPT Trigger listo. Abre ahora PASO_6_EJEMPLO_34_B_NOTA_INVALIDA.sql.

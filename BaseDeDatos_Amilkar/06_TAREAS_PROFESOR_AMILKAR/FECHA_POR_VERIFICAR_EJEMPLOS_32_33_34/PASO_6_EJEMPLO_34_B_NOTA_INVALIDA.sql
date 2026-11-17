-- ===========================================================================
-- DIAPOSITIVA 34: prueba de nota invalida (ERROR ESPERADO)
-- CONEXION: CNX_HR (o cualquier conexion al MISMO usuario donde se instalo)
-- ABRIR SOLO ESTE ARCHIVO Y PRESIONAR F5. Se puede repetir.
-- Datos y resultados coinciden con PLSQL_y_Disparadores.pdf.
-- ===========================================================================
SET SERVEROUTPUT ON
SET LINESIZE 150
SET PAGESIZE 80
PROMPT
PROMPT =================== DIAPOSITIVA 34: prueba de nota invalida (ERROR ESPERADO) ===================

-- 2 de 3 pasos. REQUIERE DIAP_34_A_CREAR_TRIGGER_NOTAS.
-- Ver un error ROJO es el OBJETIVO DE ESTA PRUEBA: confirma que valida.
-- La diapositiva muestra literalmente ORA-20001.
INSERT INTO notas VALUES (9001, 101, 6.5);
-- RESULTADO ESPERADO: ORA-20001: La nota debe estar entre 0 y 5.
-- La fila invalida NO se guarda. Puedes ejecutar varias veces.

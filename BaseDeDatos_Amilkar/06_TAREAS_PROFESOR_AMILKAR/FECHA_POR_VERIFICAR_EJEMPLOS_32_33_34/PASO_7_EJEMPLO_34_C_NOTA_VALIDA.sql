-- ===========================================================================
-- DIAPOSITIVA 34: prueba de nota valida
-- CONEXION: CNX_HR (o cualquier conexion al MISMO usuario donde se instalo)
-- ABRIR SOLO ESTE ARCHIVO Y PRESIONAR F5. Se puede repetir.
-- Datos y resultados coinciden con PLSQL_y_Disparadores.pdf.
-- ===========================================================================
SET SERVEROUTPUT ON
SET LINESIZE 150
SET PAGESIZE 80
PROMPT
PROMPT =================== DIAPOSITIVA 34: prueba de nota valida ===================

-- 3 de 3 pasos. REQUIERE DIAP_34_A_CREAR_TRIGGER_NOTAS.
INSERT INTO notas VALUES (9002, 101, 4.2);
SELECT id_nota, id_estudiante, nota
FROM notas
ORDER BY id_nota;
ROLLBACK;
-- RESULTADO: la nota 4.2 se admite y la prueba se deshace.

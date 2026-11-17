-- 11_SYS_REFCURSOR — 2 ejercicios del banco verificado (conexión: ECOACCESS_BD)
-- Generado por 05_SQL_Y_PLSQL/generar_carpetas_por_tema.ps1 desde 08_CONSULTAS_Y_EJERCICIOS. No editar a mano.
-- Selecciona un ejercicio y pulsa Ctrl+Enter (consulta) o F5 (bloque PL/SQL). Los que modifican datos terminan en ROLLBACK.
SET SERVEROUTPUT ON SIZE UNLIMITED

-- ===== E265 · 17. Procedimientos, funciones y paquetes · Procedimiento con cursor de salida
-- Objetivo: Resumen de los últimos 30 días.
-- Explicación: EXEC llama al procedimiento; PRINT muestra el cursor.
VARIABLE rc REFCURSOR
EXEC pr_consultar_estadisticas(SYSDATE - 30, SYSDATE, NULL, NULL, :rc)
PRINT rc

-- ===== E266 · 17. Procedimientos, funciones y paquetes · Ranking por función
-- Objetivo: Top 5 de 30 días.
-- Explicación: La función devuelve un SYS_REFCURSOR.
VARIABLE rc REFCURSOR
EXEC :rc := pkg_estadisticas.ranking_verificados(SYSDATE - 30, SYSDATE, 5)
PRINT rc

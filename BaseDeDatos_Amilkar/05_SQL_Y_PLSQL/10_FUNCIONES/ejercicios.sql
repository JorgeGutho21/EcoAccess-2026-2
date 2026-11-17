-- 10_FUNCIONES — 3 ejercicios del banco verificado (conexión: ECOACCESS_BD)
-- Generado por 05_SQL_Y_PLSQL/generar_carpetas_por_tema.ps1 desde 08_CONSULTAS_Y_EJERCICIOS. No editar a mano.
-- Selecciona un ejercicio y pulsa Ctrl+Enter (consulta) o F5 (bloque PL/SQL). Los que modifican datos terminan en ROLLBACK.
SET SERVEROUTPUT ON SIZE UNLIMITED

-- ===== E262 · 17. Procedimientos, funciones y paquetes · Llamar una función en SELECT
-- Objetivo: Peso estimado de 3 botellas.
-- Explicación: Una función devuelve un valor y se puede usar en SQL.
SELECT fn_peso_estimado_kg(1, 3) AS kg, fn_peso_estimado_kg(1, 3) * 1000 AS g FROM dual;

-- ===== E263 · 17. Procedimientos, funciones y paquetes · Función que resuelve caneca
-- Objetivo: ¿A qué caneca va el PET en la estación 1?
-- Explicación: Devuelve el id de la caneca.
SELECT fn_resolver_caneca_disponible(1, 101) AS caneca_pet, fn_resolver_caneca_disponible(1, 201) AS caneca_fruta FROM dual;

-- ===== E272 · 17. Procedimientos, funciones y paquetes · Peso estimado en un SELECT
-- Objetivo: Estimar 3 unidades de cada objeto del catálogo.
-- Explicación: Una función se usa como una columna más.
SELECT clave_objeto, gramos_tipico, fn_peso_estimado_kg(id_peso_referencial, 3) AS kg_3_unidades FROM peso_referencial WHERE ROWNUM <= 6;

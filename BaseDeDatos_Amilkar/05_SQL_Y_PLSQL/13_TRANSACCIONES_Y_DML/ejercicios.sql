-- 13_TRANSACCIONES_Y_DML — 15 ejercicios del banco verificado (conexión: ECOACCESS_BD)
-- Generado por 05_SQL_Y_PLSQL/generar_carpetas_por_tema.ps1 desde 08_CONSULTAS_Y_EJERCICIOS. No editar a mano.
-- Selecciona un ejercicio y pulsa Ctrl+Enter (consulta) o F5 (bloque PL/SQL). Los que modifican datos terminan en ROLLBACK.
SET SERVEROUTPUT ON SIZE UNLIMITED

-- ===== E192 · 12. Modificar datos (DML) y transacciones · INSERT y ROLLBACK
-- Objetivo: Insertar un material y deshacerlo.
-- Explicación: INSERT agrega una fila; ROLLBACK deshace lo no confirmado.
INSERT INTO material (codigo, nombre, categoria) VALUES ('PRUEBA_X', 'Material de prueba', 1);
SELECT codigo FROM material WHERE codigo = 'PRUEBA_X';
ROLLBACK;
SELECT COUNT(*) AS despues_del_rollback FROM material WHERE codigo = 'PRUEBA_X';

-- ===== E193 · 12. Modificar datos (DML) y transacciones · UPDATE
-- Objetivo: Cambiar una descripción (y deshacer).
-- Explicación: UPDATE ... SET ... WHERE. ¡Sin WHERE cambia TODAS las filas!
UPDATE material SET descripcion = 'Texto temporal' WHERE codigo = 'PET';
SELECT descripcion FROM material WHERE codigo = 'PET';
ROLLBACK;

-- ===== E194 · 12. Modificar datos (DML) y transacciones · DELETE
-- Objetivo: Borrar una fila de prueba (y deshacer).
-- Explicación: DELETE ... WHERE elimina filas.
INSERT INTO material (codigo, nombre, categoria) VALUES ('PRUEBA_Y', 'Para borrar', 3);
DELETE FROM material WHERE codigo = 'PRUEBA_Y';
SELECT COUNT(*) FROM material WHERE codigo = 'PRUEBA_Y';
ROLLBACK;

-- ===== E195 · 12. Modificar datos (DML) y transacciones · SAVEPOINT
-- Objetivo: Deshacer solo una parte.
-- Explicación: ROLLBACK TO vuelve al punto guardado.
INSERT INTO material (codigo, nombre, categoria) VALUES ('PRUEBA_A', 'Uno', 1);
SAVEPOINT despues_de_a;
INSERT INTO material (codigo, nombre, categoria) VALUES ('PRUEBA_B', 'Dos', 1);
ROLLBACK TO despues_de_a;
SELECT codigo FROM material WHERE codigo LIKE 'PRUEBA_%';
ROLLBACK;

-- ===== E196 · 12. Modificar datos (DML) y transacciones · Error ESPERADO: CHECK
-- Objetivo: Ver una restricción en acción (ORA-02290).
-- Explicación: Oracle rechaza datos que rompen una regla. ERROR ESPERADO.
INSERT INTO material (codigo, nombre, categoria) VALUES ('MAL', 'Categoría mala', 7);

-- ===== E197 · 12. Modificar datos (DML) y transacciones · Error ESPERADO: UNIQUE
-- Objetivo: Código repetido (ORA-00001).
-- Explicación: UNIQUE impide duplicados. ERROR ESPERADO.
INSERT INTO material (codigo, nombre, categoria) VALUES ('PET', 'Otro PET', 1);

-- ===== E198 · 12. Modificar datos (DML) y transacciones · Error ESPERADO: FK
-- Objetivo: Caneca en una estación que no existe (ORA-02291).
-- Explicación: La FK exige que el padre exista. ERROR ESPERADO.
INSERT INTO contenedor (id_punto_instalacion, nombre, categoria) VALUES (999, 'Caneca fantasma', 1);

-- ===== E199 · 12. Modificar datos (DML) y transacciones · Error ESPERADO: NOT NULL
-- Objetivo: Material sin nombre (ORA-01400).
-- Explicación: NOT NULL exige un valor. ERROR ESPERADO.
INSERT INTO material (codigo, nombre, categoria) VALUES ('SIN_NOMBRE', NULL, 1);

-- ===== E200 · 12. Modificar datos (DML) y transacciones · INSERT de varias filas
-- Objetivo: Agregar dos materiales a la vez (y deshacer).
-- Explicación: INSERT ... SELECT inserta todas las filas que devuelve la consulta; cada una recibe su propio id IDENTITY (con INSERT ALL se repetiría el id y daría ORA-00001).
INSERT INTO material (codigo, nombre, categoria) SELECT 'PRUEBA_M1', 'Prueba uno', 1 FROM dual UNION ALL SELECT 'PRUEBA_M2', 'Prueba dos', 3 FROM dual;
SELECT codigo FROM material WHERE codigo LIKE 'PRUEBA_M%';
ROLLBACK;

-- ===== E201 · 12. Modificar datos (DML) y transacciones · MERGE
-- Objetivo: Insertar o actualizar según exista (y deshacer).
-- Explicación: MERGE decide fila por fila: WHEN MATCHED actualiza, WHEN NOT MATCHED inserta.
MERGE INTO material m USING (SELECT 'PET' AS codigo FROM dual UNION ALL SELECT 'PRUEBA_MERGE' FROM dual) s ON (m.codigo = s.codigo) WHEN MATCHED THEN UPDATE SET m.descripcion = 'Actualizado por MERGE' WHEN NOT MATCHED THEN INSERT (codigo, nombre, categoria) VALUES (s.codigo, 'Nuevo por MERGE', 1);
SELECT codigo, descripcion FROM material WHERE codigo IN ('PET', 'PRUEBA_MERGE');
ROLLBACK;

-- ===== E202 · 12. Modificar datos (DML) y transacciones · UPDATE de varias filas
-- Objetivo: Deshabilitar todos los peligrosos (y deshacer).
-- Explicación: SQL%ROWCOUNT / el mensaje dice cuántas filas cambiaron.
UPDATE material SET habilitado = 0 WHERE categoria = 4;
SELECT codigo, habilitado FROM material WHERE categoria = 4;
ROLLBACK;

-- ===== E203 · 12. Modificar datos (DML) y transacciones · RETURNING INTO
-- Objetivo: Saber el id que generó un INSERT.
-- Explicación: RETURNING devuelve columnas de la fila insertada.
DECLARE
    v_id material.id_material%TYPE;
BEGIN
    INSERT INTO material (codigo, nombre, categoria) VALUES ('PRUEBA_RET', 'Con returning', 1) RETURNING id_material INTO v_id;
    DBMS_OUTPUT.PUT_LINE('Id generado: ' || v_id);
    ROLLBACK;
END;
/

-- ===== E204 · 12. Modificar datos (DML) y transacciones · Error ESPERADO: IDENTITY ALWAYS
-- Objetivo: Escribir el id de un usuario a mano (ORA-32795).
-- Explicación: Con GENERATED ALWAYS solo Oracle pone el id. ERROR ESPERADO.
INSERT INTO usuario (id_usuario, nombre, correo_electronico) VALUES (99999, 'Prueba', 'prueba@ejemplo.test');

-- ===== E205 · 12. Modificar datos (DML) y transacciones · Error ESPERADO: borrar un padre
-- Objetivo: Borrar un material usado por canecas (ORA-02292).
-- Explicación: Sin ON DELETE CASCADE, Oracle protege a los hijos. ERROR ESPERADO.
DELETE FROM material WHERE codigo = 'PET';

-- ===== E206 · 12. Modificar datos (DML) y transacciones · Transacción de dos pasos
-- Objetivo: Dos cambios que se confirman o se deshacen juntos.
-- Explicación: Hasta el COMMIT, nadie más ve los cambios; aquí se deshacen con ROLLBACK.
UPDATE material SET descripcion = 'Paso 1' WHERE codigo = 'PET';
UPDATE material SET descripcion = 'Paso 2' WHERE codigo = 'VIDRIO';
SELECT codigo, descripcion FROM material WHERE codigo IN ('PET', 'VIDRIO');
ROLLBACK;
SELECT codigo, descripcion FROM material WHERE codigo IN ('PET', 'VIDRIO');

-- 16_ESQUEMA_HR_PRACTICAS — 20 ejercicios del banco verificado (conexión: ECOACCESS_PRACTICAS)
-- Generado por 05_SQL_Y_PLSQL/generar_carpetas_por_tema.ps1 desde 08_CONSULTAS_Y_EJERCICIOS. No editar a mano.
-- Selecciona un ejercicio y pulsa Ctrl+Enter (consulta) o F5 (bloque PL/SQL). Los que modifican datos terminan en ROLLBACK.
SET SERVEROUTPUT ON SIZE UNLIMITED

-- ===== E290 · 19. Esquema HR (prácticas) · Empleados de HR
-- Objetivo: Leer HR desde otro esquema.
-- Explicación: Con permiso SELECT se escribe esquema.tabla.
SELECT employee_id, first_name, last_name, salary FROM hr.employees WHERE department_id = 60 ORDER BY employee_id;

-- ===== E291 · 19. Esquema HR (prácticas) · JOIN empleados-departamentos
-- Objetivo: Nombre del departamento de cada empleado.
-- Explicación: JOIN clásico de HR.
SELECT e.last_name, d.department_name FROM hr.employees e JOIN hr.departments d ON d.department_id = e.department_id WHERE e.salary > 12000 ORDER BY e.salary DESC;

-- ===== E292 · 19. Esquema HR (prácticas) · Salario promedio por departamento
-- Objetivo: GROUP BY sobre HR.
-- Explicación: HAVING filtra departamentos con promedio alto.
SELECT department_id, ROUND(AVG(salary)) AS promedio FROM hr.employees GROUP BY department_id HAVING AVG(salary) > 8000 ORDER BY promedio DESC;

-- ===== E293 · 19. Esquema HR (prácticas) · Empleados sin departamento
-- Objetivo: IS NULL en HR.
-- Explicación: Hay un empleado sin departamento en HR.
SELECT employee_id, last_name FROM hr.employees WHERE department_id IS NULL;

-- ===== E294 · 19. Esquema HR (prácticas) · Jefe de cada empleado (SELF JOIN)
-- Objetivo: manager_id apunta a employee_id.
-- Explicación: Autorrelación 1:N.
SELECT e.last_name AS empleado, j.last_name AS jefe FROM hr.employees e LEFT JOIN hr.employees j ON j.employee_id = e.manager_id WHERE e.department_id = 60;

-- ===== E295 · 19. Esquema HR (prácticas) · Procedimiento pr_empleados
-- Objetivo: Llamar al procedimiento de la tarea.
-- Explicación: Imprime con DBMS_OUTPUT.
EXEC pr_empleados(90)

-- ===== E296 · 19. Esquema HR (prácticas) · Función fn_empleados
-- Objetivo: La función con cursor de la tarea.
-- Explicación: Devuelve SYS_REFCURSOR.
VARIABLE rc REFCURSOR
EXEC :rc := fn_empleados(90)
PRINT rc

-- ===== E297 · 19. Esquema HR (prácticas) · Precio final con trigger
-- Objetivo: 1000 + 20 % = 1200 (y deshacer).
-- Explicación: El trigger calcula precio_final.
INSERT INTO producto_practica (nombre, precio_unitario, utilidad) VALUES ('Ejercicio', 1000, 20);
SELECT nombre, precio_final FROM producto_practica WHERE nombre = 'Ejercicio';
ROLLBACK;

-- ===== E298 · 19. Esquema HR (prácticas) · Secuencia CURRVAL y NEXTVAL
-- Objetivo: Ver cómo avanza una secuencia.
-- Explicación: NEXTVAL da el siguiente; CURRVAL repite el último de MI sesión.
SELECT seq_bd_evento_academico.NEXTVAL AS siguiente FROM dual;
SELECT seq_bd_evento_academico.CURRVAL AS actual FROM dual;

-- ===== E299 · 19. Esquema HR (prácticas) · Nota inválida (ORA-20001)
-- Objetivo: Ejemplo 34 de clase. ERROR ESPERADO.
-- Explicación: El trigger BEFORE rechaza notas fuera de 0..5.
INSERT INTO notas VALUES (9100, 1, 7);

-- ===== E300 · 19. Esquema HR (prácticas) · Empleados mejor pagados de HR
-- Objetivo: Top 5 salarios.
-- Explicación: FETCH FIRST sobre HR.EMPLOYEES (solo lectura).
SELECT first_name || ' ' || last_name AS empleado, salary FROM hr.employees ORDER BY salary DESC FETCH FIRST 5 ROWS ONLY;

-- ===== E301 · 19. Esquema HR (prácticas) · Departamentos con más de 5 empleados
-- Objetivo: GROUP BY + HAVING en HR.
-- Explicación: HAVING filtra los grupos.
SELECT d.department_name, COUNT(*) AS empleados FROM hr.employees e JOIN hr.departments d ON d.department_id = e.department_id GROUP BY d.department_name HAVING COUNT(*) > 5 ORDER BY empleados DESC;

-- ===== E302 · 19. Esquema HR (prácticas) · Sobre el promedio de su departamento
-- Objetivo: Subconsulta correlacionada en HR.
-- Explicación: Compara a cada empleado con su propio departamento.
SELECT e.last_name, e.department_id, e.salary FROM hr.employees e WHERE e.salary > (SELECT AVG(e2.salary) FROM hr.employees e2 WHERE e2.department_id = e.department_id) AND e.department_id = 50 ORDER BY e.salary DESC;

-- ===== E303 · 19. Esquema HR (prácticas) · Ranking de salarios por departamento
-- Objetivo: DENSE_RANK por partición.
-- Explicación: Puesto 1 = el mejor pagado de cada departamento.
SELECT department_id, last_name, salary, DENSE_RANK() OVER (PARTITION BY department_id ORDER BY salary DESC) AS puesto FROM hr.employees WHERE department_id IN (60, 90) ORDER BY department_id, puesto;

-- ===== E304 · 19. Esquema HR (prácticas) · Copia de empleados
-- Objetivo: Comparar la copia de prácticas con HR.
-- Explicación: EMPLEADO_PRACTICA es una copia: se puede modificar sin tocar HR.
SELECT (SELECT COUNT(*) FROM hr.employees) AS en_hr, (SELECT COUNT(*) FROM empleado_practica) AS en_practicas FROM dual;

-- ===== E305 · 19. Esquema HR (prácticas) · CRUD: insertar con el paquete
-- Objetivo: Insertar un empleado (y deshacer).
-- Explicación: pkg_empleado_crud.insertar toma el id de la secuencia (desde 1000).
DECLARE
    v_id NUMBER;
BEGIN
    pkg_empleado_crud.insertar('Empleado de la guía', 'GUIA1', 3000, 60, v_id);
    DBMS_OUTPUT.PUT_LINE('Nuevo empleado con id ' || v_id);
    ROLLBACK;
END;
/

-- ===== E306 · 19. Esquema HR (prácticas) · CRUD: consultar con refcursor
-- Objetivo: Leer los empleados del departamento 60.
-- Explicación: La función devuelve un SYS_REFCURSOR, igual que lo consume C#.
VARIABLE rc REFCURSOR
EXEC :rc := pkg_empleado_crud.consultar(60)
PRINT rc

-- ===== E307 · 19. Esquema HR (prácticas) · CRUD: actualizar inexistente
-- Objetivo: Actualizar un id que no existe (ORA-20002).
-- Explicación: No hay DELETE en este CRUD: solo insertar, consultar y actualizar.
BEGIN
    pkg_empleado_crud.actualizar(999999, 'Nadie', 1000, 60);
EXCEPTION
    WHEN OTHERS THEN DBMS_OUTPUT.PUT_LINE(SQLERRM);
END;
/

-- ===== E308 · 19. Esquema HR (prácticas) · Límite de aumento del 20 %
-- Objetivo: El trigger recorta un aumento exagerado (y deshacer).
-- Explicación: BEFORE UPDATE modifica :NEW.salario si sube más del 20 %.
UPDATE empleados SET salario = salario * 3 WHERE id_empleado = (SELECT MIN(id_empleado) FROM empleados);
SELECT id_empleado, salario FROM empleados WHERE id_empleado = (SELECT MIN(id_empleado) FROM empleados);
ROLLBACK;

-- ===== E309 · 19. Esquema HR (prácticas) · Precio final al cambiar la utilidad
-- Objetivo: Recalcular cuando cambia solo la utilidad (y deshacer).
-- Explicación: UPDATE OF utilidad también dispara el trigger.
INSERT INTO producto_practica (nombre, precio_unitario, utilidad) VALUES ('Ejercicio 2', 1500, 20);
UPDATE producto_practica SET utilidad = 30 WHERE nombre = 'Ejercicio 2';
SELECT nombre, precio_unitario, utilidad, precio_final FROM producto_practica WHERE nombre = 'Ejercicio 2';
ROLLBACK;

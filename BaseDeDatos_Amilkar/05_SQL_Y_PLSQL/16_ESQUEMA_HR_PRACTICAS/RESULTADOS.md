# ESQUEMA HR PRACTICAS

Ejercicios: 20 · conexión: `ECOACCESS_PRACTICAS` · temas del banco: 19. Esquema HR (prácticas).

Estado y salida de la última ejecución real en Oracle (datos sintéticos). «OK (error esperado)» significa que el ejercicio demuestra un error a propósito.

| Id | Ejercicio | Estado |
|---|---|---|
| E290 | Empleados de HR | OK |
| E291 | JOIN empleados-departamentos | OK |
| E292 | Salario promedio por departamento | OK |
| E293 | Empleados sin departamento | OK |
| E294 | Jefe de cada empleado (SELF JOIN) | OK |
| E295 | Procedimiento pr_empleados | OK |
| E296 | Función fn_empleados | OK |
| E297 | Precio final con trigger | OK |
| E298 | Secuencia CURRVAL y NEXTVAL | OK |
| E299 | Nota inválida (ORA-20001) | OK (error esperado) |
| E300 | Empleados mejor pagados de HR | OK |
| E301 | Departamentos con más de 5 empleados | OK |
| E302 | Sobre el promedio de su departamento | OK |
| E303 | Ranking de salarios por departamento | OK |
| E304 | Copia de empleados | OK |
| E305 | CRUD: insertar con el paquete | OK |
| E306 | CRUD: consultar con refcursor | OK |
| E307 | CRUD: actualizar inexistente | OK |
| E308 | Límite de aumento del 20 % | OK |
| E309 | Precio final al cambiar la utilidad | OK |

## E290 · Empleados de HR

**Objetivo:** Leer HR desde otro esquema.  
**Explicación:** Con permiso SELECT se escribe esquema.tabla.  
**Estado:** OK

```sql
SELECT employee_id, first_name, last_name, salary FROM hr.employees WHERE department_id = 60 ORDER BY employee_id;
```

Salida real:

```text
EMPLOYEE_ID FIRST_NAME		 LAST_NAME		       SALARY
----------- -------------------- ------------------------- ----------
	103 Alexander		 James				 9000
	104 Bruce		 Miller 			 6000
	105 David		 Williams			 4800
	106 Valli		 Jackson			 4800
	107 Diana		 Nguyen 			 4200

5 filas seleccionadas.
```

## E291 · JOIN empleados-departamentos

**Objetivo:** Nombre del departamento de cada empleado.  
**Explicación:** JOIN clásico de HR.  
**Estado:** OK

```sql
SELECT e.last_name, d.department_name FROM hr.employees e JOIN hr.departments d ON d.department_id = e.department_id WHERE e.salary > 12000 ORDER BY e.salary DESC;
```

Salida real:

```text
LAST_NAME		  DEPARTMENT_NAME
------------------------- ------------------------------
King			  Executive
Yang			  Executive
Garcia			  Executive
Singh			  Sales
Partners		  Sales
Martinez		  Marketing
Higgins 		  Accounting
Gruenberg		  Finance

8 filas seleccionadas.
```

## E292 · Salario promedio por departamento

**Objetivo:** GROUP BY sobre HR.  
**Explicación:** HAVING filtra departamentos con promedio alto.  
**Estado:** OK

```sql
SELECT department_id, ROUND(AVG(salary)) AS promedio FROM hr.employees GROUP BY department_id HAVING AVG(salary) > 8000 ORDER BY promedio DESC;
```

Salida real:

```text
DEPARTMENT_ID	PROMEDIO
------------- ----------
	   90	   19333
	  110	   10154
	   70	   10000
	   20	    9500
	   80	    8956
	  100	    8601

6 filas seleccionadas.
```

## E293 · Empleados sin departamento

**Objetivo:** IS NULL en HR.  
**Explicación:** Hay un empleado sin departamento en HR.  
**Estado:** OK

```sql
SELECT employee_id, last_name FROM hr.employees WHERE department_id IS NULL;
```

Salida real:

```text
EMPLOYEE_ID LAST_NAME
----------- -------------------------
	178 Grant

1 fila seleccionada.
```

## E294 · Jefe de cada empleado (SELF JOIN)

**Objetivo:** manager_id apunta a employee_id.  
**Explicación:** Autorrelación 1:N.  
**Estado:** OK

```sql
SELECT e.last_name AS empleado, j.last_name AS jefe FROM hr.employees e LEFT JOIN hr.employees j ON j.employee_id = e.manager_id WHERE e.department_id = 60;
```

Salida real:

```text
EMPLEADO		  JEFE
------------------------- -------------------------
James			  Garcia
Miller			  James
Williams		  James
Jackson 		  James
Nguyen			  James

5 filas seleccionadas.
```

## E295 · Procedimiento pr_empleados

**Objetivo:** Llamar al procedimiento de la tarea.  
**Explicación:** Imprime con DBMS_OUTPUT.  
**Estado:** OK

```sql
EXEC pr_empleados(90)
```

Salida real:

```text
100 - King - 24000
101 - Yang - 17000
102 - Garcia - 17000

Procedimiento PL/SQL terminado correctamente.
```

## E296 · Función fn_empleados

**Objetivo:** La función con cursor de la tarea.  
**Explicación:** Devuelve SYS_REFCURSOR.  
**Estado:** OK

```sql
VARIABLE rc REFCURSOR
EXEC :rc := fn_empleados(90)
PRINT rc
```

Salida real:

```text
Procedimiento PL/SQL terminado correctamente.


EMPLOYEE_ID LAST_NAME			  SALARY
----------- ------------------------- ----------
	100 King			   24000
	101 Yang			   17000
	102 Garcia			   17000

3 filas seleccionadas.
```

## E297 · Precio final con trigger

**Objetivo:** 1000 + 20 % = 1200 (y deshacer).  
**Explicación:** El trigger calcula precio_final.  
**Estado:** OK

```sql
INSERT INTO producto_practica (nombre, precio_unitario, utilidad) VALUES ('Ejercicio', 1000, 20);
SELECT nombre, precio_final FROM producto_practica WHERE nombre = 'Ejercicio';
ROLLBACK;
```

Salida real:

```text
1 fila creada.


NOMBRE
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
PRECIO_FINAL
------------
Ejercicio
	1200


1 fila seleccionada.


Rollback terminado.
```

## E298 · Secuencia CURRVAL y NEXTVAL

**Objetivo:** Ver cómo avanza una secuencia.  
**Explicación:** NEXTVAL da el siguiente; CURRVAL repite el último de MI sesión.  
**Estado:** OK

```sql
SELECT seq_bd_evento_academico.NEXTVAL AS siguiente FROM dual;
SELECT seq_bd_evento_academico.CURRVAL AS actual FROM dual;
```

Salida real:

```text
SIGUIENTE
----------
	87

1 fila seleccionada.


    ACTUAL
----------
	87

1 fila seleccionada.
```

## E299 · Nota inválida (ORA-20001)

**Objetivo:** Ejemplo 34 de clase. ERROR ESPERADO.  
**Explicación:** El trigger BEFORE rechaza notas fuera de 0..5.  
**Estado:** OK (error esperado)

```sql
INSERT INTO notas VALUES (9100, 1, 7);
```

Salida real:

```text
INSERT INTO notas VALUES (9100, 1, 7)
            *
ERROR en línea 1:
ORA-20001: La nota debe estar entre 0 y 5
ORA-06512: en "ECOACCESS_PRACTICAS.TRG_VALIDAR_NOTA", línea 3
ORA-04088: error durante la ejecución del disparador 'ECOACCESS_PRACTICAS.TRG_VALIDAR_NOTA'
```

## E300 · Empleados mejor pagados de HR

**Objetivo:** Top 5 salarios.  
**Explicación:** FETCH FIRST sobre HR.EMPLOYEES (solo lectura).  
**Estado:** OK

```sql
SELECT first_name || ' ' || last_name AS empleado, salary FROM hr.employees ORDER BY salary DESC FETCH FIRST 5 ROWS ONLY;
```

Salida real:

```text
EMPLEADO					   SALARY
---------------------------------------------- ----------
Steven King					    24000
Neena Yang					    17000
Lex Garcia					    17000
John Singh					    14000
Karen Partners					    13500

5 filas seleccionadas.
```

## E301 · Departamentos con más de 5 empleados

**Objetivo:** GROUP BY + HAVING en HR.  
**Explicación:** HAVING filtra los grupos.  
**Estado:** OK

```sql
SELECT d.department_name, COUNT(*) AS empleados FROM hr.employees e JOIN hr.departments d ON d.department_id = e.department_id GROUP BY d.department_name HAVING COUNT(*) > 5 ORDER BY empleados DESC;
```

Salida real:

```text
DEPARTMENT_NAME 		EMPLEADOS
------------------------------ ----------
Shipping			       45
Sales				       34
Finance 				6
Purchasing				6

4 filas seleccionadas.
```

## E302 · Sobre el promedio de su departamento

**Objetivo:** Subconsulta correlacionada en HR.  
**Explicación:** Compara a cada empleado con su propio departamento.  
**Estado:** OK

```sql
SELECT e.last_name, e.department_id, e.salary FROM hr.employees e WHERE e.salary > (SELECT AVG(e2.salary) FROM hr.employees e2 WHERE e2.department_id = e.department_id) AND e.department_id = 50 ORDER BY e.salary DESC;
```

Salida real:

```text
LAST_NAME		  DEPARTMENT_ID     SALARY
------------------------- ------------- ----------
Fripp				     50       8200
Weiss				     50       8000
Kaufling			     50       7900
Vollman 			     50       6500
Mourgos 			     50       5800
Sarchand			     50       4200
Bull				     50       4100
Bell				     50       4000
Everett 			     50       3900
Chung				     50       3800
Dilly				     50       3600
Ladwig				     50       3600
Rajs				     50       3500

13 filas seleccionadas.
```

## E303 · Ranking de salarios por departamento

**Objetivo:** DENSE_RANK por partición.  
**Explicación:** Puesto 1 = el mejor pagado de cada departamento.  
**Estado:** OK

```sql
SELECT department_id, last_name, salary, DENSE_RANK() OVER (PARTITION BY department_id ORDER BY salary DESC) AS puesto FROM hr.employees WHERE department_id IN (60, 90) ORDER BY department_id, puesto;
```

Salida real:

```text
DEPARTMENT_ID LAST_NAME 		    SALARY     PUESTO
------------- ------------------------- ---------- ----------
	   60 James			      9000	    1
	   60 Miller			      6000	    2
	   60 Williams			      4800	    3
	   60 Jackson			      4800	    3
	   60 Nguyen			      4200	    4
	   90 King			     24000	    1
	   90 Yang			     17000	    2
	   90 Garcia			     17000	    2

8 filas seleccionadas.
```

## E304 · Copia de empleados

**Objetivo:** Comparar la copia de prácticas con HR.  
**Explicación:** EMPLEADO_PRACTICA es una copia: se puede modificar sin tocar HR.  
**Estado:** OK

```sql
SELECT (SELECT COUNT(*) FROM hr.employees) AS en_hr, (SELECT COUNT(*) FROM empleado_practica) AS en_practicas FROM dual;
```

Salida real:

```text
EN_HR EN_PRACTICAS
---------- ------------
       107	    116

1 fila seleccionada.
```

## E305 · CRUD: insertar con el paquete

**Objetivo:** Insertar un empleado (y deshacer).  
**Explicación:** pkg_empleado_crud.insertar toma el id de la secuencia (desde 1000).  
**Estado:** OK

```sql
DECLARE
    v_id NUMBER;
BEGIN
    pkg_empleado_crud.insertar('Empleado de la guía', 'GUIA1', 3000, 60, v_id);
    DBMS_OUTPUT.PUT_LINE('Nuevo empleado con id ' || v_id);
    ROLLBACK;
END;
/
```

Salida real:

```text
Nuevo empleado con id 1086

Procedimiento PL/SQL terminado correctamente.
```

## E306 · CRUD: consultar con refcursor

**Objetivo:** Leer los empleados del departamento 60.  
**Explicación:** La función devuelve un SYS_REFCURSOR, igual que lo consume C#.  
**Estado:** OK

```sql
VARIABLE rc REFCURSOR
EXEC :rc := pkg_empleado_crud.consultar(60)
PRINT rc
```

Salida real:

```text
Procedimiento PL/SQL terminado correctamente.


ID_EMPLEADO NOMBRE					   EMAIL			SALARIO ID_DEPARTAMENTO DEPARTAMENTO
----------- ---------------------------------------------- ------------------------- ---------- --------------- ------------------------------
	103 Alexander James				   AJAMES			   9000 	     60 IT
	104 Bruce Miller				   BMILLER			   6000 	     60 IT
	105 David Williams				   DWILLIAMS			   4800 	     60 IT
	106 Valli Jackson				   VJACKSON			   4800 	     60 IT
	107 Diana Nguyen				   DNGUYEN			   4200 	     60 IT
       1004 Empleado prueba consola C# (actualizado)	   PRUEBACS			   4500 	     60 IT
       1044 Empleado prueba consola C# (actualizado)	   PRUEBACS			   4500 	     60 IT
       1056 Empleado prueba consola C# (actualizado)	   PRUEBACS			   4500 	     60 IT
       1061 Empleado demo ventana 105115		   DEMO105115			   3500 	     60 IT
       1065 Empleado prueba consola C# (actualizado)	   PRUEBACS			   4500 	     60 IT
       1067 Empleado demo ventana 105250		   DEMO105250			   3500 	     60 IT
       1072 Empleado prueba consola C# (actualizado)	   PRUEBACS			   4500 	     60 IT
       1074 Empleado demo ventana 112420		   DEMO112420			   3500 	     60 IT
       1080 Empleado prueba consola C# (actualizado)	   PRUEBACS			   4500 	     60 IT

14 filas seleccionadas.
```

## E307 · CRUD: actualizar inexistente

**Objetivo:** Actualizar un id que no existe (ORA-20002).  
**Explicación:** No hay DELETE en este CRUD: solo insertar, consultar y actualizar.  
**Estado:** OK

```sql
BEGIN
    pkg_empleado_crud.actualizar(999999, 'Nadie', 1000, 60);
EXCEPTION
    WHEN OTHERS THEN DBMS_OUTPUT.PUT_LINE(SQLERRM);
END;
/
```

Salida real:

```text
ORA-20002: No existe empleado con ese ID

Procedimiento PL/SQL terminado correctamente.
```

## E308 · Límite de aumento del 20 %

**Objetivo:** El trigger recorta un aumento exagerado (y deshacer).  
**Explicación:** BEFORE UPDATE modifica :NEW.salario si sube más del 20 %.  
**Estado:** OK

```sql
UPDATE empleados SET salario = salario * 3 WHERE id_empleado = (SELECT MIN(id_empleado) FROM empleados);
SELECT id_empleado, salario FROM empleados WHERE id_empleado = (SELECT MIN(id_empleado) FROM empleados);
ROLLBACK;
```

Salida real:

```text
Aumento recortado al 20 %: 9000000 -> 3600000

1 fila actualizada.


ID_EMPLEADO    SALARIO
----------- ----------
	  1    3600000

1 fila seleccionada.


Rollback terminado.
```

## E309 · Precio final al cambiar la utilidad

**Objetivo:** Recalcular cuando cambia solo la utilidad (y deshacer).  
**Explicación:** UPDATE OF utilidad también dispara el trigger.  
**Estado:** OK

```sql
INSERT INTO producto_practica (nombre, precio_unitario, utilidad) VALUES ('Ejercicio 2', 1500, 20);
UPDATE producto_practica SET utilidad = 30 WHERE nombre = 'Ejercicio 2';
SELECT nombre, precio_unitario, utilidad, precio_final FROM producto_practica WHERE nombre = 'Ejercicio 2';
ROLLBACK;
```

Salida real:

```text
1 fila creada.

Producto 214: precio final 1800 -> 1950

1 fila actualizada.


NOMBRE
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
PRECIO_UNITARIO   UTILIDAD PRECIO_FINAL
--------------- ---------- ------------
Ejercicio 2
	   1500 	30	   1950


1 fila seleccionada.


Rollback terminado.
```

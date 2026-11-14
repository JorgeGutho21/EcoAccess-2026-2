# PAQUETES

Ejercicios: 9 · conexión: `ECOACCESS_BD` · temas del banco: 17. Procedimientos, funciones y paquetes.

Estado y salida de la última ejecución real en Oracle (datos sintéticos). «OK (error esperado)» significa que el ejercicio demuestra un error a propósito.

| Id | Ejercicio | Estado |
|---|---|---|
| E264 | Función de paquete | OK |
| E267 | Tarifa vigente | OK |
| E268 | Verificar un depósito (y deshacer) | OK |
| E269 | Error de negocio: verificar lo propio | OK |
| E271 | Parámetros de un paquete | OK |
| E273 | Saldo con la función del paquete | OK |
| E274 | ¿Tiene el rol? | OK |
| E275 | Hora de Colombia a UTC | OK |
| E277 | Rechazar un depósito (y deshacer) | OK |

## E264 · Función de paquete

**Objetivo:** Saldo de EcoPuntos de un ciudadano.  
**Explicación:** paquete.funcion(parámetros).  
**Estado:** OK

```sql
SELECT nombre, pkg_ecopuntos.saldo(id_usuario) AS saldo FROM usuario WHERE id_usuario IN (4, 6, 11);
```

Salida real:

```text
NOMBRE
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
     SALDO
----------
Ana Castro Vargas
	72

María Gómez Oñate
	64

Jhon Zuleta Rojas
	19


3 filas seleccionadas.
```

## E267 · Tarifa vigente

**Objetivo:** ¿Qué tarifa aplica hoy al PET?  
**Explicación:** regla_vigente devuelve NULL si no hay tarifa.  
**Estado:** OK

```sql
SELECT r.id_regla, r.puntos, r.descripcion FROM regla_ecopuntos r WHERE r.id_regla = pkg_ecopuntos.regla_vigente(101, SYS_EXTRACT_UTC(SYSTIMESTAMP));
```

Salida real:

```text
ID_REGLA     PUNTOS
---------- ----------
DESCRIPCION
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	 8	   12
Tarifa sintética v2: 12 puntos por botella PET verificada


1 fila seleccionada.
```

## E268 · Verificar un depósito (y deshacer)

**Objetivo:** El flujo completo del operario.  
**Explicación:** Verificar acredita puntos en la misma transacción.  
**Estado:** OK

```sql
DECLARE
    v_dep NUMBER; v_titular NUMBER; v_ver NUMBER; v_mat NUMBER; v_cant NUMBER;
BEGIN
    SELECT d.id_deposito, i.id_usuario, d.id_material_declarado, NVL(d.cantidad_declarada, 1) INTO v_dep, v_titular, v_mat, v_cant
    FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion
    WHERE d.estado_verificacion = 0 AND i.id_usuario IS NOT NULL FETCH FIRST 1 ROWS ONLY;
    SELECT id_usuario INTO v_ver FROM usuario WHERE correo_electronico = 'operaria.dos@ejemplo.test';
    DBMS_OUTPUT.PUT_LINE('Saldo antes: ' || pkg_ecopuntos.saldo(v_titular));
    pkg_deposito.verificar(v_dep, v_ver, v_mat, v_cant);
    DBMS_OUTPUT.PUT_LINE('Saldo después: ' || pkg_ecopuntos.saldo(v_titular) || ' (si el material tiene tarifa)');
    ROLLBACK;
END;
/
```

Salida real:

```text
Saldo antes: 42
Saldo después: 57 (si el material tiene tarifa)

Procedimiento PL/SQL terminado correctamente.
```

## E269 · Error de negocio: verificar lo propio

**Objetivo:** Un titular no puede verificarse (ORA-20101).  
**Explicación:** El trigger 2 protege la regla aunque se salte la aplicación.  
**Estado:** OK

```sql
DECLARE
    v_dep NUMBER; v_titular NUMBER;
BEGIN
    SELECT d.id_deposito, i.id_usuario INTO v_dep, v_titular FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden
    JOIN interaccion i ON i.id_interaccion = o.id_interaccion WHERE d.estado_verificacion = 0 AND i.id_usuario IS NOT NULL FETCH FIRST 1 ROWS ONLY;
    pkg_deposito.verificar(v_dep, v_titular, 101, 1);
EXCEPTION
    WHEN OTHERS THEN DBMS_OUTPUT.PUT_LINE(SUBSTR(SQLERRM, 1, 120)); ROLLBACK;
END;
/
```

Salida real:

```text
ORA-20101: Nadie puede verificar su propio depósito (usuario 8).
ORA-06512: en "ECOACCESS_BD.TRG_DEPOSITO_COHERENCIA", l

Procedimiento PL/SQL terminado correctamente.
```

## E271 · Parámetros de un paquete

**Objetivo:** Ver la firma de los procedimientos.  
**Explicación:** USER_ARGUMENTS describe los parámetros.  
**Estado:** OK

```sql
SELECT object_name, argument_name, in_out, data_type FROM user_arguments WHERE package_name = 'PKG_DEPOSITO' AND object_name = 'VERIFICAR' ORDER BY position;
```

Salida real:

```text
OBJECT_NAME
--------------------------------------------------------------------------------------------------------------------------------
ARGUMENT_NAME															 IN_OUT    DATA_TYPE
-------------------------------------------------------------------------------------------------------------------------------- --------- ------------------------------
VERIFICAR
P_ID_DEPOSITO															 IN	   NUMBER

VERIFICAR
P_ID_VERIFICADOR														 IN	   NUMBER

VERIFICAR
P_ID_MATERIAL															 IN	   NUMBER

VERIFICAR
P_CANTIDAD															 IN	   NUMBER

VERIFICAR
P_PESO_MEDIDO_KG														 IN	   NUMBER

VERIFICAR
P_INSTRUMENTO															 IN	   VARCHAR2

VERIFICAR
P_PRECISION_G															 IN	   NUMBER

VERIFICAR
P_MOTIVO															 IN	   VARCHAR2


8 filas seleccionadas.
```

## E273 · Saldo con la función del paquete

**Objetivo:** Comparar la función con la vista.  
**Explicación:** Ambas leen el mismo libro: deben coincidir.  
**Estado:** OK

```sql
SELECT s.nombre, s.saldo AS saldo_vista, pkg_ecopuntos.saldo(s.id_usuario) AS saldo_funcion FROM vw_saldo_ecopuntos s WHERE s.saldo > 0 FETCH FIRST 5 ROWS ONLY;
```

Salida real:

```text
NOMBRE
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
SALDO_VISTA SALDO_FUNCION
----------- -------------
María Gómez Oñate
	 64	       64

Miguel Molina Castro
	 10	       10

Yuliana Ortega Quintero
	  8		8

Ana Castro Vargas
	 72	       72

Carlos Vargas Barros
	 53	       53


5 filas seleccionadas.
```

## E274 · ¿Tiene el rol?

**Objetivo:** Usar tiene_rol en una consulta.  
**Explicación:** Devuelve 1 o 0; no confundir con el usuario de Oracle.  
**Estado:** OK

```sql
SELECT nombre, pkg_contexto_app.tiene_rol(id_usuario, 2) AS es_operario, pkg_contexto_app.tiene_rol(id_usuario, 3) AS es_admin FROM usuario WHERE correo_electronico NOT LIKE 'ciudadano%' FETCH FIRST 5 ROWS ONLY;
```

Salida real:

```text
NOMBRE
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
ES_OPERARIO   ES_ADMIN
----------- ----------
Operario Demo Uno (SINTÉTICO)
	  1	     0

Operaria Demo Dos (SINTÉTICO)
	  1	     0

Administradora Demo (SINTÉTICO)
	  0	     1


3 filas seleccionadas.
```

## E275 · Hora de Colombia a UTC

**Objetivo:** La función a_utc del paquete de estadísticas.  
**Explicación:** Colombia está 5 horas detrás de UTC.  
**Estado:** OK

```sql
SELECT pkg_estadisticas.a_utc(DATE '2026-11-11') AS medianoche_colombia_en_utc FROM dual;
```

Salida real:

```text
MEDIANOCHE_COLOMBIA_EN_UTC
---------------------------------------------------------------------------
11/11/26 05:00:00,000000000 AM

1 fila seleccionada.
```

## E277 · Rechazar un depósito (y deshacer)

**Objetivo:** El operario rechaza con motivo.  
**Explicación:** pkg_deposito.rechazar exige motivo; el trigger 3 audita como RECHAZO.  
**Estado:** OK

```sql
DECLARE
    v_dep NUMBER; v_ver NUMBER; v_op VARCHAR2(12);
BEGIN
    SELECT MIN(d.id_deposito) INTO v_dep FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion JOIN usuario u ON u.id_usuario = i.id_usuario WHERE d.estado_verificacion = 0 AND u.correo_electronico <> 'operaria.dos@ejemplo.test';
    SELECT id_usuario INTO v_ver FROM usuario WHERE correo_electronico = 'operaria.dos@ejemplo.test';
    pkg_deposito.rechazar(v_dep, v_ver, 'Ejercicio de la guía: residuo no encontrado');
    SELECT operacion INTO v_op FROM auditoria_deposito WHERE id_deposito = v_dep AND id_auditoria = (SELECT MAX(id_auditoria) FROM auditoria_deposito WHERE id_deposito = v_dep);
    DBMS_OUTPUT.PUT_LINE('Depósito ' || v_dep || ' auditado como ' || v_op);
    ROLLBACK;
END;
/
```

Salida real:

```text
Depósito 2 auditado como RECHAZO

Procedimiento PL/SQL terminado correctamente.
```

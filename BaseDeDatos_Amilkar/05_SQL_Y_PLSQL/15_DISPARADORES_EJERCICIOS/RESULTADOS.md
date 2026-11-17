# DISPARADORES EJERCICIOS

Ejercicios: 12 · conexión: `ECOACCESS_BD` · temas del banco: 18. Disparadores (triggers).

Estado y salida de la última ejecución real en Oracle (datos sintéticos). «OK (error esperado)» significa que el ejercicio demuestra un error a propósito.

| Id | Ejercicio | Estado |
|---|---|---|
| E278 | Ver los disparadores | OK |
| E279 | Trigger 1 en acción (y deshacer) | OK |
| E280 | Trigger 4 en acción | OK |
| E281 | Trigger 3: leer la auditoría | OK |
| E282 | Trigger 2: material que la caneca no admite | OK |
| E283 | Orden de los triggers | OK |
| E284 | Tipo y evento de cada trigger | OK |
| E285 | La cláusula WHEN guardada | OK |
| E286 | Trigger 1 con el mismo estado | OK |
| E287 | Trigger 2: autoverificación | OK |
| E288 | Trigger 3: auditoría deshecha | OK |
| E289 | Historial real de canecas | OK |

## E278 · Ver los disparadores

**Objetivo:** Listar triggers y su momento.  
**Explicación:** BEFORE/AFTER, EACH ROW y el evento.  
**Estado:** OK

```sql
SELECT trigger_name, trigger_type, triggering_event, table_name FROM user_triggers ORDER BY table_name;
```

Salida real:

```text
TRIGGER_NAME															 TRIGGER_TYPE
-------------------------------------------------------------------------------------------------------------------------------- ----------------
TRIGGERING_EVENT
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
TABLE_NAME
--------------------------------------------------------------------------------------------------------------------------------
TRG_CONTENEDOR_AUD_ESTADO													 AFTER EACH ROW
UPDATE
CONTENEDOR

TRG_DEPOSITO_AUD_VERIFICACION													 AFTER EACH ROW
UPDATE
DEPOSITO

TRG_DEPOSITO_COHERENCIA 													 BEFORE EACH ROW
INSERT OR UPDATE
DEPOSITO

TRG_DEPOSITO_BLOQUEO_CERRADO													 BEFORE EACH ROW
UPDATE
DEPOSITO


4 filas seleccionadas.
```

## E279 · Trigger 1 en acción (y deshacer)

**Objetivo:** Cambiar el estado de una caneca deja historia.  
**Explicación:** AFTER UPDATE OF estado escribe en HISTORIAL_ESTADO_CONTENEDOR.  
**Estado:** OK

```sql
DECLARE
    v_adm NUMBER; v_antes NUMBER; v_despues NUMBER;
BEGIN
    SELECT id_usuario INTO v_adm FROM usuario WHERE correo_electronico = 'administradora@ejemplo.test';
    SELECT COUNT(*) INTO v_antes FROM historial_estado_contenedor;
    pr_cambiar_estado_contenedor(12, 2, v_adm, 'Ejercicio de la guía');
    SELECT COUNT(*) INTO v_despues FROM historial_estado_contenedor;
    DBMS_OUTPUT.PUT_LINE('Historial: ' || v_antes || ' -> ' || v_despues);
    ROLLBACK;
END;
/
```

Salida real:

```text
Historial: 6 -> 7

Procedimiento PL/SQL terminado correctamente.
```

## E280 · Trigger 4 en acción

**Objetivo:** UPDATE directo de un depósito verificado (ORA-20103).  
**Explicación:** BEFORE UPDATE bloquea; solo el procedimiento de corrección puede.  
**Estado:** OK

```sql
DECLARE
    v_dep NUMBER;
BEGIN
    SELECT MIN(id_deposito) INTO v_dep FROM deposito WHERE estado_verificacion = 1;
    UPDATE deposito SET cantidad_verificada = 99 WHERE id_deposito = v_dep;
EXCEPTION
    WHEN OTHERS THEN DBMS_OUTPUT.PUT_LINE(SUBSTR(SQLERRM, 1, 110)); ROLLBACK;
END;
/
```

Salida real:

```text
ORA-20103: El depósito 1 ya está VERIFICADO. Solo se cambia con PKG_DEPOSITO.corregir_verificacion y un motivo

Procedimiento PL/SQL terminado correctamente.
```

## E281 · Trigger 3: leer la auditoría

**Objetivo:** Ver :OLD y :NEW guardados.  
**Explicación:** Antes y después de cada corrección.  
**Estado:** OK

```sql
SELECT id_deposito, operacion, estado_anterior, estado_nuevo, cantidad_anterior, cantidad_nueva, motivo FROM auditoria_deposito WHERE operacion IN ('CORRECCION', 'REAPERTURA');
```

Salida real:

```text
ID_DEPOSITO OPERACION					     ESTADO_ANTERIOR ESTADO_NUEVO CANTIDAD_ANTERIOR CANTIDAD_NUEVA
----------- ------------------------------------------------ --------------- ------------ ----------------- --------------
MOTIVO
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	  5 CORRECCION							   1		1		  3		 1
Revisión de cámara: solo se depositó 1 unidad (simulación)

	  2 REAPERTURA							   2		0
El ciudadano presentó reclamo; se reabre la revisión (simulación)


2 filas seleccionadas.
```

## E282 · Trigger 2: material que la caneca no admite

**Objetivo:** Restos de fruta en la caneca blanca (ORA-20102).  
**Explicación:** BEFORE INSERT revisa CONTENEDOR_MATERIAL.  
**Estado:** OK

```sql
DECLARE
    v_o NUMBER; v_e NUMBER;
BEGIN
    SELECT MIN(id_orden) INTO v_o FROM orden_apertura WHERE estado = 7 AND id_contenedor = 11;
    INSERT INTO evento_dispositivo (id_contenedor, id_externo, tipo, id_orden, resultado) VALUES (11, 'GUIA-EJ', 3, v_o, 1) RETURNING id_evento INTO v_e;
    INSERT INTO deposito (id_orden, id_evento, id_material_declarado) VALUES (v_o, v_e, 201);
EXCEPTION
    WHEN OTHERS THEN DBMS_OUTPUT.PUT_LINE(SUBSTR(SQLERRM, 1, 110)); ROLLBACK;
END;
/
```

Salida real:

```text
ORA-20102: La caneca 11 no admite el material declarado 201.
ORA-06512: en "ECOACCESS_BD.TRG_DEPOSITO_COHERENC

Procedimiento PL/SQL terminado correctamente.
```

## E283 · Orden de los triggers

**Objetivo:** ¿Qué trigger sigue a cuál?  
**Explicación:** FOLLOWS define el orden entre triggers del mismo momento.  
**Estado:** OK

```sql
SELECT trigger_name, referenced_trigger_name FROM user_trigger_ordering;
```

Salida real:

```text
TRIGGER_NAME
--------------------------------------------------------------------------------------------------------------------------------
REFERENCED_TRIGGER_NAME
--------------------------------------------------------------------------------------------------------------------------------
TRG_DEPOSITO_COHERENCIA
TRG_DEPOSITO_BLOQUEO_CERRADO


1 fila seleccionada.
```

## E284 · Tipo y evento de cada trigger

**Objetivo:** Leer cuándo dispara cada uno.  
**Explicación:** TRIGGER_TYPE dice BEFORE/AFTER y EACH ROW.  
**Estado:** OK

```sql
SELECT trigger_name, trigger_type, triggering_event, status FROM user_triggers ORDER BY trigger_name;
```

Salida real:

```text
TRIGGER_NAME															 TRIGGER_TYPE
-------------------------------------------------------------------------------------------------------------------------------- ----------------
TRIGGERING_EVENT
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
STATUS
--------
TRG_CONTENEDOR_AUD_ESTADO													 AFTER EACH ROW
UPDATE
ENABLED

TRG_DEPOSITO_AUD_VERIFICACION													 AFTER EACH ROW
UPDATE
ENABLED

TRG_DEPOSITO_BLOQUEO_CERRADO													 BEFORE EACH ROW
UPDATE
ENABLED

TRG_DEPOSITO_COHERENCIA 													 BEFORE EACH ROW
INSERT OR UPDATE
ENABLED


4 filas seleccionadas.
```

## E285 · La cláusula WHEN guardada

**Objetivo:** Ver el WHEN de los triggers.  
**Explicación:** En WHEN se escribe OLD/NEW sin dos puntos.  
**Estado:** OK

```sql
SELECT trigger_name, when_clause FROM user_triggers WHERE when_clause IS NOT NULL;
```

Salida real:

```text
TRIGGER_NAME
--------------------------------------------------------------------------------------------------------------------------------
WHEN_CLAUSE
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
TRG_DEPOSITO_BLOQUEO_CERRADO
OLD.estado_verificacion <> 0

TRG_CONTENEDOR_AUD_ESTADO
OLD.estado <> NEW.estado


2 filas seleccionadas.
```

## E286 · Trigger 1 con el mismo estado

**Objetivo:** Un UPDATE que no cambia el estado no deja historia.  
**Explicación:** WHEN (OLD.estado <> NEW.estado) evita registros falsos.  
**Estado:** OK

```sql
DECLARE
    v_antes NUMBER; v_despues NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_antes FROM historial_estado_contenedor;
    UPDATE contenedor SET estado = estado WHERE id_contenedor = 11;
    SELECT COUNT(*) INTO v_despues FROM historial_estado_contenedor;
    DBMS_OUTPUT.PUT_LINE('Historial: ' || v_antes || ' -> ' || v_despues || ' (no cambia)');
    ROLLBACK;
END;
/
```

Salida real:

```text
Historial: 6 -> 6 (no cambia)

Procedimiento PL/SQL terminado correctamente.
```

## E287 · Trigger 2: autoverificación

**Objetivo:** El titular intenta verificar lo suyo (ORA-20101).  
**Explicación:** El BEFORE compara el verificador con el titular de la sesión.  
**Estado:** OK

```sql
DECLARE
    v_dep NUMBER; v_tit NUMBER;
BEGIN
    SELECT d.id_deposito, i.id_usuario INTO v_dep, v_tit FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion WHERE d.estado_verificacion = 0 FETCH FIRST 1 ROWS ONLY;
    UPDATE deposito SET estado_verificacion = 1, id_verificador = v_tit, fecha_verificacion_utc = SYS_EXTRACT_UTC(SYSTIMESTAMP) WHERE id_deposito = v_dep;
EXCEPTION
    WHEN OTHERS THEN DBMS_OUTPUT.PUT_LINE(SUBSTR(SQLERRM, 1, 110)); ROLLBACK;
END;
/
```

Salida real:

```text
ORA-20101: Nadie puede verificar su propio depósito (usuario 8).
ORA-06512: en "ECOACCESS_BD.TRG_DEPOSITO_COHE

Procedimiento PL/SQL terminado correctamente.
```

## E288 · Trigger 3: auditoría deshecha

**Objetivo:** Sin COMMIT en el trigger, el ROLLBACK borra también su auditoría.  
**Explicación:** La auditoría pertenece a la misma transacción.  
**Estado:** OK

```sql
DECLARE
    v_dep NUMBER; v_ver NUMBER; v_mat NUMBER; v_antes NUMBER; v_durante NUMBER; v_despues NUMBER;
BEGIN
    SELECT id_usuario INTO v_ver FROM usuario WHERE correo_electronico = 'administradora@ejemplo.test';
    SELECT d.id_deposito, d.id_material_declarado INTO v_dep, v_mat FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion WHERE d.estado_verificacion = 0 AND i.id_usuario <> v_ver FETCH FIRST 1 ROWS ONLY;
    SELECT COUNT(*) INTO v_antes FROM auditoria_deposito;
    pkg_deposito.verificar(v_dep, v_ver, v_mat, 1);
    SELECT COUNT(*) INTO v_durante FROM auditoria_deposito;
    ROLLBACK;
    SELECT COUNT(*) INTO v_despues FROM auditoria_deposito;
    DBMS_OUTPUT.PUT_LINE('Auditoría: ' || v_antes || ' -> ' || v_durante || ' -> (ROLLBACK) ' || v_despues);
END;
/
```

Salida real:

```text
Auditoría: 91 -> 92 -> (ROLLBACK) 91

Procedimiento PL/SQL terminado correctamente.
```

## E289 · Historial real de canecas

**Objetivo:** Lo que dejó el trigger 1 en la carga.  
**Explicación:** Cada fila es un cambio de disponibilidad con su actor.  
**Estado:** OK

```sql
SELECT h.id_contenedor, h.estado_anterior, h.estado_nuevo, u.nombre AS actor, h.motivo FROM historial_estado_contenedor h LEFT JOIN usuario u ON u.id_usuario = h.id_actor ORDER BY h.id_historial;
```

Salida real:

```text
ID_CONTENEDOR ESTADO_ANTERIOR ESTADO_NUEVO
------------- --------------- ------------
ACTOR
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
MOTIVO
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	   11		    2		 1
Administradora Demo (SINTÉTICO)
Puesta en servicio inicial (simulación)

	   12		    2		 1
Administradora Demo (SINTÉTICO)
Puesta en servicio inicial (simulación)

	   13		    2		 1
Administradora Demo (SINTÉTICO)
Puesta en servicio inicial (simulación)

	   21		    2		 1
Administradora Demo (SINTÉTICO)
Puesta en servicio inicial (simulación)

	   13		    1		 2
Administradora Demo (SINTÉTICO)
Mantenimiento de la tapa (simulación)

	   13		    2		 1
Administradora Demo (SINTÉTICO)
Mantenimiento terminado (simulación)


6 filas seleccionadas.
```

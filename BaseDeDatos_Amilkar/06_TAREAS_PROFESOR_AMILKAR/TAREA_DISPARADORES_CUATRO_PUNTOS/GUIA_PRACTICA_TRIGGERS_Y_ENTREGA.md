# Guía práctica — La tarea de los cuatro disparadores

**Asignatura:** Base de Datos · **Docente:** profesor Amílkar · **Proyecto:** EcoAccess

**Fecha comprobada de la clase relacionada:** 10 de noviembre de 2026. **Fecha específica de asignación y entrega de esta tarea:** por verificar. La fecha de la clase no se presenta como fecha de entrega.

Esta guía organiza los **cuatro puntos del enunciado**, explica por qué existen los triggers, indica qué archivo ejecutar y cómo demostrarlo en SQL Developer. Está escrita para quien está aprendiendo PL/SQL desde cero. La documentación extensa ya existe: [guía de los triggers línea por línea](../../07_DISPARADORES_TRIGGERS/GUIA_TRIGGERS_LINEA_POR_LINEA.pdf) y [documento de clase sobre :OLD/:NEW](../2026-11-10_CLASE_TRIGGERS_OLD_Y_NEW/LEEME.md).

## Primero: qué es un trigger y qué debe explicar ante el profesor

Un **trigger (disparador)** es un bloque PL/SQL guardado en Oracle que se ejecuta automáticamente cuando ocurre el evento definido en su cabecera, como INSERT o UPDATE. La aplicación no necesita llamar explícitamente al trigger. Se utiliza para validar cambios, mantener una regla o registrar una auditoría.

- **BEFORE:** actúa antes de guardar el cambio; permite validar o modificar campos de `:NEW` en un disparador de fila.
- **AFTER:** actúa después de ejecutar la modificación de la fila; es apropiado para insertar un registro de auditoría en la misma transacción.
- **FOR EACH ROW:** activa el trigger una vez por cada fila afectada. Sin él, `:OLD` y `:NEW` no están disponibles.
- **:OLD:** versión anterior de la fila. **No cambia** por ejecutar BEFORE o AFTER; en un INSERT sus campos son nulos.
- **:NEW:** valores propuestos o finales de la fila; en un DELETE sus campos son nulos. Puede asignarse en un BEFORE de fila, pero no en un AFTER.
- **WHEN:** filtra filas antes de ejecutar el cuerpo. En su condición se escribe `OLD.columna` y `NEW.columna` **sin dos puntos**; dentro de `BEGIN...END` se usa `:OLD` y `:NEW`.
- **ROLLBACK:** deshace el cambio de datos y la auditoría realizada en la misma transacción. No devuelve números ya consumidos de una secuencia.

**Ejemplo de clase:** si el salario pasa de 2.000.000 a 2.500.000, `:OLD.salario` conserva 2.000.000 y `:NEW.salario` representa 2.500.000 tanto antes como después de la operación. Un BEFORE que limite el aumento al 20 % puede convertir `:NEW.salario` en 2.400.000; entonces la auditoría AFTER registra 2.000.000 → 2.400.000. La explicación del 10/11 está vinculada arriba.

## Antes de ejecutar los ejemplos: elegir el esquema

Las prácticas se dividen en dos conexiones, y **no deben confundirse**.

| Conexión en SQL Developer | Para qué se usa | Dónde está el código |
|---|---|---|
| `ECOACCESS_BD` | Los cuatro triggers del sistema ambiental y sus pruebas | [08_triggers.sql](../../04_SCRIPTS_ORACLE/08_triggers.sql), [demo_cuatro_triggers.sql](../../07_DISPARADORES_TRIGGERS/demo_cuatro_triggers.sql) |
| `ECOACCESS_PRACTICAS` | La secuencia, la auditoría salarial, las notas y los precios | [P01](../../05_SQL_Y_PLSQL/00_PRACTICAS_VERIFICADAS/P01_secuencia_pk.sql), [P02](../../05_SQL_Y_PLSQL/00_PRACTICAS_VERIFICADAS/P02_precio_final.sql), [P03](../../05_SQL_Y_PLSQL/00_PRACTICAS_VERIFICADAS/P03_triggers_libres.sql) |

**Advertencia:** ejecutar los archivos de clase puede crear tablas, secuencias y triggers en `ECOACCESS_PRACTICAS`. Sus demostraciones incluyen ROLLBACK para los cambios de datos, pero en Oracle los `CREATE` y `CREATE OR REPLACE` hacen cambios de esquema que **no se revierten con ROLLBACK**. Use el esquema de prácticas, nunca P3, HR ni Oracle Cloud para estos ejercicios.

### Conexión de SQL Developer, paso a paso

1. Abra SQL Developer → **Conexiones** → botón **+**.
2. Escriba un nombre descriptivo, el **usuario** adecuado y la **contraseña local** de su propia instalación.
3. En conexión básica, utilice **Host** `localhost`, **Puerto** `1521`, **Nombre del servicio** `FREEPDB1`.
4. Pulse **Probar** y después **Conectar** si el resultado es correcto.
5. Para confirmar qué usuario tiene abierto, ejecute con Ctrl+Enter:

```sql
SELECT USER AS esquema_actual,
       SYS_CONTEXT('USERENV','CON_NAME') AS pdb
FROM dual;
```

6. Abra el archivo `.sql` desde **Archivo → Abrir**, seleccione arriba la conexión correcta y pulse **F5** para ejecutar el script completo. Lea **Salida de script** y **DBMS Output** cuando esté habilitado.
7. Algunos ejemplos prueban errores **intencionales**, como ORA-00001, ORA-20001 u ORA-20301; un error esperado y explicado no significa que falló la práctica.

Si nunca instaló Oracle, lea antes la [guía de descarga e instalación](../../00_EMPEZAR_AQUI/GUIA_DESCARGAS_Y_PRIMER_USO.md) y el [manual de recuperación](../../11_GUIAS_PASO_A_PASO/MANUAL_INSTALACION_Y_RECUPERACION.pdf).

---

## Punto 1 — Dos triggers que sí pertenecen a EcoAccess

**Enunciado:** identificar dos disparadores del proyecto final y explicar su función y momento de ejecución.

Para la sustentación se eligieron estos dos, ambos de la tabla `DEPOSITO`:

### A. `TRG_DEPOSITO_COHERENCIA` — BEFORE INSERT OR UPDATE

- **Qué hace:** comprueba coherencia entre depósito, orden, evento, material, caneca y peso. También evita que el titular se verifique a sí mismo y exige un verificador activo con rol permitido. Cuando corresponde, completa la fecha de detección.
- **Por qué existe:** no todas las reglas pueden expresarse con un `CHECK` simple, porque algunas dependen de información de **otras tablas**.
- **Por qué BEFORE:** debe detener una operación inválida antes de guardarla; además necesita asignar la fecha en `:NEW.fecha_deteccion_utc`.
- **Ejemplo explicado:** si una orden es para una caneca que no admite el material declarado, el trigger rechaza el registro. Si el usuario que verifica es el mismo titular, responde ORA-20101. Esto impide obtener validaciones indebidas.
- **Dónde se ve:** [código real](../../04_SCRIPTS_ORACLE/08_triggers.sql), [guía línea por línea](../../07_DISPARADORES_TRIGGERS/GUIA_TRIGGERS_LINEA_POR_LINEA.pdf).

### B. `TRG_DEPOSITO_AUD_VERIFICACION` — AFTER UPDATE de verificación

- **Qué hace:** guarda en `AUDITORIA_DEPOSITO` los valores anteriores y nuevos de una verificación, rechazo, corrección o reapertura.
- **Por qué existe:** sin el historial se pierde la explicación de quién cambió el registro, cuándo y desde qué estado.
- **Por qué AFTER:** registra el resultado de la modificación y participa en la misma transacción; si hay ROLLBACK, se deshace también la auditoría.
- **Uso de :OLD y :NEW:** `:OLD` representa los campos del depósito antes del UPDATE, y `:NEW` los nuevos campos.
- **Dónde se ve:** el mismo [08_triggers.sql](../../04_SCRIPTS_ORACLE/08_triggers.sql) y la [demostración](../../07_DISPARADORES_TRIGGERS/demo_cuatro_triggers.sql).

**Otros dos triggers existentes en el proyecto:** `TRG_CONTENEDOR_AUD_ESTADO` registra los cambios de estado de una caneca; `TRG_DEPOSITO_BLOQUEO_CERRADO` impide editar un depósito cerrado fuera del procedimiento formal de corrección. Están documentados, pero los dos anteriores son los seleccionados para el punto 1.

### Prueba del punto 1

1. Conéctese como `ECOACCESS_BD`.
2. Abra [demo_cuatro_triggers.sql](../../07_DISPARADORES_TRIGGERS/demo_cuatro_triggers.sql).
3. Pulse **F5** y observe cada bloque «antes → operación → después», los rechazos y el `ROLLBACK` final.
4. Compare con la [salida guardada](../../16_EVIDENCIAS_Y_PRUEBAS/salidas/demo_cuatro_triggers.txt).
5. Para descubrir qué triggers existen en su esquema, ejecute:

```sql
SELECT trigger_name, triggering_event, table_name, status
FROM user_triggers
ORDER BY table_name, trigger_name;
```

**Explicación oral sugerida:** «Elegimos uno BEFORE para impedir depósitos incoherentes y otro AFTER para guardar la historia de las verificaciones. Cada disparador corre por fila y utiliza los valores anteriores o nuevos cuando corresponde».

---

## Punto 2 — Llave primaria subrogada con SEQUENCE + TRIGGER

**Enunciado:** crear un trigger que asigne automáticamente un identificador único al insertar un registro.

- **Archivo:** [P01_secuencia_pk.sql](../../05_SQL_Y_PLSQL/00_PRACTICAS_VERIFICADAS/P01_secuencia_pk.sql).
- **Esquema:** `ECOACCESS_PRACTICAS`.
- **Objetos:** `BD_EVENTO_ACADEMICO`, `SEQ_BD_EVENTO_ACADEMICO` y `TRG_BD_EVENTO_ACADEMICO_ID`.

**Qué sucede:** al insertar un evento sin `ID_EVENTO`, el trigger `BEFORE INSERT FOR EACH ROW` comprueba que sea NULL y ejecuta `SEQ_BD_EVENTO_ACADEMICO.NEXTVAL`. Ese número se asigna a `:NEW.ID_EVENTO` antes del INSERT. La clave es *subrogada* porque no significa «tipo de evento» ni «nombre»: solo identifica la fila.

**Por qué no usar aquí IDENTITY:** el profesor pidió específicamente demostrar **SEQUENCE + TRIGGER**. El ejercicio utiliza ese mecanismo deliberadamente en su tabla académica.

**Pruebas incluidas:** tres eventos sin ID; comprobación de unicidad; ROLLBACK para mostrar que una secuencia puede dejar huecos; ID escrito manualmente; repetición de ID que debe producir ORA-00001.

**Pregunta habitual:** «¿Una secuencia garantiza que los números sean consecutivos?» **No**. Garantiza valores distintos de `NEXTVAL` conforme a la configuración, pero pueden existir huecos por rollback, caché u otras operaciones.

---

## Punto 3 — Dos disparadores de tema libre

**Enunciado:** presentar dos triggers de cualquier tema con código, explicación y pruebas.

**Archivo:** [P03_triggers_libres.sql](../../05_SQL_Y_PLSQL/00_PRACTICAS_VERIFICADAS/P03_triggers_libres.sql). **Antes de ejecutarlo**, prepare las tablas de clase con [P00_tablas_de_clase.sql](../../05_SQL_Y_PLSQL/00_PRACTICAS_VERIFICADAS/P00_tablas_de_clase.sql), siempre en `ECOACCESS_PRACTICAS`.

### Libre 1. `TRG_AUD_SALARIOS`

- **AFTER UPDATE OF salario ON empleados FOR EACH ROW**.
- Guarda en `AUDITORIA_SALARIOS` el empleado, salario anterior, nuevo, fecha y usuario de Oracle.
- Su condición `WHEN` evita auditar asignaciones sin cambio real; también contempla transiciones con NULL.
- Es el ejemplo más directo de la pregunta de clase: `:OLD.salario` **siempre** es el valor anterior, tanto en BEFORE como en AFTER.
- Prueba: procedimiento de aumento de 10 % a Carlos, consulta del salario actualizado y de la auditoría.

### Libre 2. `TRG_VALIDAR_NOTA`

- **BEFORE INSERT OR UPDATE ON notas FOR EACH ROW**.
- Rechaza una nota NULL, negativa o mayor que 5 mediante `RAISE_APPLICATION_ERROR(-20001, ...)`.
- Prueba negativa: intentar 6,5 produce ORA-20001. Prueba positiva: 4,2 se acepta. Un UPDATE que cambia 4,2 por −1 también se rechaza.
- Este ejemplo explica qué significa **validación de reglas de negocio** antes de guardar.

**Complementos del mismo archivo:** `TRG_LIMITE_AUMENTO` limita incrementos salariales al 20 % en BEFORE y `TRG_DESCONTAR_STOCK` descuenta inventario después de una venta. Se incluyen como ejercicios adicionales, no sustituyen los dos elegidos.

---

## Punto 4 — Actualización automática del precio final

**Enunciado:** calcular `PRECIO_FINAL` cuando se inserta el producto, cambia `PRECIO_UNITARIO`, cambia `UTILIDAD` o cambian ambos.

- **Archivo:** [P02_precio_final.sql](../../05_SQL_Y_PLSQL/00_PRACTICAS_VERIFICADAS/P02_precio_final.sql).
- **Esquema:** `ECOACCESS_PRACTICAS`.
- **Trigger:** `TRG_PRODUCTO_PRECIO_FINAL`, `BEFORE INSERT OR UPDATE OF precio_unitario, utilidad, precio_final`, `FOR EACH ROW`.

Fórmula:

```text
PRECIO_FINAL = ROUND(PRECIO_UNITARIO * (1 + UTILIDAD / 100), 2)
```

| Situación de clase | Precio unitario | Utilidad | Precio final |
|---|---:|---:|---:|
| Insertar primer producto | 1.000 | 20 % | 1.200 |
| Insertar segundo producto | 1.500 | 20 % | 1.800 |
| Cambiar solo el precio a 2.000 | 2.000 | 20 % | 2.400 |
| Cambiar solo la utilidad a 30 % | 1.500 | 30 % | 1.950 |

El trigger escribe directamente `:NEW.precio_final`, por eso debe ser BEFORE. **También observa cambios directos de `precio_final`**, para que nadie pueda dejar un valor falso escribiéndolo manualmente. Rechaza precios negativos y utilidades fuera del rango admitido.

**Aclaración:** `PRODUCTO_PRACTICA` no significa que EcoAccess venda productos. La tabla vive solo en el **esquema de prácticas**, para cumplir el ejercicio del profesor sin contaminar el modelo ambiental.

---

## Orden práctico para presentar esta tarea

| Orden | Qué abrir | Qué demostrar |
|---:|---|---|
| 1 | [Clase de :OLD y :NEW](../2026-11-10_CLASE_TRIGGERS_OLD_Y_NEW/LEEME.md) | Diferencia BEFORE / AFTER y por qué :OLD no cambia |
| 2 | [Guía línea por línea](../../07_DISPARADORES_TRIGGERS/GUIA_TRIGGERS_LINEA_POR_LINEA.pdf) | Cabeceras, evento, FOR EACH ROW, variables |
| 3 | [Demo EcoAccess](../../07_DISPARADORES_TRIGGERS/demo_cuatro_triggers.sql) | Los dos triggers seleccionados del proyecto |
| 4 | [P01](../../05_SQL_Y_PLSQL/00_PRACTICAS_VERIFICADAS/P01_secuencia_pk.sql) | NEXTVAL, PK subrogada y huecos de secuencia |
| 5 | [P00](../../05_SQL_Y_PLSQL/00_PRACTICAS_VERIFICADAS/P00_tablas_de_clase.sql) y [P03](../../05_SQL_Y_PLSQL/00_PRACTICAS_VERIFICADAS/P03_triggers_libres.sql) | Auditoría salarial y rechazo de notas |
| 6 | [P02](../../05_SQL_Y_PLSQL/00_PRACTICAS_VERIFICADAS/P02_precio_final.sql) | 1.000 → 1.200 y 1.500 → 1.800 |
| 7 | [P99 pruebas](../../05_SQL_Y_PLSQL/00_PRACTICAS_VERIFICADAS/P99_pruebas_practicas.sql) | Comprobación automatizada de prácticas (salida histórica: 19/19) |

**La explicación ampliada de cada trigger está en un documento existente**, no copiada dentro de esta página. El informe académico está en [01_DOCUMENTO_FINAL](../../01_DOCUMENTO_FINAL/01_INFORME_FINAL_ECOACCESS_APA7.pdf) (capítulos 8 y 9) y la explicación ampliada en el [documento detallado](../../01_DOCUMENTO_FINAL/02_DOCUMENTO_DETALLADO_ECOACCESS.pdf); la guía personal en [12_GUIA_PERSONAL_JORGE](../../12_GUIA_PERSONAL_JORGE/GUIA_PERSONAL_JORGE_DESDE_CERO.pdf).

## Qué entregar al profesor

- El informe universitario con los cinco apartados de Aula Web (no solo estos ejercicios).
- Código de los **dos triggers del proyecto**, además de la **secuencia + trigger**, los **dos libres** y el **precio final**.
- Salidas de prueba y una explicación de qué se esperaba y qué se obtuvo.
- El diccionario de datos y los modelos cuando se soliciten.
- Distinción entre prácticas de clase y esquema principal de EcoAccess.

**Evidencias que ya existían:** [LEEME de la tarea](LEEME.md), [salida de prácticas P99](../../16_EVIDENCIAS_Y_PRUEBAS/salidas/P99_practicas_20261111_noche.txt), [demostración de cuatro triggers](../../16_EVIDENCIAS_Y_PRUEBAS/salidas/demo_cuatro_triggers.txt). Estas son **salidas históricas**; si un profesor pide ejecución en vivo, vuelva a correr los scripts en el esquema correcto y registre la salida actual.

**No se afirma que la fecha específica de entrega sea el 10/11/2026**: ese día corresponde a la clase relacionada, pero la asignación exacta sigue sin verificarse.

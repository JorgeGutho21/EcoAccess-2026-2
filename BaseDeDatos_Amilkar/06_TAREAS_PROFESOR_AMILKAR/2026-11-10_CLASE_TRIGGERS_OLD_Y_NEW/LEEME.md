# Clase del 10/11/2026 — Triggers: `:OLD` y `:NEW` en la auditoría de salario

- **Fecha:** 10 de noviembre de 2026.
- **Material de apoyo:** `MATERIAL_APOYO_OLD_NEW_AUDITORIA_SALARIO.pdf`, fechado «Nov 10, 2026», con autor indicado «Diego Gómez». Se descargó el mismo 10/11/2026.

## Contenido de la clase

**Qué es un trigger.** Es un bloque PL/SQL guardado en Oracle que se ejecuta solo cuando ocurre un INSERT, UPDATE o DELETE sobre una tabla. Nadie lo llama: se dispara con el evento. Sirve para auditar, validar y mantener datos coherentes.

**BEFORE y AFTER.**

- **BEFORE** se ejecuta antes de que la fila se escriba. Sirve para validar o corregir `:NEW`.
- **AFTER** se ejecuta cuando la fila ya pasó todas las validaciones. Sirve para auditar, porque solo registra cambios que realmente se aplicaron.

**`:OLD` y `:NEW`.**

- `:OLD` es la fila antes del cambio y `:NEW` la fila como va a quedar.
- En un INSERT, `:OLD` es NULL; en un DELETE, `:NEW` es NULL.

**Por qué `:OLD` no cambia entre BEFORE y AFTER.** `:OLD` siempre guarda el valor que tenía la fila antes del UPDATE. Lo único que cambia con el momento es si se puede modificar `:NEW`: en BEFORE se puede; en AFTER es de solo lectura, y asignarlo produce ORA-04084.

**FOR EACH ROW.** Hace que el trigger se ejecute una vez por fila afectada. Sin esta cláusula, el trigger es de sentencia y `:OLD`/`:NEW` no existen. En la cláusula WHEN se escriben `OLD` y `NEW` sin dos puntos; dentro de BEGIN…END sí los llevan.

**Auditoría de modificaciones: salario anterior y salario nuevo.**

- El ejemplo `trg_auditoria_salario` (AFTER UPDATE OF salario, FOR EACH ROW, `WHEN (OLD.salario <> NEW.salario)`) inserta en `auditoria_salario` el salario anterior (`:OLD.salario`), el nuevo (`:NEW.salario`), el usuario y la fecha.
- La versión BEFORE limita el aumento al 20 % modificando `:NEW.salario` antes de guardarlo. Por ejemplo, si se pide 3.000.000 sobre 2.000.000, se guarda 2.400.000.

**Errores frecuentes que se vieron.**

- Olvidar FOR EACH ROW.
- Que el UPDATE no cambie el valor, de modo que `:OLD` y `:NEW` son iguales.
- Insertar `:OLD` en las dos columnas de auditoría.
- Anular el cambio con `:NEW.salario := :OLD.salario`.
- Consultar la misma tabla dentro del trigger (tabla mutante, ORA-04091).
- Dejar creados a la vez el trigger BEFORE y el AFTER, con lo que cada UPDATE audita dos veces.

**Pruebas en Oracle.** Insertar empleados, hacer UPDATE de salario y consultar la tabla de auditoría para ver una fila por cada cambio real.

## Cómo se aplicó en la entrega

| Concepto de la clase | Dónde está |
|---|---|
| Auditoría AFTER con `:OLD`/`:NEW` (empleados) | `05_SQL_Y_PLSQL/00_PRACTICAS_VERIFICADAS/P03_triggers_libres.sql`: `trg_aud_salarios`. Su WHEN también contempla los NULL, como recomienda el material. |
| BEFORE que modifica `:NEW` (tope del 20 %) | Mismo archivo: `trg_limite_aumento`. |
| La misma idea en el proyecto real | `04_SCRIPTS_ORACLE/08_triggers.sql`: `TRG_DEPOSITO_AUD_VERIFICACION` y `TRG_CONTENEDOR_AUD_ESTADO` guardan el antes y el después en AUDITORIA_DEPOSITO e HISTORIAL_ESTADO_CONTENEDOR. |
| Explicación línea por línea | `07_DISPARADORES_TRIGGERS/GUIA_TRIGGERS_LINEA_POR_LINEA.pdf` |
| Pruebas reales | `16_EVIDENCIAS_Y_PRUEBAS/salidas/P99_practicas_20261111_noche.txt` y `demo_cuatro_triggers.txt` |

# V033 en producción: procedimiento

> **Ejecutado el 25/11/2026 a las 17:30 con autorización escrita del responsable.** Evidencia completa en
> `docs/database/evidencias/v033_produccion_20261125.md`. Lo que sigue se conserva como procedimiento y como guía de reversión.

Estado al 25/11/2026: producción registra V030, V031 (juego) y V032 (paquete y ORDS del juego). **V033 no está
aplicada** (`OPERACION_ESTACION_DETALLE` no existe). Se probó en el esquema aislado `ECOACCESS_P3_PRUEBAS`: aplicación y
repetición sin errores, 0 objetos inválidos y `--probar-registro-expo9` 14/14. Solo se ejecuta con autorización
explícita y escrita del responsable; hoy no existe.

Qué hace: es aditiva. Añade 4 columnas ANULABLES en `OPERACION_ESTACION` (`tipo_usuario`, `metodo_reconocimiento`,
`confianza`, `sesion_anonima`) y 3 CHECK nuevos, crea la tabla `OPERACION_ESTACION_DETALLE` (FK a `OPERACION_ESTACION.CLAVE`)
con su índice y la vista `EA_R_ESTADISTICAS_ESTACION`. No ejecuta INSERT, UPDATE, DELETE ni DROP.

## 1. Comprobaciones previas (solo lectura)
```sql
SELECT version FROM eco_migracion_aplicada WHERE version IN ('V030','V031','V032','V033') ORDER BY 1;   -- V030..V032, sin V033
SELECT COUNT(*) FROM operacion_estacion;                                                              -- anotar el número
SELECT COUNT(*) FROM user_objects WHERE status <> 'VALID';                                            -- anotar el número
SELECT COUNT(*) FROM user_constraints WHERE table_name = 'OPERACION_ESTACION';                        -- anotar el número
```

## 2. Respaldo
V033 no modifica filas. Aun así, antes exporta a CSV en SQL Developer (Exportar → CSV, fuera del repositorio):
`SELECT * FROM operacion_estacion ORDER BY clave;` y guarda el DDL actual de la tabla (`DBMS_METADATA.GET_DDL('TABLE','OPERACION_ESTACION')`).

## 3. Aplicación
Con el instalador, que registra la huella SHA-256 en `ECO_MIGRACION_APLICADA` y aplica solo lo pendiente (`PlanInstalacion`
ya contiene V033 después de V032), o en SQL Developer como dueño del esquema:
`@database/06_migraciones/V033__operacion_estacion_detalle_invitados.sql` y después
`@database/00_administracion/05_conceder_permisos_roles.sql` (la tabla de detalle queda de solo lectura para los roles).

## 4. Verificación
```sql
SELECT object_name, status FROM user_objects
 WHERE object_name IN ('OPERACION_ESTACION_DETALLE','IX_OPEST_DETALLE_RESIDUO','EA_R_ESTADISTICAS_ESTACION');   -- VALID
SELECT COUNT(*) FROM operacion_estacion;                -- igual al número anotado
SELECT COUNT(*) FROM user_objects WHERE status <> 'VALID';   -- igual o menor al anotado
```
Después: `EcoAccess.P3.Bot.exe --comprobar-oracle` y una operación web de invitado. La app detecta V033 en 5 minutos como
máximo y empieza a escribir el detalle.

## 5. Reversión
`@database/06_migraciones/reversion/R033__operacion_estacion_detalle_invitados.sql`: borra solo lo que creó V033 y
conserva las filas de `OPERACION_ESTACION`. Exporta antes el detalle si quieres conservarlo. Después borra la fila
`V033` de `ECO_MIGRACION_APLICADA` para que el instalador la vea pendiente.

# 15 · Explicación académica: conceptos con ejemplos reales de EcoAccess

Para exponer ante el profesor de Base de Datos. Cada concepto se ilustra con un objeto que **existe** en la base oficial. Todos los ejemplos se ejecutaron el 18/11/2026:
- los de lectura, en Oracle Cloud;
- los que escriben, en el esquema aislado, con `ROLLBACK`.

Archivos: [`database/tests/ejercicios_academicos.sql`](../../database/tests/ejercicios_academicos.sql) y [`ejercicios_academicos_escritura.sql`](../../database/tests/ejercicios_academicos_escritura.sql).

## Conceptos

| Concepto | Qué es | En EcoAccess |
|---|---|---|
| **Tabla** | Conjunto de filas con las mismas columnas; guarda los datos | `DEPOSITO`: una fila por cada residuo depositado. Tiene 12 columnas; ver [05](05-tablas-relaciones.md#deposito) |
| **Clave primaria (PK)** | Columna que identifica cada fila sin repetirse ni faltar | `PK_DEPOSITO (ID_DEPOSITO)`. Oracle genera el número solo (`IDENTITY`) |
| **Clave foránea (FK)** | Columna que debe existir en otra tabla; une dos tablas y evita «huérfanos» | `FK_CONTENEDOR_PUNTO`: toda caneca pertenece a una estación que existe. Hay FK **compuestas**: `FK_DEPOSITO_EVENTO (ID_EVENTO, ID_ORDEN)` obliga a que el evento del sensor sea **de la misma orden** |
| **Relación** | Cómo se asocian filas de dos tablas: 1:1, 1:N o N:M | 1:N, una estación tiene muchas canecas. N:M, una caneca admite muchos materiales y un material va en muchas canecas, resuelta con la tabla puente `CONTENEDOR_MATERIAL` |
| **Restricción `CHECK` / `UNIQUE`** | Regla que Oracle revisa en cada `INSERT` o `UPDATE` | `CK_CONTENEDOR_ESTADO (estado IN (1,2,3))`; `UQ_DEPOSITO_ORDEN`: una orden produce como máximo un depósito |
| **Vista** | Consulta guardada con nombre; no guarda datos | `VW_SALDO_ECOPUNTOS` suma el libro de movimientos; `VW_BI_*` oculta nombres para Power BI. Ver [06](06-vistas.md) |
| **Función** | Bloque PL/SQL que **devuelve un valor**; se puede usar en un `SELECT` | `FN_RESOLVER_CANECA_DISPONIBLE(estación, material)` devuelve la caneca correcta o explica por qué no la hay. Ver [09](09-funciones.md) |
| **Procedimiento** | Bloque PL/SQL que **hace** algo; devuelve resultados por parámetros `OUT` | `PR_CAMBIAR_ESTADO_CONTENEDOR` cambia el estado de una caneca y exige rol de administrador. Ver [08](08-procedimientos.md) |
| **Paquete** | Agrupa procedimientos y funciones: la especificación es pública y el cuerpo es la implementación | `PKG_ECOPUNTOS`: pública `saldo`, privada `calcular`. Ver [07](07-paquetes.md) |
| **Disparador (trigger)** | Código que Oracle ejecuta **solo** ante un `INSERT`, `UPDATE` o `DELETE` | `TRG_CONTENEDOR_AUD_ESTADO` guarda la historia de cada cambio de estado; nadie tiene que acordarse de hacerlo. Ver [10](10-triggers-secuencias.md) |
| **Secuencia** | Generador de números únicos | Las 19 `ISEQ$$_*` que Oracle crea para las columnas `IDENTITY`. EcoAccess no usa `NEXTVAL` a mano |
| **Transacción** | Grupo de cambios que se guarda entero (`COMMIT`) o se deshace entero (`ROLLBACK`) | `PKG_DEPOSITO.verificar` actualiza el depósito y acredita los ecopuntos en **la misma** transacción. Los paquetes no hacen `COMMIT`: decide quien los llama |

**¿Qué hace Oracle y qué hace la aplicación?**
- **Oracle** guarda los datos y garantiza las reglas que nunca deben romperse, aunque un programa falle o alguien escriba SQL a mano: claves, `CHECK`, unicidad, un solo crédito por depósito, auditoría y la caneca correcta.
- **La aplicación C#** conversa con la persona por WinForms y Telegram, decide cuándo pedir cada paso, firma las órdenes para la caneca y controla las transacciones.

Ver [01-arquitectura.md](01-arquitectura.md).

## Ejercicio integral

El caso completo del registro al historial, paso por paso con sus tablas, funciones, disparadores y vistas, está en [11-flujo-deposito.md](11-flujo-deposito.md). El cálculo de ecopuntos, con un ejemplo numérico, está en [12-ecopuntos.md](12-ecopuntos.md).

## Ejercicios con su respuesta

**E1 · SELECT, WHERE, ORDER BY.** ¿Qué materiales van a la caneca blanca?
```sql
SELECT codigo, nombre FROM material WHERE categoria = 1 AND habilitado = 1 ORDER BY nombre;
```
*Respuesta (18/11/2026):* 11 materiales de categoría 1.

**E2 · Funciones de grupo.** ¿Cuántos materiales hay de cada categoría?
```sql
SELECT categoria, COUNT(*) FROM material GROUP BY categoria ORDER BY categoria;
```
*Respuesta:* 1 → 11, 2 → 2, 3 → 14, 4 → 14. Total, 41.

**E3 · JOIN de tres tablas.** Cada caneca con su estación y su color.
```sql
SELECT p.nombre AS estacion, c.nombre AS caneca, DECODE(k.categoria, 1,'BLANCA', 2,'VERDE', 3,'NEGRA') AS color
FROM   contenedor c
JOIN   punto_instalacion p ON p.id_punto_instalacion = c.id_punto_instalacion
LEFT JOIN configuracion_caneca k ON k.id_contenedor = c.id_contenedor
ORDER  BY p.nombre, k.categoria;
```
*Por qué `LEFT JOIN`:* una caneca sin configuración también debe aparecer, con el color vacío.

**E4 · Relación N:M.** ¿Cuántos materiales admite cada caneca en servicio?
```sql
SELECT c.nombre, COUNT(cm.id_material) FROM contenedor c
LEFT JOIN contenedor_material cm ON cm.id_contenedor = c.id_contenedor
WHERE c.estado = 1 GROUP BY c.nombre ORDER BY 2 DESC;
```
*Respuesta en la estación piloto:* la caneca blanca admite 11, la negra 14 y la verde 2.

**E5 · HAVING.** Estados de depósito con más de 10 casos.
```sql
SELECT estado_texto, COUNT(*) FROM vw_deposito_detalle GROUP BY estado_texto HAVING COUNT(*) > 10;
```
*Respuesta:* PENDIENTE 55 y VERIFICADO 58. `WHERE` filtra filas antes de agrupar; `HAVING` filtra grupos.

**E6 · Subconsulta con EXISTS.** Materiales con tarifa vigente.
```sql
SELECT m.codigo FROM material m
WHERE EXISTS (SELECT 1 FROM regla_ecopuntos r WHERE r.id_material = m.id_material AND r.hasta_utc IS NULL);
```
*Respuesta:* CARTON, LATA_ALUMINIO, METAL, MULTICAPA, PAPEL, PET y VIDRIO.

**E7 · Función en un SELECT.** Peso estimado de 3 unidades de cada objeto de referencia.
```sql
SELECT descripcion_objeto, gramos_tipico, fn_peso_estimado_kg(id_peso_referencial, 3) FROM peso_referencial;
```
*Fórmula:* `3 × gramos_típico / 1000`.

**E8 · Función de un paquete.** El saldo que da el paquete coincide con el de la vista.
```sql
SELECT v.id_usuario, v.saldo, pkg_ecopuntos.saldo(v.id_usuario) FROM vw_saldo_ecopuntos v FETCH FIRST 5 ROWS ONLY;
```

**E9 · Procedimiento con cursor de salida (`SYS_REFCURSOR`).**
```sql
VARIABLE rc REFCURSOR
EXEC pkg_estadisticas.consultar_resumen(SYSDATE - 30, SYSDATE + 1, NULL, NULL, :rc)
PRINT rc
```

**E10 · Bloque anónimo y excepciones.**
```sql
DECLARE v NUMBER;
BEGIN v := fn_resolver_caneca_disponible(-1, 1);
EXCEPTION WHEN OTHERS THEN DBMS_OUTPUT.PUT_LINE(SQLERRM);
END;
/
```
*Respuesta obtenida:* `ORA-20810: Identificadores no validos.` La función no adivina: explica por qué no hay caneca.

### Con escritura (solo en el esquema aislado; terminan con ROLLBACK)

**E11 · INSERT con RETURNING.**
```sql
INSERT INTO material (codigo, nombre, categoria, habilitado) VALUES ('PRUEBA_EJERCICIO', 'Material de ejercicio', 1, 1)
RETURNING id_material INTO v_material;
```
No se escribe el `id_material`: lo genera la columna `IDENTITY`.

**E12 · UPDATE y una restricción que lo impide.**
```sql
UPDATE material SET categoria = 9 WHERE id_material = v_material;   -- ORA-02290: CK_MATERIAL_CATEGORIA
```

**E13 · Procedimiento y disparador juntos.**
```sql
EXEC pr_cambiar_estado_contenedor(:caneca, 2, :administrador, 'Ejercicio académico');
SELECT * FROM historial_estado_contenedor WHERE motivo = 'Ejercicio académico';
```
*Resultado obtenido:* la caneca quedó en estado 2 y apareció **una fila** de historia que nadie insertó a mano: la escribió `TRG_CONTENEDOR_AUD_ESTADO`.

**E14 · Un disparador que protege la evidencia.**
```sql
UPDATE deposito SET fecha_deteccion_utc = fecha_deteccion_utc - 1 WHERE id_deposito = :dep;
```
*Resultado obtenido:* sobre un depósito verificado, `ORA-20103` de `TRG_DEPOSITO_BLOQUEO_CERRADO`. Sobre uno pendiente respondería `ORA-20109` de `TRG_DEPOSITO_COHERENCIA`, porque la fecha de detección no se modifica.

Al final, `ROLLBACK` deshizo E11–E13. Los conteos del esquema aislado antes y después fueron iguales: 86 usuarios, 101 depósitos, 0 movimientos, 0 auditorías.

## Preguntas que suele hacer el profesor

- **¿Por qué el saldo no es una columna?** Porque se puede calcular sumando el libro. Guardarlo dos veces permitiría que el saldo y el historial se contradigan. Ver [12](12-ecopuntos.md).
- **¿Qué impide acreditar dos veces?** El índice único `UQ_ECOP_CREDITO_INICIAL`. Es una restricción de la base, así que funciona aunque dos programas lo intenten a la vez.
- **¿Por qué `ROLLBACK` no deshace un `CREATE TABLE`?** Porque el DDL hace `COMMIT` implícito. Por eso las migraciones tienen un script de reversión aparte.
- **¿La base y SQL Developer «se sincronizan»?** No. SQL Developer es solo un cliente: muestra la base a la que se conecta. Ver [01](01-arquitectura.md).

---
archivo: 10_BASE_DE_DATOS_MER_Y_CONSULTAS
titulo: Base de datos de EcoAccess P3, modelo entidad-relación y consultas
subtitulo: Diccionario de las 29 tablas y 207 columnas de Oracle Cloud, cardinalidades, normalización, claves, restricciones y consultas explicadas
codigo: 10
---

# Alcance de este documento

Este documento describe la base de datos **real** de EcoAccess P3 en Oracle Cloud (esquema `ECOACCESS_P3_APP`). Las
tablas y columnas se extrajeron del diccionario de Oracle (`user_tables`, `user_tab_columns`, `user_constraints`,
`user_col_comments`) el 11/11/2026, así que coinciden exactamente con la base instalada. Los comentarios de tablas y
columnas son los que tiene Oracle. Cuando una columna no tiene comentario en Oracle, la celda queda vacía, en lugar de
inventarle uno.

# Modelo entidad-relación por módulos

Las 29 tablas se agrupan en módulos. Las flechas `→` indican una clave foránea (muchos → uno).

## Identidad y acceso

- `USUARIO` 1 — N `USUARIO_ROL` N — 1 `ROL`: una cuenta tiene uno o varios roles (relación muchos a muchos resuelta con una tabla intermedia).
- `USUARIO` 1 — 0..1 `CREDENCIAL_ACCESO`: solo el personal y los ciudadanos registrados con contraseña tienen credencial.
- `USUARIO` 1 — N `VINCULO_TELEGRAM`: historial de vínculos (como máximo uno activo).
- `TOKEN_VINCULACION` y `VINCULACION_PENDIENTE`: permisos de un solo uso para registrarse o vincularse.
- `CONSENTIMIENTO_VISION_EXTERNA`: autorización vigente o revocada para enviar fotos a un servicio externo.

## Catálogo y estaciones

- `PUNTO_INSTALACION` 1 — N `CONTENEDOR`: varias canecas en un lugar.
- `CONTENEDOR` N — N `MATERIAL` mediante `CONTENEDOR_MATERIAL`: qué materiales admite cada caneca.
- `CONFIGURACION_CANECA`: color (categoría), dispositivo y ubicación de cada caneca de una estación de tres canecas (V021, V024).

## Operación de la caneca

- `CONTENEDOR` 1 — N `INTERACCION` → `USUARIO` (opcional: la apertura manual no tiene ciudadano).
- `INTERACCION` 1 — N `ORDEN_APERTURA` (en la práctica, una vigente a la vez por caneca).
- `ORDEN_APERTURA` 1 — 0..1 `DEPOSITO` (`DEPOSITO.ID_ORDEN` es `UNIQUE`: un depósito por orden).
- `CONTENEDOR` 1 — N `EVENTO_DISPOSITIVO`; el depósito apunta al evento de detección que lo originó.
- `ANALISIS_IMAGEN` → `INTERACCION`: traza del análisis de la foto ligada a una solicitud.
- `CONSULTA_VISION` (V023): traza de cada foto atendida, haya o no solicitud.

## Ecopuntos

- `REGLA_ECOPUNTOS` → `MATERIAL`, `USUARIO` (creador): versiones de tarifa.
- `OPERACION_ECOPUNTOS` 1 — N `MOVIMIENTO_ECOPUNTOS` → `DEPOSITO`, `USUARIO`, `REGLA_ECOPUNTOS`: libro contable de créditos y reversos.

## Telegram y opiniones

- `CONVERSACION_TELEGRAM`, `UPDATE_TELEGRAM`: estado del diálogo y updates ya procesados.
- `OPINION`, `COMENTARIO_CIUDADANO`: valoraciones y comentarios.

## Operación del esquema y simulación

- `ECO_MIGRACION_APLICADA`: migraciones aplicadas con su huella SHA-256.
- `LOTE_SIMULACION` 1 — N `REGISTRO_SIMULACION`: qué filas creó cada lote experimental (V025).

# Normalización

- **Primera forma normal (1FN):** cada columna guarda un valor atómico. Los roles no se guardan como texto «1,2» en `USUARIO`: tienen su tabla `USUARIO_ROL`.
- **Segunda forma normal (2FN):** en las tablas con clave compuesta (`USUARIO_ROL`, `CONTENEDOR_MATERIAL`), las demás columnas dependen de toda la clave (por ejemplo, `FECHA_ASIGNACION_UTC` depende del usuario **y** del rol).
- **Tercera forma normal (3FN):** no hay dependencias entre columnas que no son clave. El nombre del material no se repite en `DEPOSITO`; se guarda `ID_MATERIAL_DECLARADO` y se consulta en `MATERIAL`.
- **Desnormalización deliberada:** las vistas de informes (`ea_r_*`) agregan y repiten datos para que Power BI y el tablero lean rápido. No guardan nada: son consultas.
- **Datos calculados que NO se guardan:** el saldo de ecopuntos (es la suma de `MOVIMIENTO_ECOPUNTOS`) y el estado de registro del depósito (se deduce de `SIN_SENSOR` y de la verificación).

# Diccionario de tablas

{{oracle_tablas}}

# Diccionario de columnas por tabla

En la columna «Clave», PK es clave primaria, FK es clave foránea y UNIQUE es una restricción de unicidad. «Referencia»
muestra la tabla a la que apunta la clave foránea. En las claves foráneas compuestas puede aparecer una sola de las tablas
referenciadas.

{{oracle_columnas}}

# Consultas explicadas línea por línea

## 1. Depósitos del lote experimental por estado y color

```sql
SELECT estado_registro, color_caneca, COUNT(*) AS depositos   -- qué columnas y la cuenta
FROM   vista_depositos_estado                                   -- vista de V025 (solo lectura)
WHERE  es_simulacion = 1                                        -- solo filas del lote SIM-2026-11-CIERRE
GROUP  BY estado_registro, color_caneca                         -- una fila por estado y color
ORDER  BY estado_registro, color_caneca;                        -- orden de lectura
```

Resultado real en Oracle Cloud: 7 filas (DECLARADA_SIN_SENSOR: blanca 24, verde 3; DETECTADA_POR_SENSOR: blanca 14,
verde 1; VERIFICADA: blanca 47, negra 2, verde 9). **Datos sintéticos de prueba, no reciclaje real.**

## 2. Roles y cuentas con credencial

```sql
SELECT r.codigo,
       COUNT(ur.id_usuario)                                      AS cuentas,
       SUM(CASE WHEN c.id_usuario IS NOT NULL THEN 1 ELSE 0 END) AS con_contrasena
FROM   rol r
LEFT   JOIN usuario_rol ur      ON ur.id_rol = r.id_rol        -- LEFT JOIN: muestra también roles sin cuentas
LEFT   JOIN credencial_acceso c ON c.id_usuario = ur.id_usuario
GROUP  BY r.codigo
ORDER  BY r.codigo;
```

Sirve para comprobar, sin ver contraseñas ni hashes, cuántos administradores y operarios pueden iniciar sesión. El 11/11/2026
había 1 administrador con contraseña (la cuenta 4521, creada por el propietario con la configuración inicial).

## 3. Reglas de ecopuntos vigentes

```sql
SELECT m.codigo, r.puntos, r.por_unidad, r.desde_utc
FROM   regla_ecopuntos r
JOIN   material m ON m.id_material = r.id_material
WHERE  r.hasta_utc IS NULL OR r.hasta_utc > SYS_EXTRACT_UTC(SYSTIMESTAMP)   -- vigentes ahora, en UTC
ORDER  BY r.puntos DESC;
```

Resultado real: 7 reglas (lata 5, PET 4, cartón 3, metal 3, vidrio 3, multicapa 2, papel 2).

## 4. Las 8 consultas de demostración

`database/04_consultas/11_consultas_cierre.sql` reúne las consultas preparadas para la sustentación (lote, estados,
pesos estimados frente a medidos, canecas de la estación, migraciones). Las 8 se ejecutaron sin errores en Oracle Cloud.

# Preguntas probables

**¿Por qué `USUARIO_ROL` y no una columna `rol` en `USUARIO`?** Porque una persona puede tener varios roles (por ejemplo,
operario y administrador). Una columna obligaría a repetir la cuenta o a guardar listas en un texto, y eso rompe la 1FN.

**¿Cómo se garantiza un solo depósito por orden?** Con la restricción `UNIQUE` en `DEPOSITO.ID_ORDEN`, además de la
comprobación en `ServicioInteracciones`. Si dos eventos llegaran a la vez, Oracle rechazaría el segundo.

**¿Dónde están las contraseñas?** En `CREDENCIAL_ACCESO`, solo como hash PBKDF2 con sal e iteraciones. Ninguna vista de
informes ni Power BI tiene acceso a esa tabla.

---
archivo: 06_ORACLE_Y_SQL_DESDE_CERO
titulo: Oracle y SQL desde cero con EcoAccess P3
subtitulo: Conexión con wallet, credenciales seguras, esquemas, tablas, claves, restricciones, repositorios, transacciones, vistas, PL/SQL y migraciones
codigo: 06
---

# Qué es una base de datos relacional

Una **base de datos relacional** guarda la información en **tablas**: filas (registros) y columnas (atributos). Las tablas
se relacionan con **claves**:

- **Clave primaria (PK):** identifica cada fila. En `USUARIO` es `ID_USUARIO`.
- **Clave foránea (FK):** apunta a la PK de otra tabla. `USUARIO_ROL.ID_USUARIO` apunta a `USUARIO.ID_USUARIO`.

EcoAccess P3 usa **Oracle Autonomous Database 19c** en Oracle Cloud (región Bogotá), esquema `ECOACCESS_P3_APP`. Ese
esquema tiene **29 tablas** y **207 columnas** (consulta del diccionario de Oracle, 11/11/2026).

# Cómo se conecta EcoAccess a Oracle Cloud

## El wallet

Oracle Autonomous Database exige **mTLS**: la conexión va cifrada y el cliente presenta un certificado. Ese certificado
viene en el **wallet**, una carpeta con `cwallet.sso`, `tnsnames.ora` y `sqlnet.ora`. La variable de entorno de Windows
`TNS_ADMIN` dice dónde está. El alias de conexión es `ecoaccess_tp` (definido en `tnsnames.ora`).

**El wallet nunca va al repositorio.** La prueba `SinSecretosEnRepositorioTests` recorre los archivos de texto buscando
claves y tokens.

## La cadena de conexión y los secretos de usuario

La contraseña de la base no está en el código. Se guarda con **User Secrets** de .NET en
`%APPDATA%\Microsoft\UserSecrets\ecoaccess-p3\secrets.json`, una carpeta del perfil de Windows fuera del repositorio. El
script `scripts/instalacion/Configurar-Credenciales.ps1` la pide y la guarda; `Validar-Entorno.ps1 -ProbarOracle`
comprueba la conexión con una consulta de solo lectura.

## La fábrica de conexiones

{{codigo:src/EcoAccess.P3.Datos/Oracle/FabricaConexionesOracle.cs:11:45}}

Cada repositorio pide una conexión, la usa y la cierra con `using`. ODP.NET mantiene un **pool**: abrir la primera
conexión cuesta varios segundos (mTLS hasta Bogotá) y las siguientes reutilizan conexiones abiertas. En la medición del
tablero, la primera prueba tardó 5 s y las siguientes menos de 1 s.

# Las tablas de EcoAccess

{{oracle_tablas}}

El diccionario completo, columna por columna, está en el documento 10.

# Restricciones: las reglas que Oracle hace cumplir

- **NOT NULL:** la columna no puede quedar vacía.
- **UNIQUE:** no puede haber dos filas con el mismo valor. Por ejemplo, `USUARIO.CORREO_ELECTRONICO` y `DEPOSITO.ID_ORDEN` (un depósito por orden, RN-17).
- **CHECK:** una condición. Por ejemplo, que el estado de una interacción esté entre 1 y 5.
- **FOREIGN KEY:** el valor debe existir en la tabla referenciada.

Las restricciones son la **última línea de defensa**: aunque un error en C# intentara guardar un dato inválido, Oracle lo
rechazaría. Cuando Oracle rechaza un duplicado responde **ORA-00001**, y `TraductorErroresOracle` lo convierte en
`ConflictoUnicidadException`, que la lógica sabe manejar.

# Cómo un repositorio habla con Oracle

Ejemplo real: el registro de un ciudadano inserta tres filas **en una sola transacción**.

{{codigo:src/EcoAccess.P3.Datos/Repositorios/RepositorioRegistroCiudadanosOracle.cs:22:70}}

Puntos clave:

1. **Parámetros enlazados** (`:nombre`, `:correo`...). El texto que escribe la persona **nunca** se pega dentro del SQL; se pasa como parámetro. Así se evita la **inyección SQL**.
2. **`RETURNING id_usuario INTO :id`** devuelve el identificador que generó Oracle.
3. **Transacción:** si falla la credencial, también se deshace el usuario. No quedan cuentas a medias.

# Transacciones

Una **transacción** agrupa varias operaciones que deben ocurrir todas o ninguna.

- `COMMIT` confirma los cambios.
- `ROLLBACK` los deshace.

En el ensayo de T001 (documento 13 y `database/05_pruebas/T001_ensayo_primera_insercion_aislado.sql`) se usó esa
propiedad para probar la inserción de reglas **sin dejar rastro**: se cerraron las reglas vigentes, se ejecutó el bloque
dos veces y al final un `ROLLBACK` dejó la base igual que antes (11 reglas antes y 11 después).

# Vistas

Una **vista** es una consulta guardada con nombre que se usa como si fuera una tabla. EcoAccess las usa para los informes:

- `ea_r_base_depositos`, `ea_r_base_puntos`, `ea_r_solicitudes`, `ea_r_opiniones` (V013, V015 y V019): agregan los datos por día y traen la columna **`es_ensayo`**, que vale 1 para el canal Simulador o Telegram simulado, canecas ficticias, estaciones «SIMULACIÓN» y códigos de prueba.
- `VISTA_DEPOSITOS_ESTADO` y `VISTA_LOTES_SIMULACION` (V025): estado de cada depósito y resumen de los lotes experimentales.

Las vistas de informes son de **solo lectura** (`WITH READ ONLY`), y la prueba `InformeInstitucionalOracleTests` lo
comprueba.

# PL/SQL: funciones, procedimientos, paquetes y disparadores

**PL/SQL** es el lenguaje de programación de Oracle. EcoAccess tiene:

- **Paquete** `PKG_ECOACCESS_STATS` (V022): agrupa funciones de estadísticas.
- **Función** `FN_RESOLVER_CANECA_DISPONIBLE` (V022).
- **Procedimiento** `PR_CONSULTAR_ESTADISTICAS` (V022).
- **Disparador** `tr_ecop_unidades` (V011): `BEFORE INSERT ON movimiento_ecopuntos FOR EACH ROW WHEN (NEW.tipo=1)`. Antes de guardar un crédito con tarifa por unidad, exige que el depósito tenga `cantidad_verificada` y que los puntos sean exactamente tarifa × cantidad; si no, lanza ORA-20113. Así ni un error en C# puede acreditar puntos por una cantidad solo declarada.
- **Bloque anónimo** de T001: `DECLARE ... BEGIN ... END;` con un ciclo `FOR` que inserta las reglas.

# Migraciones

Una **migración** es un script que cambia la estructura de la base de una versión a la siguiente. EcoAccess numera las
migraciones (`V002` ... `V025`) en `database/06_migraciones/`, y cada una aplicada queda registrada en la tabla
`ECO_MIGRACION_APLICADA` con la **huella SHA-256** del archivo exacto.

| Regla | Por qué |
|---|---|
| Una migración aplicada no se edita | su huella dejaría de coincidir; se crea otra |
| Las migraciones se prueban primero en el esquema aislado `ECOACCESS_P3_PRUEBAS` | aplicar, repetir (idempotencia), revertir y reaplicar antes de la nube |
| Las aplicadas se marcan `-text` en `.gitattributes` | un cambio de fin de línea (CRLF) cambiaría la huella |
| Cada migración nueva tiene reversión en `database/06_migraciones/reversion/` | para deshacer de forma controlada |

Estado verificado el 11/11/2026 en Oracle Cloud:

| Versión | Huella (primeros 8) | Contenido |
|---|---|---|
| V020 | 87237853 | comentarios ciudadanos |
| V021 | 91d88156 | configuración de tres canecas |
| V022 | 59610c70 | objetos PL/SQL de estadísticas |
| V023 | cb848482 | consulta de visión |
| V024 | 01622d6c | estación piloto de tres canecas |
| V025 | cf846b3a | lote de simulación |

V021 es idéntica en los repositorios EcoAccess-P3, EcoAccess-P3-Nube y EcoAccess_V2 (misma huella completa), así que no
hay versiones contradictorias de la estación de tres canecas.

# Scripts operativos

`database/07_operacion/T001__tarifas_ecopuntos_propuestas.sql` **no es una migración**: lo ejecuta un administrador real,
que queda como creador de las reglas. El 11/11/2026 se ejecutó en Oracle Cloud con el administrador 4521 y creó 7 reglas
vigentes (lata 5, PET 4, cartón 3, metal 3, vidrio 3, multicapa 2, papel 2). La evidencia está en
`docs/pruebas/resultados_cierre/t001_oracle_cloud.txt`.

# Consultas de ejemplo explicadas

## Cuántos depósitos hay por estado y color (modo experimental)

```sql
SELECT estado_registro, color_caneca, COUNT(*) AS depositos
FROM   vista_depositos_estado
WHERE  es_simulacion = 1
GROUP  BY estado_registro, color_caneca
ORDER  BY estado_registro, color_caneca;
```

- `SELECT ... COUNT(*)`: cuenta filas.
- `FROM vista_depositos_estado`: lee la vista de V025, no las tablas.
- `WHERE es_simulacion = 1`: solo el lote experimental.
- `GROUP BY`: un renglón por cada combinación de estado y color.

Resultado real (lote `SIM-2026-11-CIERRE`): 24, 3 y 0 declarados sin sensor en blanca, verde y negra; 14, 1 y 0 detectados
por sensor; 47, 9 y 2 verificados. En total, 100 depósitos sintéticos. **No son depósitos físicos.**

Las demás consultas de demostración están en `database/04_consultas/11_consultas_cierre.sql` (8 consultas, todas ejecutadas
sin errores en Oracle Cloud).

# Errores frecuentes

| Error | Significado | Qué hacer |
|---|---|---|
| ORA-00001 | se violó una restricción UNIQUE | el dato ya existe; la aplicación lo traduce a un mensaje |
| ORA-00904 | columna inválida | el nombre no existe en esa tabla; revise el documento 10 |
| ORA-00942 | tabla o vista no existe | esquema equivocado o migración no aplicada |
| ORA-12154 / ORA-12506 | no se resuelve el alias o falla la conexión | revise `TNS_ADMIN` y el wallet |
| ORA-20301 | error propio de T001 | el id no es un administrador activo |

# Glosario

- **Esquema:** el «dueño» de un conjunto de tablas. EcoAccess usa `ECOACCESS_P3_APP` en la nube y `ECOACCESS_P3_PRUEBAS` en el Oracle FREE local.
- **ODP.NET:** el controlador de Oracle para .NET (`Oracle.ManagedDataAccess`).
- **Idempotente:** se puede ejecutar varias veces con el mismo resultado.
- **mTLS:** TLS mutuo; el cliente y el servidor se identifican con certificados.

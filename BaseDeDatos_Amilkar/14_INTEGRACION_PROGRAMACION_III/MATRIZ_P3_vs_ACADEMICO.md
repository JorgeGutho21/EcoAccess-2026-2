# Matriz comparativa: modelo académico (ECOACCESS_BD) vs Programación III (ECOACCESS_P3_APP)

Fuente P3: rama `origin/desarrollo/telegram-doble-ia` (commit 1fa52ac), scripts `database/02_tablas/01_*` y `database/06_migraciones/V002–V025`, leídos con `git show` (sin modificar el repositorio). Fuente académica: `04_SCRIPTS_ORACLE/DDL_FINAL_EXTRAIDO.sql`.

Tablas académicas: 19 · tablas P3 en scripts: 28 · en ambos: 16

| Académico | P3 | Existe en | Col. A | Col. P3 | Mismo nombre | Adaptación | Nota |
|---|---|---|---:|---:|---:|---|---|
| AUDITORIA_DEPOSITO | — | Solo académico | 17 | 0 | 0 | Ninguna en P3: es extensión académica | Solo académica: la auditoría por trigger AFTER se añadió para la asignatura de Base de Datos. |
| — | COMENTARIO_CIUDADANO | Solo P3 | 0 | 10 | 0 | No se copia a la base académica | Solo P3: comentarios (V020). |
| — | CONFIGURACION_CANECA | Solo P3 | 0 | 5 | 0 | No se copia a la base académica | Solo P3: color y materiales de las tres canecas (V021). |
| — | CONSENTIMIENTO_VISION_EXTERNA | Solo P3 | 0 | 4 | 0 | No se copia a la base académica | Solo P3: consentimiento para enviar fotos a un proveedor externo de visión (V018). |
| CONSULTA_VISION | ANALISIS_IMAGEN | Ambos | 20 | 10 | 5 | Vista de correspondencia (no migrar tablas) | En P3 se llama ANALISIS_IMAGEN (V008): misma idea, foto analizada que NO es un depósito. |
| CONTENEDOR | CONTENEDOR | Ambos | 8 | 6 | 6 | Vista de correspondencia (no migrar tablas) | Misma caneca; P3 agrega configuración de tres canecas en CONFIGURACION_CANECA (V021). |
| CONTENEDOR_MATERIAL | CONTENEDOR_MATERIAL | Ambos | 3 | 2 | 2 | Vista de correspondencia (no migrar tablas) | Igual: qué materiales admite cada caneca. |
| — | CONVERSACION_TELEGRAM | Solo P3 | 0 | 8 | 0 | No se copia a la base académica | Solo P3: estado de la conversación del bot. |
| CREDENCIAL_ACCESO | CREDENCIAL_ACCESO | Ambos | 7 | 7 | 7 | Vista de correspondencia (no migrar tablas) | Igual idea: credencial con hash, nunca la contraseña. |
| DEPOSITO | DEPOSITO | Ambos | 18 | 11 | 11 | Vista de correspondencia (no migrar tablas) | Mismo depósito declarado y verificado; P3 añadió cantidad declarada (V009) y depósito sin sensor (V016). |
| EVENTO_DISPOSITIVO | EVENTO_DISPOSITIVO | Ambos | 8 | 8 | 8 | Vista de correspondencia (no migrar tablas) | Mismo evento del ESP32 / dispositivo. |
| HISTORIAL_ESTADO_CONTENEDOR | — | Solo académico | 8 | 0 | 0 | Ninguna en P3: es extensión académica | Solo académica: historial del estado de la caneca escrito por trigger. |
| INTERACCION | INTERACCION | Ambos | 15 | 13 | 13 | Vista de correspondencia (no migrar tablas) | Misma conversación/solicitud del ciudadano. |
| — | LOTE_SIMULACION | Solo P3 | 0 | 7 | 0 | No se copia a la base académica |  |
| MATERIAL | MATERIAL | Ambos | 6 | 9 | 6 | Vista de correspondencia (no migrar tablas) | Mismo catálogo conceptual; P3 añadió orientación y tarifas por unidad (V011, V012). |
| MOVIMIENTO_ECOPUNTOS | MOVIMIENTO_ECOPUNTOS | Ambos | 11 | 9 | 6 | Vista de correspondencia (no migrar tablas) | Mismo libro de puntos (créditos y reversos). |
| — | OPERACION_ECOPUNTOS | Solo P3 | 0 | 9 | 0 | No se copia a la base académica | Solo P3: agrupa operaciones de puntos para idempotencia de la aplicación. |
| — | OPINION | Solo P3 | 0 | 6 | 0 | No se copia a la base académica | Solo P3: opiniones (V010). |
| ORDEN_APERTURA | ORDEN_APERTURA | Ambos | 8 | 8 | 8 | Vista de correspondencia (no migrar tablas) | Misma orden de apertura con idempotencia. |
| PESO_REFERENCIAL | — | Solo académico | 9 | 0 | 0 | Ninguna en P3: es extensión académica | Solo académica: catálogo de pesos de referencia para estimar kg sin báscula. |
| PUNTO_INSTALACION | PUNTO_INSTALACION | Ambos | 6 | 4 | 4 | Vista de correspondencia (no migrar tablas) | Misma estación física con QR. |
| — | REGISTRO_SIMULACION | Solo P3 | 0 | 4 | 0 | No se copia a la base académica |  |
| REGLA_ECOPUNTOS | REGLA_ECOPUNTOS | Ambos | 10 | 9 | 9 | Vista de correspondencia (no migrar tablas) | Misma tarifa versionada. |
| ROL | ROL | Ambos | 3 | 3 | 3 | Vista de correspondencia (no migrar tablas) | Igual: CIUDADANO, OPERARIO, ADMINISTRADOR. |
| — | TOKEN_VINCULACION | Solo P3 | 0 | 7 | 0 | No se copia a la base académica | Solo P3: códigos temporales para vincular Telegram. |
| — | UPDATE_TELEGRAM | Solo P3 | 0 | 2 | 0 | No se copia a la base académica | Solo P3: deduplicación de updates de Telegram. |
| USUARIO | USUARIO | Ambos | 8 | 7 | 7 | Vista de correspondencia (no migrar tablas) | Misma idea (persona registrada). En P3 el ciudadano de Telegram puede ser invitado; en la base académica los ciudadanos son seudónimos sintéticos. |
| USUARIO_ROL | USUARIO_ROL | Ambos | 3 | 3 | 3 | Vista de correspondencia (no migrar tablas) | Igual: relación N:M usuario–rol. |
| — | VINCULACION_PENDIENTE | Solo P3 | 0 | 7 | 0 | No se copia a la base académica | Solo P3: vinculación de cuenta existente (V007). |
| VINCULO_TELEGRAM | VINCULO_TELEGRAM | Ambos | 5 | 5 | 5 | Vista de correspondencia (no migrar tablas) | Mismo vínculo cuenta ↔ chat de Telegram. |

Las columnas exactas que difieren están en `MATRIZ_P3_vs_ACADEMICO.csv` (columnas Solo_academico y Solo_P3). Las columnas de P3 se leyeron de CREATE TABLE y ALTER TABLE … ADD; si una migración cambia columnas con otra sintaxis, la matriz puede quedarse corta y debe confirmarse con la consulta de solo lectura de Oracle Cloud.

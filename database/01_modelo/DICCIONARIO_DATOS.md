# Diccionario de datos — EcoAccess P3 (versión 001)

Fuente: `database/02_tablas/01_crear_tablas_iniciales.sql`. Las longitudes y valores coinciden con las constantes y enumeraciones de `src/EcoAccess.P3.Entidades`. Si cambian aquí, deben cambiar allá (y al revés) mediante una migración en `06_migraciones/`.


## Convenciones
- Textos en `VARCHAR2(n CHAR)`: la longitud se cuenta en caracteres (AL32UTF8).
- Verdadero/falso en `NUMBER(1)` con `CHECK (0, 1)`.
- Fechas de eventos en `TIMESTAMP` con hora UTC; el sufijo `_utc` lo recuerda.
- Enumeraciones guardadas como número con los mismos valores del `enum` C#.
- Sin `ON DELETE CASCADE`: el historial no se borra en cadena.
- Nombres: `pk_`, `fk_`, `uq_`, `ck_`, `ix_` + tabla abreviada.
- Textos obligatorios: `CHECK (TRIM(x) IS NOT NULL AND x = TRIM(x))`. La entidad ya guarda el texto recortado; la base rechaza una fila escrita sin pasar por ella. `TRIM` de Oracle quita solo espacios, mientras que `string.Trim()` de C# quita cualquier espacio en blanco (tabuladores, saltos de línea): la entidad es la más estricta.
- Minúsculas: la entidad usa `ToLowerInvariant` y la base comprueba `x = LOWER(x)`. Coinciden en el alfabeto latino, incluidas las tildes y la ñ; para letras raras (por ejemplo, la «İ» turca) pueden diferir, y en ese caso Oracle rechaza la fila en vez de aceptar un dato distinto.

## ROL — catálogo de roles
| Columna | Tipo | Nulo | Regla | Entidad C# |
|---|---|---|---|---|
| id_rol | NUMBER(10) identity (by default on null) | No | PK | `Rol.Id` |
| codigo | NUMBER(2) | No | UNIQUE; `CHECK IN (1,2,3)` | `CodigoRol`: 1 Ciudadano, 2 Operario, 3 Administrador |
| descripcion | VARCHAR2(200 CHAR) | No | `ck_rol_descripcion`: no vacía y sin espacios sobrantes | `Rol.LongitudMaximaDescripcion = 200` |

## USUARIO — cuenta de una persona (RN-01)
Sin contraseñas ni credenciales: irán en otra tabla.

| Columna | Tipo | Nulo | Regla | Entidad C# |
|---|---|---|---|---|
| id_usuario | NUMBER(10) identity (always) | No | PK | `Usuario.Id` |
| nombre | VARCHAR2(100 CHAR) | No | `TRIM(nombre)` no nulo, sin espacios al inicio ni al final, `LENGTH >= 2` | mín. 2, máx. 100 |
| correo_electronico | VARCHAR2(254 CHAR) | No | UNIQUE; debe estar en minúsculas | máx. 254, se normaliza a minúsculas |
| fecha_registro_utc | TIMESTAMP | No | UTC | `TimeProvider` |
| activo | NUMBER(1) | No | default 1; `CHECK (0,1)` | `Usuario.Activo` |
| version_consentimiento | VARCHAR2(20 CHAR) | Sí | va junto con la fecha | máx. 20 |
| fecha_consentimiento_utc | TIMESTAMP | Sí | va junto con la versión | `RegistrarConsentimiento` |

`ck_usuario_consentimiento`: versión y fecha son ambas nulas o ambas no nulas (misma invariante que la entidad).

## USUARIO_ROL — roles de cada usuario (N:M)
| Columna | Tipo | Nulo | Regla |
|---|---|---|---|
| id_usuario | NUMBER(10) | No | PK compuesta; FK → USUARIO |
| id_rol | NUMBER(10) | No | PK compuesta; FK → ROL; índice `ix_usuario_rol_rol` |
| fecha_asignacion_utc | TIMESTAMP | No | default `SYS_EXTRACT_UTC(SYSTIMESTAMP)`; metadato técnico |

`fecha_asignacion_utc` es un metadato técnico: la entidad `Usuario` no la expone y el repositorio no la lee. Quién asignó el rol y cuándo se registrará en la auditoría.

«Al menos un rol por usuario» no cabe en una restricción declarativa: lo garantizan la entidad `Usuario` y la transacción del repositorio.

## MATERIAL — catálogo validado
| Columna | Tipo | Nulo | Regla | Entidad C# |
|---|---|---|---|---|
| id_material | NUMBER(10) identity (by default on null) | No | PK | `Material.Id` |
| codigo | VARCHAR2(30 CHAR) | No | UNIQUE; mín. 2; empieza por A-Z; solo A-Z, 0-9, `_` | mín. 2, máx. 30 |
| nombre | VARCHAR2(100 CHAR) | No | `TRIM(nombre)` no nulo, sin espacios al inicio ni al final, `LENGTH >= 2` | mín. 2, máx. 100 |
| categoria | NUMBER(1) | No | `CHECK IN (1,2,3,4)` | `CategoriaMaterial`: 1 Aprovechable, 2 OrganicoAprovechable, 3 NoAprovechable, 4 Peligroso |
| descripcion | VARCHAR2(500 CHAR) | Sí | — | máx. 500 |
| habilitado | NUMBER(1) | No | default 1; `CHECK (0,1)` | `Material.Habilitado` |

El formato del código se comprueba carácter por carácter: `INSTR('ABC…Z', SUBSTR(codigo, 1, 1)) > 0` para la primera letra y `TRANSLATE` para el resto. No se usa `BETWEEN 'A' AND 'Z'` ni `REGEXP_LIKE('[A-Z]')` porque su resultado depende de `NLS_SORT` y podrían aceptar «Ñ» o «Á».

## PUNTO_INSTALACION — lugar físico de las canecas
| Columna | Tipo | Nulo | Regla | Entidad C# |
|---|---|---|---|---|
| id_punto_instalacion | NUMBER(10) identity (by default on null) | No | PK | `PuntoInstalacion.Id` |
| nombre | VARCHAR2(100 CHAR) | No | UNIQUE; `TRIM(nombre)` no nulo, sin espacios al inicio ni al final, `LENGTH >= 2` | mín. 2, máx. 100 |
| referencia_ubicacion | VARCHAR2(200 CHAR) | Sí | — | máx. 200 |
| activo | NUMBER(1) | No | default 1; `CHECK (0,1)` | `PuntoInstalacion.Activo` |

El sector geográfico se agregará con una migración.

## CONTENEDOR — caneca física y su QR público
| Columna | Tipo | Nulo | Regla | Entidad C# |
|---|---|---|---|---|
| id_contenedor | NUMBER(10) identity (by default on null) | No | PK | `Contenedor.Id` |
| id_punto_instalacion | NUMBER(10) | No | FK → PUNTO_INSTALACION; índice `ix_contenedor_punto` | — |
| nombre | VARCHAR2(100 CHAR) | No | `TRIM(nombre)` no nulo, sin espacios al inicio ni al final, `LENGTH >= 2` | mín. 2, máx. 100 |
| identificador_qr | VARCHAR2(64 CHAR) | No | UNIQUE; 16 a 64 caracteres de `A-Z a-z 0-9 _ -` | mín. 16, máx. 64 |
| fecha_alta_utc | TIMESTAMP | No | UTC | `TimeProvider` |
| estado | NUMBER(1) | No | default 2; `CHECK IN (1,2,3)` | `EstadoContenedor`: 1 EnServicio, 2 FueraDeServicio, 3 DadoDeBaja |

El QR es público y no autoriza nada por sí solo; el servicio lo genera con `RandomNumberGenerator` (16 bytes en base64url, 22 caracteres).

## CONTENEDOR_MATERIAL — materiales que admite cada caneca (N:M)
| Columna | Tipo | Nulo | Regla |
|---|---|---|---|
| id_contenedor | NUMBER(10) | No | PK compuesta; FK → CONTENEDOR |
| id_material | NUMBER(10) | No | PK compuesta; FK → MATERIAL; índice `ix_contenedor_material_mat` |

Que exista la fila significa «admitido». «Una caneca en servicio admite al menos un material» y «el material debe estar habilitado» los garantiza la capa de lógica.

## CREDENCIAL_ACCESO — secreto de inicio de sesión (migración V002)
| Columna | Tipo | Nulo | Restricción | Descripción |
|---|---|---|---|---|
| `id_usuario` | NUMBER(10) | no | `pk_credencial`, `fk_credencial_usuario` → `USUARIO` | una credencial por cuenta, sin borrado en cascada |
| `hash_contrasena` | RAW(32) | no | `ck_credencial_hash`: 32 bytes | PBKDF2-SHA256; nunca la contraseña |
| `sal` | RAW(16) | no | `ck_credencial_sal`: 16 bytes | aleatoria por credencial |
| `iteraciones` | NUMBER(7) | no | `ck_credencial_iter` ≥ 100000 | se guarda para poder subirla sin invalidar hashes viejos |
| `intentos_fallidos` | NUMBER(3) | no | `ck_credencial_intentos` ≥ 0, valor por defecto 0 | se reinicia al acertar o al bloquear |
| `bloqueada_hasta_utc` | TIMESTAMP | sí | — | fin del bloqueo temporal; nulo si no hay bloqueo |
| `fecha_cambio_utc` | TIMESTAMP | no | — | último cambio de contraseña |

Los CHECK de longitud usan `LENGTH(RAWTOHEX(x))`, porque en Oracle `LENGTH` de un RAW cuenta cifras hexadecimales, no bytes.

## INTERACCION — visita a una caneca (migración V003)
| Columna | Tipo | Nulo | Restricción | Descripción |
|---|---|---|---|---|
| `id_interaccion` | NUMBER(10) | no | `pk_interaccion`, identidad | — |
| `id_contenedor` | NUMBER(10) | no | `fk_interaccion_contenedor`; `uq_interaccion_contenedor` (id, caneca) para la FK compuesta de la orden | caneca visitada |
| `id_usuario` | NUMBER(10) | sí | `fk_interaccion_usuario` | ciudadano; nulo para visitante o apertura manual |
| `canal` | NUMBER(1) | no | `ck_interaccion_canal`: 1 Telegram, 2 Manual, 3 Simulador | — |
| `estado` | NUMBER(1) | no | `ck_interaccion_estado`: 1 Iniciada, 2 Autorizada, 3 ConDeposito, 4 SinDeposito, 5 Rechazada | 3 a 5 son cerradas |
| `id_material_sugerido` / `confianza_sugerida` | NUMBER(10) / NUMBER(4,3) | sí | `ck_interaccion_sugerencia`: ambas o ninguna; confianza entre 0 y 1 | sugerencia de la IA; solo orienta |
| `id_material_confirmado` | NUMBER(10) | sí | `ck_interaccion_confirmado`: obligatorio en 2 y 3, y en 4 salvo canal Manual | material que se autorizó (RN-06) |
| `id_operario` / `motivo` | NUMBER(10) / VARCHAR2(300) | sí | `ck_interaccion_manual`: el canal Manual nace en 4 con operario y motivo, sin materiales; `ck_interaccion_rechazo`: 5 exige motivo | CU-17 |
| `fecha_inicio_utc` / `fecha_cierre_utc` | TIMESTAMP | no / sí | `ck_interaccion_cierre`: cierre nulo en 1 y 2, obligatorio y posterior al inicio en 3 a 5 | — |

## ORDEN_APERTURA — autorización de abrir (V003, RN-07, RN-16)
| Columna | Tipo | Nulo | Restricción | Descripción |
|---|---|---|---|---|
| `id_orden` | NUMBER(10) | no | `pk_orden_apertura`, identidad | — |
| `id_interaccion`, `id_contenedor` | NUMBER(10) | no | `fk_orden_interaccion` compuesta → `INTERACCION`; `uq_orden_interaccion` | una orden por interacción, siempre de su misma caneca |
| `clave_idempotencia` | VARCHAR2(64) | no | `uq_orden_clave`, `ck_orden_clave` | repetir la solicitud devuelve la misma orden |
| `estado` | NUMBER(1) | no | `ck_orden_estado`: 1 Emitida, 2 Enviada, 3 Acusada, 4 Abierta, 5 Detectada, 6 Cerrada, 7 Vencida, 8 Fallida | 1 a 5 vigentes |
| `fecha_emision_utc` / `fecha_vencimiento_utc` | TIMESTAMP | no | `ck_orden_vencimiento`: vence después de emitirse | ventana del depósito |
| `fecha_cierre_utc` | TIMESTAMP | sí | `ck_orden_cierre`: nula si vigente, obligatoria si terminal | — |

`uq_orden_vigente` es un índice único por función (`CASE WHEN estado BETWEEN 1 AND 5 THEN id_contenedor END`): dos órdenes vigentes en la misma caneca chocan con ORA-00001 aunque lleguen a la vez.

## EVENTO_DISPOSITIVO — lo que informa la caneca (V003, RN-09, RN-20)
| Columna | Tipo | Nulo | Restricción | Descripción |
|---|---|---|---|---|
| `id_evento` | NUMBER(10) | no | `pk_evento_dispositivo`, identidad | — |
| `id_contenedor`, `id_externo` | NUMBER(10), VARCHAR2(64) | no | `uq_evento_externo` (caneca, id externo) | una retransmisión no se guarda dos veces |
| `tipo` | NUMBER(1) | no | `ck_evento_tipo`: 1 Acuse, 2 Apertura, 3 Deteccion, 4 Cierre, 5 Fallo | — |
| `id_orden` | NUMBER(10) | sí | `fk_evento_orden` compuesta (orden, caneca) | nula si no había orden vigente |
| `resultado` | NUMBER(1) | no | `ck_evento_resultado`: 1 Aplicado, 2 Revision; `ck_evento_aplicado`: Aplicado exige orden | tardío, repetido o sin orden → Revision |
| `fecha_dispositivo_utc` / `fecha_recepcion_utc` | TIMESTAMP | sí / no | — | cuenta la hora del servidor |

## DEPOSITO — depósito detectado por el sensor (V003, RN-08, RN-12, RN-17)
| Columna | Tipo | Nulo | Restricción | Descripción |
|---|---|---|---|---|
| `id_deposito` | NUMBER(10) | no | `pk_deposito`, identidad | — |
| `id_orden`, `id_evento` | NUMBER(10) | no | `uq_deposito_orden`, `uq_deposito_evento`, `fk_deposito_evento` compuesta → (evento, orden) | uno por orden y, por tanto, por interacción |
| `id_material_declarado` | NUMBER(10) | no | `fk_deposito_mat_declarado` | el confirmado por la persona; no verificado |
| `id_material_verificado`, `id_verificador`, `fecha_verificacion_utc` | NUMBER(10), NUMBER(10), TIMESTAMP | sí | `ck_deposito_verificacion`: las tres o ninguna; verificación posterior a la detección | lo comprueba un operario (RN-11) |
| `peso_medido_kg` / `peso_estimado_kg` | NUMBER(6,3) | sí | mayores que 0 | medido y estimado nunca se mezclan (RN-12) |
| `fecha_deteccion_utc` | TIMESTAMP | no | — | — |

## Reglas que la base no puede garantizar sola
| Regla | Responsable |
|---|---|
| Todo usuario tiene al menos un rol | entidad `Usuario` + transacción del repositorio |
| Caneca en servicio con al menos un material admitido | entidad `Contenedor` + servicio de contenedores |
| Solo se admiten materiales habilitados (RN-06) | servicio de contenedores |
| El punto de instalación de una caneca en servicio está activo | servicio de contenedores |
| Transiciones de estado de la caneca: `DadoDeBaja` es definitivo; solo se pasa a `EnServicio` con un material admitido habilitado | entidad `Contenedor` + servicio de contenedores |
| No desactivar un punto con canecas en servicio, ni deshabilitar el único material habilitado de una caneca en servicio | servicios de puntos y de materiales |
| Transiciones de la interacción y de la orden; la ventana de tiempo del depósito (RN-08) | entidades `Interaccion`, `OrdenApertura` + servicio de interacciones |
| La orden solo se emite si la caneca está en servicio y admite el material habilitado (RN-05, RN-06) | servicio de interacciones + repositorio, con bloqueo |
| Unicidad sin distinguir mayúsculas en los nombres de punto | no se aplica: el UNIQUE y el servicio distinguen mayúsculas |

## Columnas pendientes (se agregarán con migraciones `V002__` en adelante)
| Tabla | Columna o tabla prevista | Motivo |
|---|---|---|
| PUNTO_INSTALACION | sector geográfico | filtros y estadísticas por zona |
| — | historial de consentimientos | RN-02: la tabla `USUARIO` guarda solo el vigente |
| — | auditoría (quién, qué, cuándo) | incluye asignación de roles |
| — | evidencias (foto) y ecopuntos | hitos siguientes; los depósitos ya existen (V003) y separan declarado, verificado, medido y estimado |

## Códigos de error propios de los scripts
| Código | Script | Significado |
|---|---|---|
| -20010 | `02_tablas/01_crear_tablas_iniciales.sql` | las 7 tablas ya existen: no se hace nada |
| -20011 | `02_tablas/01_crear_tablas_iniciales.sql` | instalación parcial (1 a 6 tablas); el mensaje las lista y remite al script de recuperación |
| -20012 | `02_tablas/00_recuperar_instalacion_parcial.sql` | no hay tablas: nada que recuperar |
| -20013 | `02_tablas/00_recuperar_instalacion_parcial.sql` | instalación completa: no se borra nada |
| -20014 | `02_tablas/00_recuperar_instalacion_parcial.sql` | hay datos fuera de ROL y MATERIAL: se niega a borrar |
| -20020 | `05_pruebas/01_pruebas_restricciones.sql` | al menos un caso de restricción falló (tras `ROLLBACK`) |
| -20021 | `05_pruebas/02_pruebas_credencial.sql` | al menos un caso de `CREDENCIAL_ACCESO` falló (tras `ROLLBACK`) |
| -20040 | `06_migraciones/V002__credencial_acceso.sql` | V002 ya aplicada: no se hace nada |
| -20041 | `06_migraciones/V002__credencial_acceso.sql` | falta `USUARIO`: ejecutar antes la versión 001 |
| -20030 | `03_datos_prueba/02_datos_demo.sql` | faltan materiales base (PET, CARTON, PAPEL, LATA_ALUMINIO): ejecutar antes `01_catalogos.sql` |
| -20022 | `05_pruebas/03_pruebas_interacciones.sql` | faltan materiales: ejecutar antes los catálogos |
| -20023 | `05_pruebas/03_pruebas_interacciones.sql` | al menos un caso de interacciones falló (tras `ROLLBACK`) |
| -20050 | `06_migraciones/V003__interacciones_depositos.sql` | V003 ya aplicada: no se hace nada |
| -20051 | `06_migraciones/V003__interacciones_depositos.sql` | faltan `USUARIO`, `CONTENEDOR` o `MATERIAL` |
| -20052 | `06_migraciones/V003__interacciones_depositos.sql` | V003 aplicada a medias: revertir según la cabecera |


## V005 — Ecopuntos

`regla_ecopuntos`: id_regla (versión), id_material, puntos enteros positivos, descripcion, desde_utc, hasta_utc opcional, id_creador, creada_utc del servidor.

`operacion_ecopuntos`: id_operacion, clave única (80), tipo (1 crédito, 2 anulación, 3 corrección), id_deposito, id_actor, id_credito objetivo opcional, id_regla nueva opcional, motivo (300), fecha_utc. La combinación de tipo y referencias está restringida.

`movimiento_ecopuntos`: id_movimiento, id_operacion, id_deposito, id_usuario, id_regla, tipo (1 crédito positivo, 2 reverso negativo), puntos, id_credito_revertido opcional y único, id_predecesor opcional y único. FK compuestas preservan titular y depósito entre movimientos relacionados. Un índice condicional restringe el crédito inicial por depósito. El actor, fecha y motivo se obtienen de la operación. No se ofrecen cambios ni borrados desde la API.

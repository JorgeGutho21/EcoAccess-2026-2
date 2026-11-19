# Inventario del esquema ECOACCESS_P3_APP

Generado por `database/scripts/Exportar-EsquemaOracle.ps1` el 2026-11-18 desde Oracle 19.0.0.0.0, con las vistas `USER_OBJECTS`, `USER_DEPENDENCIES`, `USER_CONSTRAINTS`, `USER_SEQUENCES` y `USER_ERRORS`. No editar a mano.

| Tipo | Cantidad | Inválidos |
|---|---:|---:|
| FUNCTION | 3 | 0 |
| INDEX | 98 | 0 |
| PACKAGE | 5 | 0 |
| PACKAGE BODY | 5 | 0 |
| PROCEDURE | 2 | 0 |
| SEQUENCE | 19 | 0 |
| TABLE | 33 | 0 |
| TRIGGER | 5 | 0 |
| VIEW | 21 | 0 |

Errores de compilación pendientes (`USER_ERRORS`): **0**. Sinónimos: los que haya no son parte de EcoAccess (el esquema no usa sinónimos).

## Objetos

| Objeto | Tipo | Estado | Dependencias (mismo esquema) | Archivo de origen | Documentación |
|---|---|---|---|---|---|
| `FN_DEPOSITOS_CIUDADANO` | FUNCTION | VALID | USUARIO, VW_DEPOSITO_DETALLE | [functions/fn_depositos_ciudadano.sql](functions/fn_depositos_ciudadano.sql) | [ver](../../docs/database/09-funciones.md) |
| `FN_PESO_ESTIMADO_KG` | FUNCTION | VALID | PESO_REFERENCIAL | [functions/fn_peso_estimado_kg.sql](functions/fn_peso_estimado_kg.sql) | [ver](../../docs/database/09-funciones.md) |
| `FN_RESOLVER_CANECA_DISPONIBLE` | FUNCTION | VALID | CONFIGURACION_CANECA, CONTENEDOR, CONTENEDOR_MATERIAL, MATERIAL, PUNTO_INSTALACION | [functions/fn_resolver_caneca_disponible.sql](functions/fn_resolver_caneca_disponible.sql) | [ver](../../docs/database/09-funciones.md) |
| `PKG_CONTEXTO_APP` | PACKAGE | VALID | ROL, USUARIO | [packages/pkg_contexto_app.pks.sql](packages/pkg_contexto_app.pks.sql) | [ver](../../docs/database/07-paquetes.md) |
| `PKG_DEPOSITO` | PACKAGE | VALID | DEPOSITO, MATERIAL, USUARIO | [packages/pkg_deposito.pks.sql](packages/pkg_deposito.pks.sql) | [ver](../../docs/database/07-paquetes.md) |
| `PKG_ECOACCESS_STATS` | PACKAGE | VALID | — | [packages/pkg_ecoaccess_stats.pks.sql](packages/pkg_ecoaccess_stats.pks.sql) | [ver](../../docs/database/07-paquetes.md) |
| `PKG_ECOPUNTOS` | PACKAGE | VALID | DEPOSITO, MATERIAL, MOVIMIENTO_ECOPUNTOS, REGLA_ECOPUNTOS, USUARIO | [packages/pkg_ecopuntos.pks.sql](packages/pkg_ecopuntos.pks.sql) | [ver](../../docs/database/07-paquetes.md) |
| `PKG_ESTADISTICAS` | PACKAGE | VALID | — | [packages/pkg_estadisticas.pks.sql](packages/pkg_estadisticas.pks.sql) | [ver](../../docs/database/07-paquetes.md) |
| `PKG_CONTEXTO_APP` | PACKAGE BODY | VALID | ROL, USUARIO, USUARIO_ROL | [packages/pkg_contexto_app.pkb.sql](packages/pkg_contexto_app.pkb.sql) | [ver](../../docs/database/07-paquetes.md) |
| `PKG_DEPOSITO` | PACKAGE BODY | VALID | AUDITORIA_DEPOSITO, DEPOSITO, INTERACCION, MATERIAL, MOVIMIENTO_ECOPUNTOS, ORDEN_APERTURA, PKG_CONTEXTO_APP, PKG_ECOPUNTOS, USUARIO | [packages/pkg_deposito.pkb.sql](packages/pkg_deposito.pkb.sql) | [ver](../../docs/database/07-paquetes.md) |
| `PKG_ECOACCESS_STATS` | PACKAGE BODY | VALID | CONTENEDOR, DEPOSITO, INTERACCION, MATERIAL, ORDEN_APERTURA | [packages/pkg_ecoaccess_stats.pkb.sql](packages/pkg_ecoaccess_stats.pkb.sql) | [ver](../../docs/database/07-paquetes.md) |
| `PKG_ECOPUNTOS` | PACKAGE BODY | VALID | DEPOSITO, INTERACCION, MATERIAL, MOVIMIENTO_ECOPUNTOS, OPERACION_ECOPUNTOS, ORDEN_APERTURA, PKG_CONTEXTO_APP, REGLA_ECOPUNTOS, USUARIO | [packages/pkg_ecopuntos.pkb.sql](packages/pkg_ecopuntos.pkb.sql) | [ver](../../docs/database/07-paquetes.md) |
| `PKG_ESTADISTICAS` | PACKAGE BODY | VALID | CONFIGURACION_CANECA, CONTENEDOR, DEPOSITO, INTERACCION, ORDEN_APERTURA, USUARIO | [packages/pkg_estadisticas.pkb.sql](packages/pkg_estadisticas.pkb.sql) | [ver](../../docs/database/07-paquetes.md) |
| `PR_CAMBIAR_ESTADO_CONTENEDOR` | PROCEDURE | VALID | CONTENEDOR, PKG_CONTEXTO_APP, USUARIO | [procedures/pr_cambiar_estado_contenedor.sql](procedures/pr_cambiar_estado_contenedor.sql) | [ver](../../docs/database/08-procedimientos.md) |
| `PR_CONSULTAR_ESTADISTICAS` | PROCEDURE | VALID | PKG_ECOACCESS_STATS | [procedures/pr_consultar_estadisticas.sql](procedures/pr_consultar_estadisticas.sql) | [ver](../../docs/database/08-procedimientos.md) |
| `ANALISIS_IMAGEN` | TABLE | VALID | FK → INTERACCION, MATERIAL | [tables/analisis_imagen.sql](tables/analisis_imagen.sql) | [ver](../../docs/database/05-tablas-relaciones.md) |
| `AUDITORIA_DEPOSITO` | TABLE | VALID | FK → DEPOSITO, USUARIO | [tables/auditoria_deposito.sql](tables/auditoria_deposito.sql) | [ver](../../docs/database/05-tablas-relaciones.md) |
| `COMENTARIO_CIUDADANO` | TABLE | VALID | FK → INTERACCION, USUARIO | [tables/comentario_ciudadano.sql](tables/comentario_ciudadano.sql) | [ver](../../docs/database/05-tablas-relaciones.md) |
| `CONFIGURACION_CANECA` | TABLE | VALID | FK → CONTENEDOR | [tables/configuracion_caneca.sql](tables/configuracion_caneca.sql) | [ver](../../docs/database/05-tablas-relaciones.md) |
| `CONSENTIMIENTO_VISION_EXTERNA` | TABLE | VALID | — | [tables/consentimiento_vision_externa.sql](tables/consentimiento_vision_externa.sql) | [ver](../../docs/database/05-tablas-relaciones.md) |
| `CONSULTA_VISION` | TABLE | VALID | FK → CONTENEDOR, MATERIAL, USUARIO | [tables/consulta_vision.sql](tables/consulta_vision.sql) | [ver](../../docs/database/05-tablas-relaciones.md) |
| `CONTENEDOR` | TABLE | VALID | FK → PUNTO_INSTALACION | [tables/contenedor.sql](tables/contenedor.sql) | [ver](../../docs/database/05-tablas-relaciones.md) |
| `CONTENEDOR_MATERIAL` | TABLE | VALID | FK → CONTENEDOR, MATERIAL | [tables/contenedor_material.sql](tables/contenedor_material.sql) | [ver](../../docs/database/05-tablas-relaciones.md) |
| `CONVERSACION_TELEGRAM` | TABLE | VALID | FK → CONTENEDOR, INTERACCION, MATERIAL | [tables/conversacion_telegram.sql](tables/conversacion_telegram.sql) | [ver](../../docs/database/05-tablas-relaciones.md) |
| `CREDENCIAL_ACCESO` | TABLE | VALID | FK → USUARIO | [tables/credencial_acceso.sql](tables/credencial_acceso.sql) | [ver](../../docs/database/05-tablas-relaciones.md) |
| `DEPOSITO` | TABLE | VALID | FK → EVENTO_DISPOSITIVO, MATERIAL, USUARIO | [tables/deposito.sql](tables/deposito.sql) | [ver](../../docs/database/05-tablas-relaciones.md) |
| `ECO_MIGRACION_APLICADA` | TABLE | VALID | — | [tables/eco_migracion_aplicada.sql](tables/eco_migracion_aplicada.sql) | [ver](../../docs/database/05-tablas-relaciones.md) |
| `EVENTO_DISPOSITIVO` | TABLE | VALID | FK → CONTENEDOR, ORDEN_APERTURA | [tables/evento_dispositivo.sql](tables/evento_dispositivo.sql) | [ver](../../docs/database/05-tablas-relaciones.md) |
| `HISTORIAL_ESTADO_CONTENEDOR` | TABLE | VALID | FK → CONTENEDOR, USUARIO | [tables/historial_estado_contenedor.sql](tables/historial_estado_contenedor.sql) | [ver](../../docs/database/05-tablas-relaciones.md) |
| `INTERACCION` | TABLE | VALID | FK → CONTENEDOR, MATERIAL, USUARIO | [tables/interaccion.sql](tables/interaccion.sql) | [ver](../../docs/database/05-tablas-relaciones.md) |
| `LOTE_SIMULACION` | TABLE | VALID | — | [tables/lote_simulacion.sql](tables/lote_simulacion.sql) | [ver](../../docs/database/05-tablas-relaciones.md) |
| `MATERIAL` | TABLE | VALID | — | [tables/material.sql](tables/material.sql) | [ver](../../docs/database/05-tablas-relaciones.md) |
| `MOVIMIENTO_ECOPUNTOS` | TABLE | VALID | FK → DEPOSITO, OPERACION_ECOPUNTOS, REGLA_ECOPUNTOS, USUARIO | [tables/movimiento_ecopuntos.sql](tables/movimiento_ecopuntos.sql) | [ver](../../docs/database/05-tablas-relaciones.md) |
| `OPERACION_ECOPUNTOS` | TABLE | VALID | FK → DEPOSITO, MOVIMIENTO_ECOPUNTOS, REGLA_ECOPUNTOS, USUARIO | [tables/operacion_ecopuntos.sql](tables/operacion_ecopuntos.sql) | [ver](../../docs/database/05-tablas-relaciones.md) |
| `OPINION` | TABLE | VALID | FK → USUARIO | [tables/opinion.sql](tables/opinion.sql) | [ver](../../docs/database/05-tablas-relaciones.md) |
| `ORDEN_APERTURA` | TABLE | VALID | FK → INTERACCION | [tables/orden_apertura.sql](tables/orden_apertura.sql) | [ver](../../docs/database/05-tablas-relaciones.md) |
| `PESO_REFERENCIAL` | TABLE | VALID | FK → MATERIAL | [tables/peso_referencial.sql](tables/peso_referencial.sql) | [ver](../../docs/database/05-tablas-relaciones.md) |
| `PUNTO_DISPOSICION` | TABLE | VALID | — | [tables/punto_disposicion.sql](tables/punto_disposicion.sql) | [ver](../../docs/database/05-tablas-relaciones.md) |
| `PUNTO_INSTALACION` | TABLE | VALID | — | [tables/punto_instalacion.sql](tables/punto_instalacion.sql) | [ver](../../docs/database/05-tablas-relaciones.md) |
| `REGISTRO_SIMULACION` | TABLE | VALID | FK → LOTE_SIMULACION | [tables/registro_simulacion.sql](tables/registro_simulacion.sql) | [ver](../../docs/database/05-tablas-relaciones.md) |
| `REGLA_ECOPUNTOS` | TABLE | VALID | FK → MATERIAL, USUARIO | [tables/regla_ecopuntos.sql](tables/regla_ecopuntos.sql) | [ver](../../docs/database/05-tablas-relaciones.md) |
| `ROL` | TABLE | VALID | — | [tables/rol.sql](tables/rol.sql) | [ver](../../docs/database/05-tablas-relaciones.md) |
| `TOKEN_VINCULACION` | TABLE | VALID | FK → USUARIO | [tables/token_vinculacion.sql](tables/token_vinculacion.sql) | [ver](../../docs/database/05-tablas-relaciones.md) |
| `UPDATE_TELEGRAM` | TABLE | VALID | — | [tables/update_telegram.sql](tables/update_telegram.sql) | [ver](../../docs/database/05-tablas-relaciones.md) |
| `USUARIO` | TABLE | VALID | — | [tables/usuario.sql](tables/usuario.sql) | [ver](../../docs/database/05-tablas-relaciones.md) |
| `USUARIO_ROL` | TABLE | VALID | FK → ROL, USUARIO | [tables/usuario_rol.sql](tables/usuario_rol.sql) | [ver](../../docs/database/05-tablas-relaciones.md) |
| `VINCULACION_PENDIENTE` | TABLE | VALID | FK → TOKEN_VINCULACION, USUARIO | [tables/vinculacion_pendiente.sql](tables/vinculacion_pendiente.sql) | [ver](../../docs/database/05-tablas-relaciones.md) |
| `VINCULO_TELEGRAM` | TABLE | VALID | FK → USUARIO | [tables/vinculo_telegram.sql](tables/vinculo_telegram.sql) | [ver](../../docs/database/05-tablas-relaciones.md) |
| `TR_ECOP_UNIDADES` | TRIGGER | VALID | DEPOSITO, MOVIMIENTO_ECOPUNTOS, REGLA_ECOPUNTOS | [triggers/tr_ecop_unidades.sql](triggers/tr_ecop_unidades.sql) | [ver](../../docs/database/10-triggers-secuencias.md) |
| `TRG_CONTENEDOR_AUD_ESTADO` | TRIGGER | VALID | CONTENEDOR, HISTORIAL_ESTADO_CONTENEDOR, PKG_CONTEXTO_APP | [triggers/trg_contenedor_aud_estado.sql](triggers/trg_contenedor_aud_estado.sql) | [ver](../../docs/database/10-triggers-secuencias.md) |
| `TRG_DEPOSITO_AUD_VERIFICACION` | TRIGGER | VALID | AUDITORIA_DEPOSITO, DEPOSITO, PKG_CONTEXTO_APP | [triggers/trg_deposito_aud_verificacion.sql](triggers/trg_deposito_aud_verificacion.sql) | [ver](../../docs/database/10-triggers-secuencias.md) |
| `TRG_DEPOSITO_BLOQUEO_CERRADO` | TRIGGER | VALID | DEPOSITO, PKG_DEPOSITO | [triggers/trg_deposito_bloqueo_cerrado.sql](triggers/trg_deposito_bloqueo_cerrado.sql) | [ver](../../docs/database/10-triggers-secuencias.md) |
| `TRG_DEPOSITO_COHERENCIA` | TRIGGER | VALID | CONTENEDOR, CONTENEDOR_MATERIAL, DEPOSITO, INTERACCION, ORDEN_APERTURA, PKG_CONTEXTO_APP, TRG_DEPOSITO_BLOQUEO_CERRADO | [triggers/trg_deposito_coherencia.sql](triggers/trg_deposito_coherencia.sql) | [ver](../../docs/database/10-triggers-secuencias.md) |
| `EA_R_BASE_DEPOSITOS` | VIEW | VALID | CONTENEDOR, DEPOSITO, INTERACCION, MATERIAL, ORDEN_APERTURA, PUNTO_INSTALACION | [views/ea_r_base_depositos.sql](views/ea_r_base_depositos.sql) | [ver](../../docs/database/06-vistas.md) |
| `EA_R_BASE_PUNTOS` | VIEW | VALID | CONTENEDOR, DEPOSITO, INTERACCION, MATERIAL, MOVIMIENTO_ECOPUNTOS, OPERACION_ECOPUNTOS, ORDEN_APERTURA, PUNTO_INSTALACION | [views/ea_r_base_puntos.sql](views/ea_r_base_puntos.sql) | [ver](../../docs/database/06-vistas.md) |
| `EA_R_CONSULTAS_VISION` | VIEW | VALID | CONSULTA_VISION, MATERIAL | [views/ea_r_consultas_vision.sql](views/ea_r_consultas_vision.sql) | [ver](../../docs/database/06-vistas.md) |
| `EA_R_DEPOSITOS` | VIEW | VALID | EA_R_BASE_DEPOSITOS | [views/ea_r_depositos.sql](views/ea_r_depositos.sql) | [ver](../../docs/database/06-vistas.md) |
| `EA_R_MATERIALES` | VIEW | VALID | MATERIAL | [views/ea_r_materiales.sql](views/ea_r_materiales.sql) | [ver](../../docs/database/06-vistas.md) |
| `EA_R_OPINIONES` | VIEW | VALID | OPINION | [views/ea_r_opiniones.sql](views/ea_r_opiniones.sql) | [ver](../../docs/database/06-vistas.md) |
| `EA_R_PARTICIPACION_MENSUAL` | VIEW | VALID | CONTENEDOR, DEPOSITO, INTERACCION, MATERIAL, ORDEN_APERTURA, PUNTO_INSTALACION | [views/ea_r_participacion_mensual.sql](views/ea_r_participacion_mensual.sql) | [ver](../../docs/database/06-vistas.md) |
| `EA_R_PUNTOS` | VIEW | VALID | EA_R_BASE_PUNTOS | [views/ea_r_puntos.sql](views/ea_r_puntos.sql) | [ver](../../docs/database/06-vistas.md) |
| `EA_R_SOLICITUDES` | VIEW | VALID | CONTENEDOR, INTERACCION, MATERIAL, PUNTO_INSTALACION | [views/ea_r_solicitudes.sql](views/ea_r_solicitudes.sql) | [ver](../../docs/database/06-vistas.md) |
| `VISTA_CANECAS_ESTACION` | VIEW | VALID | CONFIGURACION_CANECA, CONTENEDOR, PUNTO_INSTALACION | [views/vista_canecas_estacion.sql](views/vista_canecas_estacion.sql) | [ver](../../docs/database/06-vistas.md) |
| `VISTA_DEPOSITOS_ESTADO` | VIEW | VALID | CONFIGURACION_CANECA, CONTENEDOR, DEPOSITO, INTERACCION, MATERIAL, ORDEN_APERTURA, PUNTO_INSTALACION, REGISTRO_SIMULACION | [views/vista_depositos_estado.sql](views/vista_depositos_estado.sql) | [ver](../../docs/database/06-vistas.md) |
| `VISTA_LOTES_SIMULACION` | VIEW | VALID | LOTE_SIMULACION, REGISTRO_SIMULACION | [views/vista_lotes_simulacion.sql](views/vista_lotes_simulacion.sql) | [ver](../../docs/database/06-vistas.md) |
| `VW_BI_AUDITORIA` | VIEW | VALID | AUDITORIA_DEPOSITO | [views/vw_bi_auditoria.sql](views/vw_bi_auditoria.sql) | [ver](../../docs/database/06-vistas.md) |
| `VW_BI_CIUDADANOS` | VIEW | VALID | ROL, USUARIO, USUARIO_ROL | [views/vw_bi_ciudadanos.sql](views/vw_bi_ciudadanos.sql) | [ver](../../docs/database/06-vistas.md) |
| `VW_BI_CONSULTAS_VISION` | VIEW | VALID | CONSULTA_VISION, MATERIAL | [views/vw_bi_consultas_vision.sql](views/vw_bi_consultas_vision.sql) | [ver](../../docs/database/06-vistas.md) |
| `VW_BI_DEPOSITOS` | VIEW | VALID | MATERIAL, PKG_DEPOSITO, VW_DEPOSITO_DETALLE | [views/vw_bi_depositos.sql](views/vw_bi_depositos.sql) | [ver](../../docs/database/06-vistas.md) |
| `VW_BI_ECOPUNTOS` | VIEW | VALID | MATERIAL, MOVIMIENTO_ECOPUNTOS, OPERACION_ECOPUNTOS, REGLA_ECOPUNTOS | [views/vw_bi_ecopuntos.sql](views/vw_bi_ecopuntos.sql) | [ver](../../docs/database/06-vistas.md) |
| `VW_CANECA_ESTACION` | VIEW | VALID | CONFIGURACION_CANECA, CONTENEDOR, PUNTO_INSTALACION | [views/vw_caneca_estacion.sql](views/vw_caneca_estacion.sql) | [ver](../../docs/database/06-vistas.md) |
| `VW_DEPOSITO_DETALLE` | VIEW | VALID | CONFIGURACION_CANECA, CONTENEDOR, DEPOSITO, INTERACCION, MATERIAL, ORDEN_APERTURA, PKG_DEPOSITO, PUNTO_INSTALACION, USUARIO | [views/vw_deposito_detalle.sql](views/vw_deposito_detalle.sql) | [ver](../../docs/database/06-vistas.md) |
| `VW_RECORRIDO` | VIEW | VALID | CONTENEDOR, DEPOSITO, EVENTO_DISPOSITIVO, INTERACCION, ORDEN_APERTURA, PKG_DEPOSITO | [views/vw_recorrido.sql](views/vw_recorrido.sql) | [ver](../../docs/database/06-vistas.md) |
| `VW_SALDO_ECOPUNTOS` | VIEW | VALID | MOVIMIENTO_ECOPUNTOS, ROL, USUARIO, USUARIO_ROL | [views/vw_saldo_ecopuntos.sql](views/vw_saldo_ecopuntos.sql) | [ver](../../docs/database/06-vistas.md) |

## Secuencias

Todas son secuencias de sistema `ISEQ$$_*` que Oracle crea para las columnas `GENERATED ALWAYS AS IDENTITY`. No se crean a mano ni se exportan: las vuelve a crear la definición de la tabla.

| Secuencia | Columna identidad |
|---|---|
| `ISEQ$$_109624` | `ROL.ID_ROL` |
| `ISEQ$$_109628` | `USUARIO.ID_USUARIO` |
| `ISEQ$$_109635` | `MATERIAL.ID_MATERIAL` |
| `ISEQ$$_109639` | `PUNTO_INSTALACION.ID_PUNTO_INSTALACION` |
| `ISEQ$$_109643` | `CONTENEDOR.ID_CONTENEDOR` |
| `ISEQ$$_109653` | `INTERACCION.ID_INTERACCION` |
| `ISEQ$$_109662` | `ORDEN_APERTURA.ID_ORDEN` |
| `ISEQ$$_109670` | `EVENTO_DISPOSITIVO.ID_EVENTO` |
| `ISEQ$$_109677` | `DEPOSITO.ID_DEPOSITO` |
| `ISEQ$$_109686` | `VINCULO_TELEGRAM.ID_VINCULO` |
| `ISEQ$$_109705` | `REGLA_ECOPUNTOS.ID_REGLA` |
| `ISEQ$$_109708` | `OPERACION_ECOPUNTOS.ID_OPERACION` |
| `ISEQ$$_109713` | `MOVIMIENTO_ECOPUNTOS.ID_MOVIMIENTO` |
| `ISEQ$$_109732` | `OPINION.ID_OPINION` |
| `ISEQ$$_109763` | `COMENTARIO_CIUDADANO.ID_COMENTARIO` |
| `ISEQ$$_109845` | `CONSULTA_VISION.ID_CONSULTA` |
| `ISEQ$$_110354` | `PESO_REFERENCIAL.ID_PESO_REFERENCIAL` |
| `ISEQ$$_110358` | `HISTORIAL_ESTADO_CONTENEDOR.ID_HISTORIAL` |
| `ISEQ$$_110362` | `AUDITORIA_DEPOSITO.ID_AUDITORIA` |

## Índices

98 índices en total. Los que respaldan una PK o UNIQUE se crean con la tabla; los demás están en [indexes/](indexes/).

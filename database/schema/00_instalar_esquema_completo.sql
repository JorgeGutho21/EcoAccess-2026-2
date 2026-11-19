-- =====================================================================
-- EcoAccess · instalación COMPLETA del esquema en un esquema de desarrollo VACÍO
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1 desde ECOACCESS_P3_APP el 2026-11-18.
--
-- NO ejecutar en el esquema compartido de Oracle Cloud (ECOACCESS_P3_APP): allí los objetos ya existen.
-- Ejecutar conectado como el DUEÑO del esquema nuevo (nunca SYS, SYSTEM ni ADMIN):
--     sql usuario@//localhost:1521/FREEPDB1 @database/schema/00_instalar_esquema_completo.sql
-- Se detiene en el primer error. Las sentencias DDL confirman solas (COMMIT implícito): si falla a mitad,
-- revise el error, corríjalo y reinstale en un esquema vacío nuevo; un ROLLBACK no deshace DDL.
-- =====================================================================
SET SERVEROUTPUT ON
SET DEFINE OFF
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK
DECLARE
    n NUMBER;
BEGIN
    IF SYS_CONTEXT('USERENV', 'SESSION_USER') IN ('SYS', 'SYSTEM', 'ADMIN') THEN
        RAISE_APPLICATION_ERROR(-20990, 'Instale con la cuenta dueña del esquema de desarrollo, no con una cuenta administrativa.');
    END IF;
    SELECT COUNT(*) INTO n FROM user_objects WHERE object_type IN ('TABLE', 'VIEW', 'PACKAGE', 'PROCEDURE', 'FUNCTION', 'TRIGGER');
    IF n > 0 THEN
        RAISE_APPLICATION_ERROR(-20991, 'El esquema ' || USER || ' no está vacío (' || n || ' objetos). Esta instalación es solo para un esquema nuevo.');
    END IF;
END;
/
PROMPT === Tablas con PK, UNIQUE, CHECK y columnas IDENTITY (las secuencias ISEQ$$ las crea Oracle)
@@tables/analisis_imagen.sql
@@tables/auditoria_deposito.sql
@@tables/comentario_ciudadano.sql
@@tables/configuracion_caneca.sql
@@tables/consentimiento_vision_externa.sql
@@tables/consulta_vision.sql
@@tables/contenedor_material.sql
@@tables/contenedor.sql
@@tables/conversacion_telegram.sql
@@tables/credencial_acceso.sql
@@tables/deposito.sql
@@tables/eco_migracion_aplicada.sql
@@tables/evento_dispositivo.sql
@@tables/historial_estado_contenedor.sql
@@tables/interaccion.sql
@@tables/lote_simulacion.sql
@@tables/material.sql
@@tables/movimiento_ecopuntos.sql
@@tables/operacion_ecopuntos.sql
@@tables/opinion.sql
@@tables/orden_apertura.sql
@@tables/peso_referencial.sql
@@tables/punto_disposicion.sql
@@tables/punto_instalacion.sql
@@tables/registro_simulacion.sql
@@tables/regla_ecopuntos.sql
@@tables/rol.sql
@@tables/token_vinculacion.sql
@@tables/update_telegram.sql
@@tables/usuario_rol.sql
@@tables/usuario.sql
@@tables/vinculacion_pendiente.sql
@@tables/vinculo_telegram.sql

PROMPT === Claves foráneas (después de todas las tablas)
@@constraints/analisis_imagen.sql
@@constraints/auditoria_deposito.sql
@@constraints/comentario_ciudadano.sql
@@constraints/configuracion_caneca.sql
@@constraints/consulta_vision.sql
@@constraints/contenedor_material.sql
@@constraints/contenedor.sql
@@constraints/conversacion_telegram.sql
@@constraints/credencial_acceso.sql
@@constraints/deposito.sql
@@constraints/evento_dispositivo.sql
@@constraints/historial_estado_contenedor.sql
@@constraints/interaccion.sql
@@constraints/movimiento_ecopuntos.sql
@@constraints/operacion_ecopuntos.sql
@@constraints/opinion.sql
@@constraints/orden_apertura.sql
@@constraints/peso_referencial.sql
@@constraints/registro_simulacion.sql
@@constraints/regla_ecopuntos.sql
@@constraints/token_vinculacion.sql
@@constraints/usuario_rol.sql
@@constraints/vinculacion_pendiente.sql
@@constraints/vinculo_telegram.sql

PROMPT === Índices que no respaldan una restricción (incluye el índice único de crédito inicial)
@@indexes/ix_analisis_interaccion.sql
@@indexes/ix_auditoria_deposito.sql
@@indexes/ix_comentario_estado.sql
@@indexes/ix_comentario_fecha.sql
@@indexes/ix_consulta_vision_fecha.sql
@@indexes/ix_consulta_vision_usuario.sql
@@indexes/ix_contenedor_material_mat.sql
@@indexes/ix_contenedor_punto.sql
@@indexes/ix_conversacion_contenedor.sql
@@indexes/ix_conversacion_interaccion.sql
@@indexes/ix_conversacion_material.sql
@@indexes/ix_deposito_fecha.sql
@@indexes/ix_deposito_mat_declarado.sql
@@indexes/ix_deposito_mat_verificado.sql
@@indexes/ix_deposito_verificador.sql
@@indexes/ix_ecop_deposito.sql
@@indexes/ix_ecop_usuario.sql
@@indexes/ix_evento_orden.sql
@@indexes/ix_evento_resultado.sql
@@indexes/ix_historial_contenedor.sql
@@indexes/ix_interaccion_contenedor.sql
@@indexes/ix_interaccion_mat_conf.sql
@@indexes/ix_interaccion_mat_sug.sql
@@indexes/ix_interaccion_operario.sql
@@indexes/ix_interaccion_usuario.sql
@@indexes/ix_opinion_fecha.sql
@@indexes/ix_orden_contenedor.sql
@@indexes/ix_punto_municipio.sql
@@indexes/ix_registro_simulacion_fila.sql
@@indexes/ix_token_telegram.sql
@@indexes/ix_token_usuario.sql
@@indexes/ix_update_recibido.sql
@@indexes/ix_usuario_rol_rol.sql
@@indexes/ix_vinc_pend_telegram.sql
@@indexes/ix_vinc_pend_usuario.sql
@@indexes/ix_vinculo_telegram.sql
@@indexes/ix_vinculo_usuario.sql
@@indexes/uq_ecop_credito_inicial.sql
@@indexes/uq_orden_vigente.sql
@@indexes/uq_vinculo_telegram_activo.sql
@@indexes/uq_vinculo_usuario_activo.sql

PROMPT === Comentarios del diccionario
@@comments/configuracion_caneca.sql
@@comments/consentimiento_vision_externa.sql
@@comments/contenedor_material.sql
@@comments/contenedor.sql
@@comments/conversacion_telegram.sql
@@comments/credencial_acceso.sql
@@comments/deposito.sql
@@comments/evento_dispositivo.sql
@@comments/interaccion.sql
@@comments/lote_simulacion.sql
@@comments/material.sql
@@comments/movimiento_ecopuntos.sql
@@comments/orden_apertura.sql
@@comments/punto_instalacion.sql
@@comments/registro_simulacion.sql
@@comments/regla_ecopuntos.sql
@@comments/rol.sql
@@comments/token_vinculacion.sql
@@comments/update_telegram.sql
@@comments/usuario_rol.sql
@@comments/usuario.sql
@@comments/vinculacion_pendiente.sql
@@comments/vinculo_telegram.sql

PROMPT === Especificaciones de paquetes (las vistas usan PKG_DEPOSITO.ESTADO)
@@packages/pkg_contexto_app.pks.sql
@@packages/pkg_deposito.pks.sql
@@packages/pkg_ecoaccess_stats.pks.sql
@@packages/pkg_ecopuntos.pks.sql
@@packages/pkg_estadisticas.pks.sql

PROMPT === Vistas (FORCE: se compilan aunque falte un objeto que se crea después)
@@views/ea_r_base_depositos.sql
@@views/ea_r_base_puntos.sql
@@views/ea_r_consultas_vision.sql
@@views/ea_r_depositos.sql
@@views/ea_r_materiales.sql
@@views/ea_r_opiniones.sql
@@views/ea_r_participacion_mensual.sql
@@views/ea_r_puntos.sql
@@views/ea_r_solicitudes.sql
@@views/vista_canecas_estacion.sql
@@views/vista_depositos_estado.sql
@@views/vista_lotes_simulacion.sql
@@views/vw_bi_auditoria.sql
@@views/vw_bi_ciudadanos.sql
@@views/vw_bi_consultas_vision.sql
@@views/vw_bi_depositos.sql
@@views/vw_bi_ecopuntos.sql
@@views/vw_caneca_estacion.sql
@@views/vw_deposito_detalle.sql
@@views/vw_recorrido.sql
@@views/vw_saldo_ecopuntos.sql

PROMPT === Funciones independientes
@@functions/fn_depositos_ciudadano.sql
@@functions/fn_peso_estimado_kg.sql
@@functions/fn_resolver_caneca_disponible.sql

PROMPT === Procedimientos independientes
@@procedures/pr_cambiar_estado_contenedor.sql
@@procedures/pr_consultar_estadisticas.sql

PROMPT === Cuerpos de paquetes
@@packages/pkg_contexto_app.pkb.sql
@@packages/pkg_deposito.pkb.sql
@@packages/pkg_ecoaccess_stats.pkb.sql
@@packages/pkg_ecopuntos.pkb.sql
@@packages/pkg_estadisticas.pkb.sql

PROMPT === Disparadores (TRG_DEPOSITO_COHERENCIA va después de TRG_DEPOSITO_BLOQUEO_CERRADO: FOLLOWS)
@@triggers/tr_ecop_unidades.sql
@@triggers/trg_contenedor_aud_estado.sql
@@triggers/trg_deposito_aud_verificacion.sql
@@triggers/trg_deposito_bloqueo_cerrado.sql
@@triggers/trg_deposito_coherencia.sql

PROMPT === Recompilar y comprobar
EXEC DBMS_UTILITY.COMPILE_SCHEMA(schema => USER, compile_all => FALSE)
DECLARE
    n NUMBER;
BEGIN
    SELECT COUNT(*) INTO n FROM user_objects WHERE status <> 'VALID';
    IF n > 0 THEN
        RAISE_APPLICATION_ERROR(-20992, n || ' objetos inválidos: SELECT name, type, line, text FROM user_errors;');
    END IF;
    DBMS_OUTPUT.PUT_LINE('Esquema instalado sin objetos inválidos. Siguiente paso: catálogos en database/03_datos_prueba (ver docs/database/04-instalacion.md).');
END;
/
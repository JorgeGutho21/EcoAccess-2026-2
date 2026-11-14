-- =====================================================================
-- EcoAccess BD — 09 · Permisos de Oracle (mínimo privilegio)
-- Ejecutar como ECOACCESS_BD después de 08 (F5). Se puede repetir.
--
-- Dos niveles de seguridad que NO hay que confundir:
--   * Roles de NEGOCIO (Ciudadano, Operario, Administrador): filas de la
--     tabla ROL. Los controla la aplicación C# (MatrizPermisos) y, como
--     segunda línea, PKG_CONTEXTO_APP.exigir_rol y los triggers.
--   * Roles de ORACLE (ROL_ECO_LECTURA_BI): quién puede LEER qué objeto
--     conectado a la base. Power BI y el lector C# usan la cuenta
--     ECOACCESS_BD_LECTOR, que solo ve las vistas VW_BI_* (sin nombres,
--     correos, hashes ni tokens).
-- =====================================================================
SET SERVEROUTPUT ON
WHENEVER SQLERROR EXIT SQL.SQLCODE

GRANT SELECT ON vw_bi_ciudadanos       TO rol_eco_lectura_bi;
GRANT SELECT ON vw_bi_depositos        TO rol_eco_lectura_bi;
GRANT SELECT ON vw_bi_consultas_vision TO rol_eco_lectura_bi;
GRANT SELECT ON vw_bi_ecopuntos        TO rol_eco_lectura_bi;
GRANT SELECT ON vw_bi_auditoria        TO rol_eco_lectura_bi;
-- Catálogo público de canecas, sin datos personales:
GRANT SELECT ON vw_caneca_estacion     TO rol_eco_lectura_bi;

SET LINESIZE 160
COLUMN grantee    FORMAT A20
COLUMN table_name FORMAT A26
COLUMN privilege  FORMAT A10
PROMPT ==== Lo que puede leer el rol de informes (debe listar solo 6 vistas) ====
SELECT grantee, table_name, privilege
FROM   user_tab_privs_made
WHERE  grantee = 'ROL_ECO_LECTURA_BI'
ORDER  BY table_name;

PROMPT ==== Comprobación: ninguna TABLA concedida al rol ====
SELECT COUNT(*) AS tablas_concedidas
FROM   user_tab_privs_made p
JOIN   user_tables t ON t.table_name = p.table_name
WHERE  p.grantee = 'ROL_ECO_LECTURA_BI';
WHENEVER SQLERROR CONTINUE
-- Nota: si una aplicación de escritura se conectara con otra cuenta, recibiría
-- EXECUTE sobre PKG_DEPOSITO, PKG_ECOPUNTOS y PR_CAMBIAR_ESTADO_CONTENEDOR,
-- nunca UPDATE directo sobre DEPOSITO ni MOVIMIENTO_ECOPUNTOS.

-- =====================================================================
-- EcoAccess BD · Exportar las vistas de Power BI a CSV
-- Conexión: ECOACCESS_BD_LECTOR (solo ve vistas con seudónimos).
-- Los SPOOL usan nombres simples: el CSV se escribe en la carpeta desde la que se ejecuta SQL*Plus.
-- INSTALAR_Y_PROBAR.ps1 lo corre en una carpeta corta del sistema (SQL*Plus no escribe rutas de más de ~240
-- caracteres) y luego copia los CSV a 09_POWER_BI\DATOS_Y_CONSULTAS\datos_csv. A mano: abre SQL*Plus en datos_csv.
--   pwsh -File 18_ENTREGA_Y_RESPALDOS\herramientas\ejecutar_sql.ps1 -Usuario ECOACCESS_BD_LECTOR -Scripts 09_POWER_BI\DATOS_Y_CONSULTAS\exportar_csv.sql
-- Escribe 6 archivos en 09_POWER_BI\DATOS_Y_CONSULTAS\datos_csv\ que lee el proyecto EcoAccess_BD.pbip.
-- =====================================================================
ALTER SESSION SET NLS_NUMERIC_CHARACTERS='.,';
ALTER SESSION SET NLS_TIMESTAMP_FORMAT='YYYY-MM-DD HH24:MI:SS';
ALTER SESSION SET NLS_DATE_FORMAT='YYYY-MM-DD HH24:MI:SS';
SET MARKUP CSV ON QUOTE ON
SET FEEDBACK OFF
SET TERMOUT OFF
SET PAGESIZE 0 EMBEDDED ON
SET HEADING ON
SPOOL vw_bi_ciudadanos.csv
SELECT * FROM ecoaccess_bd.vw_bi_ciudadanos;
SPOOL OFF
SPOOL vw_bi_depositos.csv
SELECT * FROM ecoaccess_bd.vw_bi_depositos;
SPOOL OFF
SPOOL vw_bi_consultas_vision.csv
SELECT * FROM ecoaccess_bd.vw_bi_consultas_vision;
SPOOL OFF
SPOOL vw_bi_ecopuntos.csv
SELECT * FROM ecoaccess_bd.vw_bi_ecopuntos;
SPOOL OFF
SPOOL vw_bi_auditoria.csv
SELECT * FROM ecoaccess_bd.vw_bi_auditoria;
SPOOL OFF
SPOOL vw_caneca_estacion.csv
SELECT * FROM ecoaccess_bd.vw_caneca_estacion;
SPOOL OFF
SET MARKUP CSV OFF
SET TERMOUT ON
PROMPT CSV exportados en la carpeta actual

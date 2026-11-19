-- EcoAccess · views/vw_saldo_ecopuntos.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  CREATE OR REPLACE FORCE EDITIONABLE VIEW "VW_SALDO_ECOPUNTOS" ("ID_USUARIO", "NOMBRE", "SALDO", "ACREDITADOS", "REVERTIDOS", "MOVIMIENTOS", "ES_SINTETICO") DEFAULT COLLATION "USING_NLS_COMP"  AS
  SELECT u.id_usuario, u.nombre,
       NVL(SUM(m.puntos), 0) AS saldo,
       NVL(SUM(CASE WHEN m.tipo = 1 THEN m.puntos END), 0) AS acreditados,
       NVL(SUM(CASE WHEN m.tipo = 2 THEN m.puntos END), 0) AS revertidos,
       COUNT(m.id_movimiento) AS movimientos,
       0 AS es_sintetico
FROM   usuario u
JOIN   usuario_rol ur ON ur.id_usuario = u.id_usuario
JOIN   rol r          ON r.id_rol = ur.id_rol AND r.codigo = 1
LEFT JOIN movimiento_ecopuntos m ON m.id_usuario = u.id_usuario
GROUP  BY u.id_usuario, u.nombre
WITH READ ONLY;

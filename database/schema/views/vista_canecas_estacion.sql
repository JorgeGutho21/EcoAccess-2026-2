-- EcoAccess · views/vista_canecas_estacion.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  CREATE OR REPLACE FORCE EDITIONABLE VIEW "VISTA_CANECAS_ESTACION" ("ID_PUNTO_INSTALACION", "ESTACION", "ID_CONTENEDOR", "CANECA", "ESTADO", "CATEGORIA", "COLOR", "IDENTIFICADOR_DISPOSITIVO", "UBICACION_EN_ESTACION", "IDENTIFICADOR_QR") DEFAULT COLLATION "USING_NLS_COMP"  AS
  SELECT p.id_punto_instalacion,
       p.nombre                         AS estacion,
       c.id_contenedor,
       c.nombre                         AS caneca,
       c.estado,
       k.categoria,
       DECODE(k.categoria, 1, 'BLANCA', 2, 'VERDE', 3, 'NEGRA', 'SIN CONFIGURAR') AS color,
       k.identificador_dispositivo,
       k.ubicacion_en_estacion,
       c.identificador_qr
FROM   contenedor c
JOIN   punto_instalacion p ON p.id_punto_instalacion = c.id_punto_instalacion
LEFT JOIN configuracion_caneca k ON k.id_contenedor = c.id_contenedor;

-- =====================================================================
-- EcoAccess P3 — migración V024: estación piloto de tres canecas con el QR OFICIAL
-- Archivo: database/06_migraciones/V024__estacion_piloto_tres_canecas.sql
--
-- Fase 2 de la misión de cierre. La caneca oficial del piloto (QR compartido
-- 'EcoAccessCanecaPiloto1', punto «EcoAccess P3 · Estación piloto») pasa a ser la
-- caneca BLANCA de una estación de tres. El QR NO cambia: el mismo código lleva
-- a la estación y el backend elige el color con ResolutorDestino.
--
--   * Caneca piloto existente          -> BLANCA (no se cambia su estado ni sus materiales).
--   * Nueva «EcoAccess · Caneca verde» -> VERDE (orgánicos aprovechables).
--   * Nueva «EcoAccess · Caneca negra» -> NEGRA (no aprovechables).
--
-- Las dos nuevas quedan FUERA DE SERVICIO (2) hasta que exista la caneca
-- física y un administrador las habilite. Su identificador_qr es interno
-- (columna única y obligatoria) y no se imprime.
--
-- Aditiva e idempotente (MERGE). No borra ni modifica filas existentes: las
-- canecas DEMO (1, verde y negra de D002) siguen igual. En una base sin la
-- caneca piloto (instalación nueva) no hace nada.
-- Requiere V021 (configuracion_caneca).
-- Reversión (solo en desarrollo): database/06_migraciones/reversion/R024__estacion_piloto_tres_canecas.sql
-- =====================================================================

SET SERVEROUTPUT ON
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK

DECLARE
    n NUMBER;
BEGIN
    SELECT COUNT(*) INTO n FROM user_tables WHERE table_name = 'CONFIGURACION_CANECA';
    IF n <> 1 THEN RAISE_APPLICATION_ERROR(-20241, 'V024 requiere V021 (configuracion_caneca).'); END IF;
END;
/

MERGE INTO contenedor c
USING (SELECT piloto.id_punto_instalacion, x.nombre, x.identificador_qr
       FROM contenedor piloto
       CROSS JOIN (SELECT 'EcoAccess · Caneca verde' AS nombre, 'EcoAccessPilotoInternoVerde1' AS identificador_qr FROM dual
                   UNION ALL
                   SELECT 'EcoAccess · Caneca negra', 'EcoAccessPilotoInternoNegra1' FROM dual) x
       WHERE piloto.identificador_qr = 'EcoAccessCanecaPiloto1') d
ON (c.identificador_qr = d.identificador_qr)
WHEN NOT MATCHED THEN INSERT (id_punto_instalacion, nombre, identificador_qr, fecha_alta_utc, estado)
                      VALUES (d.id_punto_instalacion, d.nombre, d.identificador_qr, SYS_EXTRACT_UTC(SYSTIMESTAMP), 2);

-- Cada caneca nueva admite los materiales habilitados de SU categoría (2 verde, 3 negra).
MERGE INTO contenedor_material cm
USING (SELECT c.id_contenedor, m.id_material
       FROM contenedor c
       JOIN material m ON m.habilitado = 1
                      AND m.categoria = CASE c.identificador_qr WHEN 'EcoAccessPilotoInternoVerde1' THEN 2 ELSE 3 END
       WHERE c.identificador_qr IN ('EcoAccessPilotoInternoVerde1', 'EcoAccessPilotoInternoNegra1')) d
ON (cm.id_contenedor = d.id_contenedor AND cm.id_material = d.id_material)
WHEN NOT MATCHED THEN INSERT (id_contenedor, id_material) VALUES (d.id_contenedor, d.id_material);

-- Color, ESP32 propio de cada tapa y ubicación. Si la caneca piloto ya tuviera configuración, se respeta.
MERGE INTO configuracion_caneca k
USING (SELECT c.id_contenedor,
              CASE c.identificador_qr WHEN 'EcoAccessCanecaPiloto1' THEN 1 WHEN 'EcoAccessPilotoInternoVerde1' THEN 2 ELSE 3 END AS categoria,
              CASE c.identificador_qr WHEN 'EcoAccessCanecaPiloto1' THEN 'esp32-piloto-blanca'
                                      WHEN 'EcoAccessPilotoInternoVerde1' THEN 'esp32-piloto-verde' ELSE 'esp32-piloto-negra' END AS dispositivo,
              CASE c.identificador_qr WHEN 'EcoAccessCanecaPiloto1' THEN 'izquierda'
                                      WHEN 'EcoAccessPilotoInternoVerde1' THEN 'centro' ELSE 'derecha' END AS ubicacion
       FROM contenedor c
       WHERE c.identificador_qr IN ('EcoAccessCanecaPiloto1', 'EcoAccessPilotoInternoVerde1', 'EcoAccessPilotoInternoNegra1')) d
ON (k.id_contenedor = d.id_contenedor)
WHEN NOT MATCHED THEN INSERT (id_contenedor, categoria, identificador_dispositivo, ubicacion_en_estacion, fecha_actualizacion_utc)
                      VALUES (d.id_contenedor, d.categoria, d.dispositivo, d.ubicacion, SYS_EXTRACT_UTC(SYSTIMESTAMP));

COMMIT;

SELECT estacion, caneca, color, estado, identificador_dispositivo, ubicacion_en_estacion
FROM   vista_canecas_estacion
WHERE  id_punto_instalacion = (SELECT id_punto_instalacion FROM contenedor WHERE identificador_qr = 'EcoAccessCanecaPiloto1')
ORDER  BY categoria;

PROMPT V024_ESTACION_PILOTO_TRES_CANECAS_APLICADA

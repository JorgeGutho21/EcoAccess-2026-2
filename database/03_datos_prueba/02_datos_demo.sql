-- =====================================================================
-- EcoAccess P3 — datos de DEMOSTRACIÓN (ficticios)
-- Archivo: database/03_datos_prueba/02_datos_demo.sql
--
-- Crea un punto de instalación y una caneca de demostración, claramente
-- marcados como ficticios en su nombre, para poder probar consultas y la
-- interfaz. No representan la caneca real del piloto. Requiere haber
-- ejecutado antes 01_catalogos.sql. Se puede ejecutar varias veces.
--
-- El QR 'DEMO_qr_ficticio_0001' es PREDECIBLE a propósito y solo sirve para
-- demostración. Las canecas reales reciben un identificador aleatorio de
-- 128 bits generado por la lógica (GeneradorIdentificadorQrAleatorio).
-- =====================================================================

SET SERVEROUTPUT ON
WHENEVER SQLERROR EXIT SQL.SQLCODE

-- Sin los catálogos, los MERGE de abajo no fallarían: simplemente no admitirían
-- ningún material. Se comprueba antes para no dejar una caneca demo incompleta.
DECLARE
    v_materiales NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_materiales
    FROM   material
    WHERE  codigo IN ('PET', 'CARTON', 'PAPEL', 'LATA_ALUMINIO');

    IF v_materiales < 4 THEN
        RAISE_APPLICATION_ERROR(-20030,
            'Faltan materiales del catálogo (' || v_materiales || ' de 4). Ejecuta antes 01_catalogos.sql.');
    END IF;
END;
/

MERGE INTO punto_instalacion p
USING (SELECT 'DEMO - Bloque ficticio' AS nombre,
              'Dato de demostración; no es una ubicación real' AS referencia FROM dual) d
ON (p.nombre = d.nombre)
WHEN NOT MATCHED THEN INSERT (nombre, referencia_ubicacion, activo) VALUES (d.nombre, d.referencia, 1);

-- Identificador QR ficticio con el formato de Telegram; no corresponde a ningún QR impreso.
MERGE INTO contenedor c
USING (SELECT p.id_punto_instalacion,
              'DEMO - Caneca ficticia' AS nombre,
              'DEMO_qr_ficticio_0001' AS identificador_qr
       FROM punto_instalacion p
       WHERE p.nombre = 'DEMO - Bloque ficticio') d
ON (c.identificador_qr = d.identificador_qr)
WHEN NOT MATCHED THEN INSERT (id_punto_instalacion, nombre, identificador_qr, fecha_alta_utc, estado)
                      VALUES (d.id_punto_instalacion, d.nombre, d.identificador_qr,
                              SYS_EXTRACT_UTC(SYSTIMESTAMP), 2);

-- La caneca demo admite aprovechables secos y queda FUERA DE SERVICIO (estado 2):
-- la admisión real del piloto sigue pendiente (P-02).
MERGE INTO contenedor_material cm
USING (SELECT c.id_contenedor, m.id_material
       FROM contenedor c
       CROSS JOIN material m
       WHERE c.identificador_qr = 'DEMO_qr_ficticio_0001'
         AND m.codigo IN ('PET', 'CARTON', 'PAPEL', 'LATA_ALUMINIO')) d
ON (cm.id_contenedor = d.id_contenedor AND cm.id_material = d.id_material)
WHEN NOT MATCHED THEN INSERT (id_contenedor, id_material) VALUES (d.id_contenedor, d.id_material);

COMMIT;

SELECT c.nombre, c.estado, COUNT(cm.id_material) AS materiales_admitidos
FROM   contenedor c
LEFT JOIN contenedor_material cm ON cm.id_contenedor = c.id_contenedor
WHERE  c.identificador_qr = 'DEMO_qr_ficticio_0001'
GROUP BY c.nombre, c.estado;

WHENEVER SQLERROR CONTINUE

-- Caneca piloto del QR oficial (assets/identidad/QR_Caneca_piloto.png → start=EcoAccessCanecaPiloto1).
-- Autorizado por el propietario el 09/11/2026 para Oracle Cloud. No borra ni modifica registros existentes: solo crea la
-- estación y la caneca si faltan, y agrega materiales aprovechables que aún no estén asociados. Repetirlo no duplica nada.
-- Fase de software: la caneca queda en servicio para que el QR identifique la estación, pero NO hay actuador conectado ni
-- aperturas verificadas; sin Bridge:Eventos/Comandos el bot no emite órdenes físicas.
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK
SET SERVEROUTPUT ON
DECLARE
    qr CONSTANT VARCHAR2(64) := 'EcoAccessCanecaPiloto1';
    nombre_caneca CONSTANT VARCHAR2(100) := 'EcoAccess — Caneca piloto';
    nombre_punto CONSTANT VARCHAR2(100) := 'EcoAccess P3 — Estación piloto';
    punto NUMBER;
    caneca NUMBER;
    n NUMBER;
    agregados NUMBER := 0;
BEGIN
    SELECT COUNT(*), MIN(id_contenedor) INTO n, caneca FROM contenedor WHERE identificador_qr = qr;
    IF n = 1 THEN
        -- Ya existe: solo se acepta si es la caneca piloto; nunca se reasigna un QR de otra caneca.
        SELECT COUNT(*) INTO n FROM contenedor WHERE id_contenedor = caneca AND nombre = nombre_caneca;
        IF n = 0 THEN RAISE_APPLICATION_ERROR(-20141, 'El QR oficial ya pertenece a otra caneca; revisar sin modificar datos.'); END IF;
    ELSE
        SELECT COUNT(*) INTO n FROM contenedor WHERE nombre = nombre_caneca;
        IF n > 0 THEN RAISE_APPLICATION_ERROR(-20142, 'Existe una caneca piloto con otro QR; revisar sin modificar datos.'); END IF;
        SELECT COUNT(*), MIN(id_punto_instalacion) INTO n, punto FROM punto_instalacion WHERE nombre = nombre_punto;
        IF n = 0 THEN
            INSERT INTO punto_instalacion(nombre, referencia_ubicacion, activo)
            VALUES (nombre_punto, 'Modo software: sin ESP32 conectado; ninguna apertura física verificada.', 1)
            RETURNING id_punto_instalacion INTO punto;
        END IF;
        INSERT INTO contenedor(id_punto_instalacion, nombre, identificador_qr, fecha_alta_utc, estado)
        VALUES (punto, nombre_caneca, qr, SYS_EXTRACT_UTC(SYSTIMESTAMP), 1)
        RETURNING id_contenedor INTO caneca;
    END IF;
    -- Solo aprovechables reales (categoría 1), sin materiales de prueba.
    MERGE INTO contenedor_material cm USING
        (SELECT caneca id_contenedor, id_material FROM material WHERE habilitado = 1 AND categoria = 1
            AND NOT REGEXP_LIKE(codigo, '^(BR_|BN_|IT_|TEST_|PRUEBA_)', 'i')
            AND NOT REGEXP_LIKE(nombre, '^(material ficticio|material de prueba|material temporal)', 'i')) d
        ON (cm.id_contenedor = d.id_contenedor AND cm.id_material = d.id_material)
        WHEN NOT MATCHED THEN INSERT (id_contenedor, id_material) VALUES (d.id_contenedor, d.id_material);
    agregados := SQL%ROWCOUNT;
    COMMIT;
    DBMS_OUTPUT.PUT_LINE('PILOTO_ID=' || caneca || ' MATERIALES_AGREGADOS=' || agregados);
EXCEPTION WHEN OTHERS THEN ROLLBACK; RAISE;
END;
/

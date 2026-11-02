-- Plantilla del preparador local: no ejecutar sin reemplazar <<QR_PUBLICO>>.
-- Solo en la base de pruebas PRUEBAP3; una caneca simulada, ninguna ubicación física confirmada.
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK
SET SERVEROUTPUT ON
DECLARE
    qr VARCHAR2(64):='<<QR_PUBLICO>>';
    punto NUMBER;
    caneca NUMBER;
    n NUMBER;
BEGIN
    IF SYS_CONTEXT('USERENV','CON_NAME')<>'PRUEBAP3' THEN
        RAISE_APPLICATION_ERROR(-20131,'Este preparador solo admite la base de pruebas PRUEBAP3.'); END IF;
    IF NOT REGEXP_LIKE(qr,'^[A-Za-z0-9_-]{22}$','c') THEN RAISE_APPLICATION_ERROR(-20132,'Se requiere el QR generado por el preparador.'); END IF;
    SELECT COUNT(*) INTO n FROM material WHERE codigo IN ('PET','CARTON','PAPEL','LATA_ALUMINIO') AND habilitado=1 AND categoria=1;
    IF n<>4 THEN RAISE_APPLICATION_ERROR(-20133,'Falta el catálogo aprobado para la simulación.'); END IF;
    SELECT COUNT(*),MIN(id_punto_instalacion) INTO n,punto FROM punto_instalacion WHERE nombre='Piloto local (simulación)';
    IF n>1 THEN RAISE_APPLICATION_ERROR(-20134,'Hay puntos de simulación duplicados; revisar sin borrar datos.'); END IF;
    IF n=0 THEN
        INSERT INTO punto_instalacion(nombre,referencia_ubicacion,activo)
            VALUES('Piloto local (simulación)','Sin ubicación física confirmada; ensayo de software',1) RETURNING id_punto_instalacion INTO punto;
    END IF;
    SELECT COUNT(*),MIN(id_contenedor) INTO n,caneca FROM contenedor WHERE nombre='EcoAccess — Caneca piloto';
    IF n>1 THEN RAISE_APPLICATION_ERROR(-20135,'Hay canecas piloto duplicadas; revisar sin borrar datos.'); END IF;
    IF n=0 THEN
        INSERT INTO contenedor(id_punto_instalacion,nombre,identificador_qr,fecha_alta_utc,estado)
            VALUES(punto,'EcoAccess — Caneca piloto',qr,SYS_EXTRACT_UTC(SYSTIMESTAMP),1) RETURNING id_contenedor INTO caneca;
    ELSE
        SELECT identificador_qr INTO qr FROM contenedor WHERE id_contenedor=caneca AND id_punto_instalacion=punto;
    END IF;
    MERGE INTO contenedor_material cm USING
        (SELECT caneca id_contenedor,id_material FROM material WHERE codigo IN ('PET','CARTON','PAPEL','LATA_ALUMINIO') AND habilitado=1) d
        ON (cm.id_contenedor=d.id_contenedor AND cm.id_material=d.id_material)
        WHEN NOT MATCHED THEN INSERT(id_contenedor,id_material) VALUES(d.id_contenedor,d.id_material);
    COMMIT;
    DBMS_OUTPUT.PUT_LINE('PILOTO_QR='||qr);
    DBMS_OUTPUT.PUT_LINE('PILOTO_ID='||caneca);
EXCEPTION WHEN OTHERS THEN ROLLBACK; RAISE;
END;
/

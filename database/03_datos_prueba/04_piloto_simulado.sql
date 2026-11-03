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
        UPDATE contenedor SET identificador_qr=qr WHERE id_contenedor=caneca AND id_punto_instalacion=punto;
    END IF;
    -- El catálogo conserva cualquier residuo para orientar a la persona; la caneca piloto solo recibe aprovechables (categoría 1).
    SELECT COUNT(*) INTO n FROM material WHERE codigo='OTRO_RESIDUO';
    IF n=0 THEN
        INSERT INTO material(codigo,nombre,categoria,descripcion,habilitado,sinonimos)
            VALUES('OTRO_RESIDUO','Otro residuo',3,'Cualquier residuo que no aparezca en el catálogo.',1,'otro otros varios mezclado desconocido basura');
    END IF;
    UPDATE material SET ruta_separacion=REGEXP_REPLACE(ruta_separacion,';\s*(no va en la |fuera de la |no )caneca piloto\.?','.')
        WHERE REGEXP_LIKE(ruta_separacion,';\s*(no va en la |fuera de la |no )caneca piloto');
    UPDATE material SET descripcion='Ciérralo bien antes de depositarlo y avísale al personal.' WHERE codigo='ENVASE_CONTAMINADO';
    UPDATE material SET descripcion='Guárdalo en un recipiente rígido antes de depositarlo y avísale al personal.' WHERE codigo='CORTOPUNZANTE';
    MERGE INTO contenedor_material cm USING
        (SELECT caneca id_contenedor,id_material FROM material WHERE habilitado=1 AND categoria=1
            AND NOT REGEXP_LIKE(codigo,'^(BR_|IT_|TEST_|PRUEBA_)','i')
            AND NOT REGEXP_LIKE(nombre,'^(material ficticio|material de prueba|material temporal)','i')) d
        ON (cm.id_contenedor=d.id_contenedor AND cm.id_material=d.id_material)
        WHEN NOT MATCHED THEN INSERT(id_contenedor,id_material) VALUES(d.id_contenedor,d.id_material);
    COMMIT;
    DBMS_OUTPUT.PUT_LINE('PILOTO_QR='||qr);
    DBMS_OUTPUT.PUT_LINE('PILOTO_ID='||caneca);
EXCEPTION WHEN OTHERS THEN ROLLBACK; RAISE;
END;
/

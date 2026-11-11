-- Reversión de V024. Solo quita lo que V024 creó: la configuración de las tres canecas de la estación piloto y las dos
-- canecas internas (verde y negra) con sus materiales, y la fila V024 en ECO_MIGRACION_APLICADA. La caneca piloto
-- oficial (QR 'EcoAccessCanecaPiloto1') se conserva con su estado y sus materiales.
-- Se detiene sin borrar si las canecas nuevas ya tienen interacciones, órdenes o depósitos: entonces son historia.
-- Este script no está en la carpeta de migraciones a propósito: ningún instalador lo ejecuta solo.
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK
SET SERVEROUTPUT ON
DECLARE
    n NUMBER;
BEGIN
    SELECT COUNT(*) INTO n FROM interaccion i JOIN contenedor c ON c.id_contenedor = i.id_contenedor
     WHERE c.identificador_qr IN ('EcoAccessPilotoInternoVerde1', 'EcoAccessPilotoInternoNegra1');
    IF n > 0 THEN RAISE_APPLICATION_ERROR(-20244, 'Las canecas de V024 ya tienen historia; no se borran.'); END IF;
    DELETE FROM configuracion_caneca WHERE id_contenedor IN (SELECT id_contenedor FROM contenedor
        WHERE identificador_qr IN ('EcoAccessCanecaPiloto1', 'EcoAccessPilotoInternoVerde1', 'EcoAccessPilotoInternoNegra1'));
    DELETE FROM contenedor_material WHERE id_contenedor IN (SELECT id_contenedor FROM contenedor
        WHERE identificador_qr IN ('EcoAccessPilotoInternoVerde1', 'EcoAccessPilotoInternoNegra1'));
    DELETE FROM contenedor WHERE identificador_qr IN ('EcoAccessPilotoInternoVerde1', 'EcoAccessPilotoInternoNegra1');
    SELECT COUNT(*) INTO n FROM user_tables WHERE table_name = 'ECO_MIGRACION_APLICADA';
    IF n = 1 THEN EXECUTE IMMEDIATE 'DELETE FROM eco_migracion_aplicada WHERE version = ''V024'''; END IF;
    COMMIT;
    DBMS_OUTPUT.PUT_LINE('R024: estación piloto devuelta a caneca única');
END;
/

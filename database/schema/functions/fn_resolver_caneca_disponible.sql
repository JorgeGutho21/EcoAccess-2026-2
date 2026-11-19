-- EcoAccess · functions/fn_resolver_caneca_disponible.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  CREATE OR REPLACE EDITIONABLE FUNCTION "FN_RESOLVER_CANECA_DISPONIBLE" (
    p_id_punto IN NUMBER,
    p_id_material IN NUMBER
) RETURN NUMBER AUTHID DEFINER IS
-- ECOACCESS_V022_SOURCE_SHA256=9716e7c4540c765c31a10c33ff0ac3562b8f30f918147fb12351a1996a0598a5
    v_activo punto_instalacion.activo%TYPE;
    v_categoria material.categoria%TYPE;
    v_habilitado material.habilitado%TYPE;
    v_cantidad PLS_INTEGER;
    v_id contenedor.id_contenedor%TYPE;
    v_estado contenedor.estado%TYPE;
BEGIN
    IF p_id_punto IS NULL OR p_id_material IS NULL
       OR p_id_punto <= 0 OR p_id_material <= 0
       OR p_id_punto <> TRUNC(p_id_punto)
       OR p_id_material <> TRUNC(p_id_material) THEN
        RAISE_APPLICATION_ERROR(-20810,'Identificadores no validos.');
    END IF;
    BEGIN
        SELECT activo INTO v_activo FROM punto_instalacion
        WHERE id_punto_instalacion=p_id_punto;
    EXCEPTION WHEN NO_DATA_FOUND THEN
        RAISE_APPLICATION_ERROR(-20818,'Estacion inexistente.');
    END;
    IF v_activo <> 1 THEN
        RAISE_APPLICATION_ERROR(-20819,'Estacion inactiva.');
    END IF;
    BEGIN
        SELECT categoria,habilitado INTO v_categoria,v_habilitado
        FROM material WHERE id_material=p_id_material;
    EXCEPTION WHEN NO_DATA_FOUND THEN
        RAISE_APPLICATION_ERROR(-20811,'Material inexistente.');
    END;
    IF v_habilitado <> 1 THEN
        RAISE_APPLICATION_ERROR(-20812,'Material deshabilitado.');
    END IF;
    IF v_categoria NOT IN (1,2,3) THEN
        RAISE_APPLICATION_ERROR(-20813,'Material de ruta especial.');
    END IF;
    SELECT COUNT(*),MIN(c.id_contenedor),MIN(c.estado)
    INTO v_cantidad,v_id,v_estado
    FROM configuracion_caneca k
    JOIN contenedor c ON c.id_contenedor=k.id_contenedor
    WHERE c.id_punto_instalacion=p_id_punto
      AND c.estado<>3 AND k.categoria=v_categoria;
    IF v_cantidad=0 THEN
        RAISE_APPLICATION_ERROR(-20814,'Sin caneca configurada.');
    ELSIF v_cantidad>1 THEN
        RAISE_APPLICATION_ERROR(-20815,'Configuracion ambigua.');
    END IF;
    IF v_estado<>1 THEN
        RAISE_APPLICATION_ERROR(-20816,'Caneca fuera de servicio.');
    END IF;
    SELECT COUNT(*) INTO v_cantidad FROM contenedor_material
    WHERE id_contenedor=v_id AND id_material=p_id_material;
    IF v_cantidad<>1 THEN
        RAISE_APPLICATION_ERROR(-20817,'La caneca no admite el material.');
    END IF;
    RETURN v_id;
END FN_RESOLVER_CANECA_DISPONIBLE;
/

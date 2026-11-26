-- Configuración ficticia del piloto: 2 puntos por unidad verificada, sin valor monetario.
-- Solo añade reglas cuando no hay ninguna vigente; no cambia tarifas, roles ni movimientos anteriores.
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK
SET SERVEROUTPUT ON
DECLARE actor NUMBER; n NUMBER;
BEGIN
    -- Base aislada: el PDB PRUEBAP3 (Docker) o un esquema ECOACCESS_P3_PRUEBAS* (Oracle FREE local). Nunca la nube.
    IF SYS_CONTEXT('USERENV','CON_NAME')<>'PRUEBAP3' AND SUBSTR(USER,1,20)<>'ECOACCESS_P3_PRUEBAS' THEN
        RAISE_APPLICATION_ERROR(-20150,'Las tarifas ficticias solo se preparan en el esquema aislado');
    END IF;
    SELECT COUNT(*) INTO n FROM contenedor c JOIN punto_instalacion p ON p.id_punto_instalacion=c.id_punto_instalacion
    WHERE c.nombre='Estación Oficial EcoAccess' AND p.nombre='Piloto local (simulación)' AND c.estado=1;
    IF n<>1 THEN RAISE_APPLICATION_ERROR(-20151,'Debe existir un único piloto de simulación en servicio'); END IF;
    SELECT MIN(u.id_usuario) INTO actor FROM usuario u JOIN usuario_rol ur ON ur.id_usuario=u.id_usuario
    JOIN rol r ON r.id_rol=ur.id_rol WHERE u.activo=1 AND r.codigo=3
      AND (u.correo_electronico='admin.demo@ejemplo.com' OR u.correo_electronico LIKE '%@example.invalid');
    IF actor IS NULL THEN RAISE_APPLICATION_ERROR(-20152,'Falta un administrador ficticio existente; no se asignan permisos'); END IF;
    -- Las reglas son por material en este esquema, no exclusivas de una caneca.
    -- Serializar también en Oracle si el SQL se invoca fuera del mutex del helper.
    LOCK TABLE regla_ecopuntos IN EXCLUSIVE MODE WAIT 10;
    FOR m IN (SELECT cm.id_material FROM contenedor_material cm JOIN contenedor c ON c.id_contenedor=cm.id_contenedor
              WHERE c.nombre='Estación Oficial EcoAccess') LOOP
        SELECT COUNT(*) INTO n FROM regla_ecopuntos WHERE id_material=m.id_material
          AND desde_utc<=SYS_EXTRACT_UTC(SYSTIMESTAMP) AND (hasta_utc IS NULL OR hasta_utc>SYS_EXTRACT_UTC(SYSTIMESTAMP));
        IF n=0 THEN
            INSERT INTO regla_ecopuntos(id_material,puntos,descripcion,desde_utc,id_creador,por_unidad)
            VALUES(m.id_material,2,'Tarifa ficticia del piloto de software; sin valor monetario',SYS_EXTRACT_UTC(SYSTIMESTAMP),actor,1);
        END IF;
    END LOOP;
    COMMIT;
END;
/
SELECT m.codigo,r.puntos,r.por_unidad FROM regla_ecopuntos r JOIN material m ON m.id_material=r.id_material
WHERE m.codigo IN ('PET','CARTON','PAPEL','LATA_ALUMINIO') AND r.desde_utc<=SYS_EXTRACT_UTC(SYSTIMESTAMP)
 AND (r.hasta_utc IS NULL OR r.hasta_utc>SYS_EXTRACT_UTC(SYSTIMESTAMP)) ORDER BY m.codigo;

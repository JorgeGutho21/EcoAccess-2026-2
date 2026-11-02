-- Materiales adicionales del catálogo ambiental. Reconocer un material no autoriza depositarlo en la caneca piloto:
-- la admisión depende de los materiales habilitados en cada contenedor. Repetir el script no duplica filas.
DECLARE
    PROCEDURE agregar(p_codigo VARCHAR2,p_nombre VARCHAR2,p_categoria NUMBER,p_guia VARCHAR2,
        p_alias VARCHAR2,p_precauciones VARCHAR2,p_ruta VARCHAR2) IS
        incompatibles NUMBER;
    BEGIN
        SELECT COUNT(*) INTO incompatibles FROM material WHERE codigo=p_codigo AND categoria<>p_categoria;
        IF incompatibles<>0 THEN RAISE_APPLICATION_ERROR(-20123,'Revisar categoria existente: '||p_codigo); END IF;
        MERGE INTO material m USING (SELECT p_codigo codigo FROM dual) s ON (m.codigo=s.codigo)
        WHEN MATCHED THEN UPDATE SET
            m.sinonimos=COALESCE(m.sinonimos,p_alias),m.precauciones=COALESCE(m.precauciones,p_precauciones),
            m.ruta_separacion=COALESCE(m.ruta_separacion,p_ruta)
        WHEN NOT MATCHED THEN INSERT(codigo,nombre,categoria,descripcion,habilitado,sinonimos,precauciones,ruta_separacion)
            VALUES(p_codigo,p_nombre,p_categoria,p_guia,1,p_alias,p_precauciones,p_ruta);
    END;

BEGIN
    agregar('BOLSA_PLASTICA','Bolsa plástica',1,'Entrégala limpia y seca; las bolsas sucias van con residuos no aprovechables.',
        'bolsa bolsita empaque plastico flexible','Las bolsas contaminadas no se aprovechan.','Aprovechables si está limpia.');
    agregar('PLASTICO_FLEXIBLE','Plástico flexible',1,'Envolturas y películas limpias; consulta su recepción con el gestor.',
        'envoltura pelicula film paquete mecato','Muchas envolturas metalizadas no se reciclan.','Aprovechables cuando el gestor lo reciba.');
    agregar('METAL','Metal o chatarra menor',1,'Entrégalo limpio y sin bordes cortantes expuestos.',
        'metal chatarra hierro tapa tornillo','Los objetos con bordes cortantes requieren protección.','Aprovechables; separar de la caneca si corta.');
    agregar('RESTOS_ALIMENTOS','Restos de alimentos',2,'Separa los restos de comida de sus empaques.',
        'comida sobras cascara fruta restos alimenticios','No mezclar con aprovechables secos.','Orgánicos aprovechables cuando exista recolección.');
    agregar('MADERA','Madera',3,'Piezas pequeñas sin tratar; las grandes requieren entrega especial.',
        'madera tabla palo estiba','La madera tratada o pintada no se compost.','No aprovechables salvo gestor autorizado.');
    agregar('OBJETO_COMPUESTO','Objeto compuesto',3,'Si puedes separar sus materiales, hazlo; si no, consulta al personal.',
        'compuesto mezclado juguete varios materiales','Revisa si contiene pilas o partes electrónicas.','Según sus materiales.');
    agregar('ENVASE_CONTAMINADO','Envase contaminado',4,'No lo deposites en la caneca piloto; entrégalo a un punto de residuos especiales.',
        'envase quimico veneno pintura solvente limpiador','Puede contener sustancias peligrosas. No lo destapes ni lo mezcles.','Gestión especial; no va en la caneca piloto.');
    agregar('ACEITE','Aceite usado',4,'Guárdalo en un recipiente cerrado y llévalo a un punto de recolección.',
        'aceite cocina lubricante grasa','Nunca lo viertas al desagüe.','Gestión especial; no va en la caneca piloto.');
    agregar('BOMBILLO','Bombillo',4,'Consérvalo entero y llévalo a un punto de recolección autorizado.',
        'bombillo foco lampara led fluorescente','Puede contener mercurio y romperse.','Gestión especial; no va en la caneca piloto.');
    agregar('ANALGESICO','Analgésico o medicamento vencido',4,'Llévalo a un punto de recolección de medicamentos en su empaque.',
        'analgesico acetaminofen ibuprofeno pastilla jarabe','No lo tires a la basura ni al desagüe.','Puntos de recolección de medicamentos.');
    agregar('BLISTER','Blíster de medicamentos',4,'Entrégalo junto con los medicamentos en un punto de recolección.',
        'blister empaque pastillas','Puede conservar residuos del medicamento.','Puntos de recolección de medicamentos.');
    agregar('CORTOPUNZANTE','Elemento cortopunzante',4,'No lo deposites; guárdalo en un recipiente rígido y entrégalo a un servicio de salud.',
        'aguja cuchilla bisturi vidrio roto lanceta','Riesgo de cortes e infección: no lo manipules sin protección.','Gestión sanitaria; no va en la caneca piloto.');
    COMMIT;
END;
/
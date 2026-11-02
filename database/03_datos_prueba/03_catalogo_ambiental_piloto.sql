-- Contenido ambiental, no depósitos ficticios. Requiere V012.
-- No crea ni modifica admisiones de canecas. No borra ni reclasifica registros existentes.
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK
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
    agregar('PET','Botella PET identificada',1,'Entrega el envase vacío, limpio y seco.',
        'botella pet plastico numero 1 tereftalato','Confirma la identificación PET; una foto no prueba el polímero.','Aprovechables; admisión según configuración de la caneca.');
    agregar('PEAD','Envase PEAD identificado',1,'Consulta su recepción con el recuperador local.',
        'pead hdpe plastico numero 2','No confundir con cualquier envase plástico.','Aprovechables cuando el gestor lo reciba.');
    agregar('PLASTICO_SIN_ID','Envase plástico sin identificar',3,'No se presume aprovechable ni PET: confirma material y ruta antes de entregarlo.',
        'envase plastico desconocido recipiente sin simbolo','No autorizar por similitud visual.','Consulta con el gestor local; fuera de admisión automática.');
    agregar('PAPEL','Papel limpio',1,'Entrega limpio y seco.',
        'hoja hojas papel oficina periodico','El papel contaminado requiere otra disposición.','Separación blanca; recepción local por confirmar.');
    agregar('CARTON','Cartón limpio',1,'Entrega seco y sin restos de comida.',
        'caja cajas carton corrugado','El cartón grasoso no se trata como cartón limpio.','Separación blanca; recepción local por confirmar.');
    agregar('VIDRIO','Envase de vidrio',1,'Entrega el envase sin contenido.',
        'botella vidrio frasco envase vidrio','No mezclar con vidrio cortopunzante ni cerámica.','Separación blanca; recepción local por confirmar.');
    agregar('LATA_ALUMINIO','Lata de aluminio identificada',1,'Entrega vacía y limpia.',
        'lata aluminio bebida gaseosa','No confundir aerosoles ni cualquier metal con esta entrada.','Separación blanca; recepción local por confirmar.');
    agregar('MULTICAPA','Envase multicapa',1,'Confirma que exista un gestor que lo reciba.',
        'tetrapak tetra pak tetra brik caja leche jugo','No se presume PET ni se admite por parecer cartón.','Ruta de envases multicapa por confirmar con gestor.');
    agregar('ORGANICO','Restos orgánicos aprovechables',2,'Separa de empaques.',
        'cascara cascaras fruta restos comida jardin','No mezclar sustancias químicas.','Separación verde; aprovechamiento local por confirmar.');
    agregar('CONDON','Condón usado doméstico',3,'Residuo sanitario doméstico no aprovechable.',
        'condon condones preservativo preservativos','No arrojar al inodoro ni mezclar con reciclables.','Bolsa negra doméstica; no caneca piloto.');
    agregar('TAMPON','Tampón usado doméstico',3,'Residuo sanitario doméstico no aprovechable.',
        'tampon tampones higiene menstrual','No arrojar al inodoro. Si procede de atención en salud, consulta su ruta sanitaria.','Bolsa negra doméstica; no caneca piloto.');
    agregar('TOALLA_HIGIENICA','Toalla higiénica usada doméstica',3,'Residuo sanitario doméstico no aprovechable.',
        'toalla higienica compresa protector menstrual','No mezclar con papel aprovechable.','Bolsa negra doméstica; no caneca piloto.');
    agregar('PANAL','Pañal usado doméstico',3,'Residuo sanitario doméstico no aprovechable.',
        'panal panales pañal pañales desechable','Los residuos de atención en salud siguen su protocolo específico.','Bolsa negra doméstica; no caneca piloto.');
    agregar('CHANCLETAS','Chancletas deterioradas',3,'Prioriza reparación o reúso si siguen siendo utilizables.',
        'chancleta chancla sandalia chancletas','No se clasifican como envase plástico reciclable.','Reúso o gestor de calzado; recepción local por confirmar.');
    agregar('ZAPATOS','Zapatos deteriorados',3,'Evalúa reparación o reúso antes del descarte.',
        'zapato zapatos tenis calzado zapatilla','No se admiten como plástico, papel ni PET.','Gestor de calzado; recepción local por confirmar.');
    agregar('ROPA','Ropa deteriorada',3,'Separa textiles y evalúa reparación o reúso.',
        'ropa camiseta camisa pantalon textil tela','La donación requiere prendas aptas y receptor que las acepte.','Reúso o gestor textil; recepción local por confirmar.');
    agregar('COLILLA','Colilla de cigarrillo',3,'Residuo no aprovechable en el piloto.',
        'colilla cigarrillo filtro tabaco','Nunca depositar encendida.','Residuos ordinarios según gestión local; no caneca piloto.');
    agregar('ICOPOR_CONTAMINADO','Icopor con restos de comida',3,'No mezclar con aprovechables limpios.',
        'icopor sucio poliestireno expandido bandeja contaminada','No se identifica como PET.','No aprovechables; fuera de la caneca piloto.');
    agregar('CABLE','Cable eléctrico en desuso',4,'Entrega a una ruta autorizada de residuos eléctricos.',
        'cable cables alambre electrico','No quemar ni desarmar para recuperar metales.','Gestor RAEE; recepción local por confirmar.');
    agregar('CARGADOR','Cargador en desuso',4,'Entrega mediante una ruta RAEE.',
        'cargador cargadores adaptador corriente','No tratar como plástico ordinario.','Gestor RAEE; recepción local por confirmar.');
    agregar('PILA','Pila doméstica usada',4,'Entrega a un programa posconsumo de pilas.',
        'pila pilas alcalina boton acumulador','No abrir, quemar ni mezclar con reciclables.','Posconsumo de pilas; no caneca piloto.');
    agregar('BATERIA','Batería usada',4,'Consulta el programa o gestor apropiado para su tecnología.',
        'bateria baterias litio recargable bateria celular','No perforar ni desarmar. Si está dañada, consulta al gestor antes de transportarla.','Ruta diferenciada de baterías; no caneca piloto.');
    agregar('MEDICAMENTO','Medicamento vencido o sin uso',4,'Entrega a un programa posconsumo de medicamentos.',
        'medicamento medicamentos vencidos pastilla farmaco medicina','No mezclar agujas ni residuos biológicos en el punto de medicamentos.','Posconsumo de medicamentos; confirma receptor local.');
    agregar('JERINGA','Jeringa o aguja usada',4,'Consulta a tu EPS o IPS la recepción de cortopunzantes.',
        'jeringa jeringas aguja agujas lanceta cortopunzante','No manipular la aguja ni llevarla a la caneca piloto o al punto de medicamentos.','Ruta de cortopunzantes indicada por EPS o IPS.');
    agregar('RAEE','Aparato eléctrico o electrónico',4,'Consulta un sistema de recolección RAEE o gestor autorizado.',
        'raee electronico aparato celular computador teclado','No desmontar ni mezclar con materiales ordinarios.','Gestión RAEE; recepción local por confirmar.');
    agregar('ESPECIAL','Residuo especial sin identificar',4,'Solicita orientación al responsable ambiental antes de disponerlo.',
        'residuo especial peligroso desconocido quimico','No manipular un residuo desconocido para clasificarlo.','Gestor autorizado según identificación; no caneca piloto.');
    COMMIT;
EXCEPTION WHEN OTHERS THEN ROLLBACK; RAISE;
END;
/
PROMPT CATALOGO_AMBIENTAL_26_ENTRADAS_PROCESADAS_SIN_ADMISION_AUTOMATICA

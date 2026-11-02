-- Auditoría de contenido/admisiones. Solo lectura: no crea canecas ni cambia listas.
SET LINESIZE 220 PAGESIZE 100
SELECT codigo,nombre,categoria,CASE WHEN sinonimos IS NOT NULL THEN 'SI' ELSE 'NO' END alias_configurado,
    CASE WHEN precauciones IS NOT NULL THEN 'SI' ELSE 'NO' END precauciones_configuradas
FROM material WHERE codigo IN ('PET','PEAD','PLASTICO_SIN_ID','PAPEL','CARTON','VIDRIO','LATA_ALUMINIO','MULTICAPA',
    'ORGANICO','CONDON','TAMPON','TOALLA_HIGIENICA','PANAL','CHANCLETAS','ZAPATOS','ROPA','COLILLA','ICOPOR_CONTAMINADO',
    'CABLE','CARGADOR','PILA','BATERIA','MEDICAMENTO','JERINGA','RAEE','ESPECIAL') ORDER BY codigo;
SELECT COUNT(*) admisiones_especiales_configuradas FROM contenedor_material cm JOIN material m ON m.id_material=cm.id_material WHERE m.categoria=4;
SELECT COUNT(*) interacciones FROM interaccion;
SELECT COUNT(*) depositos FROM deposito;
SELECT COUNT(*) movimientos_ecopuntos FROM movimiento_ecopuntos;

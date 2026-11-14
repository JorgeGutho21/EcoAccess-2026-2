-- =====================================================================
-- EcoAccess BD — 10 · Catálogos
-- Ejecutar como ECOACCESS_BD después de 09 (F5). Se puede repetir:
-- usa MERGE, que inserta solo lo que falta y no duplica.
--
-- Catálogos = datos «de referencia» que casi no cambian: roles, materiales,
-- estaciones, canecas, qué recibe cada caneca y pesos de referencia.
-- Colores según la Resolución 2184 de 2019 (Colombia):
--   BLANCA (1) aprovechables · VERDE (2) orgánicos aprovechables ·
--   NEGRA (3) no aprovechables · 4 = peligrosos (ruta especial, sin caneca).
-- =====================================================================
SET SERVEROUTPUT ON
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK

-- ---------------- Roles ----------------
MERGE INTO rol d USING (
    SELECT 1 id_rol, 1 codigo, 'Ciudadano' descripcion FROM dual UNION ALL
    SELECT 2, 2, 'Operario' FROM dual UNION ALL
    SELECT 3, 3, 'Administrador' FROM dual) s
ON (d.codigo = s.codigo)
WHEN NOT MATCHED THEN INSERT (id_rol, codigo, descripcion) VALUES (s.id_rol, s.codigo, s.descripcion);

-- ---------------- Materiales ----------------
MERGE INTO material d USING (
    SELECT 101 id, 'PET' codigo, 'Botella plástica PET' nombre, 1 cat, 'Botellas de bebidas marcadas con el número 1.' descr FROM dual UNION ALL
    SELECT 102, 'PLASTICO_RIGIDO', 'Envase plástico rígido', 1, 'Envases de yogur, champú o limpieza, vacíos y escurridos.' FROM dual UNION ALL
    SELECT 103, 'CARTON', 'Cartón', 1, 'Cajas y cartón corrugado limpios y secos.' FROM dual UNION ALL
    SELECT 104, 'PAPEL', 'Papel de oficina', 1, 'Hojas, cuadernos y periódico limpios y secos.' FROM dual UNION ALL
    SELECT 105, 'VIDRIO', 'Vidrio', 1, 'Botellas y frascos de vidrio sin tapa.' FROM dual UNION ALL
    SELECT 106, 'LATA_ALUMINIO', 'Lata de aluminio', 1, 'Latas de bebidas.' FROM dual UNION ALL
    SELECT 107, 'METAL_FERROSO', 'Lata de acero', 1, 'Latas de conservas enjuagadas.' FROM dual UNION ALL
    SELECT 108, 'TETRAPAK', 'Envase multicapa', 1, 'Cajas de leche o jugo (cartón, plástico y aluminio).' FROM dual UNION ALL
    SELECT 201, 'RESTO_FRUTA_VERDURA', 'Restos de frutas y verduras', 2, 'Cáscaras y residuos crudos de frutas y verduras.' FROM dual UNION ALL
    SELECT 202, 'CASCARA_HUEVO', 'Cáscara de huevo', 2, 'Cáscaras de huevo.' FROM dual UNION ALL
    SELECT 203, 'RESTO_COMIDA', 'Restos de comida', 2, 'Sobras de comida preparada.' FROM dual UNION ALL
    SELECT 204, 'HOJAS_PODA', 'Hojas y poda', 2, 'Hojas secas, flores y residuos de jardín.' FROM dual UNION ALL
    SELECT 205, 'BORRA_CAFE', 'Borra de café e infusiones', 2, 'Café usado y bolsitas de té sin grapa.' FROM dual UNION ALL
    SELECT 301, 'SERVILLETA_USADA', 'Servilleta o papel higiénico usado', 3, 'Papel sucio o contaminado: no se recicla.' FROM dual UNION ALL
    SELECT 302, 'TAPABOCAS', 'Tapabocas', 3, 'Residuo sanitario de uso personal.' FROM dual UNION ALL
    SELECT 303, 'ICOPOR_SUCIO', 'Icopor contaminado', 3, 'Bandejas o vasos de icopor con restos de comida.' FROM dual UNION ALL
    SELECT 304, 'EMPAQUE_METALIZADO', 'Empaque metalizado', 3, 'Bolsas de papas, galletas y similares.' FROM dual UNION ALL
    SELECT 305, 'CARTON_CONTAMINADO', 'Cartón o papel con comida', 3, 'Cajas de pizza o papeles con grasa.' FROM dual UNION ALL
    SELECT 401, 'PILAS', 'Pilas y baterías', 4, 'Peligroso: punto posconsumo, nunca en las canecas.' FROM dual UNION ALL
    SELECT 402, 'MEDICAMENTOS', 'Medicamentos vencidos', 4, 'Peligroso: punto posconsumo de farmacia.' FROM dual UNION ALL
    SELECT 403, 'BOMBILLO', 'Bombillo o luminaria', 4, 'Peligroso: contiene mercurio o componentes electrónicos.' FROM dual) s
ON (d.codigo = s.codigo)
WHEN NOT MATCHED THEN INSERT (id_material, codigo, nombre, categoria, descripcion)
VALUES (s.id, s.codigo, s.nombre, s.cat, s.descr);

-- ---------------- Estaciones (marcadas como SIMULACIÓN) ----------------
MERGE INTO punto_instalacion d USING (
    SELECT 1 id, 'Estación UPC Sabanas - bloque académico (SIMULACIÓN)' nombre,
           'Pasillo principal, primer piso. Ubicación ficticia para prácticas.' ref,
           'EcoAccessEstacionUPC01' qr FROM dual UNION ALL
    SELECT 2, 'Estación piloto legado QR por caneca (SIMULACIÓN)',
           'Representa el modelo antiguo de P3: un QR por caneca.', NULL FROM dual) s
ON (d.id_punto_instalacion = s.id)
WHEN NOT MATCHED THEN INSERT (id_punto_instalacion, nombre, referencia_ubicacion, codigo_qr_estacion, activo, es_sintetico)
VALUES (s.id, s.nombre, s.ref, s.qr, 1, 1);

-- ---------------- Canecas (nacen FUERA DE SERVICIO = 2) ----------------
-- Se ponen en servicio en 11_datos_sinteticos.sql con PR_CAMBIAR_ESTADO_CONTENEDOR,
-- para que el trigger 1 deje su historia.
MERGE INTO contenedor d USING (
    SELECT 11 id, 1 punto, 'UPC01 - Caneca blanca' nombre, 1 cat, NULL qr, 'esp32-upc01-blanca' disp FROM dual UNION ALL
    SELECT 12, 1, 'UPC01 - Caneca verde',  2, NULL, 'esp32-upc01-verde'  FROM dual UNION ALL
    SELECT 13, 1, 'UPC01 - Caneca negra',  3, NULL, 'esp32-upc01-negra'  FROM dual UNION ALL
    SELECT 21, 2, 'Legado - Caneca blanca', 1, 'EcoAccessCanecaLegado01', NULL FROM dual) s
ON (d.id_contenedor = s.id)
WHEN NOT MATCHED THEN INSERT (id_contenedor, id_punto_instalacion, nombre, categoria, identificador_qr, identificador_dispositivo, estado)
VALUES (s.id, s.punto, s.nombre, s.cat, s.qr, s.disp, 2);

-- ---------------- Qué recibe cada caneca ----------------
-- Las tres canecas de UPC01 reciben todos los materiales de su color.
MERGE INTO contenedor_material d USING (
    SELECT c.id_contenedor, m.id_material, m.categoria
    FROM   contenedor c
    JOIN   material m ON m.categoria = c.categoria AND m.habilitado = 1
    WHERE  c.id_punto_instalacion = 1
    UNION ALL
    -- La caneca legado solo recibe tres materiales (como la piloto de P3).
    SELECT 21, m.id_material, m.categoria FROM material m
    WHERE  m.codigo IN ('PET', 'LATA_ALUMINIO', 'CARTON')) s
ON (d.id_contenedor = s.id_contenedor AND d.id_material = s.id_material)
WHEN NOT MATCHED THEN INSERT (id_contenedor, id_material, categoria) VALUES (s.id_contenedor, s.id_material, s.categoria);

-- ---------------- Pesos de referencia (para ESTIMAR) ----------------
-- estado_visual: 0 no aplica, 1 vacío, 2 parcialmente lleno, 3 lleno.
-- Fuente honesta: son rangos típicos de envases comerciales usados como
-- punto de partida académico; deben reemplazarse por mediciones propias.
MERGE INTO peso_referencial d USING (
    SELECT 1 id, 'PET' mat, 'BOTELLA_PET_600ML' obj, 'Botella PET de 600 ml' descr, 1 ev, 18 gmin, 22 gtip, 28 gmax FROM dual UNION ALL
    SELECT 2, 'PET', 'BOTELLA_PET_600ML', 'Botella PET de 600 ml', 2, 100, 300, 450 FROM dual UNION ALL
    SELECT 3, 'PET', 'BOTELLA_PET_600ML', 'Botella PET de 600 ml', 3, 560, 620, 650 FROM dual UNION ALL
    SELECT 4, 'PET', 'BOTELLA_PET_1500ML', 'Botella PET de 1,5 L', 1, 30, 38, 45 FROM dual UNION ALL
    SELECT 5, 'PLASTICO_RIGIDO', 'ENVASE_YOGUR', 'Envase de yogur individual', 1, 8, 12, 18 FROM dual UNION ALL
    SELECT 6, 'CARTON', 'CAJA_CARTON_PEQUENA', 'Caja de cartón pequeña', 0, 50, 120, 250 FROM dual UNION ALL
    SELECT 7, 'PAPEL', 'HOJA_PAPEL_CARTA', 'Hoja de papel tamaño carta', 0, 4, 5, 6 FROM dual UNION ALL
    SELECT 8, 'VIDRIO', 'BOTELLA_VIDRIO_330ML', 'Botella de vidrio de 330 ml', 1, 180, 200, 250 FROM dual UNION ALL
    SELECT 9, 'LATA_ALUMINIO', 'LATA_330ML', 'Lata de aluminio de 330 ml', 1, 12, 14, 17 FROM dual UNION ALL
    SELECT 10, 'METAL_FERROSO', 'LATA_CONSERVA', 'Lata de conserva (atún)', 1, 40, 55, 70 FROM dual UNION ALL
    SELECT 11, 'TETRAPAK', 'CAJA_TETRAPAK_1L', 'Caja multicapa de 1 L', 1, 25, 28, 32 FROM dual UNION ALL
    SELECT 12, 'RESTO_FRUTA_VERDURA', 'CASCARA_BANANO', 'Cáscara de banano', 0, 20, 30, 45 FROM dual UNION ALL
    SELECT 13, 'CASCARA_HUEVO', 'CASCARA_HUEVO_UNIDAD', 'Cáscara de un huevo', 0, 5, 6, 8 FROM dual UNION ALL
    SELECT 14, 'RESTO_COMIDA', 'SOBRA_PLATO', 'Sobras de un plato de comida', 0, 50, 150, 400 FROM dual UNION ALL
    SELECT 15, 'BORRA_CAFE', 'BORRA_CAFE_PORCION', 'Borra de café de una preparación', 0, 10, 20, 40 FROM dual UNION ALL
    SELECT 16, 'HOJAS_PODA', 'PUNADO_HOJAS', 'Puñado de hojas secas', 0, 5, 20, 100 FROM dual UNION ALL
    SELECT 17, 'SERVILLETA_USADA', 'SERVILLETA', 'Servilleta de papel usada', 0, 1, 2, 4 FROM dual UNION ALL
    SELECT 18, 'TAPABOCAS', 'TAPABOCAS_DESECHABLE', 'Tapabocas desechable', 0, 3, 4, 5 FROM dual UNION ALL
    SELECT 19, 'ICOPOR_SUCIO', 'BANDEJA_ICOPOR', 'Bandeja de icopor', 0, 3, 5, 10 FROM dual UNION ALL
    SELECT 20, 'EMPAQUE_METALIZADO', 'BOLSA_SNACK', 'Bolsa de papas o galletas', 0, 2, 4, 8 FROM dual UNION ALL
    SELECT 21, 'CARTON_CONTAMINADO', 'CAJA_PIZZA', 'Caja de pizza con grasa', 0, 150, 250, 400 FROM dual UNION ALL
    SELECT 22, 'PILAS', 'PILA_AA', 'Pila AA', 0, 20, 23, 26 FROM dual) s
ON (d.clave_objeto = s.obj AND d.estado_visual = s.ev)
WHEN NOT MATCHED THEN INSERT (id_peso_referencial, id_material, clave_objeto, descripcion_objeto, estado_visual,
                              gramos_minimo, gramos_tipico, gramos_maximo, fuente)
VALUES (s.id, (SELECT id_material FROM material WHERE codigo = s.mat), s.obj, s.descr, s.ev, s.gmin, s.gtip, s.gmax,
        'Rango típico de referencia académica; pendiente de medición propia con báscula');

COMMIT;

-- Como se escribieron ids a mano, se avisa a cada IDENTITY que continúe
-- después del mayor id existente (si no, el próximo INSERT sin id chocaría).
ALTER TABLE rol               MODIFY id_rol               GENERATED BY DEFAULT ON NULL AS IDENTITY (START WITH LIMIT VALUE);
ALTER TABLE material          MODIFY id_material          GENERATED BY DEFAULT ON NULL AS IDENTITY (START WITH LIMIT VALUE);
ALTER TABLE punto_instalacion MODIFY id_punto_instalacion GENERATED BY DEFAULT ON NULL AS IDENTITY (START WITH LIMIT VALUE);
ALTER TABLE contenedor        MODIFY id_contenedor        GENERATED BY DEFAULT ON NULL AS IDENTITY (START WITH LIMIT VALUE);
ALTER TABLE peso_referencial  MODIFY id_peso_referencial  GENERATED BY DEFAULT ON NULL AS IDENTITY (START WITH LIMIT VALUE);

-- Un ALTER TABLE marca como INVALID las vistas que dependen de la tabla.
-- Oracle las recompilaría solas al usarlas, pero se recompilan ya para que
-- 14_verificar_invalidos.sql muestre 0 objetos inválidos.
BEGIN
    DBMS_UTILITY.COMPILE_SCHEMA(schema => USER, compile_all => FALSE);
END;
/

PROMPT ==== Conteos de catálogo (esperado: 3 roles, 21 materiales, 2 estaciones, 4 canecas, 22 pesos) ====
SELECT 'ROL' tabla, COUNT(*) filas FROM rol
UNION ALL SELECT 'MATERIAL', COUNT(*) FROM material
UNION ALL SELECT 'PUNTO_INSTALACION', COUNT(*) FROM punto_instalacion
UNION ALL SELECT 'CONTENEDOR', COUNT(*) FROM contenedor
UNION ALL SELECT 'CONTENEDOR_MATERIAL', COUNT(*) FROM contenedor_material
UNION ALL SELECT 'PESO_REFERENCIAL', COUNT(*) FROM peso_referencial;
WHENEVER SQLERROR CONTINUE

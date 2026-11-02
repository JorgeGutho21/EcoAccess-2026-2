-- =====================================================================
-- EcoAccess P3 — catálogos iniciales: roles y materiales
-- Archivo: database/03_datos_prueba/01_catalogos.sql
--
-- No son datos personales: es el vocabulario del sistema. Se usa MERGE por
-- código, así que ejecutarlo varias veces no duplica filas y actualiza los
-- textos si cambiaron.
--
-- La lista de materiales es una PROPUESTA inicial para orientar al ciudadano.
-- Qué materiales admite la caneca piloto sigue pendiente (P-02); por eso aquí
-- solo se define el catálogo, no la admisión.
-- =====================================================================

SET SERVEROUTPUT ON
WHENEVER SQLERROR EXIT SQL.SQLCODE

MERGE INTO rol r
USING (
    SELECT 1 AS codigo, 'Ciudadano: consulta su actividad y solicita aperturas' AS descripcion FROM dual UNION ALL
    SELECT 2, 'Operario: atiende la caneca y verifica el material depositado' FROM dual UNION ALL
    SELECT 3, 'Administrador: configura catálogos, usuarios y contenedores' FROM dual
) d
ON (r.codigo = d.codigo)
WHEN MATCHED THEN UPDATE SET r.descripcion = d.descripcion
WHEN NOT MATCHED THEN INSERT (codigo, descripcion) VALUES (d.codigo, d.descripcion);

-- Categorías: 1 Aprovechable (blanca), 2 OrganicoAprovechable (verde), 3 NoAprovechable (negra), 4 Peligroso.
MERGE INTO material m
USING (
    SELECT 'PET' AS codigo, 'Botella plástica PET' AS nombre, 1 AS categoria,
           'Vacía, enjuagada y aplastada, con la tapa aparte' AS descripcion FROM dual UNION ALL
    SELECT 'PLASTICO_RIGIDO', 'Envase plástico rígido', 1,
           'Envases de aseo o alimentos limpios y secos' FROM dual UNION ALL
    SELECT 'CARTON', 'Cartón', 1,
           'Seco y doblado; sin restos de comida ni grasa' FROM dual UNION ALL
    SELECT 'PAPEL', 'Papel de oficina y cuadernos', 1,
           'Seco, sin plastificar; se pueden dejar las grapas' FROM dual UNION ALL
    SELECT 'VIDRIO', 'Botella o frasco de vidrio', 1,
           'Entero, vacío y limpio; el vidrio roto se entrega envuelto al operario' FROM dual UNION ALL
    SELECT 'LATA_ALUMINIO', 'Lata de aluminio', 1,
           'Vacía y aplastada' FROM dual UNION ALL
    SELECT 'ORGANICO', 'Restos de comida', 2,
           'Va en la caneca verde, no en una caneca de aprovechables secos' FROM dual UNION ALL
    SELECT 'SERVILLETA_USADA', 'Servilleta o papel higiénico usado', 3,
           'Va en la caneca negra: el papel sucio no se puede aprovechar' FROM dual UNION ALL
    SELECT 'ICOPOR_CONTAMINADO', 'Icopor con restos de comida', 3,
           'Va en la caneca negra' FROM dual UNION ALL
    SELECT 'PILA', 'Pila o batería', 4,
           'Residuo peligroso: llévela a un punto de recolección de pilas' FROM dual UNION ALL
    SELECT 'MEDICAMENTO', 'Medicamento vencido', 4,
           'Residuo peligroso: entréguelo en un punto posconsumo de medicamentos' FROM dual
) d
ON (m.codigo = d.codigo)
WHEN MATCHED THEN UPDATE SET m.nombre = d.nombre, m.categoria = d.categoria, m.descripcion = d.descripcion
WHEN NOT MATCHED THEN INSERT (codigo, nombre, categoria, descripcion, habilitado)
                      VALUES (d.codigo, d.nombre, d.categoria, d.descripcion, 1);

COMMIT;

SELECT codigo, descripcion FROM rol ORDER BY codigo;
SELECT categoria, COUNT(*) AS materiales FROM material GROUP BY categoria ORDER BY categoria;

WHENEVER SQLERROR CONTINUE

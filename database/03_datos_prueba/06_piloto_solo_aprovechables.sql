-- Opcional y manual: una caneca piloto creada antes de noviembre de 2026 admite todo el catálogo. Esto la deja solo con aprovechables.
-- No toca el catálogo, el historial ni los ecopuntos. Para deshacerlo: INSERT INTO contenedor_material con los materiales que se quieran admitir.
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK
DELETE FROM contenedor_material
    WHERE id_contenedor=(SELECT id_contenedor FROM contenedor WHERE nombre='EcoAccess — Caneca piloto')
    AND id_material IN (SELECT id_material FROM material WHERE categoria<>1);
COMMIT;

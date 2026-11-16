-- Totales de referencia para contrastar el informe. Conectado como ECOACCESS_P3_INFORMES (solo lectura) o propietario.
-- Compare cada tarjeta de Power BI con la fila de su alcance: ES_ENSAYO 0 = operativo, 1 = demostración.
ALTER SESSION SET CURRENT_SCHEMA=ECOACCESS_P3_APP;
SET PAGESIZE 100 LINESIZE 140 FEEDBACK OFF
PROMPT == Depósitos
SELECT es_ensayo, SUM(depositos) depositos, SUM(verificados) verificados, SUM(unidades_verificadas) unidades, SUM(verificados_sin_conteo) sin_conteo FROM ea_r_base_depositos GROUP BY es_ensayo ORDER BY 1;
PROMPT == Ecopuntos
SELECT es_ensayo, SUM(acreditados) acreditados, SUM(reversos) reversos, SUM(netos) netos FROM ea_r_base_puntos GROUP BY es_ensayo ORDER BY 1;
PROMPT == Solicitudes
SELECT es_ensayo, SUM(solicitudes) solicitudes FROM ea_r_solicitudes GROUP BY es_ensayo ORDER BY 1;
PROMPT == Opiniones (globales)
SELECT SUM(opiniones) opiniones, ROUND(SUM(suma_estrellas)/NULLIF(SUM(opiniones),0),2) promedio FROM ea_r_opiniones;
PROMPT == Participación mensual (no sumar meses)
SELECT mes, es_ensayo, ciudadanos_unicos, depositos FROM ea_r_participacion_mensual ORDER BY 1, 2;
PROMPT == Materiales por categoría
SELECT categoria, COUNT(*) materiales FROM ea_r_materiales WHERE habilitado=1 GROUP BY categoria ORDER BY 1;

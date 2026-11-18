-- Totales de referencia del informe académico (EcoAccess_BD.pbix) contra Oracle Cloud (alias ecoaccess_tp).
-- Conectarse como ECOACCESS_P3_APP o con una cuenta del rol ECOACCESS_ROL_CONSULTA. Solo lectura.
-- Cada tarjeta de Power BI debe coincidir con su fila. Las vistas VW_BI_* las crea la migración V027.
ALTER SESSION SET CURRENT_SCHEMA = ECOACCESS_P3_APP;
SET PAGESIZE 100 LINESIZE 160 FEEDBACK OFF
PROMPT == Depósitos por estado y origen (tarjetas «Depósitos registrados / verificados / pendientes / rechazados»)
SELECT origen_dato, estado, COUNT(*) depositos, SUM(unidades) unidades FROM vw_bi_depositos GROUP BY origen_dato, estado ORDER BY 1, 2;
PROMPT == Ciudadanos (tarjeta «Ciudadanos»)
SELECT origen_dato, COUNT(*) ciudadanos FROM vw_bi_ciudadanos GROUP BY origen_dato ORDER BY 1;
PROMPT == Ecopuntos (créditos y reversos)
SELECT tipo, COUNT(*) movimientos, NVL(SUM(puntos), 0) puntos FROM vw_bi_ecopuntos GROUP BY tipo ORDER BY 1;
PROMPT == Consultas de visión por resultado
SELECT resultado, COUNT(*) consultas FROM vw_bi_consultas_vision GROUP BY resultado ORDER BY 1;
PROMPT == Auditoría de depósitos por operación
SELECT operacion, COUNT(*) registros FROM vw_bi_auditoria GROUP BY operacion ORDER BY 1;

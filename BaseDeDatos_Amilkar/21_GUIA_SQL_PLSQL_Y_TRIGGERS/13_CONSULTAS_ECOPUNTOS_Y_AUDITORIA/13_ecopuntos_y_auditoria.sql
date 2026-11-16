-- EcoAccess · Base de Datos (profesor Amílkar) · Carpeta 21 · 13 EcoPuntos y auditoría
-- SOLO LECTURA. Conexión: EcoAccess BD LOCAL. Ejecuta con F5.
-- Las personas se muestran con seudónimo C-0000, sin nombre.
SET PAGESIZE 100
SET LINESIZE 200

PROMPT ===== A. Tarifas: versiones de REGLA_ECOPUNTOS por material (vigente = hasta_utc NULL) =====
SELECT m.codigo AS material, r.id_regla, r.puntos,
       CASE r.por_unidad WHEN 1 THEN 'por unidad' ELSE 'por depósito' END AS forma,
       TO_CHAR(r.desde_utc, 'YYYY-MM-DD') AS desde,
       NVL(TO_CHAR(r.hasta_utc, 'YYYY-MM-DD'), 'VIGENTE') AS hasta
FROM   regla_ecopuntos r
JOIN   material m ON m.id_material = r.id_material          -- FK_REGLA_MATERIAL
ORDER  BY m.codigo, r.desde_utc;

PROMPT ===== B. Saldo de cada ciudadano con la vista VW_SALDO_ECOPUNTOS (sin nombre) =====
SELECT 'C-' || LPAD(v.id_usuario, 4, '0') AS ciudadano,
       v.acreditados, v.revertidos, v.saldo, v.movimientos
FROM   vw_saldo_ecopuntos v
ORDER  BY v.saldo DESC, v.id_usuario
FETCH  FIRST 10 ROWS ONLY;

PROMPT ===== C. EcoPuntos por material (créditos y reversos) =====
SELECT m.codigo AS material,
       SUM(CASE WHEN mv.tipo = 1 THEN mv.puntos ELSE 0 END) AS acreditado,
       SUM(CASE WHEN mv.tipo = 2 THEN mv.puntos ELSE 0 END) AS revertido,
       SUM(mv.puntos)                                       AS neto
FROM   movimiento_ecopuntos mv
JOIN   regla_ecopuntos r ON r.id_regla    = mv.id_regla     -- FK_MOV_REGLA
JOIN   material m        ON m.id_material = r.id_material   -- FK_REGLA_MATERIAL
GROUP  BY m.codigo
ORDER  BY neto DESC;

PROMPT ===== D. Comprobación: ningún depósito con dos créditos VIGENTES (sin revertir) (debe salir 0 filas) =====
SELECT mv.id_deposito, COUNT(*) AS creditos_vigentes
FROM   movimiento_ecopuntos mv
WHERE  mv.tipo = 1
AND    NOT EXISTS (SELECT 1 FROM movimiento_ecopuntos rv WHERE rv.id_movimiento_revertido = mv.id_movimiento)
GROUP  BY mv.id_deposito
HAVING COUNT(*) > 1;

PROMPT ===== D2. Historia de un depósito corregido: crédito, reverso y nuevo crédito =====
SELECT mv.id_deposito, mv.id_movimiento, mv.clave_idempotencia, mv.tipo, mv.puntos, mv.id_movimiento_revertido
FROM   movimiento_ecopuntos mv
WHERE  mv.id_deposito IN (SELECT id_deposito FROM movimiento_ecopuntos WHERE tipo = 2)
ORDER  BY mv.id_deposito, mv.id_movimiento;

PROMPT ===== E. Comprobación: créditos sin depósito verificado (debe salir 0 filas) =====
SELECT mv.id_movimiento, mv.id_deposito, d.estado_verificacion
FROM   movimiento_ecopuntos mv
JOIN   deposito d ON d.id_deposito = mv.id_deposito         -- FK_MOV_DEPOSITO
WHERE  mv.tipo = 1
AND    d.estado_verificacion <> 1
AND    NOT EXISTS (SELECT 1 FROM movimiento_ecopuntos rv WHERE rv.id_movimiento_revertido = mv.id_movimiento);

PROMPT ===== F. Auditoría de depósitos por operación (la escribe el disparador 3) =====
SELECT a.operacion, COUNT(*) AS filas, COUNT(DISTINCT a.id_deposito) AS depositos
FROM   auditoria_deposito a
GROUP  BY a.operacion
ORDER  BY a.operacion;

PROMPT ===== G. Últimas 8 auditorías con actor (seudónimo) =====
SELECT a.id_deposito, a.operacion, a.estado_anterior || ' -> ' || a.estado_nuevo AS cambio,
       'C-' || LPAD(a.id_actor, 4, '0') AS actor, a.usuario_bd,
       TO_CHAR(a.fecha_utc, 'YYYY-MM-DD HH24:MI') AS fecha_utc
FROM   auditoria_deposito a
ORDER  BY a.id_auditoria DESC
FETCH  FIRST 8 ROWS ONLY;

PROMPT ===== H. Historial de estados de canecas (lo escribe el disparador 1) =====
SELECT c.nombre AS caneca, h.estado_anterior || ' -> ' || h.estado_nuevo AS cambio,
       h.motivo, TO_CHAR(h.fecha_utc, 'YYYY-MM-DD HH24:MI') AS fecha_utc
FROM   historial_estado_contenedor h
JOIN   contenedor c ON c.id_contenedor = h.id_contenedor    -- FK_HIST_CONTENEDOR
ORDER  BY h.id_historial DESC
FETCH  FIRST 8 ROWS ONLY;

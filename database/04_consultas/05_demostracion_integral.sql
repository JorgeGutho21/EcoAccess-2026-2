-- EcoAccess P3: ejecutar con F5 en SQL Developer como ECOACCESS_P3 (demostración: PRUEBAP3).
-- Solo lectura. No contiene contraseñas, hashes de credenciales ni tokens.
-- Sustituir 0 por el ID REAL de orden mostrado por EcoAccess; 0 no inventa una operación.
SET PAGESIZE 100
SET LINESIZE 220
SET VERIFY OFF
VARIABLE id_orden NUMBER
EXEC :id_orden := 0;

-- 1. Confirmar dónde estamos conectados antes de interpretar resultados.
SELECT SYS_CONTEXT('USERENV','SESSION_USER') esquema,
       SYS_CONTEXT('USERENV','CON_NAME') pdb FROM dual;

-- 2. Catálogo y canecas existentes. Estado administrativo no demuestra conectividad.
SELECT id_material,codigo,nombre,categoria,habilitado FROM material
ORDER BY id_material FETCH FIRST 100 ROWS ONLY;
SELECT c.id_contenedor,c.nombre,c.estado,c.identificador_qr,
       m.codigo material_admitido
FROM contenedor c LEFT JOIN contenedor_material cm ON cm.id_contenedor=c.id_contenedor
LEFT JOIN material m ON m.id_material=cm.id_material
ORDER BY c.id_contenedor,m.codigo FETCH FIRST 100 ROWS ONLY;

-- 3. Ciudadano de ESTA orden: no consultar credencial_acceso ni token_vinculacion.
SELECT u.id_usuario,u.nombre,u.activo,i.id_interaccion,i.canal
FROM orden_apertura o JOIN interaccion i ON i.id_interaccion=o.id_interaccion
JOIN usuario u ON u.id_usuario=i.id_usuario WHERE o.id_orden=:id_orden;
-- Vinculación Telegram vigente de ese ciudadano (identificador enmascarado) y su rol.
SELECT v.id_vinculo,'***'||SUBSTR(TO_CHAR(v.telegram_user_id),-3) telegram,
       v.fecha_vinculo_utc,v.fecha_baja_utc,r.descripcion rol
FROM orden_apertura o JOIN interaccion i ON i.id_interaccion=o.id_interaccion
JOIN vinculo_telegram v ON v.id_usuario=i.id_usuario
JOIN usuario_rol ur ON ur.id_usuario=i.id_usuario JOIN rol r ON r.id_rol=ur.id_rol
WHERE o.id_orden=:id_orden;

-- 4. Sugerencia, confirmación y autorización son hechos distintos.
-- Una sugerencia de IA se guarda en interaccion; no hay una tabla duplicada de clasificación.
SELECT i.id_interaccion,i.id_usuario,i.id_contenedor,i.estado,
       i.id_material_sugerido,i.confianza_sugerida,i.id_material_confirmado,
       i.fecha_inicio_utc,i.fecha_cierre_utc,i.motivo
FROM interaccion i JOIN orden_apertura o ON o.id_interaccion=i.id_interaccion
WHERE o.id_orden=:id_orden;
SELECT id_orden,id_interaccion,id_contenedor,estado,fecha_emision_utc,
       fecha_vencimiento_utc,fecha_cierre_utc
FROM orden_apertura WHERE id_orden=:id_orden;

-- 5. Eventos recibidos: tipo 1 acuse, 2 apertura, 3 detección, 4 cierre, 5 fallo.
-- Resultado 1 aplicado, 2 revisión. Su existencia no prueba por sí sola hardware físico.
SELECT id_evento,id_externo,id_contenedor,tipo,resultado,
       fecha_dispositivo_utc,fecha_recepcion_utc
FROM evento_dispositivo WHERE id_orden=:id_orden ORDER BY id_evento;

-- 6. El sensor no verifica el material; pesos nulos si no hubo medición.
SELECT id_deposito,id_evento,id_material_declarado,id_material_verificado,
       id_verificador,fecha_verificacion_utc,peso_medido_kg,peso_estimado_kg,fecha_deteccion_utc
FROM deposito WHERE id_orden=:id_orden;

-- 7. Libro de esta orden. El saldo es suma de créditos/reversos, no filas de premios.
SELECT me.id_movimiento,me.id_usuario,me.tipo,me.puntos,me.id_regla,
       me.id_credito_revertido,me.id_predecesor,op.fecha_utc
FROM movimiento_ecopuntos me JOIN deposito d ON d.id_deposito=me.id_deposito
JOIN operacion_ecopuntos op ON op.id_operacion=me.id_operacion
WHERE d.id_orden=:id_orden ORDER BY me.id_movimiento;
SELECT me.id_usuario,SUM(me.puntos) saldo_actual
FROM movimiento_ecopuntos me
WHERE me.id_usuario IN (SELECT i.id_usuario FROM orden_apertura o
 JOIN interaccion i ON i.id_interaccion=o.id_interaccion WHERE o.id_orden=:id_orden)
GROUP BY me.id_usuario;

-- 8. Resumen sin fabricar ceros como pruebas de un depósito que todavía no existe.
SELECT :id_orden orden_consultada,
 (SELECT COUNT(*) FROM orden_apertura WHERE id_orden=:id_orden) ordenes,
 (SELECT COUNT(*) FROM evento_dispositivo WHERE id_orden=:id_orden) eventos,
 (SELECT COUNT(*) FROM deposito WHERE id_orden=:id_orden) depositos
FROM dual;

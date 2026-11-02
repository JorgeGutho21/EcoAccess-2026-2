-- EcoAccess P3: consultas para SQL Developer (F5) como ECOACCESS_P3 en PRUEBAP3. Solo lectura.
-- No muestra contraseñas, hashes, tokens ni el identificador completo de Telegram.
-- Cuenta DEPÓSITOS (eventos de detección), no kilogramos: no hay báscula; un peso estimado no es una medición.
-- Para una operación concreta usa 05_demostracion_integral.sql con su id_orden real.
SET PAGESIZE 100
SET LINESIZE 220

-- 1. Ciudadanos registrados (con rol Ciudadano) y si tienen Telegram vinculado.
SELECT u.id_usuario, u.nombre, u.activo, u.fecha_registro_utc,
       CASE WHEN EXISTS (SELECT 1 FROM vinculo_telegram v WHERE v.id_usuario = u.id_usuario AND v.fecha_baja_utc IS NULL)
            THEN 'Sí' ELSE 'No' END telegram_vinculado
FROM usuario u JOIN usuario_rol ur ON ur.id_usuario = u.id_usuario JOIN rol r ON r.id_rol = ur.id_rol
WHERE r.codigo = 1 ORDER BY u.id_usuario;

-- 2. Materiales disponibles.
SELECT id_material, codigo, nombre, categoria, habilitado FROM material ORDER BY nombre;

-- 3. Canecas y materiales que admiten (estado 1 = en servicio).
SELECT c.id_contenedor, c.nombre, c.estado, c.identificador_qr,
       LISTAGG(m.codigo, ', ') WITHIN GROUP (ORDER BY m.codigo) materiales
FROM contenedor c LEFT JOIN contenedor_material cm ON cm.id_contenedor = c.id_contenedor
LEFT JOIN material m ON m.id_material = cm.id_material
GROUP BY c.id_contenedor, c.nombre, c.estado, c.identificador_qr ORDER BY c.id_contenedor;

-- 4. Últimas órdenes con su ciudadano, caneca, material sugerido (IA) y declarado.
--    material_sugerido vacío = elección manual, no hubo reconocimiento visual.
SELECT o.id_orden, o.estado estado_orden, o.fecha_emision_utc, o.fecha_cierre_utc,
       i.id_interaccion, i.id_usuario, c.nombre caneca,
       ms.nombre material_sugerido_ia, i.confianza_sugerida, mc.nombre material_declarado,
       CASE WHEN i.id_material_sugerido IS NOT NULL AND i.id_material_sugerido <> i.id_material_confirmado
            THEN 'Sí' ELSE 'No' END ciudadano_corrigio
FROM orden_apertura o JOIN interaccion i ON i.id_interaccion = o.id_interaccion
JOIN contenedor c ON c.id_contenedor = o.id_contenedor
LEFT JOIN material ms ON ms.id_material = i.id_material_sugerido
LEFT JOIN material mc ON mc.id_material = i.id_material_confirmado
ORDER BY o.id_orden DESC FETCH FIRST 30 ROWS ONLY;

-- 5. Eventos del sensor/caneca: tipo 1 acuse, 2 apertura, 3 detección, 4 cierre, 5 fallo.
SELECT e.id_evento, e.id_orden, e.id_contenedor, e.tipo, e.resultado, e.fecha_dispositivo_utc, e.fecha_recepcion_utc
FROM evento_dispositivo e ORDER BY e.id_evento DESC FETCH FIRST 40 ROWS ONLY;

-- 6. Depósitos con verificación y quién verificó.
SELECT d.id_deposito, d.id_orden, md.nombre declarado, mv.nombre verificado,
       d.fecha_deteccion_utc, d.fecha_verificacion_utc, v.nombre verificador,
       CASE WHEN d.fecha_verificacion_utc IS NULL THEN 'Pendiente' ELSE 'Verificado' END estado
FROM deposito d JOIN material md ON md.id_material = d.id_material_declarado
LEFT JOIN material mv ON mv.id_material = d.id_material_verificado
LEFT JOIN usuario v ON v.id_usuario = d.id_verificador
ORDER BY d.id_deposito DESC FETCH FIRST 30 ROWS ONLY;

-- 7. Ecopuntos: saldo por ciudadano (suma del libro de créditos y reversos).
SELECT me.id_usuario, u.nombre, SUM(CASE WHEN me.puntos > 0 THEN me.puntos ELSE 0 END) acreditados,
       SUM(me.puntos) saldo, COUNT(*) movimientos
FROM movimiento_ecopuntos me JOIN usuario u ON u.id_usuario = me.id_usuario
GROUP BY me.id_usuario, u.nombre ORDER BY saldo DESC;

-- 8. Depósitos por día (hora de Colombia) y por mes.
SELECT TRUNC(CAST(FROM_TZ(d.fecha_deteccion_utc, 'UTC') AT TIME ZONE 'America/Bogota' AS DATE)) dia,
       COUNT(*) depositos, SUM(CASE WHEN d.fecha_verificacion_utc IS NOT NULL THEN 1 ELSE 0 END) verificados
FROM deposito d GROUP BY TRUNC(CAST(FROM_TZ(d.fecha_deteccion_utc, 'UTC') AT TIME ZONE 'America/Bogota' AS DATE)) ORDER BY dia DESC;
SELECT TO_CHAR(CAST(FROM_TZ(d.fecha_deteccion_utc, 'UTC') AT TIME ZONE 'America/Bogota' AS DATE), 'YYYY-MM') mes, COUNT(*) depositos
FROM deposito d GROUP BY TO_CHAR(CAST(FROM_TZ(d.fecha_deteccion_utc, 'UTC') AT TIME ZONE 'America/Bogota' AS DATE), 'YYYY-MM') ORDER BY mes DESC;

-- 9. Materiales más registrados (declarados) y por caneca.
SELECT m.nombre, COUNT(*) depositos FROM deposito d JOIN material m ON m.id_material = d.id_material_declarado
GROUP BY m.nombre ORDER BY depositos DESC;
SELECT c.nombre caneca, COUNT(*) depositos FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden
JOIN contenedor c ON c.id_contenedor = o.id_contenedor GROUP BY c.nombre ORDER BY depositos DESC;

-- 10. Clasificaciones visuales: cuántas sugerencias hubo y cuántas confirmó el ciudadano sin corregir.
SELECT COUNT(*) sugerencias_ia,
       SUM(CASE WHEN id_material_confirmado = id_material_sugerido THEN 1 ELSE 0 END) confirmadas_igual,
       SUM(CASE WHEN id_material_confirmado <> id_material_sugerido THEN 1 ELSE 0 END) corregidas
FROM interaccion WHERE id_material_sugerido IS NOT NULL;

-- 11. Operaciones sin depósito: rechazadas, canceladas o cerradas sin detección (con su motivo registrado).
SELECT i.id_interaccion, i.estado, i.fecha_inicio_utc, i.fecha_cierre_utc, i.motivo, o.id_orden, o.estado estado_orden
FROM interaccion i LEFT JOIN orden_apertura o ON o.id_interaccion = i.id_interaccion
WHERE NOT EXISTS (SELECT 1 FROM deposito d WHERE d.id_orden = o.id_orden)
ORDER BY i.id_interaccion DESC FETCH FIRST 30 ROWS ONLY;

-- EcoAccess · Base de Datos (profesor Amílkar) · Universidad Popular del Cesar
-- Carpeta 21 · 12_USUARIOS_Y_RESIDUOS_DEPOSITADOS · SOLO LECTURA (SELECT). No modifica datos.
-- Conexión: EcoAccess BD LOCAL (usuario ECOACCESS_BD). Ejecuta con Ctrl+Enter (una consulta) o F5 (script).
-- Los datos son SINTÉTICOS (es_sintetico = 1). Las personas se muestran con seudónimo C-0001.
-- Q12 · ¿Qué movimientos de EcoPuntos fueron reversados?
-- JOIN: MOVIMIENTO_ECOPUNTOS consigo misma (auto-JOIN): rv = reverso, cr = crédito original.
-- FK: FK_MOV_REVERTIDO (id_movimiento_revertido → id_movimiento de la misma tabla)

SELECT rv.id_movimiento                     AS id_reverso,
       rv.id_movimiento_revertido           AS id_credito_anulado,
       'C-' || LPAD(rv.id_usuario, 4, '0')  AS usuario,
       cr.puntos                            AS puntos_del_credito,
       rv.puntos                            AS puntos_del_reverso,
       rv.motivo
FROM   movimiento_ecopuntos rv
JOIN   movimiento_ecopuntos cr ON cr.id_movimiento = rv.id_movimiento_revertido
WHERE  rv.tipo = 2
ORDER  BY rv.id_movimiento;

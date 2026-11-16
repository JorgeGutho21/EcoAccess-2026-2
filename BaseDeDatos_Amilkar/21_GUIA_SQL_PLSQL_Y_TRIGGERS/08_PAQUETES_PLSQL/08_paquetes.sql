-- EcoAccess · Base de Datos (profesor Amílkar) · Carpeta 21 · 08 Paquetes PL/SQL
-- SOLO LECTURA: solo se llaman funciones y procedimientos que leen. Conexión: EcoAccess BD LOCAL. F5.
SET SERVEROUTPUT ON
SET PAGESIZE 100
SET LINESIZE 200

PROMPT ===== A. Los 4 paquetes y su estado (especificación y cuerpo) =====
SELECT object_name, object_type, status
FROM   user_objects
WHERE  object_type IN ('PACKAGE', 'PACKAGE BODY')
ORDER  BY object_name, object_type;

PROMPT ===== B. Qué contiene cada paquete (procedimientos y funciones públicos) =====
SELECT object_name AS paquete, procedure_name AS subprograma
FROM   user_procedures
WHERE  object_type = 'PACKAGE' AND procedure_name IS NOT NULL
ORDER  BY object_name, subprogram_id;

PROMPT ===== C. PKG_CONTEXTO_APP.tiene_rol: quién es Operario (2) o Administrador (3) =====
SELECT u.id_usuario,
       'C-' || LPAD(u.id_usuario, 4, '0')        AS usuario,
       pkg_contexto_app.tiene_rol(u.id_usuario, 2) AS es_operario,
       pkg_contexto_app.tiene_rol(u.id_usuario, 3) AS es_administrador
FROM   usuario u
WHERE  pkg_contexto_app.tiene_rol(u.id_usuario, 2) = 1
   OR  pkg_contexto_app.tiene_rol(u.id_usuario, 3) = 1
ORDER  BY u.id_usuario;

PROMPT ===== D. PKG_ECOPUNTOS.saldo comparado con la suma directa de movimientos =====
SELECT mv.id_usuario,
       'C-' || LPAD(mv.id_usuario, 4, '0') AS usuario,
       SUM(mv.puntos)                       AS suma_movimientos,
       pkg_ecopuntos.saldo(mv.id_usuario)   AS saldo_paquete
FROM   movimiento_ecopuntos mv
GROUP  BY mv.id_usuario
ORDER  BY suma_movimientos DESC
FETCH  FIRST 5 ROWS ONLY;

PROMPT ===== E. PKG_ECOPUNTOS.regla_vigente: qué tarifa aplica hoy a cada material =====
SELECT m.codigo,
       pkg_ecopuntos.regla_vigente(m.id_material, SYS_EXTRACT_UTC(SYSTIMESTAMP)) AS id_regla_vigente
FROM   material m
ORDER  BY m.codigo;

PROMPT ===== F. PKG_ESTADISTICAS.a_utc y ranking_verificados (función que devuelve SYS_REFCURSOR) =====
SELECT TO_CHAR(pkg_estadisticas.a_utc(TRUNC(SYSDATE)), 'YYYY-MM-DD HH24:MI') AS hoy_00h_colombia_en_utc FROM dual;

VARIABLE rc REFCURSOR
BEGIN
    :rc := pkg_estadisticas.ranking_verificados(TRUNC(SYSDATE) - 30, TRUNC(SYSDATE) + 1, 5);
END;
/
PRINT rc

-- G. Los procedimientos que SÍ modifican datos NO se ejecutan aquí:
--    PKG_DEPOSITO.verificar / rechazar / corregir_verificacion   (cambian DEPOSITO; el disparador 3 audita)
--    PKG_ECOPUNTOS.acreditar / revertir / registrar_regla       (escriben MOVIMIENTO_ECOPUNTOS o REGLA_ECOPUNTOS)
--    Su demostración controlada, que termina en ROLLBACK, está en ../../07_DISPARADORES_TRIGGERS/demo_cuatro_triggers.sql.

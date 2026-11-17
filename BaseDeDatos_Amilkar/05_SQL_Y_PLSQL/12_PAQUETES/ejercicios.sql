-- 12_PAQUETES — 9 ejercicios del banco verificado (conexión: ECOACCESS_BD)
-- Generado por 05_SQL_Y_PLSQL/generar_carpetas_por_tema.ps1 desde 08_CONSULTAS_Y_EJERCICIOS. No editar a mano.
-- Selecciona un ejercicio y pulsa Ctrl+Enter (consulta) o F5 (bloque PL/SQL). Los que modifican datos terminan en ROLLBACK.
SET SERVEROUTPUT ON SIZE UNLIMITED

-- ===== E264 · 17. Procedimientos, funciones y paquetes · Función de paquete
-- Objetivo: Saldo de EcoPuntos de un ciudadano.
-- Explicación: paquete.funcion(parámetros).
SELECT nombre, pkg_ecopuntos.saldo(id_usuario) AS saldo FROM usuario WHERE id_usuario IN (4, 6, 11);

-- ===== E267 · 17. Procedimientos, funciones y paquetes · Tarifa vigente
-- Objetivo: ¿Qué tarifa aplica hoy al PET?
-- Explicación: regla_vigente devuelve NULL si no hay tarifa.
SELECT r.id_regla, r.puntos, r.descripcion FROM regla_ecopuntos r WHERE r.id_regla = pkg_ecopuntos.regla_vigente(101, SYS_EXTRACT_UTC(SYSTIMESTAMP));

-- ===== E268 · 17. Procedimientos, funciones y paquetes · Verificar un depósito (y deshacer)
-- Objetivo: El flujo completo del operario.
-- Explicación: Verificar acredita puntos en la misma transacción.
DECLARE
    v_dep NUMBER; v_titular NUMBER; v_ver NUMBER; v_mat NUMBER; v_cant NUMBER;
BEGIN
    SELECT d.id_deposito, i.id_usuario, d.id_material_declarado, NVL(d.cantidad_declarada, 1) INTO v_dep, v_titular, v_mat, v_cant
    FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion
    WHERE d.estado_verificacion = 0 AND i.id_usuario IS NOT NULL FETCH FIRST 1 ROWS ONLY;
    SELECT id_usuario INTO v_ver FROM usuario WHERE correo_electronico = 'operaria.dos@ejemplo.test';
    DBMS_OUTPUT.PUT_LINE('Saldo antes: ' || pkg_ecopuntos.saldo(v_titular));
    pkg_deposito.verificar(v_dep, v_ver, v_mat, v_cant);
    DBMS_OUTPUT.PUT_LINE('Saldo después: ' || pkg_ecopuntos.saldo(v_titular) || ' (si el material tiene tarifa)');
    ROLLBACK;
END;
/

-- ===== E269 · 17. Procedimientos, funciones y paquetes · Error de negocio: verificar lo propio
-- Objetivo: Un titular no puede verificarse (ORA-20101).
-- Explicación: El trigger 2 protege la regla aunque se salte la aplicación.
DECLARE
    v_dep NUMBER; v_titular NUMBER;
BEGIN
    SELECT d.id_deposito, i.id_usuario INTO v_dep, v_titular FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden
    JOIN interaccion i ON i.id_interaccion = o.id_interaccion WHERE d.estado_verificacion = 0 AND i.id_usuario IS NOT NULL FETCH FIRST 1 ROWS ONLY;
    pkg_deposito.verificar(v_dep, v_titular, 101, 1);
EXCEPTION
    WHEN OTHERS THEN DBMS_OUTPUT.PUT_LINE(SUBSTR(SQLERRM, 1, 120)); ROLLBACK;
END;
/

-- ===== E271 · 17. Procedimientos, funciones y paquetes · Parámetros de un paquete
-- Objetivo: Ver la firma de los procedimientos.
-- Explicación: USER_ARGUMENTS describe los parámetros.
SELECT object_name, argument_name, in_out, data_type FROM user_arguments WHERE package_name = 'PKG_DEPOSITO' AND object_name = 'VERIFICAR' ORDER BY position;

-- ===== E273 · 17. Procedimientos, funciones y paquetes · Saldo con la función del paquete
-- Objetivo: Comparar la función con la vista.
-- Explicación: Ambas leen el mismo libro: deben coincidir.
SELECT s.nombre, s.saldo AS saldo_vista, pkg_ecopuntos.saldo(s.id_usuario) AS saldo_funcion FROM vw_saldo_ecopuntos s WHERE s.saldo > 0 FETCH FIRST 5 ROWS ONLY;

-- ===== E274 · 17. Procedimientos, funciones y paquetes · ¿Tiene el rol?
-- Objetivo: Usar tiene_rol en una consulta.
-- Explicación: Devuelve 1 o 0; no confundir con el usuario de Oracle.
SELECT nombre, pkg_contexto_app.tiene_rol(id_usuario, 2) AS es_operario, pkg_contexto_app.tiene_rol(id_usuario, 3) AS es_admin FROM usuario WHERE correo_electronico NOT LIKE 'ciudadano%' FETCH FIRST 5 ROWS ONLY;

-- ===== E275 · 17. Procedimientos, funciones y paquetes · Hora de Colombia a UTC
-- Objetivo: La función a_utc del paquete de estadísticas.
-- Explicación: Colombia está 5 horas detrás de UTC.
SELECT pkg_estadisticas.a_utc(DATE '2026-11-11') AS medianoche_colombia_en_utc FROM dual;

-- ===== E277 · 17. Procedimientos, funciones y paquetes · Rechazar un depósito (y deshacer)
-- Objetivo: El operario rechaza con motivo.
-- Explicación: pkg_deposito.rechazar exige motivo; el trigger 3 audita como RECHAZO.
DECLARE
    v_dep NUMBER; v_ver NUMBER; v_op VARCHAR2(12);
BEGIN
    SELECT MIN(d.id_deposito) INTO v_dep FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion JOIN usuario u ON u.id_usuario = i.id_usuario WHERE d.estado_verificacion = 0 AND u.correo_electronico <> 'operaria.dos@ejemplo.test';
    SELECT id_usuario INTO v_ver FROM usuario WHERE correo_electronico = 'operaria.dos@ejemplo.test';
    pkg_deposito.rechazar(v_dep, v_ver, 'Ejercicio de la guía: residuo no encontrado');
    SELECT operacion INTO v_op FROM auditoria_deposito WHERE id_deposito = v_dep AND id_auditoria = (SELECT MAX(id_auditoria) FROM auditoria_deposito WHERE id_deposito = v_dep);
    DBMS_OUTPUT.PUT_LINE('Depósito ' || v_dep || ' auditado como ' || v_op);
    ROLLBACK;
END;
/

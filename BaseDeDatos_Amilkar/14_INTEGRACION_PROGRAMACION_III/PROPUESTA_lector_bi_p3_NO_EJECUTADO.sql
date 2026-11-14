-- =====================================================================================
-- PROPUESTA — NO EJECUTADO. Requiere autorización expresa del dueño de Oracle Cloud.
-- Objetivo: que Power BI (escenario B) lea las vistas de Programación III SIN usar
-- ADMIN ni la cuenta de la aplicación ECOACCESS_P3_APP.
-- Se ejecutaría como ADMIN en la Autonomous Database ECOACCESS (región sa-bogota-1).
-- La contraseña NO se escribe aquí: se teclea en el momento con ACCEPT ... HIDE.
-- =====================================================================================
ACCEPT clave_lector CHAR PROMPT 'Contraseña para ECOACCESS_P3_LECTOR: ' HIDE
CREATE USER ECOACCESS_P3_LECTOR IDENTIFIED BY "&clave_lector";
GRANT CREATE SESSION TO ECOACCESS_P3_LECTOR;

-- Solo las vistas de informes (agregados, sin identidades). Nunca las tablas.
GRANT READ ON ECOACCESS_P3_APP.EA_R_MATERIALES            TO ECOACCESS_P3_LECTOR;
GRANT READ ON ECOACCESS_P3_APP.EA_R_PARTICIPACION_MENSUAL TO ECOACCESS_P3_LECTOR;
GRANT READ ON ECOACCESS_P3_APP.EA_R_DEPOSITOS             TO ECOACCESS_P3_LECTOR;
GRANT READ ON ECOACCESS_P3_APP.EA_R_PUNTOS                TO ECOACCESS_P3_LECTOR;
GRANT READ ON ECOACCESS_P3_APP.EA_R_OPINIONES             TO ECOACCESS_P3_LECTOR;

-- Comprobación (como ECOACCESS_P3_LECTOR): debe funcionar la vista y FALLAR la tabla (ORA-00942).
-- SELECT COUNT(*) FROM ECOACCESS_P3_APP.EA_R_DEPOSITOS;
-- SELECT COUNT(*) FROM ECOACCESS_P3_APP.DEPOSITO;

-- Reversión:
-- DROP USER ECOACCESS_P3_LECTOR;

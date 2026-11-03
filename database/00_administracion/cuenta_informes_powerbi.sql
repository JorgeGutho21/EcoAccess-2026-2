-- Cuenta de solo lectura para Power BI, limitada a las vistas agregadas de informes.
-- Lo ejecuta el administrador de Oracle como SYSDBA en el PDB del proyecto. SQL*Plus pide la contraseña: no la guarde
-- aquí ni en el informe. Repetirlo falla si la cuenta ya existe; no borra nada.
--   ALTER SESSION SET CONTAINER=PRUEBAP3;
--   @cuenta_informes_powerbi.sql
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK
ACCEPT clave_informes CHAR PROMPT 'Contraseña de ECOACCESS_P3_INFORMES: ' HIDE
CREATE USER ecoaccess_p3_informes IDENTIFIED BY "&clave_informes" DEFAULT TABLESPACE users TEMPORARY TABLESPACE temp QUOTA 0 ON users;
GRANT CREATE SESSION TO ecoaccess_p3_informes;
GRANT SELECT ON ecoaccess_p3_app.ea_r_base_depositos TO ecoaccess_p3_informes;
GRANT SELECT ON ecoaccess_p3_app.ea_r_base_puntos TO ecoaccess_p3_informes;
GRANT SELECT ON ecoaccess_p3_app.ea_r_solicitudes TO ecoaccess_p3_informes;
GRANT SELECT ON ecoaccess_p3_app.ea_r_opiniones TO ecoaccess_p3_informes;
GRANT SELECT ON ecoaccess_p3_app.ea_r_materiales TO ecoaccess_p3_informes;
GRANT SELECT ON ecoaccess_p3_app.ea_r_participacion_mensual TO ecoaccess_p3_informes;

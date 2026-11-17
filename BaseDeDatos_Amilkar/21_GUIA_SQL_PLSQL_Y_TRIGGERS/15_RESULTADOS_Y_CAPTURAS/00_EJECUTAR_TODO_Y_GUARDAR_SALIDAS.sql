-- EcoAccess · Base de Datos (profesor Amílkar) · Carpeta 21 · 15 Resultados
-- Ejecuta TODOS los scripts de la carpeta 21 que usan EcoAccess BD y guarda cada salida en un .txt.
-- SOLO LECTURA: cada script llamado solo hace SELECT o bloques que leen. No hay COMMIT.
--
-- Cómo usarlo en SQL Developer:
--   1. Archivo → Abrir → este archivo. Conexión (arriba a la derecha): EcoAccess BD LOCAL.
--   2. Cambia la ruta de DEFINE salida por la ruta de ESTA carpeta en tu PC (sin barra final).
--   3. Pulsa F5 (Ejecutar script). Al terminar, aparecen los archivos salida_XX_*.txt aquí.
-- El script 10 usa la conexión ECOACCESS_PRACTICAS y se ejecuta aparte.
SET ECHO OFF
SET VERIFY OFF
DEFINE salida = 'C:\RUTA\A\BaseDeDatos_Amilkar\21_GUIA_SQL_PLSQL_Y_TRIGGERS\15_RESULTADOS_Y_CAPTURAS'

SPOOL "&salida\salida_01_primeros_comandos.txt"
@@../01_CONEXIONES_Y_PRIMEROS_COMANDOS/01_primeros_comandos.sql
SPOOL OFF
SPOOL "&salida\salida_02_select_basico.txt"
@@../02_SELECT_Y_CONSULTAS_BASICAS/02_select_basico.sql
SPOOL OFF
SPOOL "&salida\salida_03_where_join_group_by.txt"
@@../03_WHERE_JOINS_GROUP_BY_HAVING/03_where_join_group_by.sql
SPOOL OFF
SPOOL "&salida\salida_04_bloques_y_variables.txt"
@@../04_BLOQUES_ANONIMOS_Y_VARIABLES/04_bloques_y_variables.sql
SPOOL OFF
SPOOL "&salida\salida_05_cursores.txt"
@@../05_CURSORES/05_cursores.sql
SPOOL OFF
SPOOL "&salida\salida_06_procedimientos.txt"
@@../06_PROCEDIMIENTOS/06_procedimientos.sql
SPOOL OFF
SPOOL "&salida\salida_07_funciones.txt"
@@../07_FUNCIONES/07_funciones.sql
SPOOL OFF
SPOOL "&salida\salida_08_paquetes.txt"
@@../08_PAQUETES_PLSQL/08_paquetes.sql
SPOOL OFF
SPOOL "&salida\salida_09_triggers_consulta.txt"
@@../09_TRIGGERS_ECOACCESS/09_triggers_consulta.sql
SPOOL OFF
SPOOL "&salida\salida_11_secuencias_y_pk.txt"
@@../11_SECUENCIAS_Y_CLAVES_PRIMARIAS/11_secuencias_y_pk.sql
SPOOL OFF
SPOOL "&salida\..\12_USUARIOS_Y_RESIDUOS_DEPOSITADOS\RESULTADOS_ESPERADOS.txt"
@@../12_USUARIOS_Y_RESIDUOS_DEPOSITADOS/00_EJECUTAR_TODAS_SOLO_SELECT.sql
SPOOL OFF
SPOOL "&salida\salida_13_ecopuntos_y_auditoria.txt"
@@../13_CONSULTAS_ECOPUNTOS_Y_AUDITORIA/13_ecopuntos_y_auditoria.sql
SPOOL OFF
SPOOL "&salida\salida_14_soluciones.txt"
@@../14_EJERCICIOS_PROGRESIVOS/14_soluciones.sql
SPOOL OFF
PROMPT ===== Fin: salidas guardadas en &salida =====

-- =====================================================================
-- EcoAccess — Proyecto Final de Base de Datos — Profesor Amílkar
-- 00 · SCRIPT MAESTRO DE INSTALACIÓN DEL ESQUEMA ECOACCESS_BD
--
-- Conexión: ECOACCESS_BD @ localhost:1521/FREEPDB1. En SQL Developer: abre
-- este archivo y pulsa F5. En SQL*Plus: @00_INSTALAR_TODO.sql desde esta carpeta.
-- Antes, una sola vez, un administrador ejecuta 01_esquema_practicas.sql
-- (crea las cuentas ECOACCESS_BD, ECOACCESS_PRACTICAS y ECOACCESS_BD_LECTOR).
--
-- Protección de datos: 02_tablas.sql se DETIENE (ORA-20010 / ORA-20011) si las
-- tablas ya existen, y 11_datos_sinteticos.sql se detiene (ORA-20020) si los datos
-- ya se cargaron. Este script nunca borra nada. Para reinstalar desde cero existe
-- 99_reversion_esquema_academico.sql (pide escribir BORRAR_ECOACCESS_BD).
--
-- Orden lógico pedido → script que lo cumple (respetando las dependencias de Oracle):
--   1  Creación y configuración del esquema ........ 01_esquema_practicas.sql (administrador) y 00_preflight
--   2  Tablas principales ............................ 02_tablas.sql
--   3  Claves primarias .............................. 02_tablas.sql (cada PK nace con su tabla)
--   3b Claves foráneas ............................... 03_restricciones.sql
--   4  NOT NULL y DEFAULT ............................ 02_tablas.sql (son parte de la columna)
--   4b UNIQUE y CHECK con nombre ..................... 03_restricciones.sql
--   5  Índices justificados .......................... 04_indices.sql
--   6  Secuencias .................................... 05_secuencias.sql (las PK usan IDENTITY; no se
--                                                      mezclan secuencias e IDENTITY en una misma clave)
--   7  Vistas ........................................ 06_vistas.sql
--   8–10 Funciones, procedimientos y paquetes ........ 07_procedimientos_funciones_paquetes.sql
--        (un solo archivo porque se llaman entre sí: PKG_CONTEXTO_APP debe existir antes
--         que las funciones y procedimientos que lo usan)
--   11 Triggers ...................................... 08_triggers.sql
--   12 Usuarios, roles y permisos .................... 01 (cuentas) y 09_permisos.sql (grants)
--   13 Catálogos y datos sintéticos .................. 10_catalogos.sql y 11_datos_sinteticos.sql
--   13b Comentarios del diccionario ................... 15_comentarios_diccionario.sql
--   14 Consultas y pruebas ........................... 12_pruebas.sql (153 pruebas, termina en ROLLBACK)
--   15 Verificación final ............................ 14_verificar_invalidos.sql
-- =====================================================================
SET SERVEROUTPUT ON SIZE UNLIMITED
PROMPT ===== EcoAccess BD · instalación completa del esquema académico =====
PROMPT ----- Paso 1: revisión previa (solo lectura)
@@00_preflight_solo_lectura.sql
PROMPT ----- Pasos 2-4: tablas con PK, NOT NULL y DEFAULT
@@02_tablas.sql
PROMPT ----- Pasos 3b-4b: claves foráneas, UNIQUE y CHECK
@@03_restricciones.sql
PROMPT ----- Paso 5: índices
@@04_indices.sql
PROMPT ----- Paso 6: identidades y secuencias
@@05_secuencias.sql
PROMPT ----- Paso 7: vistas
@@06_vistas.sql
PROMPT ----- Pasos 8-10: funciones, procedimientos y paquetes
@@07_procedimientos_funciones_paquetes.sql
PROMPT ----- Paso 11: disparadores
@@08_triggers.sql
PROMPT ----- Paso 12: permisos de lectura para Power BI y C#
@@09_permisos.sql
PROMPT ----- Paso 13: catálogos y datos sintéticos
@@10_catalogos.sql
@@11_datos_sinteticos.sql
PROMPT ----- Paso 13b: diccionario de datos dentro de Oracle (COMMENT ON)
@@15_comentarios_diccionario.sql
PROMPT ----- Paso 14: pruebas automáticas
@@12_pruebas.sql
PROMPT ----- Paso 15: verificación final
@@14_verificar_invalidos.sql
PROMPT ===== Instalación terminada. Revisa arriba «TOTAL: 153 pruebas · PASAN: 153 · FALLAN: 0» =====

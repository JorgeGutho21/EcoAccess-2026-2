# Scripts Oracle del esquema ECOACCESS_BD

Los archivos están numerados en el **orden que exigen las dependencias de Oracle**: primero las tablas, luego las restricciones que las relacionan, después los objetos que las usan y al final los datos y las pruebas. Están en una sola carpeta a propósito, porque `00_INSTALAR_TODO.sql` los llama con `@@`, que busca en su misma carpeta. Separarlos en subcarpetas rompería la instalación.

| # | Archivo | Categoría | Qué hace | Conexión |
|---|---|---|---|---|
| 00 | `00_INSTALAR_TODO.sql` | Instalación | Script maestro: ejecuta en orden el preflight, del 02 al 11, el 15 (comentarios), el 12 (pruebas) y el 14 (inválidos). | ECOACCESS_BD |
| 00 | `00_preflight_solo_lectura.sql` | Verificación | Comprueba usuario, versión y que no haya restos; no modifica nada. | ECOACCESS_BD |
| 01 | `01_esquema_practicas.sql` | Instalación | Crea, si no existen, los usuarios `ECOACCESS_BD`, `ECOACCESS_PRACTICAS` y `ECOACCESS_BD_LECTOR` y el rol `ROL_ECO_LECTURA_BI` (pide las contraseñas al ejecutarse). | administrador |
| 02 | `02_tablas.sql` | Tablas | Las 19 tablas con PK, NOT NULL y DEFAULT. | ECOACCESS_BD |
| 03 | `03_restricciones.sql` | Restricciones | UNIQUE, CHECK y las 38 FK (6 compuestas). | ECOACCESS_BD |
| 04 | `04_indices.sql` | Índices | Índices de FK y de consulta, y 4 índices únicos basados en función. | ECOACCESS_BD |
| 05 | `05_secuencias.sql` | Secuencias | Muestra las secuencias internas de las columnas IDENTITY; el patrón SEQUENCE + TRIGGER se practica en `05_SQL_Y_PLSQL/00_PRACTICAS_VERIFICADAS/P01_secuencia_pk.sql`. | ECOACCESS_BD |
| 06 | `06_vistas.sql` | Vistas | 4 vistas de trabajo y 5 vistas `VW_BI_*` para Power BI, todas WITH READ ONLY. | ECOACCESS_BD |
| 07 | `07_procedimientos_funciones_paquetes.sql` | Funciones, procedimientos y paquetes | `PKG_CONTEXTO_APP`, `PKG_ECOPUNTOS`, `PKG_DEPOSITO`, `PKG_ESTADISTICAS`, funciones y procedimientos. | ECOACCESS_BD |
| 08 | `08_triggers.sql` | Triggers | Los 4 disparadores del proyecto. | ECOACCESS_BD |
| 09 | `09_permisos.sql` | Seguridad | El rol `ROL_ECO_LECTURA_BI`, que solo puede leer las vistas `VW_BI_*`. | ECOACCESS_BD |
| 10 | `10_catalogos.sql` | Datos | Roles, materiales, estaciones, canecas y pesos de referencia (con MERGE, repetible). | ECOACCESS_BD |
| 11 | `11_datos_sinteticos.sql` | Datos de prueba | 53 personas, 130 sesiones, 187 fotos, 105 depósitos y 48 movimientos, todos sintéticos. | ECOACCESS_BD |
| 12 | `12_pruebas.sql` | Verificación | 153 pruebas automáticas; termina en ROLLBACK. | ECOACCESS_BD |
| 13 | `13_consultas_exposicion.sql` | Consultas | Las 22 consultas de exposición (Q01–Q22). | ECOACCESS_BD |
| 14 | `14_verificar_invalidos.sql` | Verificación | Lista los objetos inválidos (deben ser 0). | ECOACCESS_BD |
| 15 | `15_comentarios_diccionario.sql` | Diccionario | `COMMENT ON` de las 19 tablas y las 173 columnas (generado desde el diccionario). | ECOACCESS_BD |
| 16 | `16_prueba_lector.sql` | Verificación | La cuenta lectora ve las vistas BI y recibe ORA-00942 en las tablas. | ECOACCESS_BD_LECTOR |
| 17 | `17_extraer_ddl_final.sql` | DDL | Extrae de Oracle el DDL real con `DBMS_METADATA`. | ECOACCESS_BD |
| — | `DDL_FINAL_EXTRAIDO.sql` | DDL | El DDL definitivo que Oracle extrajo de sí mismo; se compara con los scripts (D01–D10). | — |
| 18 | `18_inventario_objetos.sql` | Verificación | Inventario de los objetos del esquema. | ECOACCESS_BD |
| 99 | `99_reversion_esquema_academico.sql` | Recuperación | Elimina **solo** los objetos de ECOACCESS_BD; lo usa `INSTALAR_Y_PROBAR.ps1 -Reinstalar` tras pedir confirmación escrita. | ECOACCESS_BD |

## Cómo ejecutar

- **Todo de una vez y con comprobaciones:** `pwsh -File .\INSTALAR_Y_PROBAR.ps1` desde la carpeta raíz. Si el esquema ya existe, no reinstala ni borra nada.
- **En SQL Developer:** abre el archivo con la conexión indicada y pulsa **F5** (ejecutar script).

El plan de reversión y la restauración del respaldo están en `18_ENTREGA_Y_RESPALDOS/PLAN_DE_REVERSION.md` y en `11_GUIAS_PASO_A_PASO/MANUAL_INSTALACION_Y_RECUPERACION.pdf`.

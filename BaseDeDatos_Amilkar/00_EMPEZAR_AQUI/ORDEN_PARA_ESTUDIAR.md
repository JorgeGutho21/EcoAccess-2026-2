# Orden para estudiar

Este recorrido va de lo básico a lo avanzado. Cada etapa dice qué abrir, qué hacer con las manos y cómo saber que quedó entendido. Conviene tener SQL Developer abierto con la conexión `ECOACCESS_BD` (ver `11_GUIAS_PASO_A_PASO/GUIA_SQL_DEVELOPER.pdf`).

## Etapa 1. Entender el proyecto (1 hora)

1. Lee `12_GUIA_PERSONAL_JORGE/GUIA_PERSONAL_JORGE_DESDE_CERO.pdf`, en la parte inicial: qué es EcoAccess y qué hay en cada herramienta.
2. Lee la Parte I de `03_DICCIONARIO_DE_DATOS/GUIA_COMPLETA_173_ATRIBUTOS_Y_38_RELACIONES.pdf`: qué es EcoAccess, los cinco módulos y las cinco ideas del diseño.

**Lo entendiste si** puedes explicar en un minuto por qué una foto no es un depósito.

## Etapa 2. Oracle y las herramientas (1 hora)

1. `11_GUIAS_PASO_A_PASO/GUIA_ORACLE_CONEXIONES_Y_ERRORES_FRECUENTES.pdf`: qué Oracle se usa, los esquemas y cómo conectarse.
2. `11_GUIAS_PASO_A_PASO/GUIA_SQL_DEVELOPER.pdf`: crear la conexión y ejecutar con Ctrl+Enter y con F5.
3. Ejecuta `SELECT USER, SYS_CONTEXT('USERENV','CON_NAME') FROM dual;`.

**Lo entendiste si** obtienes `ECOACCESS_BD  FREEPDB1`.

## Etapa 3. Los modelos (2 horas)

1. Parte II de la guía de los 173 atributos: diferencia entre MER, MEER y modelo relacional.
2. Abre los diagramas de `02_MODELOS_DE_DATOS/01_MER`, `02_MEER` y `03_MODELO_RELACIONAL_GENERAL`.
3. Abre el `.dmd` siguiendo `11_GUIAS_PASO_A_PASO/GUIA_DATA_MODELER.pdf` y recorre las cinco subvistas M1–M5.
4. Lee `02_MODELOS_DE_DATOS/06_FLUJO_DE_DATOS/INTERPRETACION_MODELO_Y_FLUJO_DE_DATOS.pdf`.

**Lo entendiste si** puedes explicar por qué hay 19 relaciones en el MER y 38 FK en Oracle.

## Etapa 4. SQL paso a paso (6–8 horas, en varias sesiones)

Abre cada carpeta de `05_SQL_Y_PLSQL` en este orden. En cada una, ejecuta `ejercicios.sql` un ejercicio a la vez y compara con `RESULTADOS.md`:

1. `01_FUNDAMENTOS_SQL`
2. `02_SELECT_WHERE_ORDER_BY`
3. `03_JOIN`
4. `04_GROUP_BY_HAVING_Y_ANALITICAS`
5. `05_SUBCONSULTAS_Y_CONJUNTOS`
6. `13_TRANSACCIONES_Y_DML`

**Lo entendiste si** obtienes la misma salida que está en `RESULTADOS.md`. Los ejercicios marcados «error esperado» deben fallar.

## Etapa 5. El diccionario (3 horas)

1. Parte III de la guía de los 173 atributos: las 19 tablas.
2. Parte IV: un módulo por sesión. Para cada atributo intenta responder antes de leer: qué es, por qué existe, quién lo llena y qué pasa si no existiera.
3. Comprueba en Oracle: `SELECT column_name, data_type, nullable FROM user_tab_columns WHERE table_name = 'DEPOSITO' ORDER BY column_id;`.

## Etapa 6. PL/SQL (4–6 horas)

Sigue las carpetas de `05_SQL_Y_PLSQL` en este orden, con `11_GUIAS_PASO_A_PASO/GUIA_SQL_Y_PLSQL.pdf` al lado:

1. `06_BLOQUES_ANONIMOS_Y_CICLOS`
2. `07_VARIABLES_Y_CONDICIONALES`
3. `08_CURSORES`
4. `09_PROCEDIMIENTOS`
5. `10_FUNCIONES`
6. `11_SYS_REFCURSOR`
7. `12_PAQUETES`
8. `14_EXCEPCIONES`
9. `16_ESQUEMA_HR_PRACTICAS`

Después revisa `05_SQL_Y_PLSQL/00_PRACTICAS_VERIFICADAS/P04_hr_plsql.sql` y `P05_crud_sin_delete.sql`.

## Etapa 7. Disparadores (3 horas)

1. `06_TAREAS_PROFESOR_AMILKAR/2026-11-10_CLASE_TRIGGERS_OLD_Y_NEW/LEEME.md`: el contenido de la clase.
2. `07_DISPARADORES_TRIGGERS/GUIA_TRIGGERS_LINEA_POR_LINEA.pdf`.
3. Ejecuta `07_DISPARADORES_TRIGGERS/demo_cuatro_triggers.sql` con F5 y lee la salida: antes, sentencia y después.
4. Ejercicios de `05_SQL_Y_PLSQL/15_DISPARADORES_EJERCICIOS`.

**Lo entendiste si** puedes decir por qué el trigger de auditoría es AFTER y el de coherencia es BEFORE.

## Etapa 8. Integración (2 horas)

1. `11_GUIAS_PASO_A_PASO/GUIA_POWER_BI.pdf` y abrir `09_POWER_BI/ARCHIVO_PBIX/EcoAccess_BD.pbix`.
2. `11_GUIAS_PASO_A_PASO/GUIA_CSHARP_VISUAL_STUDIO.pdf` y abrir `10_CSHARP_VISUAL_STUDIO/SOLUCION_CSHARP/EcoAccessBD.sln`.
3. `14_INTEGRACION_PROGRAMACION_III/LEEME.md`.

## Etapa 9. Repaso para la sustentación

1. `13_SUSTENTACION/PREGUNTAS_SUSTENTACION_MODELO_RELACIONAL.pdf`: tapa la respuesta y contesta en voz alta.
2. `00_EMPEZAR_AQUI/ORDEN_PARA_EXPONER.md`.

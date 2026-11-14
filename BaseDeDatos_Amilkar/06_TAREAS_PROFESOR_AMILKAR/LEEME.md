# Tareas y clases del profesor Amílkar

Cada clase o tarea tiene su carpeta con:

- el material de origen (enunciado, captura o documento de clase);
- dónde está la solución dentro de la entrega;
- cómo se comprobó y qué evidencia la respalda.

**Regla de fechas:** solo se escribe una fecha cuando hay una fuente que la acredita. Si no la hay, la carpeta dice `FECHA_POR_VERIFICAR` y no se inventa ninguna.

## Cronología

| Fecha | Carpeta | Tema | Fuente de la fecha | Estado |
|---|---|---|---|---|
| 18/09/2026 (apertura) – 22/09/2026 (cierre) | `2026-09-18_AVANCE_PROYECTO_FINAL_AULA_WEB` | Avance del proyecto final: introducción, objetivos, MER, modelo relacional y diccionario | Captura original del Aula Web («Apertura: viernes, 18 de septiembre de 2026, 01:08 · Cierre: martes, 22 de septiembre de 2026, 06:08») | Cumplido en el informe final, capítulos 1 a 5 |
| 22/09/2026 | `2026-09-22_CLASE_SQLPLUS_CONSULTAS_DESDE_CONSOLA` | Oracle desde la consola: SQL*Plus, usuarios, tablas y consultas | Fecha escrita en la guía de clase de Jorge («Fecha: 22/09/2026») | Apuntes de clase |
| Por verificar | `FECHA_POR_VERIFICAR_PLSQL_HR_CURSORES_PROCEDIMIENTOS_FUNCIONES` | Bloques anónimos, variables, %TYPE, IF, FOR, cursores, funciones, procedimientos y paquetes sobre HR | Solo se sabe que el material se descargó el 09/11/2026; la fecha de la clase no está acreditada | Practicado y verificado (P04) |
| Por verificar | `FECHA_POR_VERIFICAR_EJEMPLOS_32_33_34` | Ejemplos 32 (stock), 33 (auditoría de salarios) y 34 (validar nota) | Sin fecha acreditada | Practicado y verificado (P03) |
| 10/11/2026 | `2026-11-10_CLASE_TRIGGERS_OLD_Y_NEW` | Triggers: BEFORE/AFTER, `:OLD` y `:NEW`, FOR EACH ROW, auditoría de salario | Documento de apoyo fechado «Nov 10, 2026» y descargado ese mismo día | Practicado y verificado (P03) |
| Asignación por verificar (vinculada a la clase del 10/11/2026) | `TAREA_DISPARADORES_CUATRO_PUNTOS` | Los cuatro puntos de la tarea de disparadores | La fecha de asignación no aparece en ninguna fuente; solo se acredita la fecha de la clase | Cumplida y verificada |
| Por verificar | `FECHA_POR_VERIFICAR_CRUD_SIN_DELETE_CSHARP` | CRUD de empleado sin eliminación, desde C# | Sin fecha acreditada | Cumplido y verificado (P05 y C# 10/10) |

## Separación que se respeta

- Los **cuatro disparadores del proyecto EcoAccess** viven en el esquema `ECOACCESS_BD` (`04_SCRIPTS_ORACLE/08_triggers.sql`).
- Los **ejercicios de clase** (secuencia, precio final, triggers libres, HR y CRUD) viven en el esquema separado `ECOACCESS_PRACTICAS` (`05_SQL_Y_PLSQL/00_PRACTICAS_VERIFICADAS/`).
- El esquema `HR` solo se lee: nunca se modifica.

## Autoría del material de clase

- **La captura del Aula Web** es del curso del profesor Amílkar.
- **El documento de apoyo sobre `:OLD`/`:NEW`** indica como autor a «Diego Gómez» y está fechado el 10/11/2026. Se conserva como material de apoyo de esa clase, sin atribuirlo al profesor.
- **La exposición «PL/SQL y disparadores»** la presentaron Juan José San Juan Fragozo, María del Mar Maestre Luna y Saray Marcela Cadena Palomino (Saray en la sección de disparadores). Sus diapositivas 32, 33 y 34 son los «ejemplos 32–34».
- **Las soluciones, los scripts y las explicaciones** de esta entrega son del equipo del proyecto.

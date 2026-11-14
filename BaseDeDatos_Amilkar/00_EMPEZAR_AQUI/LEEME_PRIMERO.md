# EcoAccess — Base de Datos

**Universidad Popular del Cesar · Asignatura: Base de Datos · Docente: profesor Amílkar**

**Proyecto:** EcoAccess — Sistema Inteligente de Gestión y Clasificación de Residuos.

**Integrantes del proyecto:**
- Jorge Alfonso Gutiérrez Thomas
- Heyder Hedrey Canchila de Ángel
- Saray Marcela Cadena Palomino

**Organización de la entrega y guía personal de estudio:** Jorge Alfonso Gutiérrez Thomas.

## Qué abrir primero

| Quiero… | Abro |
|---|---|
| **Ver el documento que se entrega al profesor** | `01_DOCUMENTO_FINAL/INFORME_FINAL_BASE_DE_DATOS_ECOACCESS.pdf` |
| **Saber qué hay en cada carpeta** | `00_EMPEZAR_AQUI/INDICE_GENERAL.pdf` |
| **Estudiar desde cero** | `00_EMPEZAR_AQUI/ORDEN_PARA_ESTUDIAR.md` |
| **Preparar la exposición** | `00_EMPEZAR_AQUI/ORDEN_PARA_EXPONER.md` |
| **Saber qué está comprobado y qué falta** | `00_EMPEZAR_AQUI/ESTADO_DEL_PROYECTO.md` |
| **Instalar y probar la base** | En PowerShell 7, desde la carpeta raíz: `pwsh -File .\INSTALAR_Y_PROBAR.ps1` |

## Las carpetas

| Carpeta | Para qué sirve |
|---|---|
| `00_EMPEZAR_AQUI` | Este archivo, el índice general, los recorridos para estudiar y exponer, y el estado del proyecto. |
| `01_DOCUMENTO_FINAL` | El informe universitario definitivo (Word y PDF). Los cinco puntos del Aula Web van primero; los anexos traen el diccionario completo, las 38 relaciones y los disparadores. |
| `02_MODELOS_DE_DATOS` | MER, MEER, modelo relacional general y por módulos, el proyecto editable de Data Modeler (`.dmd`), el flujo de datos y las evidencias. |
| `03_DICCIONARIO_DE_DATOS` | La guía de los 173 atributos y 38 relaciones, las fichas de 24 campos, la verificación atributo por atributo y los scripts que generan y comprueban el diccionario. |
| `04_SCRIPTS_ORACLE` | Todo el SQL del esquema `ECOACCESS_BD`, numerado en el orden de instalación, y el DDL extraído de Oracle. |
| `05_SQL_Y_PLSQL` | Las prácticas verificadas de clase y los ejercicios ordenados por tema, cada uno con su salida real. |
| `06_TAREAS_PROFESOR_AMILKAR` | Cada clase y tarea con su fecha comprobada, su material de origen, la solución y la evidencia. |
| `07_DISPARADORES_TRIGGERS` | Los cuatro disparadores del proyecto, los de clase y la guía que explica cada línea. |
| `08_CONSULTAS_Y_EJERCICIOS` | El banco de 309 ejercicios verificados, su guía de estudio y las consultas para la exposición. |
| `09_POWER_BI` | El tablero (`.pbix`), el proyecto (`.pbip`), Power Query, DAX y los datos de respaldo. |
| `10_CSHARP_VISUAL_STUDIO` | La solución de Visual Studio: consola de pruebas y ventana CRUD sin eliminación. |
| `11_GUIAS_PASO_A_PASO` | Guías de Oracle y errores frecuentes, SQL Developer, Data Modeler, SQL y PL/SQL, Power BI, C# y el manual de instalación y recuperación. |
| `12_GUIA_PERSONAL_JORGE` | La guía personal de estudio de Jorge, desde cero. |
| `13_SUSTENTACION` | El manual de sustentación, el banco de 43 preguntas y la lista de comprobación del día de la exposición. |
| `14_INTEGRACION_PROGRAMACION_III` | La relación con EcoAccess de Programación III: matriz de correspondencia y verificaciones de GitHub y Oracle Cloud. |
| `15_MATRIZ_DE_TRAZABILIDAD` | La matriz K en Excel: requisitos del profesor → archivo → código → prueba → estado. |
| `16_EVIDENCIAS_Y_PRUEBAS` | Salidas reales de SQL*Plus, pruebas de C#, capturas de Power BI y el resumen de la última instalación. |
| `17_COMO_SE_CONSTRUYO` | La historia de la construcción, paso a paso, y los registros históricos. |
| `18_ENTREGA_Y_RESPALDOS` | Herramientas de generación, fuentes de los documentos, manifiesto, plan de reversión, correspondencia de rutas e historial de commits. |

## Reglas de esta entrega

- **Datos:** todos los datos de EcoAccess son **sintéticos**. Se generaron para estudiar y exponer, y las vistas de Power BI los rotulan como «DATOS SINTÉTICOS».
- **Contraseñas:** ningún documento ni archivo publicado contiene contraseñas. En el PC de trabajo están en `_LOCAL_NO_PUBLICAR/`, carpeta que nunca se sube a GitHub ni entra en el ZIP.
- **Separación de proyectos:** el esquema académico (`ECOACCESS_BD`) y el de prácticas (`ECOACCESS_PRACTICAS`) están separados de la base de Programación III, que no se modifica.
- **Verificación:** lo que no se pudo comprobar aparece como **PARCIAL** o **PENDIENTE** en `ESTADO_DEL_PROYECTO.md`; no se presenta como hecho.

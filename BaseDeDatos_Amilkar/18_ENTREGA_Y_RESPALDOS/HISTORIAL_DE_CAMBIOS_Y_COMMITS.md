# Historial de cambios y commits — BaseDeDatos_Amilkar

**Publicación en GitHub**

| Dato | Valor |
|---|---|
| Repositorio | `JorgeGutho21/EcoAccess-2026-2` (privado) |
| Rama | `entrega/base-de-datos-amilkar-2026` |
| Base de la rama | `main` en el commit `13eb17b`, la referencia estable verificada |
| Integración | Pull Request hacia `main`, sin fusión automática |

**Reglas que se siguieron**

- Todos los cambios están dentro de `BaseDeDatos_Amilkar/`. No se tocaron `src/`, `database/`, `hardware/`, `tests/` ni ningún otro archivo de Programación III.
- No hubo force-push, no se reescribió el historial y no se borraron ramas.
- Antes de cada commit se revisaron, con `git status`, los archivos preparados y se pasó `herramientas/revisar_secretos.ps1`. Este script compara las contraseñas reales sin mostrarlas y busca wallets, `.env`, claves y tokens. Resultado en todos los commits: 0 hallazgos.

**Fechas:** las de los commits son las reales de la publicación, el 14/11/2026, hora de Colombia. Son distintas de las fechas históricas de las clases, que están en `06_TAREAS_PROFESOR_AMILKAR`.

| # | Commit | Fecha (UTC−5) | Mensaje | Archivos |
|---:|---|---|---|---|
| 1 | `50636b1` | 2026-11-14 07:48 | docs(bd): crear estructura BaseDeDatos_Amilkar e índice general | 9 |
| 2 | `a957751` | 2026-11-14 07:48 | docs(bd): organizar requisitos de Aula Web y cronología de clases | 23 |
| 3 | `bfe3908` | 2026-11-14 07:48 | docs(bd): incorporar MER MEER DMD y diagramas relacionales | 271 |
| 4 | `950480f` | 2026-11-14 07:49 | docs(bd): integrar diccionario ampliado de 173 atributos | 36 |
| 5 | `df0e82a` | 2026-11-14 07:49 | docs(bd): documentar las 38 relaciones y el flujo de datos | 9 |
| 6 | `d28ee16` | 2026-11-14 07:49 | feat(bd): incorporar scripts Oracle y prácticas PL/SQL verificadas | 69 |
| 7 | `d10c1ee` | 2026-11-14 07:49 | docs(bd): documentar triggers de EcoAccess y tareas del profesor | 4 |
| 8 | `4c5fc0a` | 2026-11-14 07:49 | docs(bd): organizar ejercicios SQL y consultas de sustentación | 11 |
| 9 | `e77f2c4` | 2026-11-14 07:49 | feat(bi): incorporar informe académico y documentación Oracle | 90 |
| 10 | `b32aef8` | 2026-11-14 07:49 | docs(bd): documentar integración CSharp y Programación III | 18 |
| 11 | `b4d8b3e` | 2026-11-14 07:50 | docs(bd): publicar informe académico definitivo y anexos | 21 |
| 12 | `d846e5e` | 2026-11-14 07:50 | docs(bd): incorporar guías de estudio y sustentación | 26 |
| 13 | `68df0ad` | 2026-11-14 07:50 | test(bd): registrar verificaciones y matriz de trazabilidad | 56 |
| 14 | `f1933ac` | 2026-11-14 07:51 | docs(bd): actualizar manifiesto y preparar entrega definitiva | 15 |
| 15 | (este commit) | 2026-11-14 | docs(bd): registrar el identificador del commit de cierre | 1 |

## Detalle por commit

### 1. Estructura e índice general (`50636b1`)
- **Archivos:** `README.md`, `.gitignore`, `00_EMPEZAR_AQUI/` (LEEME_PRIMERO, INDICE_GENERAL en Word y PDF, ORDEN_PARA_ESTUDIAR, ORDEN_PARA_EXPONER, ESTADO_DEL_PROYECTO) y `18_ENTREGA_Y_RESPALDOS/LEEME.md`.
- **Qué cambió:** se crea la carpeta académica separada de Programación III, con un punto de entrada claro y un `.gitignore` propio que excluye credenciales, respaldos Data Pump, ZIP, wallets y salidas de compilación.
- **Por qué:** la entrega tenía 19 carpetas con nombres heredados de varias etapas y documentos identificados por letras A–O. Hacía falta un índice único y recorridos para estudiar y para exponer.
- **Cómo se comprobó:** el índice se generó en Word y se auditó (0 marcadores, 0 errores de campo); revisión de secretos: 0 hallazgos.

### 2. Requisitos y cronología (`a957751`)
- **Archivos:** `06_TAREAS_PROFESOR_AMILKAR/` (23 archivos).
- **Qué cambió:** una carpeta por clase o tarea, con su material de origen, la solución y la evidencia. Fechas acreditadas:
  - **18–22/09/2026:** captura original del Aula Web.
  - **22/09/2026:** fecha escrita en la guía de la clase.
  - **10/11/2026:** fecha del documento de apoyo, descargado ese mismo día.

  Las demás fechas quedan como «por verificar».
- **Por qué:** el profesor revisa tareas concretas, y cada una debe poder encontrarse con su fecha y su evidencia. No se inventaron fechas, y el material de compañeros se atribuye a sus autores.
- **Cómo se comprobó:** los totales e identificadores citados de P99 (19/19; SQ01, PR01–PR06, TL01–TL02, EJ32, EJ34, CR01–CR04) se contrastaron con la salida real.

### 3. Modelos (`bfe3908`)
- **Archivos:** `02_MODELOS_DE_DATOS/` sin el flujo de datos (271 archivos, la mayoría XML del proyecto `.dmd`).
- **Qué cambió:** se separan MER, MEER, modelo relacional general, modelos por módulo, proyecto editable, evidencias y herramientas. Los scripts generadores escriben ahora en esas carpetas.
- **Por qué:** se pedían el modelo editable auténtico y las imágenes, organizados por tipo.
- **Cómo se comprobó:** `verificar_dmd.ps1` en la ruta nueva: PASA, con 19 tablas, 173 columnas y 38 FK iguales a Oracle.

### 4. Diccionario ampliado (`950480f`)
- **Archivos:** `03_DICCIONARIO_DE_DATOS/` (36 archivos).
- **Qué cambió:** guía de los 173 atributos (182 páginas), fichas de 24 campos, verificación atributo por atributo, matriz de atributos frente a Oracle, fuentes y generadores.
- **Por qué:** cada atributo debe poder defenderse ante el profesor: qué significa, por qué existe, qué guarda, qué regla lo protege, quién lo modifica, quién lo consulta y cómo comprobarlo.
- **Cómo se comprobó:** `generar_diccionario_ampliado.ps1` confirma 173 de 173 atributos (entre 90 y 165 palabras y al menos 5 oraciones cada uno) y 19 de 19 tablas. Las pruebas D09 y D10 del instalador confirman diccionario = Oracle.

### 5. Relaciones y flujo (`df0e82a`)
- **Archivos:** `03_DICCIONARIO_DE_DATOS/ampliado/` (`relaciones.txt`, `restricciones_oracle.txt`, `salida/relaciones_38.html`, `salida/pk_19.html`) y `02_MODELOS_DE_DATOS/06_FLUJO_DE_DATOS/`.
- **Qué cambió:** las 38 FK con su explicación, su cardinalidad calculada con las restricciones reales y el flujo de los datos.
- **Por qué:** el MER (19 relaciones) y el esquema físico (38 FK) miden cosas distintas, y había que explicar cada FK por separado.
- **Cómo se comprobó:** 38 FK leídas de Oracle (32 simples y 6 compuestas), todas explicadas; el generador se detiene si falta alguna.

### 6. Scripts Oracle y PL/SQL (`d28ee16`)
- **Archivos:** `04_SCRIPTS_ORACLE/`, `05_SQL_Y_PLSQL/`, `INSTALAR_Y_PROBAR.ps1` y `18_ENTREGA_Y_RESPALDOS/herramientas/` (`comun.ps1`, `ejecutar_sql.ps1`, `revisar_secretos.ps1`).
- **Qué cambió:**
  - el SQL queda en orden de dependencias, con un LEEME por categoría;
  - las prácticas verificadas se acompañan de 16 carpetas temáticas generadas desde el banco de ejercicios, con su salida real;
  - las rutas internas del instalador se ajustaron a la estructura nueva.
- **Por qué:** la instalación debía seguir funcionando después de la reorganización.
- **Cómo se comprobó:** `INSTALAR_Y_PROBAR.ps1` ejecutado desde `BaseDeDatos_Amilkar` el 14/11/2026: **27 de 27 comprobaciones aprobadas**.

### 7. Disparadores (`d10c1ee`)
- **Archivos:** `07_DISPARADORES_TRIGGERS/`.
- **Qué cambió:** la guía línea por línea, la demostración de los cuatro triggers y un índice de todos los disparadores. El código no se duplica.
- **Cómo se comprobó:** prueba T05 (rechazos ORA-20120, ORA-20101 y ORA-20103, auditoría `:OLD`/`:NEW` y ROLLBACK) y P99 19/19.

### 8. Ejercicios (`4c5fc0a`)
- **Archivos:** `08_CONSULTAS_Y_EJERCICIOS/`.
- **Qué cambió:** el banco de 309 ejercicios, la guía de estudio y la clasificación por nivel (109 básicos, 106 intermedios y 94 avanzados).
- **Cómo se comprobó:** prueba G01: 309 ejercicios, 299 correctos, 10 con error intencional y 0 fallan.

### 9. Power BI (`e77f2c4`)
- **Archivos:** `09_POWER_BI/` (90 archivos). La caché local de Power BI (`.pbi`) queda fuera por `.gitignore`.
- **Qué cambió:** el PBIX verificado, el PBIP en formato PBIR, Power Query, DAX y los CSV de respaldo. Se actualizó la ruta absoluta del origen CSV y se excluyó un PBIX vacío guardado por accidente.
- **Cómo se comprobó:** B01 y B02 aprobadas. La conexión directa a Oracle sigue **PARCIAL**: falta escribir la contraseña de la cuenta lectora en Power BI.

### 10. C# y Programación III (`b32aef8`)
- **Archivos:** `10_CSHARP_VISUAL_STUDIO/` y `14_INTEGRACION_PROGRAMACION_III/`.
- **Qué cambió:** la solución académica en C# con el flujo C# → Oracle documentado, y la integración con P3, que aclara que los modelos no son idénticos (17 tablas comunes y 3 solo académicas). La rama por defecto del comparador ahora es la que se verificó.
- **Cómo se comprobó:** N01 (C# 10/10) y N02 (la ventana compila). El inventario de Oracle Cloud sigue **PENDIENTE**.

### 11. Informe definitivo (`b4d8b3e`)
- **Archivos:** `01_DOCUMENTO_FINAL/` y `18_ENTREGA_Y_RESPALDOS/fuentes_documentos/`, `generar_documentos.ps1` y `generar_todos.ps1`.
- **Qué cambió:**
  - el informe definitivo (227 páginas) parte del informe ampliado (226), que conservaba el original de 97 páginas;
  - se agregó el capítulo «Observaciones técnicas del modelo»;
  - la tabla de documentos se actualizó a la estructura nueva;
  - la generación de los 19 documentos se unificó en un solo catálogo.
- **Cómo se comprobó:** auditoría en Word de los 19 documentos (0 marcadores, 0 errores de campo, 0 imágenes rotas, 0 rutas antiguas); los cinco puntos del Aula Web están en las páginas 7, 9, 10, 13 y 23.

### 12. Guías de estudio y sustentación (`d846e5e`)
- **Archivos:** `11_GUIAS_PASO_A_PASO/`, `12_GUIA_PERSONAL_JORGE/` y `13_SUSTENTACION/`.
- **Qué cambió:**
  - guía nueva de Oracle, conexiones y errores frecuentes;
  - guías con nombres descriptivos en lugar de letras;
  - la guía personal identificada como material individual;
  - se retiraron restos de redacción de trabajo.
- **Cómo se comprobó:** los documentos se regeneraron y auditaron; revisión de secretos: 0 hallazgos.

### 13. Verificaciones y matriz (`68df0ad`)
- **Archivos:** `15_MATRIZ_DE_TRAZABILIDAD/` y `16_EVIDENCIAS_Y_PRUEBAS/`.
- **Qué cambió:** la matriz K se regeneró con el catálogo nuevo, y se agregaron las salidas reales de la última ejecución.
- **Cómo se comprobó:**
  - la matriz tiene 87 filas: 83 verificadas, 2 parciales, 1 pendiente y 1 no disponible;
  - las 12 tareas del profesor aparecen con su página real.

### 14. Manifiesto y cierre (`f1933ac`)
- **Archivos:**
  - `17_COMO_SE_CONSTRUYO/`: el documento de construcción y los registros históricos, sin modificar.
  - `18_ENTREGA_Y_RESPALDOS/`: `MANIFIESTO_FINAL.md`, este historial, la correspondencia de rutas, los archivos excluidos, el plan de reversión, el registro del respaldo Data Pump y las herramientas `empaquetar.ps1` y `reorganizar_entrega.ps1`.
  - `02_MODELOS_DE_DATOS/05_DATA_MODELER_EDITABLE/importacion_ddl.log`: el registro de la importación del DDL a Data Modeler. Se agrega de forma explícita porque el `.gitignore` general del repositorio ignora los `.log` y este es evidencia.
- **Qué cambió:** se cierra la entrega con su inventario y su trazabilidad.
- **Cómo se comprobó:** `empaquetar.ps1` generó `BaseDeDatos_Amilkar_ENTREGA_20261114_0750.zip` (local, no se publica): 661 entradas, 54,27 MB, 0 contraseñas y 0 archivos prohibidos.

### 15. Identificador del cierre (este commit)
- **Archivos:** este historial.
- **Qué cambió:** se registra el identificador real del commit 14, que no podía conocerse dentro de ese mismo commit.
- **Cómo se comprobó:** la cabeza de la rama en GitHub era `f1933aca64a67113929e99aa029905491a86dc8c` después del primer push.

## Lo que no se publica

| Qué | Dónde queda |
|---|---|
| `_LOCAL_NO_PUBLICAR/` (credenciales locales y el respaldo Data Pump `.DMP`) | Solo en el PC |
| El ZIP de entrega | Solo en el PC |
| El respaldo completo de la estructura anterior | `RESPALDOS_LOCALES/`, en el PC (987 archivos verificados con SHA-256) |

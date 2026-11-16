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
- No se borraron ramas ni se tocó `main`. El 14/11/2026, a pedido del dueño del repositorio, se reescribieron los mensajes de los commits de esta rama (todavía sin integrar) y se publicó con `--force-with-lease`; el contenido de los archivos quedó idéntico.
- Antes de cada commit se revisaron, con `git status`, los archivos preparados y se pasó `herramientas/revisar_secretos.ps1`. Este script compara las contraseñas reales sin mostrarlas y busca wallets, `.env`, claves y tokens. Resultado en todos los commits: 0 hallazgos.

**Fechas:** las de los commits son las reales de la publicación, el 14/11/2026, hora de Colombia. Son distintas de las fechas históricas de las clases, que están en `06_TAREAS_PROFESOR_AMILKAR`.

| # | Commit | Fecha (UTC−5) | Mensaje | Archivos |
|---:|---|---|---|---|
| 1 | `a675e5f` | 2026-11-14 07:48 | docs(bd): crear estructura BaseDeDatos_Amilkar e índice general | 9 |
| 2 | `45256b6` | 2026-11-14 07:48 | docs(bd): organizar requisitos de Aula Web y cronología de clases | 23 |
| 3 | `a5577d1` | 2026-11-14 07:48 | docs(bd): incorporar MER MEER DMD y diagramas relacionales | 271 |
| 4 | `a768260` | 2026-11-14 07:49 | docs(bd): integrar diccionario ampliado de 173 atributos | 36 |
| 5 | `36b01c4` | 2026-11-14 07:49 | docs(bd): documentar las 38 relaciones y el flujo de datos | 9 |
| 6 | `f792e17` | 2026-11-14 07:49 | feat(bd): incorporar scripts Oracle y prácticas PL/SQL verificadas | 69 |
| 7 | `a1d54f4` | 2026-11-14 07:49 | docs(bd): documentar triggers de EcoAccess y tareas del profesor | 4 |
| 8 | `a798839` | 2026-11-14 07:49 | docs(bd): organizar ejercicios SQL y consultas de sustentación | 11 |
| 9 | `d288f4f` | 2026-11-14 07:49 | feat(bi): incorporar informe académico y documentación Oracle | 90 |
| 10 | `a05eb7d` | 2026-11-14 07:49 | docs(bd): documentar integración CSharp y Programación III | 18 |
| 11 | `8da9d9a` | 2026-11-14 07:50 | docs(bd): publicar informe académico definitivo y anexos | 21 |
| 12 | `b06cd27` | 2026-11-14 07:50 | docs(bd): incorporar guías de estudio y sustentación | 26 |
| 13 | `ec901c9` | 2026-11-14 07:50 | test(bd): registrar verificaciones y matriz de trazabilidad | 56 |
| 14 | `7cd0f52` | 2026-11-14 07:51 | docs(bd): actualizar manifiesto y preparar entrega definitiva | 15 |
| 15 | `1b9a7cf` | 2026-11-14 | docs(bd): registrar el identificador del commit de cierre | 1 |
| 16 | `b3ef908` | 2026-11-14 14:19 | fix(bd): rechazar mediciones incompletas en ck_deposito_medido | 2 |
| 17 | `1580d4e` | 2026-11-14 14:19 | fix(bd): bloquear la fila de MATERIAL antes de registrar una tarifa | 2 |
| 18 | `ea0ec49` | 2026-11-14 14:19 | test(bd): añadir pruebas negativas y de concurrencia de la revisión | 4 |
| 19 | `b35590f` | 2026-11-14 14:19 | test(bd): registrar evidencias de la corrección y el DDL extraído | 20 |
| 20 | `a58fad2` | 2026-11-14 14:19 | docs(bd): usar el nombre completo en el material personal de estudio | 4 |
| 21 | `850fb30` | 2026-11-14 14:19 | docs(bd): actualizar diccionario, informe y guías con las correcciones | 60 |
| 22 | `24db69e` | 2026-11-14 | docs(bd): registrar la revisión del PR #12 en el historial y el manifiesto | 2 |
| 23 | `6e325a7` | 2026-11-14 14:34 | docs(bd): actualizar los identificadores de los commits | 2 |
| 24 | `a1b3a52` | 2026-11-14 15:13 | docs(bd): aclarar referencia histórica y ramas vigentes de P3 (cuenta `JorgeGutho21`) | 1 |
| 25 | `00255a5` | 2026-11-14 15:17 | docs(bd): regenerar el manifiesto con la aclaración de P3 | 1 |
| 26 | (este commit) | 2026-11-14 | docs(bd): registrar la revisión editorial final en el historial | 2 |

## Detalle por commit

### 1. Estructura e índice general (`a675e5f`)
- **Archivos:** `README.md`, `.gitignore`, `00_EMPEZAR_AQUI/` (LEEME_PRIMERO, INDICE_GENERAL en Word y PDF, ORDEN_PARA_ESTUDIAR, ORDEN_PARA_EXPONER, ESTADO_DEL_PROYECTO) y `18_ENTREGA_Y_RESPALDOS/LEEME.md`.
- **Qué cambió:** se crea la carpeta académica separada de Programación III, con un punto de entrada claro y un `.gitignore` propio que excluye credenciales, respaldos Data Pump, ZIP, wallets y salidas de compilación.
- **Por qué:** la entrega tenía 19 carpetas con nombres heredados de varias etapas y documentos identificados por letras A–O. Hacía falta un índice único y recorridos para estudiar y para exponer.
- **Cómo se comprobó:** el índice se generó en Word y se auditó (0 marcadores, 0 errores de campo); revisión de secretos: 0 hallazgos.

### 2. Requisitos y cronología (`45256b6`)
- **Archivos:** `06_TAREAS_PROFESOR_AMILKAR/` (23 archivos).
- **Qué cambió:** una carpeta por clase o tarea, con su material de origen, la solución y la evidencia. Fechas acreditadas:
  - **18–22/09/2026:** captura original del Aula Web.
  - **22/09/2026:** fecha escrita en la guía de la clase.
  - **10/11/2026:** fecha del documento de apoyo, descargado ese mismo día.

  Las demás fechas quedan como «por verificar».
- **Por qué:** el profesor revisa tareas concretas, y cada una debe poder encontrarse con su fecha y su evidencia. No se inventaron fechas, y el material de compañeros se atribuye a sus autores.
- **Cómo se comprobó:** los totales e identificadores citados de P99 (19/19; SQ01, PR01–PR06, TL01–TL02, EJ32, EJ34, CR01–CR04) se contrastaron con la salida real.

### 3. Modelos (`a5577d1`)
- **Archivos:** `02_MODELOS_DE_DATOS/` sin el flujo de datos (271 archivos, la mayoría XML del proyecto `.dmd`).
- **Qué cambió:** se separan MER, MEER, modelo relacional general, modelos por módulo, proyecto editable, evidencias y herramientas. Los scripts generadores escriben ahora en esas carpetas.
- **Por qué:** se pedían el modelo editable auténtico y las imágenes, organizados por tipo.
- **Cómo se comprobó:** `verificar_dmd.ps1` en la ruta nueva: PASA, con 19 tablas, 173 columnas y 38 FK iguales a Oracle.

### 4. Diccionario ampliado (`a768260`)
- **Archivos:** `03_DICCIONARIO_DE_DATOS/` (36 archivos).
- **Qué cambió:** guía de los 173 atributos (182 páginas), fichas de 24 campos, verificación atributo por atributo, matriz de atributos frente a Oracle, fuentes y generadores.
- **Por qué:** cada atributo debe poder defenderse ante el profesor: qué significa, por qué existe, qué guarda, qué regla lo protege, quién lo modifica, quién lo consulta y cómo comprobarlo.
- **Cómo se comprobó:** `generar_diccionario_ampliado.ps1` confirma 173 de 173 atributos (entre 90 y 165 palabras y al menos 5 oraciones cada uno) y 19 de 19 tablas. Las pruebas D09 y D10 del instalador confirman diccionario = Oracle.

### 5. Relaciones y flujo (`36b01c4`)
- **Archivos:** `03_DICCIONARIO_DE_DATOS/ampliado/` (`relaciones.txt`, `restricciones_oracle.txt`, `salida/relaciones_38.html`, `salida/pk_19.html`) y `02_MODELOS_DE_DATOS/06_FLUJO_DE_DATOS/`.
- **Qué cambió:** las 38 FK con su explicación, su cardinalidad calculada con las restricciones reales y el flujo de los datos.
- **Por qué:** el MER (19 relaciones) y el esquema físico (38 FK) miden cosas distintas, y había que explicar cada FK por separado.
- **Cómo se comprobó:** 38 FK leídas de Oracle (32 simples y 6 compuestas), todas explicadas; el generador se detiene si falta alguna.

### 6. Scripts Oracle y PL/SQL (`f792e17`)
- **Archivos:** `04_SCRIPTS_ORACLE/`, `05_SQL_Y_PLSQL/`, `INSTALAR_Y_PROBAR.ps1` y `18_ENTREGA_Y_RESPALDOS/herramientas/` (`comun.ps1`, `ejecutar_sql.ps1`, `revisar_secretos.ps1`).
- **Qué cambió:**
  - el SQL queda en orden de dependencias, con un LEEME por categoría;
  - las prácticas verificadas se acompañan de 16 carpetas temáticas generadas desde el banco de ejercicios, con su salida real;
  - las rutas internas del instalador se ajustaron a la estructura nueva.
- **Por qué:** la instalación debía seguir funcionando después de la reorganización.
- **Cómo se comprobó:** `INSTALAR_Y_PROBAR.ps1` ejecutado desde `BaseDeDatos_Amilkar` el 14/11/2026: **27 de 27 comprobaciones aprobadas**.

### 7. Disparadores (`a1d54f4`)
- **Archivos:** `07_DISPARADORES_TRIGGERS/`.
- **Qué cambió:** la guía línea por línea, la demostración de los cuatro triggers y un índice de todos los disparadores. El código no se duplica.
- **Cómo se comprobó:** prueba T05 (rechazos ORA-20120, ORA-20101 y ORA-20103, auditoría `:OLD`/`:NEW` y ROLLBACK) y P99 19/19.

### 8. Ejercicios (`a798839`)
- **Archivos:** `08_CONSULTAS_Y_EJERCICIOS/`.
- **Qué cambió:** el banco de 309 ejercicios, la guía de estudio y la clasificación por nivel (109 básicos, 106 intermedios y 94 avanzados).
- **Cómo se comprobó:** prueba G01: 309 ejercicios, 299 correctos, 10 con error intencional y 0 fallan.

### 9. Power BI (`d288f4f`)
- **Archivos:** `09_POWER_BI/` (90 archivos). La caché local de Power BI (`.pbi`) queda fuera por `.gitignore`.
- **Qué cambió:** el PBIX verificado, el PBIP en formato PBIR, Power Query, DAX y los CSV de respaldo. Se actualizó la ruta absoluta del origen CSV y se excluyó un PBIX vacío guardado por accidente.
- **Cómo se comprobó:** B01 y B02 aprobadas. La conexión directa a Oracle sigue **PARCIAL**: falta escribir la contraseña de la cuenta lectora en Power BI.

### 10. C# y Programación III (`a05eb7d`)
- **Archivos:** `10_CSHARP_VISUAL_STUDIO/` y `14_INTEGRACION_PROGRAMACION_III/`.
- **Qué cambió:** la solución académica en C# con el flujo C# → Oracle documentado, y la integración con P3, que aclara que los modelos no son idénticos (17 tablas comunes y 3 solo académicas). La rama por defecto del comparador ahora es la que se verificó.
- **Cómo se comprobó:** N01 (C# 10/10) y N02 (la ventana compila). El inventario de Oracle Cloud sigue **PENDIENTE**.

### 11. Informe definitivo (`8da9d9a`)
- **Archivos:** `01_DOCUMENTO_FINAL/` y `18_ENTREGA_Y_RESPALDOS/fuentes_documentos/`, `generar_documentos.ps1` y `generar_todos.ps1`.
- **Qué cambió:**
  - el informe definitivo (227 páginas) parte del informe ampliado (226), que conservaba el original de 97 páginas;
  - se agregó el capítulo «Observaciones técnicas del modelo»;
  - la tabla de documentos se actualizó a la estructura nueva;
  - la generación de los 19 documentos se unificó en un solo catálogo.
- **Cómo se comprobó:** auditoría en Word de los 19 documentos (0 marcadores, 0 errores de campo, 0 imágenes rotas, 0 rutas antiguas); los cinco puntos del Aula Web están en las páginas 7, 9, 10, 13 y 23.

### 12. Guías de estudio y sustentación (`b06cd27`)
- **Archivos:** `11_GUIAS_PASO_A_PASO/`, `12_GUIA_PERSONAL_JORGE/` y `13_SUSTENTACION/`.
- **Qué cambió:**
  - guía nueva de Oracle, conexiones y errores frecuentes;
  - guías con nombres descriptivos en lugar de letras;
  - la guía personal identificada como material individual;
  - se retiraron restos de redacción de trabajo.
- **Cómo se comprobó:** los documentos se regeneraron y auditaron; revisión de secretos: 0 hallazgos.

### 13. Verificaciones y matriz (`ec901c9`)
- **Archivos:** `15_MATRIZ_DE_TRAZABILIDAD/` y `16_EVIDENCIAS_Y_PRUEBAS/`.
- **Qué cambió:** la matriz K se regeneró con el catálogo nuevo, y se agregaron las salidas reales de la última ejecución.
- **Cómo se comprobó:**
  - la matriz tiene 87 filas: 83 verificadas, 2 parciales, 1 pendiente y 1 no disponible;
  - las 12 tareas del profesor aparecen con su página real.

### 14. Manifiesto y cierre (`7cd0f52`)
- **Archivos:**
  - `17_COMO_SE_CONSTRUYO/`: el documento de construcción y los registros históricos, sin modificar.
  - `18_ENTREGA_Y_RESPALDOS/`: `MANIFIESTO_FINAL.md`, este historial, la correspondencia de rutas, los archivos excluidos, el plan de reversión, el registro del respaldo Data Pump y las herramientas `empaquetar.ps1` y `reorganizar_entrega.ps1`.
  - `02_MODELOS_DE_DATOS/05_DATA_MODELER_EDITABLE/importacion_ddl.log`: el registro de la importación del DDL a Data Modeler. Se agrega de forma explícita porque el `.gitignore` general del repositorio ignora los `.log` y este es evidencia.
- **Qué cambió:** se cierra la entrega con su inventario y su trazabilidad.
- **Cómo se comprobó:** `empaquetar.ps1` generó `BaseDeDatos_Amilkar_ENTREGA_20261114_0750.zip` (local, no se publica): 661 entradas, 54,27 MB, 0 contraseñas y 0 archivos prohibidos.

### 15. Identificador del cierre (`1b9a7cf`)
- **Archivos:** este historial.
- **Qué cambió:** se registra el identificador real del commit 14, que no podía conocerse dentro de ese mismo commit.
- **Cómo se comprobó:** la cabeza de la rama en GitHub era `7cd0f52d5f4d6a6c455a4489b6955ab19bbce31d` después del primer push.

## Revisión técnica del Pull Request #12 (commits 16 a 22)

La revisión automática de código del PR #12 dejó dos observaciones P2 sobre el esquema. Antes de cambiar Oracle se hizo un respaldo Data Pump nuevo. Cada defecto se reprodujo con una prueba que fallaba, se corrigió y se repitió toda la regresión. Los dos hilos de revisión se respondieron con la explicación técnica y se marcaron como resueltos, sin borrar ningún comentario.

### 16. Medición completa en `CK_DEPOSITO_MEDIDO` (`b3ef908`)
- **Archivos:** `04_SCRIPTS_ORACLE/03_restricciones.sql` y la tabla `DEPOSITO` dentro del `.dmd`.
- **Qué cambió:** la rama de la medición exige `IS NOT NULL` en el peso, el instrumento y la precisión. Un CHECK que da UNKNOWN se acepta en Oracle, y antes `precision_medicion_g > 0` con un NULL daba UNKNOWN.
- **Cómo se comprobó:** 0 depósitos violan la regla nueva; `verificar_dmd.ps1` PASA.

### 17. Bloqueo de la fila de `MATERIAL` en `REGISTRAR_REGLA` (`1580d4e`)
- **Archivos:** `07_procedimientos_funciones_paquetes.sql` y el nuevo `19_correccion_revision_pr12.sql`.
- **Qué cambió:** el procedimiento bloquea la fila padre antes de revisar las tarifas y responde ORA-20207 si el material no existe. El script 19 aplica las dos correcciones a un esquema ya instalado sin borrar datos.
- **Cómo se comprobó:** aplicado en `ECOACCESS_BD` local; 0 objetos inválidos.

### 18. Pruebas negativas y de concurrencia (`ea0ec49`)
- **Archivos:** `12_pruebas.sql`, `20_prueba_concurrencia_tarifas.sql`, `herramientas/prueba_concurrencia_tarifas.ps1` e `INSTALAR_Y_PROBAR.ps1` (comprobación T06).
- **Qué cambió:** 10 pruebas nuevas (P144–P153) y una prueba con dos sesiones reales de SQL*Plus.
- **Cómo se comprobó:** antes de corregir fallaban P144, P145 y P152, y la sesión B registraba en 0,0 s. Después: 163/163, T06 PASA (B espera unos 4 s) y el instalador aprueba 28 de 28.

### 19. Evidencias y DDL (`b35590f`)
- **Archivos:** `16_EVIDENCIAS_Y_PRUEBAS/` (incluida `salidas/revision_pr12/`), `DDL_FINAL_EXTRAIDO.sql`, los resultados del banco de ejercicios y el registro del respaldo del 14/11/2026. El registro es un `.log` y se agrega de forma explícita.
- **Cómo se comprobó:** revisión de secretos, 0 hallazgos en 20 archivos.

### 20. Nombre completo en el material personal (`a58fad2`)
- **Archivos:** `generar_documentos.ps1` y los LEEME de `00`, `06` y `12`.
- **Qué cambió:** la portada de la guía personal dice «Guía personal de estudio de Jorge Alfonso Gutiérrez Thomas». Los documentos colectivos siguen con los tres integrantes.
- **Revisión editorial:** se buscaron menciones de herramientas de redacción, instrucciones a un asistente y nombres temporales en los textos, en los Word y en los nombres de archivo. No quedaban restos. Las menciones de «asistente» corresponden al asistente Eco de EcoAccess o al asistente de importación de Data Modeler, y «chat» corresponde al chat de Telegram. Los registros de `17_COMO_SE_CONSTRUYO/historial/` se conservan tal como se escribieron, porque son evidencia histórica.

### 21. Documentación actualizada (`850fb30`)
- **Archivos:** 60, entre ellos el informe final (capítulo «Revisión técnica del Pull Request y correcciones»), el diccionario y sus metadatos, la guía de triggers, la guía personal, el manual de sustentación, la guía de construcción, el índice, la matriz K y el estado del proyecto.
- **Cómo se comprobó:** 12 documentos Word y PDF regenerados y auditados (0 marcadores, 0 errores de campo, 0 conteos antiguos). La matriz tiene 109 filas, y los cinco puntos del Aula Web siguen en las páginas 7, 9, 10, 13 y 23.

### 22. Historial y manifiesto (`24db69e`)
- **Archivos:** este historial y `MANIFIESTO_FINAL.md`, generado por `empaquetar.ps1` junto con el ZIP local.
### 23. Identificadores actualizados (`6e325a7`)
- **Qué cambió:** el 14/11/2026 se reescribieron los mensajes de los commits 1 a 22 de esta rama, que todavía no estaba integrada a `main`; el contenido de los archivos no cambió (el árbol final es idéntico). Por eso cambiaron los identificadores, y este historial y el manifiesto usan los nuevos.

### 24. Aclaración de la referencia histórica de P3 (`a1b3a52`)
- **Autor:** Jorge Gutierrez, desde la cuenta `JorgeGutho21`.
- **Qué cambió:** `14_INTEGRACION_PROGRAMACION_III/LEEME.md` explica que `1fa52ac` identifica la copia histórica de P3 usada en la comparación del 11/11/2026. También indica que no debe sustituirse sin una nueva verificación. Por eso las evidencias de noviembre conservan ese identificador.

### 25. Manifiesto (`00255a5`)
- **Qué cambió:** el inventario SHA-256 se regeneró después del commit 24.

### 26. Revisión editorial final (`fe7df02` y `a116be9`)
- **Ramas revisadas:** `main`, `desarrollo/auditoria-tercer-corte`, `desarrollo/mision-nocturna-ia`, `desarrollo/telegram-doble-ia`, `recovery/ecoaccess-oracle-cloud` y esta rama.
- **Qué se revisó:** archivos de texto, scripts, Word, PDF, PowerPoint y Excel. Los Word y PDF se revisaron con su texto extraído.
- **Cambios publicados en las ramas de P3, con commits nuevos y push normal:**
  - notas de autoría y de operación redactadas sin mencionar herramientas de redacción: `4023d2a`, `15774ed`, `e53ac1e` y `9789d44`;
  - en `14_GITHUB_INSTALACION_Y_OPERACION`, el Markdown, el Word y el PDF exportado de nuevo;
  - el nombre de la sesión anterior del bot en `docs/telegram/DIAGNOSTICO_TELEGRAM.md`: `da85e67`, `471a146`, `0bf18c5` y `c5b24f7`.
- **Lo que se conserva y por qué:**
  - **Componentes del sistema:** el modelo de visión, Ollama, Groq (su API se llama `api.groq.com/openai/v1`), la palabra *prompt* del modelo y el retiro de Gemini, OpenAI y ONNX describen componentes reales de EcoAccess.
  - **Oracle Cloud:** `ECOACCESS_CODEX_IT` es el nombre real de un esquema.
  - **Registros históricos:** los de `17_COMO_SE_CONSTRUYO/historial/` y el registro de progreso de P3 están fechados y se conservan como se escribieron.
  - **Material de terceros:** el PDF de clase de Diego Gómez no se modifica.
  - **GitHub:** los comentarios de revisión de la cuenta del bot se conservan con sus respuestas.

### 27. Guías de descarga y de la tarea de triggers (`baef4b5` a `ef3152f`)
- **Autor:** `JorgeGutho21`, desde la web de GitHub, el 15/11/2026 entre las 08:59 y las 09:00.
- **Qué cambió:** se agregaron `00_EMPEZAR_AQUI/GUIA_DESCARGAS_Y_PRIMER_USO.md` y `06_TAREAS_PROFESOR_AMILKAR/TAREA_DISPARADORES_CUATRO_PUNTOS/GUIA_PRACTICA_TRIGGERS_Y_ENTREGA.md`, y se enlazaron desde el README, `LEEME_PRIMERO.md` y los léeme de la carpeta 06 (seis commits).
- **Cómo se integró:** la copia local se puso al día con un avance rápido y los cambios se combinaron con los de la carpeta de OneDrive sin conflictos. Los enlaces al informe con su nombre anterior se actualizaron en los commits 32 y 33.

### 28. Biblioteca de modelos visuales (`7ab087d`)
- **Qué cambió:** se creó `19_MODELOS_VISUALES` con copias exactas de los diagramas (SHA-256 en `ORIGENES_Y_HASHES.csv`) y un PDF horizontal por diagrama: MER, MEER, relacional completo, cinco módulos, siete exportaciones de Data Modeler y flujo.
- **Corrección:** al SVG del flujo de datos le faltaba la etiqueta `</svg>`; se cerró en el original y en su generador.

### 29. Guías para abrir el DMD y el DDL (`5bd2fe4`)
- **Qué cambió:** guías en Markdown y PDF para abrir el DMD en SQL Developer y en Data Modeler, y para ver el DDL y generar un modelo de prueba desde él.

### 30. Atlas visual y evidencias (`839779c`)
- **Qué cambió:** atlas de 35 páginas, índice visual, galería HTML sin Internet y 24 capturas reales de SQL Developer del 15/11/2026.

### 31. Informe final APA 7 (`8cbe743`)
- **Qué cambió:** `01_DOCUMENTO_FINAL/01_INFORME_FINAL_ECOACCESS_APA7` (Word y PDF, 40 páginas): primero los cinco puntos del Aula Web y después los capítulos 6 a 13. Se agregaron su fuente y su generador.

### 32. Documento técnico detallado (`4033d72`)
- **Qué cambió:** el informe ampliado pasó a llamarse `02_DOCUMENTO_DETALLADO_ECOACCESS` (229 páginas). Su contenido se conserva; cambiaron el nombre, la portada y un párrafo que explica su relación con el informe APA 7.

### 33. Índices y trazabilidad (`ddb200e`)
- **Qué cambió:** README, `LEEME_PRIMERO.md`, estado del proyecto, índice general, guía paso a paso, léeme de sustentación y de la carpeta 06, y la matriz K (115 filas, con cinco filas nuevas sobre el informe APA 7, la biblioteca visual y la apertura del DMD).

### 34. Consolidación y manifiesto (`2662522`)
- **Qué cambió:** `CONSOLIDACION_CARPETAS_20261115.md` explica qué carpetas locales se retiraron a `RESPALDOS_LOCALES` y cómo se comprobaron; este historial, el ZIP y el manifiesto se regeneraron.

### 35. Carpeta 20 · Abrir los modelos en Oracle (16/11/2026)
- **Qué cambió:** se creó `20_ABRIR_MODELOS_EN_ORACLE`:
  - el DMD completo con su carpeta;
  - 5 DMD independientes por módulo, creados en SQL Developer importando el DDL de cada módulo;
  - el DDL completo y por módulo (con advertencia de no ejecutar y sin sentencias destructivas);
  - tres guías en Markdown y PDF, el índice PDF, 36 capturas reales, scripts de generación y verificación, y el informe de pruebas.
- **Pruebas:** `verificar_carpeta20.ps1` dio 18/18, también desde un ZIP extraído en otra ubicación, donde el DMD se abrió en SQL Developer.

### 36. Carpeta 21 · Guía de SQL, PL/SQL y disparadores (16/11/2026)
- **Qué cambió:** se creó `21_GUIA_SQL_PLSQL_Y_TRIGGERS`:
  - 16 temas con 12 puntos cada uno;
  - los 4 triggers explicados línea por línea;
  - la tarea del profesor enlazada sin duplicarla;
  - los 4 paquetes;
  - 15 consultas de «qué residuos deposita cada usuario»;
  - 12 ejercicios progresivos y la guía de errores;
  - el manual completo en PDF.
- **Pruebas:** todos los scripts se ejecutaron en SQL Developer (F5) sobre `ECOACCESS_BD`, en solo lectura, con 0 errores; el tema 10 se ejecutó en `ECOACCESS_PRACTICAS`. Las salidas reales y 5 capturas están en `15_RESULTADOS_Y_CAPTURAS`. Gracias a la ejecución real se corrigió el control de créditos duplicados del tema 13 (el depósito 5 tiene crédito, reverso y crédito nuevo).

### 37. Índices, trazabilidad y manifiesto de las carpetas 20 y 21 (16/11/2026)
- **Qué cambió:**
  - se agregaron los accesos «QUIERO ABRIR LOS MODELOS EN ORACLE → CARPETA 20» y «QUIERO ESTUDIAR Y EJECUTAR SQL/PLSQL/TRIGGERS → CARPETA 21» en README, `00_EMPEZAR_AQUI`, `19_MODELOS_VISUALES` y `13_SUSTENTACION`;
  - se actualizaron el estado del proyecto y la matriz K (tres filas nuevas o actualizadas);
  - se regeneraron este historial, el manifiesto y el ZIP 00–21.
## Lo que no se publica

| Qué | Dónde queda |
|---|---|
| `_LOCAL_NO_PUBLICAR/` (credenciales locales y el respaldo Data Pump `.DMP`) | Solo en el PC |
| El ZIP de entrega | Solo en el PC |
| El respaldo completo de la estructura anterior | `RESPALDOS_LOCALES/`, en el PC (987 archivos verificados con SHA-256) |

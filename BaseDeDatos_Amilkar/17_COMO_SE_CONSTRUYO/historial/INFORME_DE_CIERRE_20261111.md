# Informe de cierre — EcoAccess Base de Datos (profesor Amílkar) — 11/11/2026, noche

**Estados:**

- **VERIFICADO:** abierto, ejecutado y comprobado en este PC, con evidencia.
- **PARCIAL:** hecho, pero falta una comprobación concreta.
- **PENDIENTE:** no se pudo comprobar; se dice por qué.

La versión anterior de este informe (de las 14:17) está en `99_VERSIONES_ANTERIORES/cierre_20261111_noche/fuentes/`.

## 1. Punto de partida

Se trabajó sobre la carpeta permanente de OneDrive (`EcoAccessBD_Exposoftware`), que era la versión más reciente: documentos A–O de las 19:51, DMD, PBIX, matriz K y respaldo Data Pump.

Antes de cambiar nada se copió todo lo que se iba a tocar a `99_VERSIONES_ANTERIORES/`:

- `cierre_20261111_noche/` con fuentes, documentos A–O, K, diccionario, matriz P3 y la evidencia de C#;
- `PBIP_y_PBIX_CSV_antes_de_oracle_directo_20261111/`.

No se reinstaló ni se borró ningún esquema.

## 2. Qué se corrigió

### Revisión de los Word/PDF A–O

Se auditaron con Word (texto, rutas, imágenes, tablas, títulos y páginas) y se revisaron visualmente páginas de muestra en PDF.

- **Imágenes fuera del margen** en A, B, C, E, H y O: 10 en A, hasta 480 pt en un ancho útil de 442 pt.
  - Causa: Word no respetaba los márgenes de `@page`.
  - Arreglo: el generador fija los márgenes y ajusta cada imagen al ancho real. Auditoría final: 0 imágenes grandes.
- **La portada llevaba encabezado y número de página.** Ahora no los lleva.
- **Rutas largas en `<code>`** estiraban el texto justificado. Se agregaron puntos de corte invisibles.
- **Ruta equivocada del DMD** (`02_MODELOS_MER_MEER_RELACIONAL/EcoAccess_BD.dmd`, sin `DATA_MODELER_DMD/`). Se corrigió en las guías B y E, en el paso a paso, en el checklist y en `verificar_dmd.ps1`.
- **El informe A decía «no hace falta un MEER»,** aunque incluye el MEER. Se corrigió.

### Informe A según el Aula Web

- **Parte I:** los 5 puntos en el orden exigido (1 Introducción, 2 Objetivos, 3 MER y MEER, 4 Modelo relacional, 5 Diccionario).
- **Parte II:** todo lo complementario, separado.
- La tabla inicial trae capítulo y **página real calculada por Word** (campos PAGEREF): 7, 9, 10, 13 y 23, más el Anexo A en la 55.
- Las referencias a capítulos dentro del texto también se calculan (`[[CAP:…]]`).

### Diccionario C

En las 173 fichas, el campo «Para qué sirve» ahora explica el papel de la columna:

- si es PK, que identifica cada fila;
- si es FK, a qué tabla la une;
- si no es clave, qué dato guarda.

Antes solo decía «Se usa en: …».

### Instalador

- **B02** ya no regenera el PBIP (eso habría borrado el formato PBIR, el glosario y el origen Oracle). Ahora comprueba su estructura.
- **N03** (capturas de la ventana C#, que insertan un empleado de demostración) solo corre con `-CapturasCSharp`.

### C#

La prueba de consola ya no deja un empleado nuevo en cada ejecución:

- reutiliza su fila de prueba (`PRUEBACS`, id 1004);
- prueba el INSERT dentro de una transacción con ROLLBACK.

Quedan 9 filas de prueba de ejecuciones anteriores (ids 1004–1080). **No se borraron:** la práctica es «sin DELETE», y borrarlas requiere su autorización.

### Generador de documentos

`generar_todos.ps1 -Solo A,C` no regeneraba nada, porque `pwsh -File` entrega la lista como un solo texto. Se corrigió.

## 3. Qué se agregó

- **Power BI con Oracle directo:** `13_POWER_BI/agregar_origen_oracle.ps1`.
  - Parámetros `OrigenDatos` (ORACLE/CSV) y `ServidorOracle`.
  - Las 5 tablas navegan a las vistas `VW_BI_*` con la cuenta `ECOACCESS_BD_LECTOR`; los CSV quedan como respaldo.
- **Guía H:** capítulo «Origen de datos: Oracle directo, con respaldo CSV», con pasos, riesgos y estado.
- **Matriz K:** 7 hojas.
  - **Resumen**, con fórmulas `CONTAR.SI` que se recalculan.
  - Aula Web, Requisitos y Contenidos.
  - **Tareas del profesor:** documento, página real, script, carpeta, prueba y estado.
  - **Documentos A–O:** páginas, DOCX y PDF.
  - **Cómo leer:** significado de cada columna.
  - Además, 5 CSV.
- **P3:**
  - `18_INTEGRACION_P3/VERIFICACION_GITHUB_Y_NUBE_20261111.md`;
  - la matriz P3 vs académico, regenerada contra la rama actual;
  - `ejecutar_inventario_cloud.ps1` (inventario de la nube en solo lectura; la contraseña se escribe en la terminal).

## 4. Pruebas ejecutadas y resultados

| Prueba | Resultado |
|---|---|
| Instalador completo sin reinstalar (11/11/2026, 20:3x) | **27 de 27 PASAN**: 153/153 esquema · 19/19 prácticas · 10/10 lector · 22 consultas · 4 triggers (T05) · DDL = scripts = diccionario (D01–D10) · 0 inválidos · 309 ejercicios (299 + 10 intencionales, 0 fallan) · C# 10/10 · WinForms compila · CSV del lector · Power BI íntegro — VERIFICADO |
| `verificar_dmd.ps1` | 19 tablas, 173 columnas y 38 FK = Oracle — VERIFICADO |
| Lector en SQL*Plus | Lee VW_BI_DEPOSITOS (105, 76 verificados), VW_BI_CONSULTAS_VISION (187), VW_BI_ECOPUNTOS (48 movimientos, suma 675), VW_BI_AUDITORIA (91) y VW_BI_CIUDADANOS (51). USUARIO da ORA-00942 — VERIFICADO |
| Auditoría Word de A–O | 0 marcadores sin resolver; 0 imágenes fuera del margen. Las rutas «no encontradas» que quedan son nombres sin extensión, no errores — VERIFICADO |
| Matriz K | Resumen calculado: 84 filas · 80 verificadas · 2 parciales · 1 pendiente · 1 no disponible — VERIFICADO |

## 5. Power BI

- **El PBIX entregado** (`13_POWER_BI/ARCHIVO_PBIX/EcoAccess_BD.pbix`) es el verificado en pantalla con los CSV de la cuenta lectora: 5 páginas, filtros, glosario, guardado y reapertura. **VERIFICADO.**
- **Oracle directo: PARCIAL.**
  - Al pulsar Actualizar, el conector llegó al servidor.
  - Oracle respondió ORA-01017: Power BI tenía guardada una contraseña antigua del lector.
  - Con los reintentos de las 5 tablas, la cuenta se **bloqueó** (ORA-28000, perfil DEFAULT: 10 intentos fallidos). Se desbloqueó como administrador local con `ALTER USER ECOACCESS_BD_LECTOR ACCOUNT UNLOCK`. No se cambiaron ni la contraseña ni el perfil.
  - Se dejó abierta la ventana de credenciales con el campo de contraseña vacío para que usted la escribiera; no se escribió.
  - Después, las ventanas de Power BI dejaron de redibujarse en las capturas de pantalla, así que no se pudo terminar el paso de credenciales.
- **Falta:** abrir el `.pbip` → Transformar datos → clic en «Ciudadanos» → Editar credenciales → Base de datos → `ECOACCESS_BD_LECTOR` + contraseña → Cerrar y aplicar → Actualizar → Guardar como `.pbix`. El paso a paso está en la guía H.

## 6. Oracle Cloud y GitHub (Programación III)

- **GitHub** (solo lectura; sin commits ni push): **VERIFICADO**.
  - `main` llega a V017.
  - `desarrollo/telegram-doble-ia` (PR #11 abierto, commit `1fa52ac`) y `desarrollo/auditoria-tercer-corte` llegan a **V025**.
  - El informe anterior («V022–V023 sin versionar») quedó desactualizado.
  - P3 tiene 29 tablas en scripts y 17 en común con el modelo académico. `CONSULTA_VISION` existe en P3 desde V023 y se crea con SQL dinámico.
  - Diferencia de nombres de rol: el PR #11 de P3 habla de «Supervisor» y el modelo académico usa «Administrador». Debe aclararse con el equipo de P3.
- **Inventario de la nube: PENDIENTE.**
  - `ejecutar_inventario_cloud.ps1` se lanzó en la terminal a las 20:09: sesión `SET TRANSACTION READ ONLY`, solo SELECT.
  - Quedó esperando la contraseña de `ECOACCESS_P3_APP`, que no se escribió.
  - No se modificó nada en la nube.

## 7. Lo que sigue sin comprobar (y qué hace falta)

1. **Power BI con Oracle directo:** que usted escriba la contraseña del lector en Power BI (sección 5).
2. **Inventario de Oracle Cloud:** `pwsh -File 18_INTEGRACION_P3\ejecutar_inventario_cloud.ps1` y escribir la contraseña de ECOACCESS_P3_APP.
3. **Capturas de la hoja de trabajo de SQL Developer** conectada: requieren la contraseña.
4. **Prueba de restauración** del respaldo Data Pump: no se ejecutó (es destructiva sobre el esquema y requiere autorización).
5. **Limpieza de las 9 filas de prueba de C#:** solo con su autorización.
6. **Hardware ESP32:** fuera del alcance de Base de Datos.
7. **Rama de Git y commits:** sin autorización; no se hizo ninguno.
8. **Rúbrica final del docente:** no se encontró una posterior al Aula Web.

## 7 bis. Ampliación del diccionario (13 y 14/11/2026)

**Qué se generó.** En `00_DOCUMENTOS_FINALES_WORD_PDF/AMPLIADOS_173_ATRIBUTOS/` hay cuatro entregables:

- el informe A ampliado (226 páginas);
- la guía completa de los 173 atributos y las relaciones (181 páginas);
- el banco de 43 preguntas de sustentación (14 páginas);
- el registro de verificación `ECOACCESS_VERIFICACION_DICCIONARIO_173_ATRIBUTOS.md`.

**Estado: VERIFICADO.**

- `05_DICCIONARIO_DE_DATOS/generar_diccionario_ampliado.ps1` comprueba los 173 atributos, las 19 tablas y las 38 FK contra los metadatos de Oracle.
- Las 12 consultas SQL de ejemplo se ejecutaron en Oracle sin errores.
- Los documentos se auditaron en Word y se revisaron páginas de muestra.

**Qué se tocó y qué no.**

- No se modificó el esquema, ningún trigger ni ningún paquete.
- El informe A original queda igual: las páginas del Aula Web siguen siendo 7, 9, 10, 13 y 23.
- El generador compartido `07_INFORME_FINAL_WORD_PDF/generar_documentos.ps1` ahora incluye el estilo de las fichas y ajusta las tablas al ancho de la página. Los documentos A–O no se han regenerado con este cambio.

**Discrepancias documentadas sin cambiar nada** (detalle en el registro de verificación):

- `AUDITORIA_DEPOSITO.ID_ACTOR` no tiene FK, mientras que `HISTORIAL_ESTADO_CONTENEDOR.ID_ACTOR` sí la tiene.
- Ninguna restricción impide vigencias solapadas en REGLA_ECOPUNTOS; solo lo controla el procedimiento.
- El peso estimado de CONSULTA_VISION no se valida contra el rango del catálogo.
- El texto breve «Ventana de 10 s» no coincide con los 2 minutos que muestran los datos.

**Matriz K.** Se regeneró: 87 filas (83 verificadas, 2 parciales, 1 pendiente y 1 no disponible) y 18 documentos, entre ellos los tres nuevos.

## 8. Qué abrir primero

- **Para estudiar:**
  1. `LEEME_PRIMERO.md`;
  2. `00_DOCUMENTOS_FINALES_WORD_PDF/L_GUIA_PRACTICA_PERSONAL_DE_JORGE_ECOACCESS_DESDE_CERO.pdf`;
  3. después O (modelo y flujo) y C (atributos).
- **Para exponer:**
  1. A (Parte I, páginas 7–23);
  2. M (manual de sustentación);
  3. la hoja «Tareas del profesor» de `15_MATRIZ_TRAZABILIDAD/K_MATRIZ_TRAZABILIDAD_FINAL.xlsx`, para abrir de inmediato cualquier tarea con su página.

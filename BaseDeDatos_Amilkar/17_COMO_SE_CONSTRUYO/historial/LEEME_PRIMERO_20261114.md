# EcoAccess — Proyecto Final de Base de Datos — Profesor Amílkar

**Universidad Popular del Cesar · Asignatura: Base de Datos**
EcoAccess — Sistema Inteligente de Gestión y Clasificación de Residuos.

**Integrantes:**
- Jorge Alfonso Gutiérrez Thomas
- Heyder Hedrey Canchila de Ángel
- Saray Marcela Cadena Palomino

## Empieza aquí

| Quiero… | Abre o ejecuta |
|---|---|
| **Exponer**: el documento que se entrega al profesor (los 5 puntos del Aula Web) | `00_DOCUMENTOS_FINALES_WORD_PDF/A_INFORME_FINAL_BASE_DE_DATOS_AMILKAR.pdf` (o `.docx`) |
| **Exponer**: cómo exponer, qué mostrar y dónde está cada tarea | `00_DOCUMENTOS_FINALES_WORD_PDF/M_MANUAL_DE_SUSTENTACION_COMO_EXPONER.pdf` |
| **Estudiar** todo desde cero, clic por clic | `00_DOCUMENTOS_FINALES_WORD_PDF/L_GUIA_PRACTICA_PERSONAL_DE_JORGE_ECOACCESS_DESDE_CERO.pdf` |
| **Estudiar** cómo se lee el modelo y el flujo de los datos | `00_DOCUMENTOS_FINALES_WORD_PDF/O_INTERPRETACION_MODELO_RELACIONAL_Y_FLUJO_DE_DATOS.pdf` |
| **Estudiar** qué significa cada uno de los 173 atributos, las 38 FK y los 4 triggers | `00_DOCUMENTOS_FINALES_WORD_PDF/AMPLIADOS_173_ATRIBUTOS/ECOACCESS_GUIA_COMPLETA_173_ATRIBUTOS_Y_RELACIONES.pdf` |
| **Practicar** las preguntas del profesor sobre el modelo | `00_DOCUMENTOS_FINALES_WORD_PDF/AMPLIADOS_173_ATRIBUTOS/ECOACCESS_PREGUNTAS_SUSTENTACION_MODELO_RELACIONAL.pdf` |
| Ver el informe A con el diccionario ampliado | `00_DOCUMENTOS_FINALES_WORD_PDF/AMPLIADOS_173_ATRIBUTOS/A_INFORME_FINAL_BASE_DE_DATOS_AMILKAR_AMPLIADO.pdf` |
| Ver la matriz de trazabilidad (K) | `15_MATRIZ_TRAZABILIDAD/K_MATRIZ_TRAZABILIDAD_FINAL.xlsx` |
| Saber qué está verificado y qué falta | `INFORME_DE_CIERRE_20261111.md`, `MANIFIESTO_FINAL.md` y la hoja «Resumen» de la matriz K |
| Instalar y probar la base | PowerShell 7 en esta carpeta: `pwsh -File .\INSTALAR_Y_PROBAR.ps1` |

## Los documentos (Word y PDF, en `00_DOCUMENTOS_FINALES_WORD_PDF/`)

| Letra | Documento |
|---|---|
| A | Informe final principal (5 puntos del Aula Web + anexos) |
| B | Guía de estudio completa (309 ejercicios) |
| C | Diccionario de datos explicado atributo por atributo (173 fichas de 24 puntos) |
| D | Guía de SQL Developer desde cero |
| E | Guía de Oracle Data Modeler (.dmd ordenado por módulos) |
| F | Guía de SQL y PL/SQL |
| G | Guía de triggers del profesor |
| H | Guía de Power BI + Oracle |
| I | Guía de C# + Oracle y relación con P3 |
| J | Manual técnico de instalación y recuperación |
| K | Matriz de trazabilidad (Excel, en `15_MATRIZ_TRAZABILIDAD/`) |
| L | Guía práctica personal de Jorge |
| M | Manual de sustentación — cómo exponer |
| N | Cómo se construyó, paso a paso |
| O | Interpretación del modelo relacional y flujo de los datos |

**Ampliación del diccionario (13/11/2026)**, en `00_DOCUMENTOS_FINALES_WORD_PDF/AMPLIADOS_173_ATRIBUTOS/`:

- **Informe A ampliado.** Es el mismo informe A, con el Anexo A reescrito (19 tablas explicadas y 173 fichas) y dos anexos nuevos: A.1 con las 38 FK y A.2 con los 4 triggers. El informe A original no cambió.
- **Guía completa.** Explica los 173 atributos y las relaciones en las Partes I a X.
- **Banco de preguntas.** Tiene 43 preguntas de sustentación con su respuesta.
- **`ECOACCESS_VERIFICACION_DICCIONARIO_173_ATRIBUTOS.md`.** Comprueba los atributos uno por uno y registra las discrepancias encontradas.
- **Cómo regenerarlos:** `pwsh -File 05_DICCIONARIO_DE_DATOS\construir_documentos_ampliados.ps1`.

## Cómo abrir cada cosa

- **La base de datos:** Oracle Database 26ai Free debe estar encendido (`lsnrctl status` muestra `freepdb1`). Conexión: host `localhost`, puerto `1521`, servicio `FREEPDB1`, usuario `ECOACCESS_BD`. Guía D.
- **Ejecutar consultas:** en SQL Developer abre `03_SCRIPTS_ORACLE/13_consultas_exposicion.sql` con la conexión ECOACCESS_BD y pulsa **F5** (todo el script) o **Ctrl+Enter** (solo la consulta del cursor). Guía D.
- **Modelo DMD:** SQL Developer → Archivo → Data Modeler → Abrir → `02_MODELOS_MER_MEER_RELACIONAL/DATA_MODELER_DMD/EcoAccess_BD.dmd` (la carpeta `EcoAccess_BD/` debe estar al lado). Pestañas «Logical» (MER), «Relational_1» (modelo ordenado por módulos) y las subvistas M1–M5. Comprobación: `pwsh -File 02_MODELOS_MER_MEER_RELACIONAL\verificar_dmd.ps1 -Dmd 02_MODELOS_MER_MEER_RELACIONAL\DATA_MODELER_DMD\EcoAccess_BD.dmd` → PASA (Guía E).
- **Diagramas en imagen/PDF:** `02_MODELOS_MER_MEER_RELACIONAL/EXPORTACIONES/` (MER, MEER, `MR_00_general_por_modulos`, `MR_M1`…`MR_M5`, `FLUJO_datos_ecoaccess`) y `DATA_MODELER_DMD/EXPORTACIONES_DMD/` (exportados desde Data Modeler).
- **Power BI:** `13_POWER_BI/ARCHIVO_PBIX/EcoAccess_BD.pbix` es el tablero verificado en pantalla (lee los CSV de la cuenta lectora). `13_POWER_BI/PROYECTO_PBIP/EcoAccess_BD/EcoAccess_BD.pbip` ya lee **Oracle directo** (vistas `VW_BI_*` con `ECOACCESS_BD_LECTOR`; parámetro `OrigenDatos` = ORACLE o CSV). Al abrirlo: Transformar datos → Editar credenciales → Base de datos → `ECOACCESS_BD_LECTOR` + su contraseña → Actualizar → Guardar como `.pbix`. Botones con Ctrl+clic en Desktop. Guía H, capítulo «Origen de datos».
- **C#:** `12_GUIA_CSHARP_ORACLE/SOLUCION_CSHARP/EcoAccessBD.sln` en Visual Studio; variables de entorno `PRACTICAS_USUARIO` y `PRACTICAS_CLAVE` (Guía I).
- **Programación III / Oracle Cloud:** `18_INTEGRACION_P3/`: matriz P3 vs académico (regenerada desde GitHub el 11/11/2026), `VERIFICACION_GITHUB_Y_NUBE_20261111.md` y el inventario de solo lectura. Para ejecutarlo: `pwsh -File 18_INTEGRACION_P3\ejecutar_inventario_cloud.ps1`; usted escribe la contraseña de ECOACCESS_P3_APP cuando SQL*Plus la pida. También está la propuesta de cuenta lectora, sin ejecutar.

## Instalador

```
pwsh -File .\INSTALAR_Y_PROBAR.ps1              # instala si falta y prueba todo; NO borra nada
pwsh -File .\INSTALAR_Y_PROBAR.ps1 -Reinstalar  # borra y recrea solo ECOACCESS_BD (pide escribir BORRAR_ECOACCESS_BD)
pwsh -File .\INSTALAR_Y_PROBAR.ps1 -CrearUsuarios  # primera vez en un PC nuevo
```

Última ejecución completa (11/11/2026, 20:3x, sin reinstalar): **27 de 27 comprobaciones PASAN** (N03, las capturas de la ventana C#, ahora solo corre con `-CapturasCSharp` para no insertar filas de demostración) — 153/153 pruebas del esquema, 19/19 prácticas, 10/10 cuenta lectora, 22 consultas, 4 triggers demostrados, 309 ejercicios (299 OK + 10 errores intencionales, 0 fallan), DDL = scripts = diccionario, 0 inválidos, C# 10/10 y proyecto Power BI íntegro. Detalle: `17_VALIDACION_FINAL/RESUMEN_ULTIMA_EJECUCION.md`.

## Qué hay en cada carpeta

| Carpeta | Contenido |
|---|---|
| `00_DOCUMENTOS_FINALES_WORD_PDF/` | Documentos A–O (menos K) en .docx y .pdf, sus fuentes y `generar_todos.ps1`. |
| `00_HERRAMIENTAS/` | `ejecutar_sql.ps1`, `comun.ps1` (contraseñas nunca en archivos) y `empaquetar.ps1`. |
| `00_LEEME_PRIMERO/` | `PASO_A_PASO.pdf/.docx`. |
| `01_REQUISITOS_Y_ANALISIS/` | Diagnóstico de V3 y P3, brechas y riesgos. |
| `02_MODELOS_MER_MEER_RELACIONAL/` | MER, MEER, modelo relacional ordenado por módulos y flujo de datos (PNG/SVG/PDF); el `.dmd` en `DATA_MODELER_DMD/`; los generadores y `verificar_dmd.ps1`. |
| `03_SCRIPTS_ORACLE/` | Scripts 00–18 y 99, y `DDL_FINAL_EXTRAIDO.sql`. |
| `04_PLSQL_Y_TRIGGERS/` | Demo de los 4 triggers y prácticas P00–P05 + P99 (tareas del profesor). |
| `05_DICCIONARIO_DE_DATOS/` | Diccionario de 173 columnas (.md, .csv, .html), explicación ampliada y su generador. |
| `06_PRUEBAS_Y_EVIDENCIAS/` | Salidas reales y capturas de C#, Data Modeler y Power BI. |
| `07_INFORME_FINAL_WORD_PDF/` | Fuente del informe A y el generador Word/PDF. |
| `08_GUIA_DE_ESTUDIO_WORD_PDF/` | Banco de 309 ejercicios y la fuente de la guía B. |
| `11_GUIA_PLSQL/` | Fuente de la guía F. |
| `12_GUIA_CSHARP_ORACLE/` | Solución .NET con ODP.NET. |
| `13_POWER_BI/` | `.pbix`, proyecto PBIP (formato PBIR), CSV, medidas DAX, Power Query, `convertir_a_pbir.ps1`, `agregar_glosario_inicio.ps1` y verificador. |
| `14_GUIA_SUSTENTACION/`, `15_MATRIZ_TRAZABILIDAD/` | Checklist y matriz K. |
| `16_RESPALDO_Y_RECUPERACION/` | Plan de reversión y respaldo Data Pump del 11/11/2026. |
| `17_VALIDACION_FINAL/` | Resumen de la última ejecución. |
| `18_INTEGRACION_P3/` | Comparación con Programación III y scripts de solo lectura para Oracle Cloud. |
| `99_VERSIONES_ANTERIORES/` | Versiones reemplazadas (diagramas desordenados, .dmd y PBIX anteriores, documentos con letras antiguas). No va en el ZIP. |
| `_LOCAL_NO_PUBLICAR/` | Contraseñas locales. **No se entrega, no va al ZIP ni a Git.** |

Todos los datos son **SINTÉTICOS**. No se tocaron P3, Oracle Cloud, HR (solo lectura), `main` ni Git.

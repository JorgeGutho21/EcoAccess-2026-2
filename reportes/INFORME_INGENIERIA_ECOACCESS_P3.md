# Informe Técnico de Ingeniería — ECOACCESS P3

**Fecha UTC:** 2026-11-23 · **Base de partida:** `v2026.11.22` (commit `642c85d85e6442040e4bd474e1a188736a075671`, `VERSION.txt`)  
**Workspace evaluado:** `EcoAccess-P3-Final` (exportación sin `.git`) · **Institución:** Universidad Popular del Cesar (Programación de Computadores III y Base de Datos)

---

## A. Auditoría Técnica

### Arquitectura encontrada
- **Solución:** `EcoAccess.P3.sln` · **.NET Framework 4.8** · **C# 9.0** (`Nullable=enable`, `ImplicitUsings=disable`) · SDK instalado: **.NET 10.0.403**.
- **Tipo de aplicación:** Escritorio Windows Forms (`EcoAccess.P3.Presentacion`, proyecto de inicio) con `Krypton.Toolkit`, `FontAwesome.Sharp` y `LiveChartsCore`.
- **Capas y dependencias verificadas por `PruebasArquitectura`:**
  - `EcoAccess.P3.Entidades` (dominio puro, 0 dependencias de proyecto).
  - `EcoAccess.P3.Logica` (servicios y reglas, solo depende de `Entidades`).
  - `EcoAccess.P3.Datos` (repositorios ODP.NET parametrizados con `BindByName = true`, sin SQL interpolado).
  - `EcoAccess.P3.Presentacion` (UI WinForms; `Program.cs` es el único punto de composición con `Datos`).
  - `EcoAccess.P3.Bot` (Telegram, visión local, bridge HMAC-SHA256 hacia ESP32, CLI de evaluación).
  - `EcoAccess.P3.Instalador` y `EcoAccess.P3.Compatibilidad`.
- **Estado de compilación línea base y final:** **0 errores** (`dotnet build EcoAccess.P3.sln -c Release --no-restore -m:1 -nr:false`).

### Estado de Ollama y modelo Qwen vs. LFM3-VL
Auditado directamente contra `http://127.0.0.1:11434` (`ollama show` y `EcoAccess.P3.Bot.exe --auditar-vision-ollama`):

| Modelo instalado en Ollama (`0.40.16`) | Tamaño | Arquitectura | Capacidades (`/api/show`) | ¿Tiene visión? |
|---|---:|---|---|---|
| `qwen3:1.7b` | 1,36 GB | `qwen3` | `completion`, `tools`, `thinking` | **NO (solo texto)** |
| `qwen3:4b` | 2,50 GB | `qwen3` | `completion`, `tools`, `thinking` | **NO (solo texto)** |
| `qwen3:4b-instruct` | 2,50 GB | `qwen3` | `completion`, `tools`, `thinking` | **NO (solo texto)** |
| `lfm3-vl:latest` | 1,48 GB | `lfm3` + CLIP 189M | `completion`, `vision`, `tools`, `thinking` | **SÍ (único modelo visual instalado)** |

- **Por qué se usa `lfm3-vl:latest` y no Qwen-VL:**
  1. Ninguno de los tres modelos `qwen3:*` instalados localmente posee capacidad `vision`.
  2. Según la directiva de no descargar modelos grandes sin autorización, no se descargó `qwen2.5vl:3b` (~3,2 GB) ni `qwen3-vl:2b` (~1,9 GB).
  3. Además, mediciones previas documentadas en `docs/vision/07_reconocimiento_visual.md` en este mismo hardware (GTX 1650 4 GB) demostraron que `qwen3-vl:2b` tardaba **17,94 s** (P95 46,24 s) frente a **2,33 s** de `lfm3-vl:latest`.
  4. **¿Cumple `lfm3-vl:latest` todos los requisitos del proyecto?** Cumple latencia local (~3,5–6,6 s) y operación 100 % local sin nube, pero **NO alcanza por sí solo el objetivo de ≥ 95 % por material ni ≥ 99 % en combinaciones multirresiduo** (ver Sección D). Por ello el motor de reglas en C# exige confirmación humana ante dudas y se documenta el requisito de evaluar `qwen2.5vl:3b` / `qwen2.5vl:7b` (o detector dedicado) con autorización previa.
  5. El nuevo `ClienteVisionOllamaMultirresiduo` está completamente desacoplado y soporta `qwen2.5vl:*`, `qwen3-vl:*` o `lfm3-vl:latest` mediante `ECOACCESS_VISION_MODELO` sin recompilar.

### Estado de Oracle Cloud y política de aislamiento de pruebas
- **Conexión oficial (`App.config`):** `Data Source=ecoaccess_tp;User Id=ECOACCESS_P3_APP` (Oracle Cloud Autonomous Database, wallet mTLS fuera del repositorio, contraseñas solo en User Secrets `ecoaccess-p3-framework`).
- **Comprobación de solo lectura:** `EcoAccess.P3.Bot.exe --comprobar-oracle` respondió correctamente: *41 materiales habilitados, sin modificar datos ni aplicar migraciones*.
- **Política estricta de pruebas Oracle (cumpliendo la instrucción de seguridad):**
  - **NO se ejecutó** `EcoAccess.P3.Datos.Tests` contra `ECOACCESS_P3_APP` ni se modificó ningún dato persistente en Oracle Cloud.
  - `BaseIntegracionOracle.EsDestinoDePruebas` bloquea con `Inconclusive` cualquier destino cuyo usuario no termine en `*_PRUEBAS` ni apunte al PDB aislado `/PRUEBAP3`.
  - Un `ROLLBACK` en Oracle **no revierte** secuencias (`SEQUENCE`) ni sentencias DDL (`CREATE/ALTER/DROP`, con commit implícito). Por tanto, las 192 pruebas de integración con Oracle deben ejecutarse únicamente sobre un esquema aislado (`ECOACCESS_P3_PRUEBAS` en `localhost:1521/freepdb1`, 500 MB de cuota, secreto separado `ecoaccess-p3-pruebas`) creado por el titular mediante `scripts/pruebas/Crear-Esquema-Aislado-Local.ps1` e `Instalar-Esquema-Aislado-Local.ps1`.
  - La migración opcional `database/migrations/V028_catalogo_35_prioritarios_y_multirresiduo.sql` y su reversión `V028_reversion.sql` quedaron preparadas y **NO aplicadas** a producción.

### Estado del hardware
- **Puertos serie detectados en el equipo:** `COM4` y `COM5`.
- **Estado de ejecución:** `GuardiaHardware` bloquea el acceso a puertos COM y bridges externos durante todas las pruebas automáticas y evaluaciones. **No se activó ningún motor ni servomotor.** La prueba física con el ESP32 (blanca 1093 GPIO 19, negra 1401 GPIO 18, verde 1400 GPIO 23) permanece pendiente de supervisión presencial.

---

## B. Cambios Realizados

### Archivos creados
1. `src/EcoAccess.P3.Logica/Vision35/CatalogoPrioritario35.cs` — Catálogo único, versionado (`2026.11.22-35`) y administrable (JSON import/export) de los **35 residuos prioritarios** (`V01`–`V10`, `B01`–`B13`, `N01`–`N12`), mapeado a `CatalogoPrototipo` y códigos Oracle, con protección de reutilizables (cubeta para hielo), residuos peligrosos y bandera de simulación sanitaria en stand.
2. `src/EcoAccess.P3.Logica/Vision35/ModelosVisionMultirresiduo.cs` — Contratos tipados para observaciones visuales, JSON validado, combinaciones `C01`–`C07`, cajas delimitadoras verificadas (nunca inventadas) e interfaces desacopladas.
3. `src/EcoAccess.P3.Logica/Vision35/ValidadorJsonVision.cs` — Validación de firmas mágicas de imagen (JPEG/PNG/WebP ≤ 5 MB) y validación estricta de JSON estructurado (T027).
4. `src/EcoAccess.P3.Logica/Vision35/MotorReglasMultirresiduo.cs` — Motor determinista en C# que clasifica individualmente cada objeto, filtra etiquetas de reciclaje (T023) y pantallas (T024), protege reutilizables y vidrio roto, exige confirmación ante líquido/ambigüedad/película plástica `B13`, y deduplica canecas (5 botellas limpias = 1 `BLANCA`).
5. `src/EcoAccess.P3.Logica/Vision35/ClienteVisionOllamaMultirresiduo.cs` — Cliente HTTP desacoplado de Ollama (`/api/version`, `/api/tags`, `/api/show`, `/api/chat`) con JSON Schema, `think: false`, timeout y soporte configurable por entorno (`ECOACCESS_OLLAMA_URL`, `ECOACCESS_VISION_MODELO`).
6. `src/EcoAccess.P3.Logica/Vision35/MaquinaEstadosDeposito.cs` — Máquina de estados finitos con los 11 estados exactos (`INACTIVO` a `ERROR_SEGURO`), prevención de duplicados consecutivos (T025), cancelación (T030) y apertura secuencial guiada diferenciando comando enviado vs. apertura confirmada (T029).
7. `src/EcoAccess.P3.Logica/Vision35/ColaLocalOperaciones.cs` — Almacenamiento local atómico en JSON para tolerancia a desconexiones temporales de Oracle, sin PII y con sincronización idempotente sin duplicados.
8. `src/EcoAccess.P3.Logica/Vision35/EvaluadorCatalogo35.cs` — Esquema de manifiestos con procedencia explícita (`RealCatalogo`, `SinteticaCompuesta`, etc.), ejecutor repetible con filtros y cálculo completo de métricas y matriz formal `OBJETIVO / MEDICIÓN REAL / CASOS / ESTADO`.
9. `src/EcoAccess.P3.Bot/Vision/ComandoVision35.cs` — Comandos CLI `--auditar-vision-ollama`, `--probar-imagen35` y `--evaluar-catalogo35` (con `--material`, `--categoria`, `--combinacion`, `--limite`, `--repetir-fallidos`, `--salida`).
10. `src/EcoAccess.P3.Presentacion/FormReconocimientoVisual.cs` — Interfaz WinForms de reconocimiento visual multirresiduo con vista previa y etiquetas visuales, carga/repetición, cancelación asíncrona sin congelar la UI, búsqueda escrita diferenciada, tarjetas de las 3 canecas, estado de sesión, apertura secuencial simulada e historial local.
11. `scripts/pruebas/vision/Auditar-Dataset35.ps1` y `Auditar-Dataset35.py` — Auditoría de integridad/SHA-256/deduplicación del dataset y generador de manifiestos.
12. `database/migrations/V028_catalogo_35_prioritarios_y_multirresiduo.sql` y `V028_reversion.sql` — Scripts verificables y reversibles (no ejecutados en producción).
13. `tests/unitarias/EcoAccess.P3.Tests/Logica/Vision35/`: `CatalogoPrioritario35Tests.cs`, `MatrizMultirresiduoT001aT030Tests.cs` y `MaquinaEstadosYRespaldoOfflineTests.cs`.
14. `docs/desarrollo/GUIA_REPRODUCCION_VISION_35.md` y `datasets/ecoaccess_35/*` (manifiestos, cobertura y 18 composiciones `SinteticaCompuesta`).

### Archivos modificados
- `src/EcoAccess.P3.Bot/Program.cs` — Registro de los 3 comandos CLI de auditoría y evaluación (bloqueados contra hardware por `GuardiaHardware`).
- `src/EcoAccess.P3.Presentacion/FormExplorarEcoAccess.cs` — Botón no intrusivo «Reconocer foto» en la barra superior, escalado con `Adaptar()` sin afectar las vistas previas.

### Dependencias incorporadas
- **0 paquetes NuGet nuevos** (se reutilizaron `System.Net.Http`, `System.Text.Json` y `Microsoft.Bcl.TimeProvider`, preservando 100 % la compatibilidad con Visual Studio y `PruebasArquitectura`).

---

## C. Dataset (Auditoría Real vs. Meta 3.500)

Evidencia exacta en `datasets/ecoaccess_35/COBERTURA_35_MATERIALES.md` y `cobertura_35_materiales.json`:

| Concepto | Cantidad verificada |
|---|---:|
| Archivos de imagen escaneados en `assets/imagenes/residuos/` | **139** |
| Imágenes corruptas / Duplicados exactos SHA-256 rechazados | **0** / **0** |
| Imágenes reales mapeadas a los 35 materiales prioritarios (`RealCatalogo`) | **77** (cubre **30/35** materiales) |
| Imágenes reales adicionales del catálogo extendido de 54 | **50** |
| Imágenes en `pendientes/` (sin mapeo automático seguro) | **12** |
| Materiales prioritarios con **0** imágenes reales actualmente | **5** (`B03`, `B13`, `N10`, `N11`, `N12`) |
| Escenas multirresiduo **reales** capturadas en cámara | **0** |
| Escenas multirresiduo compuestas (`SinteticaCompuesta`, C01–C07, con cajas verificadas) | **18** |
| **Meta objetivo real (35 × 100)** | **3.500** |
| **Evaluaciones reales pendientes de captura en stand** | **3.423** |

---

## D. Pruebas y Métricas Verificables

### 1. Pruebas automatizadas de software (`EcoAccess.P3.Tests`, sin tocar Oracle)
- **Total ejecutado:** **2.374** (2.309 preexistentes + 65 nuevas de `Vision35`: catálogo 35, matriz completa `T001`–`T030`, máquina de 11 estados, E2E secuencial simulado, cola offline sin duplicados y regresiones).
- **Aprobadas:** **2.373** · **Fallidas:** **0** · **Omitidas:** **1** (`EnlaceSimbolicoSeRechazaAntesDeClasificar`, requiere privilegio de enlace simbólico de Windows).
- **Arquitectura y seguridad (`PruebasArquitectura`, `PruebasTercerCorte`, `SinSecretosEnRepositorioTests`):** **100 % aprobadas**.
- **Suite `EcoAccess.P3.Datos.Tests` (192 pruebas Oracle + 59 auxiliares):** **BLOQUEADA / NO EJECUTADA** por política de seguridad hasta disponer del esquema aislado `ECOACCESS_P3_PRUEBAS` verificado y autorización puntual.

### 2. Evaluaciones reales ejecutadas con Ollama (`lfm3-vl:latest`, tras correcciones v2)

| Corrida (archivo de evidencia en `reportes/vision35/`) | Procedencia | Casos | Aprobados | Exactitud comb. | Falsas aperturas | Latencia media / P95 | Estado frente a objetivo (≥99 %, 3.500) |
|---|---|---:|---:|---:|---:|---|---|
| `eval_real_verde_10.json` | `RealCatalogo` (VERDE) | 10 | 10 | **100,0 %** | **0** | 4,01 s / 5,03 s | NO APROBADO (muestra parcial) |
| `eval_real_negra_10.json` | `RealCatalogo` (NEGRA) | 10 | 9 | **90,0 %** | 1 | 4,10 s / 6,17 s | NO APROBADO |
| `eval_real_catalogo_15_v2.json` | `RealCatalogo` (BLANCA) | 15 | 9 | **60,0 %** | 2* | 3,53 s / 4,58 s | NO APROBADO |
| **Subtotal imágenes reales del catálogo** | **`RealCatalogo`** | **35** | **28** | **80,0 %** | **3** | **~3,8 s** | **NO APROBADO (faltan 3.465 casos reales y precisión ≥95 %)** |
| `eval_multirresiduo_compuesto.json` (antes v1) | `SinteticaCompuesta` (C01–C07) | 18 | 6 | 33,3 % | 1 | 11,89 s / 24,88 s | Línea base v1 |
| `eval_multirresiduo_compuesto_v2.json` (después v2) | `SinteticaCompuesta` (C01–C07) | 18 | **12** | **66,7 %** | 1 | **6,63 s / 7,85 s** | **NO APROBADO** (`VERDE` 92,3 %, `BLANCA` 85,7 %, `NEGRA` 76,9 %) |

*\*Nota técnica sobre las 2 falsas aperturas en `BLANCA`: corresponden a `botella-gaseosa.jpg` y `lata-aluminio-1.jpg`, donde `lfm3-vl` reportó estado `contaminado` y el motor de reglas conservadoramente envió el objeto a `NEGRA` (nunca contaminó la `BLANCA`).*

---

## E. Problemas Encontrados y Correcciones Aplicadas

| # | Descripción | Causa raíz | Corrección aplicada | Prueba de regresión | Estado final |
|---|---|---|---|---|---|
| 1 | `T025` lanzaba excepción al recibir foto idéntica consecutiva | `GestorSesionMultirresiduo` intentaba `CAPTURANDO → FINALIZADO`, transición prohibida por `MaquinaEstadosDeposito` | Cambiar a `CAPTURANDO → INACTIVO` al bloquear duplicado consecutivo | `T025_DosFotografiasConsecutivasDelMismoDeposito_NoDuplicanAperturas` | **RESUELTO** |
| 2 | Falsa apertura `BLANCA` en `COMP_C03_T019_3Negras` (sanitarios) | `lfm3-vl` leyó envoltorio sanitario como película plástica (`B13`) y abría `BLANCA` automáticamente | `B13` exige siempre confirmación humana (`RequiereConfirmacion`, `ElegibleApertura = false`) | `Regresion_PeliculaPlasticaB13SiempreRequiereConfirmacion_EvitaFalsaAperturaBlanca` | **RESUELTO** |
| 3 | Colilla aplastada (`cigarette`, estado `DanadoORoto`) clasificada como `FueraDeCatalogo` | `MotorReglasMultirresiduo` trataba todo `DanadoORoto` como residuo peligroso/vidrio roto | Acotar rechazo peligroso por `DanadoORoto` únicamente a vidrio (`glass`/`vidrio`) | `Regresion_ColillaDanadaVaANegra_MientrasVidrioRotoEsFueraDeCatalogo` | **RESUELTO** |
| 4 | 14/15 imágenes reales `BLANCA` y 4 escenas multirresiduo devolvían `SinResiduos` (6,7 % exactitud) | `lfm3-vl` (1.7B) ponía `hay_residuos: false` en reciclables limpios y `es_etiqueta_impresa: true` en botellas con etiqueta comercial | `ValidadorJsonVision` preserva `objetos` si `Count > 0` para que decida C#; `EsEtiquetaImpresa` no descarta envases físicos reales; prompt aclarado | `Regresion_BotellaConEtiquetaComercialNoSeDescartaComoSticker` + `Regresion_JsonConObjetosYHayResiduosFalse_SeEvaluaPorMotorDeReglas` + corrida real (**6,7 % → 60,0 %** y **33,3 % → 66,7 %**) | **RESUELTO (mejora verificada)** |
| 5 | Modelos `qwen3:*` instalados no tienen visión; precisión de `lfm3-vl` (1.7B) en multirresiduo es 66,7 % (< 99 %) | Limitación inherente de modelo 1.7B y ausencia de `qwen2.5vl`/`qwen3-vl` instalado + falta de 3.423 fotos reales de cámara | Desacoplado por variable de entorno `ECOACCESS_VISION_MODELO`, confirmación humana obligatoria ante duda y plan de captura del stand | `--auditar-vision-ollama` + informes `reportes/vision35/*.json` | **BLOQUEADO (requiere autorización para descargar Qwen-VL y captura real)** |

---

## F. Validación de la Demostración

| Pregunta de validación | Estado | Evidencia / Condición |
|---|---|---|
| ¿Reconoce un residuo? | **PROBADO** | 28/35 (80,0 %) exactos en imágenes reales del catálogo con Ollama `lfm3-vl:latest` (`VERDE` 10/10, `NEGRA` 9/10, `BLANCA` 9/15). |
| ¿Reconoce varios residuos? | **PROBADO** | Verificado en pruebas unitarias `T004`–`T019` y en 18 escenas compuestas reales de inferencia con `lfm3-vl` (12/18 exactas). |
| ¿Identifica dos categorías? | **PROBADO** | `C04`, `C05`, `C06` probados en software (`T004`–`T011`) y verificados en Ollama (`eval_multirresiduo_compuesto_v2.json`). |
| ¿Identifica tres categorías? | **PROBADO** | `C07` probado en software (`T012`–`T016`), E2E secuencial y verificado en Ollama (`eval_multirresiduo_compuesto_v2.json`). |
| ¿Genera etiquetas correctas? | **PROBADO** | Etiquetas del catálogo `V01`–`N12` y renderizado visual en `FormReconocimientoVisual`; en inferencia real 1.7B el detalle fino de submaterial aún requiere confirmación humana. |
| ¿Controla las canecas? | **PROBADO** (simulado) / **NO PROBADO** (físico) | Apertura secuencial guiada verificada 100 % en `SimuladorEstacionTresCanecas` (0 duplicados, 11 estados). Hardware físico no activado sin supervisión. |
| ¿Confirma físicamente aperturas? | **NO PROBADO** | Requiere prueba física supervisada presencial con ESP32 conectado y calibrado (`CALIBRACION_VERIFICADA = true`). |
| ¿Registra resultados? | **PROBADO** | Registro local idempotente probado (`ColaLocalOperaciones`) y lectura Oracle verificada (41 materiales); escritura de pruebas en Oracle bloqueada hasta esquema aislado `ECOACCESS_P3_PRUEBAS`. |
| ¿Funciona sin conexión temporal a Oracle? | **PROBADO** | `ColaLocalOperaciones` encola atómicamente sin PII y sincroniza sin duplicados al reconectar (`MaquinaEstadosYRespaldoOfflineTests`). |
| ¿Mantiene las interfaces previas? | **PROBADO** | Compilación limpia (`0 errores`), 2.309/2.309 pruebas originales intactas + botón no intrusivo «Reconocer foto» en `FormExplorarEcoAccess`. |
| ¿Está preparado para una demostración supervisada? | **PROBADO (modo software/simulado con confirmación humana)** / **BLOQUEADO (apertura física autónoma y meta 3.500)** | Listo para demo supervisada en modo simulado + confirmación humana; **no** debe operar apertura física autónoma sin completar calibración ESP32, captura de fotos reales del stand y evaluación de un modelo VLM más preciso. |

---

## G. Pruebas Pendientes (Previas a Solicitar Nuevas Autorizaciones)

1. **Integración contra Oracle (`EcoAccess.P3.Datos.Tests`, 192 pruebas):** crear esquema aislado `ECOACCESS_P3_PRUEBAS` en Oracle local (`scripts/pruebas/Crear-Esquema-Aislado-Local.ps1` + `Instalar-Esquema-Aislado-Local.ps1`) y solicitar autorización puntual antes de ejecutar.
2. **Evaluación de modelo Qwen-VL (`qwen2.5vl:3b` o `qwen2.5vl:7b`):** requiere autorización expresa del usuario para descargar (~3,2–5,0 GB) y comparar exactitud/latencia contra `lfm3-vl:latest` usando el mismo comando `--evaluar-catalogo35`.
3. **Captura del dataset real del stand (3.423 evaluaciones pendientes, incluyendo `B03`, `B13`, `N10`, `N11`, `N12` y escenas reales multirresiduo):** capturar con la cámara del stand usando representaciones sanitarias artificiales limpias.
4. **Prueba física supervisada del ESP32 (servos en GPIO 18, 19, 23):** requiere presencia humana, verificación de fuente externa 5 V y autorización explícita.

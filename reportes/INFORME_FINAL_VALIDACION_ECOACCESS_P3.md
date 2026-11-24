# INFORME FINAL DE VALIDACIÓN — ECOACCESS P3

- **Proyecto:** ECOACCESS P3 — Prototipo inteligente para orientar y registrar la disposición de residuos aprovechables en Valledupar (Universidad Popular del Cesar — Programación de Computadores III y Base de Datos).
- **Responsable de la entrega:** Jorge Gutierrez.
- **Fecha de verificación:** 2026-11-22 / 2026-11-23 UTC.
- **Versión base auditada:** `v2026.11.22` (`VERSION.txt` / commit base `642c85d85e6442040e4bd474e1a188736a075671`).
- **Principio de reporte:** Toda métrica incluida en este documento proviene de ejecuciones reales verificadas. Los pendientes que requieren descarga de modelos, fotos reales con cámara del stand, esquema Oracle aislado o acceso a Git/GitHub se declaran abiertamente.

---

## A. Estado general del proyecto

| Módulo | Estado verificado | Evidencia / Observaciones |
|---|---|---|
| Solución `EcoAccess.P3.sln` (`.NET Framework 4.8`, `C# 9.0`) | **Compila sin errores (`Release`)** | 6 proyectos principales (`Compatibilidad`, `Entidades`, `Logica`, `Datos`, `Presentacion`, `Bot`) + 2 proyectos de pruebas: **0 errores**. |
| Catálogo prioritario 35 materiales (`CatalogoPrioritario35.cs`) | **Implementado y verificado** | 10 VERDE (`V01`–`V10`), 13 BLANCA (`B01`–`B13`), 12 NEGRA (`N01`–`N12`), exportación/importación JSON, búsqueda escrita separada de visión, protección de reutilizables (`cubeta plástica para hielo`) y bandera de simulación sanitaria (`N02`, `N05`, `N11`, `N12`). |
| Motor determinista multirresiduo (`MotorReglasMultirresiduo.cs`) | **Implementado y corregido (Fase 3)** | 35/35 materiales verificados en pruebas unitarias. Corregidas 3 causas raíz reales: (1) `EsReutilizable` ya no descarta empaques desechables reconocidos (`cardboard box` → `B09`); (2) precedencia de `milk/juice carton` (`B10`/`B11`) sobre `carton` genérico (`B09`); (3) `"face"` en `PalabrasNoResiduo` ya no descarta `"used face mask"` (`N12`). |
| Servicio visual desacoplado (`ClienteVisionOllamaMultirresiduo.cs`) | **Implementado (LFM3-VL activo; Qwen3-VL listo para descarga)** | Compatible con `/api/version`, `/api/tags`, `/api/show`, `/api/chat`. Cambio de modelo por configuración (`ECOACCESS_VISION_MODELO`) o selector en UI. |
| Consenso y comparativa multimodelo (`ComparadorModelosVision.cs`) | **Implementado y probado en unitarias** | Regla de seguridad verificada: cualquier discrepancia entre modelos (`LFM3-VL` vs `Qwen3-VL`) bloquea la apertura automática (`PuedeAbrirCanecas = false`, `ErrorTecnico = DISCREPANCIA_MODELOS`). CLI `--comparar-modelos35` integrado. |
| Máquina de estados y modos de apertura (`MaquinaEstadosDeposito.cs`) | **Implementado y probado** | 11 estados (`INACTIVO`..`ERROR_SEGURO`), apertura **Secuencial guiada** (predeterminada) y **Simultánea supervisada** (opcional), bloqueo antiduplicado de foto (`T025`) y por `idOperacion`, honestidad ante ausencia de sensores físicos (`TieneSensorFisico`, `ConfirmacionObservacionalRequerida`). |
| Interfaz de escritorio (`FormReconocimientoVisual.cs`, `FormExplorarEcoAccess.cs`) | **Implementada y compilada** | Selector de modelo (`lfm3-vl:latest`, `qwen3-vl:4b`, `qwen3-vl:2b`, `qwen3-vl:8b`), selector de modo de apertura, lista de objetos con columna `Validación / Motivo`, confirmación humana obligatoria ante ambigüedad y acceso directo desde `FormExplorarEcoAccess`. |
| Respaldo offline (`ColaLocalOperaciones.cs`) | **Implementado y probado** | Persistencia local atómica en JSON sin datos personales, deduplicación por `ClaveIdempotencia` y sincronización idempotente al recuperar Oracle. |
| Integración Qwen3-VL en vivo | **Pendiente de autorización de descarga** | `qwen3:1.7b`, `qwen3:4b` y `qwen3:4b-instruct` instalados son solo texto (0 capacidad `vision`). Pendiente autorizar `ollama pull qwen3-vl:4b` (≈ 3,2–3,4 GB; GTX 1650 4 GB VRAM + 24 GB RAM + 610,38 GB libres en disco). |
| Evaluación de 3.500 fotos reales y pruebas físicas en stand | **Incompleto (requiere captura real)** | 77 fotos reales de catálogo (30/35 materiales), 0 capturas reales multirresiduo de cámara, 5 materiales sin foto real (`B03`, `B13`, `N10`, `N11`, `N12`). **El sistema NO se declara «100 % funcional» para despliegue desatendido hasta completar estas validaciones.** |

---

## B. Resultados de pruebas automatizadas

| Suite / Proyecto | Total | Aprobadas | Fallidas | Omitidas | Estado y observaciones |
|---|---:|---:|---:|---:|---|
| `tests/unitarias/EcoAccess.P3.Tests` (`Release`) | **2.388** | **2.387** | **0** | **1** | **APROBADA** (duración 13 s). Incluye las 2.309 pruebas originales intactas + arquitectura + seguridad de secretos + 79 pruebas nuevas de `Vision35` (los 35 materiales individuales, `T001`–`T030`, 10 objetos misma categoría = 1 caneca, mezclas 4–6 objetos, consenso multimodelo, apertura secuencial/simultánea, fallos de hardware/modelo/imagen y cola offline). |
| `tests/integracion/EcoAccess.P3.Datos.Tests` | 192 | 0 | 0 | 192 (no ejecutadas) | **BLOQUEADA POR SEGURIDAD**: no se ejecutó contra Oracle Cloud compartido (`ECOACCESS_P3_APP`). Requiere crear y autorizar el esquema aislado `ECOACCESS_P3_PRUEBAS`. |

### Análisis de la única prueba omitida
- **Nombre:** `EcoAccess.P3.Tests.Bot.EvaluacionVisionArchivosTests.EnlaceSimbolicoSeRechazaAntesDeClasificar` (`tests/unitarias/EcoAccess.P3.Tests/Bot/EvaluacionVisionArchivosTests.cs:139`).
- **Causa comprobada:** invoca Win32 `CreateSymbolicLink(..., 2)` y retorna `Assert.Inconclusive` cuando la sesión de Windows no cuenta con privilegios de enlace simbólico / Modo de Desarrollador. No es un defecto del código ni de la lógica de seguridad.

---

## C. Resultados de precisión visual (ejecuciones reales con Ollama local)

> **Modelo evaluado en vivo:** `lfm3-vl:latest` (único modelo con capacidad `vision` actualmente instalado en Ollama `0.40.16` en `http://127.0.0.1:11434`).  
> **Comparativa `LFM3-VL` vs `Qwen3-VL`:** pendiente de autorización de descarga de `qwen3-vl:4b` (≈ 3,2–3,4 GB) para ejecutar `--comparar-modelos35`.

### 1. Fotografías reales individuales del catálogo (35 evaluaciones reales)

| Subconjunto real | Reporte | Antes (v2) | Después (v3 — Fase 3) | Falsas aperturas | Latencia media / P95 |
|---|---|---:|---:|---:|---:|
| **VERDE (`C01_Verde`)** | `eval_real_verde_10.json` | 10/10 (100,0 %) | **10/10 (100,0 %)** | 0 | 3,9 s / 5,3 s |
| **BLANCA (`C02_Blanca`)** | `eval_real_catalogo_15_v2` → `eval_real_catalogo_15_v3` | 9/15 (60,0 %) | **12/15 (80,0 %)** *(+20,0 pp)* | 2 *(enrutadas conservadoramente a NEGRA)* | 4,27 s / 15,10 s |
| **NEGRA (`C03_Negra`)** | `eval_real_negra_10.json` | 9/10 (90,0 %) | **9/10 (90,0 %)** | 1 (`condon-usado-1.jpg` confundido por modelo 1.7B con botella) | 4,1 s / 6,2 s |
| **Total catálogo real medido** | — | 28/35 (80,0 %) | **31/35 (88,6 %)** | 3 | — |

*Mejora verificada en `eval_real_catalogo_15_v3`:* `carton-limpio-1.jpg`, `carton-limpio-2.jpg` y `envase-plastico-5.jpg` se recuperaron limpiamente tras la corrección de `EsReutilizable` sin alterar ninguna etiqueta esperada.

### 2. Escenas multirresiduo controladas (`SinteticaCompuesta`) separadas por las 7 combinaciones (`C01`–`C07`)

> **Aclaración metodológica obligatoria:** estas escenas son composiciones sintéticas controladas construidas sobre fotografías reales del catálogo para probar las 7 combinaciones, fondos variados (claro, oscuro, mesa de madera, cemento universitario), sombras, solapamiento parcial y 4 objetos. **No sustituyen fotografías reales tomadas con la cámara del stand.**

| Combinación de canecas | Corrida previa (`eval_multirresiduo_compuesto_v2`, 18 casos) | Nueva evaluación independiente (`eval_independiente_lfm3vl`, 15 casos) | Falsas aperturas (eval independiente) | Observaciones reales |
|---|---:|---:|---:|---|
| **1. VERDE (`C01_Verde`)** | 1/1 (100,0 %) | **2/2 (100,0 %)** | 0 | Acierta bajo fondo oscuro con sombra y mesa con 3 objetos solapados. |
| **2. BLANCA (`C02_Blanca`)** | 0/1 (0,0 %) | **2/2 (100,0 %)** | 0 | Recuperación total (`EVAL_C02_Claro`, `EVAL_C02_Campus`). |
| **3. NEGRA (`C03_Negra`)** | 1/1 (100,0 %) | **1/2 (50,0 %)** | 1 | `EVAL_C03_Mesa` (3 objetos solapados) generó falsa apertura en BLANCA+NEGRA. |
| **4. VERDE + BLANCA (`C04_VerdeBlanca`)** | 3/3 (100,0 %) | **2/2 (100,0 %)** | 0 | 100 % exacto incluyendo 3 objetos en 2 categorías (`EVAL_C04_Campus`). |
| **5. VERDE + NEGRA (`C05_VerdeNegra`)** | 1/2 (50,0 %) | **1/2 (50,0 %)** | 0 | `EVAL_C05_Oscuro` pasó de forma segura a `Ninguna` (confirmación humana, 0 falsas aperturas). |
| **6. BLANCA + NEGRA (`C06_BlancaNegra`)** | 3/3 (100,0 %) | **1/2 (50,0 %)** | 0 | `EVAL_C06_Mesa` omitió cartón con grasa (`N03`), abriendo solo BLANCA. |
| **7. VERDE + BLANCA + NEGRA (`C07_VerdeBlancaNegra`)** | 3/7 (42,9 %) | **2/3 (66,7 %)** | 0 | Acierta clásico (`EVAL_C07_Claro`) y mesa con solape/sombra (`EVAL_C07_Mesa`); en 4 objetos pequeños (`EVAL_C07_Campus4Obj`) pidió confirmación segura (`Ninguna`). |
| **Total multirresiduo controlado** | **12/18 (66,7 %)** | **11/15 (73,3 %)** | **1** | Latencia independiente: media **6,00 s**, P95 **7,92 s**. Precisión material 81,3 %, recall 79,0 %. |

### Cumplimiento frente a criterios formales de aceptación (Sección 12)
- **Fallos de hardware en simulación:** 0 (**APROBADO**).
- **Cobertura 3.500 evaluaciones reales (35 × 100), precisión ≥95 %, recall ≥95 %, combinaciones ≥99 % y 0 falsas aperturas:** **NO APROBADO (EVALUACIÓN INCOMPLETA)** con `lfm3-vl:latest` (1.7B). Se requiere comparar contra `qwen3-vl:4b` e incorporar fotografías reales del stand.

---

## D. Estado del dataset (`datasets/ecoaccess35/`)

- **Estructura creada y verificada (`Construir-DatasetEcoAccess35.ps1`):** las 12 subcarpetas estándar (`originales/`, `reales/`, `sinteticas/`, `recortes_individuales/`, `composiciones_multirresiduo/`, `anotaciones/`, `entrenamiento/`, `validacion/`, `evaluacion_independiente/`, `casos_fallidos/`, `reportes/`, `bandeja_entrada/`).
- **Inventario real verificado (`Auditar-Dataset35.ps1`):**
  - 139 archivos escaneados en `assets/imagenes/residuos/`: **0 corruptos**, **0 duplicados SHA-256**.
  - **77 fotografías reales** mapeadas a **30 de los 35 materiales prioritarios** (referenciadas en `datasets/ecoaccess35/reales/manifiesto_reales.json` sin duplicar archivos originales).
  - **5 materiales aún sin fotografía real:** `B03` (tapa plástica), `B13` (película plástica limpia), `N10` (chicle usado), `N11` (toallita húmeda usada), `N12` (tapabocas desechable usado).
  - **Escenas reales multirresiduo tomadas con la cámara del stand:** **0** (pendientes de captura presencial).
  - **Divisiones sintéticas controladas sin fuga de escenas:** `validacion/` (7 escenas, una por `C01`–`C07`) y `evaluacion_independiente/` (15 escenas con fondos claro/oscuro/mesa/campus, sombras, solapamiento y 4 objetos), todas explícitamente marcadas como `SinteticaCompuesta`.
- **Pipeline de incorporación sin recompilar (`Incorporar-NuevasFotos35.ps1`):** verificado (escaneo limpio de `bandeja_entrada/`, validación de imagen real, deduplicación SHA-256, sugerencias opcionales con Ollama y compuerta obligatoria `"revisado_humano": true` antes de promover a manifiesto de referencia).

---

## E. Estado de base de datos (Oracle Cloud y esquema aislado)

- **Oracle Cloud (`ecoaccess_tp` / `ECOACCESS_P3_APP`):** verificado solo en lectura previamente (41 materiales habilitados). **Protegido con 0 escrituras de pruebas automatizadas.**
- **Credenciales y wallets:** ningún secreto, contraseña ni wallet (`cwallet.sso`, `ewallet.p12`) fue añadido al código fuente (`SinSecretosEnRepositorioTests` aprobado dentro de las 2.387 pruebas unitarias).
- **Scripts preparados (NO aplicados a producción):**
  - `database/migrations/V028_catalogo_35_prioritarios_y_multirresiduo.sql` (migración del catálogo 35 y trazabilidad multirresiduo).
  - `database/migrations/V028_reversion.sql` (reversión completa).
  - `scripts/pruebas/Crear-Esquema-Aislado-Local.ps1` e `Instalar-Esquema-Aislado-Local.ps1` (para crear `ECOACCESS_P3_PRUEBAS` en Oracle FREE local `localhost:1521/freepdb1` con cuota 500 MB y probar las 192 pruebas de `EcoAccess.P3.Datos.Tests` sin tocar Oracle Cloud).
- **Resiliencia offline:** `ColaLocalOperaciones` verificada mediante pruebas unitarias de caída de conexión, reintento idempotente (0 duplicados) y recuperación tras reconexión.

---

## F. Estado del hardware (MODO 1 vs MODO 2)

- **Protección activa:** `GuardiaHardware` permaneció activo durante toda la sesión; **0 activaciones de motores físicos** durante pruebas automatizadas.
- **Separación estricta de modos implementada en `MaquinaEstadosDeposito.cs`:**
  - **MODO 1 — Simulación (`SimuladorEstacionTresCanecas`):** probado de extremo a extremo para 1, 2 y 3 canecas en modo `SecuencialGuiada` y `SimultaneaSupervisada`.
  - **MODO 2 — Hardware físico supervisado:** bloqueado por defecto (`permitirSoloSimulador = true`); requiere autorización explícita del supervisor. Cuando el ESP32 no dispone de sensores físicos de apertura/cierre, el resultado registra explícitamente `TieneSensorFisico = false`, `ConfirmacionObservacionalRequerida = true` y el aviso de que la apertura física requiere verificación visual directa.
- **Validación física en el stand:** pendiente de ejecución presencial supervisada con el ESP32 conectado y representaciones artificiales limpias para `N02`, `N05`, `N11` y `N12`.

---

## G. Estado de GitHub y control de versiones

- **Hallazgo real de auditoría:** la carpeta abierta del proyecto (`EcoAccess-P3-Final` en OneDrive) es una copia de entrega exportada y **no contiene directorio `.git`** (`git status` retorna `fatal: not a git repository`).
- **Clon Git oficial identificado en documentación previa (`reportes/ECOACCESS_VALIDACION_FINAL.md`):** `%USERPROFILE%\Documents\01_UNIVERSIDAD\2026-2\Programación de Computadores III-SS462-\EcoAccess-P3` (remoto `https://github.com/JorgeGutho21/EcoAccess-2026-2`).
- **Estado de publicación:** no se han ejecutado commits, `push` ni creación de Pull Request todavía, a la espera de autorización expresa del usuario para:
  1. Sincronizar los archivos verificados hacia el clon Git (o crear un clon limpio local) en la rama `feat/ecoaccess-qwen-vision-oracle`.
  2. Configurar localmente (sin modificar el config global) el autor **Jorge Gutierrez** con su correo verificado de GitHub.
  3. Publicar la rama y abrir un **Draft Pull Request** titulado `feat(ecoaccess): integracion Qwen-VL, Oracle y reconocimiento multirresiduo` (como borrador mientras estén pendientes las fotos reales del stand, la comparativa Qwen3-VL y la validación física).

---

## H. Guía de entrega y compatibilidad para el equipo

1. **Sin rutas personales ni secretos:** todo el código C# y los scripts PowerShell usan rutas relativas desde la raíz de la solución y variables de entorno / User Secrets.
2. **Reproducción en otro computador:** seguir [GUIA_REPRODUCCION_VISION_35.md](file:///c:/Users/Usuario/OneDrive%20-%20unicesar.edu.co/Documentos/02_PROYECTOS_PERSONALES/01_EcoAccess/01_ProyectoDeAula_EcoAccess/EcoAccess-P3-Final/docs/desarrollo/GUIA_REPRODUCCION_VISION_35.md) para compilar (`dotnet build EcoAccess.P3.sln -c Release`), ejecutar las 2.388 pruebas unitarias y probar el flujo visual local.
3. **Recolección de fotos faltantes por el equipo:** para cubrir los 5 materiales sin foto real (`B03`, `B13`, `N10`, `N11`, `N12` — usando muestras limpias para sanitarios) y las escenas reales multirresiduo (`C04`–`C07`), basta copiar las fotografías en `datasets/ecoaccess35/bandeja_entrada/` y ejecutar `Incorporar-NuevasFotos35.ps1` sin recompilar el proyecto.
4. **Regla de oro para ExpoSoftware:** nunca mostrar datos sintéticos como si fueran capturas reales del stand, nunca ejecutar pruebas de integración Oracle sobre `ECOACCESS_P3_APP`, y mantener el modo **Secuencial guiado** como opción segura predeterminada durante la demostración.

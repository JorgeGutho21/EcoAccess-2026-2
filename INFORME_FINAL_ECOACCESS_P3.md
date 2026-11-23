# INFORME FINAL DE CONSOLIDACIÓN, VALIDACIÓN Y ENTREGA — ECOACCESS P3

- **Proyecto:** ECOACCESS P3 — Prototipo inteligente para orientar y registrar la disposición de residuos aprovechables en Valledupar (Universidad Popular del Cesar — Programación de Computadores III y Base de Datos).
- **Responsable:** Jorge Gutierrez.
- **Fecha de verificación:** 23 de noviembre de 2026.
- **Principio de transparencia:** Todos los conteos, métricas, commits y resultados de este informe corresponden a ejecuciones reales verificadas. No se confunde simulación con funcionamiento físico, ni pruebas matemáticas combinatorias con precisión empírica de visión artificial, ni un Pull Request abierto en borrador con cambios ya fusionados a `main`.

---

## A. Ruta definitiva del proyecto y soluciones de Visual Studio

- **Carpeta personal raíz:** `C:\Users\Usuario\OneDrive - unicesar.edu.co\Documentos\02_PROYECTOS_PERSONALES\01_EcoAccess`
- **Carpeta única y oficial del repositorio activo:**
  `C:\Users\Usuario\OneDrive - unicesar.edu.co\Documentos\02_PROYECTOS_PERSONALES\01_EcoAccess\01_ProyectoDeAula_EcoAccess\EcoAccess-P3`
- **Soluciones de Visual Studio auditadas (ambas conservadas con su rol diferenciado):**
  1. **Solución principal ejecutable de ECOACCESS:** `EcoAccess.P3.sln` (6 proyectos productivos `.NET Framework 4.8` / `C# 9.0`: `Compatibilidad`, `Entidades`, `Logica`, `Datos`, `Presentacion`, `Bot`, `Instalador` + 2 proyectos de pruebas).
  2. **Solución académica de la asignatura Base de Datos (profesor Amílkar):** `BaseDeDatos_Amilkar/10_CSHARP_VISUAL_STUDIO/SOLUCION_CSHARP/EcoAccessBD.sln` (conservada íntegra como entregable académico independiente).

---

## B. Carpetas consolidadas, respaldos verificados y versiones archivadas

| Ubicación auditada | Rol / Hallazgo | Estado actual |
|---|---|---|
| `01_EcoAccess\01_ProyectoDeAula_EcoAccess\EcoAccess-P3` | **Única carpeta activa y repositorio Git oficial** | **ACTIVA** (`323` commits históricos + 6 commits nuevos) |
| `01_EcoAccess\02_Imagenes_Reales_EcoAcess` | **41 fotografías reales** (`1200×1600`, 23/11/2026) | **CONSERVADA INTACTA** (inventario generado en `inventario/`) |
| `01_EcoAccess\03_VideoEcoAccess` (2 archivos), `05_Imagenes_EcoAccess` (162 archivos), `06_Imagenes_Futuro` (3), `07_MVP_EcoAccess` (41) | Recursos audiovisuales, galería y MVP personal | **CONSERVADAS INTACTAS** dentro de `01_EcoAccess` |
| `Documents\01_UNIVERSIDAD\2026-2\_Respaldos_EcoAccess\Respaldo_Consolidacion_2026-11-22` | Respaldo verificable SHA-256 fuera de la carpeta definitiva | **VERIFICADO** (`589/589` SHA-256 de `EcoAccess-P3-LImpio`, `90/90` avances de `EcoAccess-P3-Final`, `Docs_Previos`) |
| `01_ProyectoDeAula_EcoAccess\EcoAccess-P3-LImpio` | Copia obsoleta del 03/11/2026 (`OBSOLETA_NO_USAR.txt`, 16 scripts viejos anteriores al PR #22) | **ARCHIVADA / INACTIVA** (no eliminada hasta confirmación separada) |
| `01_ProyectoDeAula_EcoAccess\EcoAccess-P3-Final` | Exportación sin `.git` usada como origen de avances | **ARCHIVADA / INACTIVA** (todos sus 90 cambios ya integrados y respaldados) |
| `Documents\01_UNIVERSIDAD\2026-2\Programación de Computadores III-SS462-\EcoAccess-P3` | Clon universitario anterior (`main` en `642c85d`, PR #22) | **ARCHIVADA / INACTIVA** (conservada sin borrar) |

---

## C. Identidad Git configurada y repositorio GitHub oficial

- **Raíz Git:** `C:\Users\Usuario\OneDrive - unicesar.edu.co\Documentos\02_PROYECTOS_PERSONALES\01_EcoAccess\01_ProyectoDeAula_EcoAccess\EcoAccess-P3\.git`
- **Remoto oficial:** `origin = https://github.com/JorgeGutho21/EcoAccess-2026-2.git`
- **Cuenta GitHub autenticada (`gh auth status`):** `JorgeGutho21`
- **Configuración local exclusiva de este repositorio (sin alterar configuración global ni commits históricos):**
  - `git config --local user.name` → **`Jorge Gutierrez`**
  - `git config --local user.email` → **`263990811+JorgeGutho21@users.noreply.github.com`** (correo verificado de GitHub)
  - `git config --local core.longpaths` → **`true`** (requerido en Windows por rutas largas en `BaseDeDatos_Amilkar`)
- **Rama base preservada:** `main` en `642c85d` (`Merge pull request #22`, sincronizada `0/0` con `origin/main`).
- **Rama de trabajo publicada:** `feat/ecoaccess-vision-integracion-final` (push sin `--force`).

---

## D. Estado de la compilación (`EcoAccess.P3.sln`)

| Configuración | Resultado | Errores |
|---|---|---:|
| `dotnet build EcoAccess.P3.sln -c Debug` | **Compilación correcta** | **0** |
| `dotnet build EcoAccess.P3.sln -c Release` | **Compilación correcta** | **0** |
| Escaneo de secretos y rutas personales en archivos nuevos | **Limpio** (sin contraseñas, wallets ni `C:\Users\...`) | **0** |

---

## E. Resultados de pruebas automatizadas

| Suite | Ejecutadas | Aprobadas | Fallidas | Omitidas | Observación verificada |
|---|---:|---:|---:|---:|---|
| `tests/unitarias/EcoAccess.P3.Tests` (`Release`) | **2.394** | **2.393** | **0** | **1** | Única omitida: `EnlaceSimbolicoSeRechazaAntesDeClasificar` (`Inconclusive` por ausencia de privilegio `CreateSymbolicLink` en Windows; no es defecto del código). |
| `tests/integracion/EcoAccess.P3.Datos.Tests` (`Release`) | **251** | **244** | **0** | **7** | Ejecutada exclusivamente en el esquema local aislado **`ECOACCESS_P3_PRUEBAS@localhost:1521/freepdb1`** (**0 contacto sobre `ECOACCESS_P3_APP`**). Las 7 omitidas son pruebas opt-in (`ECOACCESS_PROBAR_VISION_REAL=1` / restricción de último admin). |
| **Total combinado verificado** | **2.645** | **2.637** | **0** | **8** | **0 fallos** en toda la solución. |

---

## F. Auditoría de fotografías reales, catálogo 35, `FALTA IMAGEN` y referencias externas

### 1. Inventario de fotografías reales auditadas
- **Catálogo en repositorio (`assets/imagenes/residuos/`):** `139` archivos escaneados (`0` corruptos, `0` duplicados SHA-256), con **`77` fotografías reales** mapeadas a **`30` de los `35` materiales** del catálogo prioritario (`10/10` VERDE, `11/13` BLANCA, `9/12` NEGRA).
- **Fotografías reales nuevas en `02_Imagenes_Reales_EcoAcess` (23/11/2026):** **`41` fotografías reales** (`1200×1600` JPEG, todas disponibles localmente en OneDrive, `0` corruptas, `0` duplicados exactos SHA-256, `7` pares perceptualmente similares dHash ≤ 8 por múltiples perspectivas).
  - Clasificación observada en vivo con `lfm3-vl:latest`: **`33` individuales** y **`8` escenas multirresiduo reales** (`15` VERDE, `8` BLANCA, `9` NEGRA, `2` VERDE+BLANCA, `3` VERDE+NEGRA, `1` BLANCA+NEGRA, `3` con `GESTION_ESPECIAL`/sin apertura).
  - Materiales reales confirmados en esas 41 fotos: manzana verde entera/cortada/corazón, papa, tomate, limón/lima, naranja, cebolla morada y cáscara, papel/pañuelo arrugado, cajas de cartón, cartas de juego, vaso de yogur, máquina de afeitar desechable, **3 fotos reales de tapabocas (`N12`)**, cable USB / cable de carga (`GESTION_ESPECIAL` RAEE) y blíster de pastillas (`GESTION_ESPECIAL` farmacéutico).
  - Reportes generados: `inventario_imagenes.csv`, `duplicados_detectados.csv`, `escenas_multirresiduo.json`, `cobertura_materiales.json`, `imagenes_faltantes.csv` y `fuentes_imagenes_web.csv` (tanto en `02_Imagenes_Reales_EcoAcess/inventario/` como en `datasets/ecoaccess35/reportes/`).
- **Escenas sintéticas controladas (separadas explícitamente como `SinteticaCompuesta`):** `18` composiciones en `datasets/ecoaccess_35/` + `7` validación + `15` evaluación independiente en `datasets/ecoaccess35/`. Nunca se cuentan como capturas físicas del stand.

### 2. Materiales en estado `FALTA IMAGEN` y referencias externas verificadas (Wikimedia Commons)
| Código | Material | Caneca | Estado local | Referencia externa documentada (`fuentes_imagenes_web.csv`, NO mezclada con fotos locales) |
|---|---|---|---|---|
| `B03` | Tapa plástica aprovechable | BLANCA | **`FALTA IMAGEN`** | `https://commons.wikimedia.org/wiki/Category:Bottle_caps` (CC BY-SA / DP, verificar archivo) |
| `B13` | Película plástica limpia | BLANCA | **`FALTA IMAGEN`** | `https://commons.wikimedia.org/wiki/File:Clingfilm.jpg` (Wikimedia Commons, verificar licencia exacta) |
| `N10` | Chicle usado | NEGRA | **`FALTA IMAGEN`** | `https://commons.wikimedia.org/wiki/File:Chewing_gum.jpg` (Wikimedia Commons, verificar licencia exacta) |
| `N11` | Toallita húmeda usada | NEGRA | **`FALTA IMAGEN`** | `https://commons.wikimedia.org/wiki/File:Wet_wipe.JPG` (Wikimedia Commons; usar muestra limpia en stand) |
| `N12` | Tapabocas desechable usado | NEGRA | **3 fotos reales en `02_Imagenes_Reales_EcoAcess`** (pendiente promoción tras revisión humana) | `https://commons.wikimedia.org/wiki/File:A_surgical_mask_(2017).jpg` (Public Domain) |

---

## G. Validación matemática combinatoria (`MotorCombinacionesCanecas`) vs. evaluación visual empírica

> Las pruebas matemáticas combinatorias verifican la exactitud determinista de la regla `CanecasAOrdenar = CategoriasOrdinariasUnicas(MaterialesDetectadosValidos)` al 100 %; **no equivalen a precisión visual del 100 %**.

### 1. Universo matemático verificado por código (`PruebasCombinacionesYEscenasRealesTests`)
Para el catálogo vigente de **35 materiales** (`10` VERDE + `13` BLANCA + `12` NEGRA):
- **Pares sin repetición `C(35,2) = 595`:** `189` requieren 1 caneca (`C(10,2)+C(13,2)+C(12,2)=45+78+66`) y `406` requieren 2 canecas (`130+120+156`).
- **Tríos sin repetición `C(35,3) = 6.545`:** `626` requieren 1 caneca (`120+286+220`), `4.359` requieren 2 canecas (`1.365+1.200+1.794`) y `1.560` requieren 3 canecas (`10×13×12`).
- **Total sin repetición (pares + tríos):** **`7.140` combinaciones verificadas**.
- **Con repetición:** **`630` pares** (`C(36,2)`) y **`7.770` tríos** (`C(37,3)`) = **`8.400` combinaciones verificadas**.
- **Escenas obligatorias `A`–`I` y `GESTION_ESPECIAL`:** todas aprobadas (incluyendo `Papa + papel limpio + cable USB` → abre `VERDE + BLANCA` y reporta `GESTION_ESPECIAL` sin abrir tercera caneca; `6/6` permutaciones de 3 categorías idénticas; `2 manzanas + 1 papa` = 1 sola caneca VERDE).

### 2. Evaluación visual empírica medida con `lfm3-vl:latest`
- **Catálogo real individual (35 evaluaciones reales):**
  - VERDE (`eval_real_verde_10`): **10/10 (100,0 %)**, 0 falsas aperturas.
  - BLANCA (`eval_real_catalogo_15_v3`, mejoró +20 pp tras corrección de reglas): **12/15 (80,0 %)**, 2 enrutadas conservadoramente a NEGRA.
  - NEGRA (`eval_real_negra_10`): **9/10 (90,0 %)**, 1 falsa apertura (`condon-usado-1.jpg`).
  - **Total catálogo real medido:** **31/35 (88,6 %)**.
- **Las 7 combinaciones en evaluación independiente (`eval_independiente_lfm3vl`, 15 escenas `SinteticaCompuesta`, media 6,00 s / P95 7,92 s):**
  - `C01_Verde`: **2/2 (100 %)** · `C02_Blanca`: **2/2 (100 %)** · `C03_Negra`: **1/2 (50 %, 1 falsa apertura)** · `C04_VerdeBlanca`: **2/2 (100 %)** · `C05_VerdeNegra`: **1/2 (50 %, confirmación segura)** · `C06_BlancaNegra`: **1/2 (50 %)** · `C07_VerdeBlancaNegra`: **2/3 (66,7 %)**.
  - **Total independiente:** **11/15 (73,3 %)** (subió desde 12/18 = 66,7 % en `v2`). En `eval_validacion_7comb_lfm3vl`: **3/7 (42,9 %)** (media 5,87 s, 0 errores integración).

---

## H. Comparación real `Qwen3-VL` (`qwen3-vl:4b`) vs `LFM3-VL` (`lfm3-vl:latest`)

- **Hardware verificado:** NVIDIA GeForce GTX 1650 (**4 GB VRAM**), **31,84 GB RAM**, **616,76 GB libres** en disco, Ollama `0.40.16`.
- **Capacidad visual verificada (`/api/show`):** tanto `lfm3-vl:latest` (1,48 GB) como `qwen3-vl:4b` (3,34 GB, arquitectura `qwen3vl`) reportan `TieneVision = true`.
- **Comparativa A/B en igualdad de condiciones sobre los 3 casos base reales (`V01` VERDE, `B01` BLANCA, `N01` NEGRA — `reportes/vision35/comparativa_3base_real_lfm3_vs_qwen3vl.md`):**

| Métrica | `lfm3-vl:latest` (Predeterminado) | `qwen3-vl:4b` (Alternativa configurable) |
|---|---:|---:|
| Exactitud combinaciones | **100,0 % (3/3)** | **100,0 % (3/3)** |
| Falsas aperturas | **0** | **0** |
| Fallos de integración | **0** | **0** |
| Distribución GPU/CPU en GTX 1650 (4 GB) | **100 % GPU** | **68 % CPU / 32 % GPU** |
| Latencia media / P95 | **9,02 s / 19,41 s** | **84,78 s / 103,70 s** |
| Escenas multirresiduo de 2–3 objetos | **4–8 s** | **> 150 s** (bloque `<think>` sobre CPU) |

- **Decisión por evidencia:** **`lfm3-vl:latest`** se mantiene como modelo predeterminado por latencia real en 4 GB VRAM; **`qwen3-vl:4b`** queda completamente integrado y configurable (`ECOACCESS_VISION_MODELO=qwen3-vl:4b` o selector en `FormReconocimientoVisual`), recomendado para equipos con ≥6–8 GB VRAM. Toda discrepancia entre modelos bloquea la apertura automática.

---

## I. Estado de Oracle y migración `V028`

- **Oracle Cloud (`ecoaccess_tp` / `ECOACCESS_P3_APP`):** protegido durante todo el proceso (**0 escrituras de prueba**, 0 contraseñas o wallets en el repositorio).
- **Esquema local aislado (`ECOACCESS_P3_PRUEBAS@localhost:1521/freepdb1`):** verificado con **`244` pruebas de integración aprobadas y `0` fallidas** (`EcoAccess.P3.Datos.Tests`).
- **Migración `V028` (`database/migrations/V028_catalogo_35_prioritarios_y_multirresiduo.sql` y `V028_reversion.sql`):** preparadas y versionadas; **NO aplicadas a `ECOACCESS_P3_APP`** hasta recibir autorización independiente para despliegue en producción.
- **Resiliencia offline:** `ColaLocalOperaciones` validada con pruebas unitarias de caída de conexión e idempotencia (0 duplicados).

---

## J. Estado del hardware (ESP32 y servomotores)

- **MODO 1 (Simulación — `SimuladorEstacionTresCanecas`):** validado de extremo a extremo para las 7 combinaciones (`C01`–`C07`), apertura **Secuencial guiada** (predeterminada) y **Simultánea supervisada** (opcional), cierre, cancelación, timeout, recuperación desde `ERROR_SEGURO` y antiduplicado.
- **MODO 2 (Hardware físico supervisado):** `GuardiaHardware` permaneció activo (**0 activaciones físicas desatendidas**). Cuando no existen sensores de posición de tapa, el sistema registra explícitamente `TieneSensorFisico = false` y `ConfirmacionObservacionalRequerida = true`. **La prueba física presencial con ESP32 y servomotores queda pendiente de ejecución supervisada en el stand.**

---

## K. Fallos reales detectados y corregidos durante esta consolidación

1. **`HTTP 400` de Ollama sobre fotografías reales de celular (`1200×1600`):** corregido con escalado seguro en memoria (`≤ 768 px` en `ClienteVisionOllamaMultirresiduo.PrepararBytesParaOllama`) sin alterar originales.
2. **Respuesta vacía en `qwen3-vl:4b` (`DoneReason=length` dentro de `<think>`):** corregido adaptando `format="json"`, `num_predict=768` y timeout de 150 s específicamente para `qwen3-vl:*`.
3. **Precedencia de `B10`/`B11` frente a `B09`:** `milk carton` y `juice carton` ahora se evalúan antes que `carton` genérico.
4. **Falso descarte de `N12` (`used face mask`):** la subcadena `"face"` en `PalabrasNoResiduo` se sustituyó por `"human face"` / `"rostro"`.
5. **Empaques desechables marcados como `es_reutilizable=true`:** `cardboard box` ya clasifica en `B09` BLANCA manteniendo protegida la cubeta plástica para hielo.
6. **Errores detectados en las 41 fotos reales:** `USB cable`/`charging cable` priorizado en `GESTION_ESPECIAL` antes de `EsReutilizable`; `red onion`/`red onion skin`/`citrus` enrutados siempre a VERDE; `crumpled tissue paper` enrutado a `N01` NEGRA.

---

## L. Commits creados y enlace real del Pull Request en GitHub

- **Rama publicada:** `feat/ecoaccess-vision-integracion-final`
- **Pull Request oficial (Draft PR, no fusionado a `main`):**
  **https://github.com/JorgeGutho21/EcoAccess-2026-2/pull/23**
- **Commits nuevos (autor: `Jorge Gutierrez <263990811+JorgeGutho21@users.noreply.github.com>`):**
  - `f3f5089` — `feat(vision): integra catalogo 35, motor multirresiduo, GESTION_ESPECIAL y Qwen3-VL / LFM3-VL`
  - `8ae8df5` — `feat(canecas): agrega vista de reconocimiento visual, cobertura FALTA IMAGEN y apertura segura`
  - `ce03e62` — `feat(datos): prepara migracion V028 y reversion del catalogo de 35 materiales en Oracle`
  - `1f80a8c` — `test(catalogo): organiza inventario de fotos reales, dataset 35 y pruebas combinatorias (7140/8400)`
  - `7ef03de` — `docs(proyecto): actualiza documentacion, guias de reproduccion y reportes de evaluacion`
  - *(+ commit final que incorpora este `INFORME_FINAL_ECOACCESS_P3.md` en la misma rama y PR #23)*

---

## M. Limitaciones pendientes declaradas honestamente

1. Validación física supervisada presencial con ESP32, servomotores y representaciones artificiales limpias para `N02`, `N05`, `N11` y `N12`.
2. Recolección y etiquetado revisado de fotografías reales para los 4 materiales aún en `FALTA IMAGEN` (`B03`, `B13`, `N10`, `N11`), promoción humana de las 3 fotos de `N12` ya capturadas y ampliación hacia la meta de 3.500 evaluaciones reales independientes.
3. Autorización explícita para aplicar `V028` sobre Oracle Cloud compartido (`ECOACCESS_P3_APP`) y revisión/fusión del Draft PR #23 hacia `main`.
4. Confirmación separada del usuario para eliminar definitivamente las carpetas antiguas archivadas (`EcoAccess-P3-LImpio`, `EcoAccess-P3-Final`).

---

## N. Pasos para que los compañeros actualicen y ejecuten el proyecto

```powershell
# 1. Clonar con soporte de rutas largas en Windows
git clone -c core.longpaths=true https://github.com/JorgeGutho21/EcoAccess-2026-2.git EcoAccess-P3
cd EcoAccess-P3
git checkout feat/ecoaccess-vision-integracion-final   # o main tras la fusion del PR #23

# 2. Compilar y ejecutar las 2.394 pruebas unitarias (sin modificar Oracle)
dotnet build EcoAccess.P3.sln -c Release
dotnet test tests\unitarias\EcoAccess.P3.Tests -c Release --no-build --verbosity minimal

# 3. Instalar modelos visuales localmente en Ollama (no se almacenan en GitHub)
ollama pull lfm3-vl:latest
ollama pull qwen3-vl:4b   # opcional, recomendado en GPUs >= 6-8 GB VRAM

# 4. Configurar wallet y credenciales de Oracle fuera del repositorio
# Ver docs/database/14-guia-companeros.md y docs/desarrollo/GUIA_REPRODUCCION_VISION_35.md
```
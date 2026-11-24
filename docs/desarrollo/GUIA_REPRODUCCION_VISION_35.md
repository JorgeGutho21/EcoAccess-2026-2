# Guía de Reproducción — ECOACCESS P3 (Visión Multirresiduo, Qwen-VL / LFM3-VL y Dataset 35)

Esta guía permite a cualquier integrante del equipo (Jorge Gutierrez, Heyder, Saray) o evaluador de ExpoSoftware reproducir la compilación, las pruebas automatizadas, la incorporación de nuevas fotografías sin recompilación y las comparativas de modelos locales en otro computador Windows **sin rutas absolutas personales ni credenciales en el repositorio**.

---

## 1. Requisitos del entorno

| Componente | Versión / Requisito | Comprobación |
|---|---|---|
| Sistema operativo | Windows 10 / 11 (x64) | `winver` |
| .NET SDK | 10.0.100 o superior (según `global.json`) | `dotnet --list-sdks` |
| Target Framework | `.NET Framework 4.8` (Developer Pack) | `EcoAccess.P3.sln` |
| Visual Studio (opcional para UI) | 2022 o 2026 con carga «Desarrollo de escritorio de .NET» | Abrir `EcoAccess.P3.sln` |
| Ollama local | 0.40.13 o superior (`http://127.0.0.1:11434`) | `ollama --version` |
| Modelos visuales en Ollama | `lfm3-vl:latest` (instalado por defecto) y `qwen3-vl:4b` (≈ 3,2–3,4 GB; alternativa `qwen3-vl:2b` ≈ 1,9 GB) | `ollama list` |
| Wallet de Oracle Cloud | Carpeta descomprimida fuera del repo (`%USERPROFILE%\.oracle\wallets\EcoAccess` o `%LOCALAPPDATA%\EcoAccessP3\wallet`) | Ver `docs/database/14-guia-companeros.md` |

### Nota crítica sobre modelos Qwen vs LFM3-VL

Los modelos `qwen3:1.7b`, `qwen3:4b` y `qwen3:4b-instruct` son **únicamente de texto** (no tienen capacidad `vision` en `/api/show`).  
Para usar Qwen con capacidad visual real en Ollama se requiere un modelo multimodal `qwen3-vl:*` (priorizando `qwen3-vl:4b` en equipos con 4 GB VRAM + ≥16 GB RAM, o `qwen3-vl:2b` en equipos más ajustados).

El servicio desacoplado `ClienteVisionOllamaMultirresiduo` y la ventana `FormReconocimientoVisual` permiten cambiar el modelo sin recompilar mediante el selector de interfaz o variables de entorno:

```powershell
$env:ECOACCESS_OLLAMA_URL = "http://127.0.0.1:11434"
$env:ECOACCESS_VISION_MODELO = "lfm3-vl:latest"   # o "qwen3-vl:4b" cuando esté instalado y autorizado
$env:ECOACCESS_VISION_TIMEOUT_SEG = "45"
```

---

## 2. Compilación y pruebas automatizadas

Ejecutar desde la raíz del proyecto:

```powershell
# Compilar toda la solución (Debug o Release)
dotnet build EcoAccess.P3.sln -c Release

# Ejecutar las 2.388 pruebas unitarias, de arquitectura y batería masiva Vision35 (sin escrituras a Oracle)
dotnet test tests\unitarias\EcoAccess.P3.Tests -c Release --no-build --verbosity minimal

# Ejecutar solo las pruebas del módulo Vision35 (35 materiales, T001–T030, consenso multimodelo, FSM, offline)
dotnet test tests\unitarias\EcoAccess.P3.Tests -c Release --no-build --filter FullyQualifiedName~Vision35
```

> **Nota sobre la prueba omitida:** `EnlaceSimbolicoSeRechazaAntesDeClasificar` se marca omitida (`Inconclusive`) cuando Windows no tiene habilitado el Modo de Desarrollador o privilegio `CreateSymbolicLink`. No indica fallo de código.

---

## 3. Dataset `datasets/ecoaccess35/` e incorporación de nuevas fotos sin recompilar

### Estructura estándar de 12 subcarpetas

```powershell
# Auditar catálogo real y construir datasets/ecoaccess35/ (reales, validación, evaluación independiente)
powershell -ExecutionPolicy Bypass -File scripts\pruebas\vision\Auditar-Dataset35.ps1
powershell -ExecutionPolicy Bypass -File scripts\pruebas\vision\Construir-DatasetEcoAccess35.ps1
```

### Incorporar nuevas fotografías reales del stand sin recompilar Visual Studio

1. Copiar las nuevas fotos (`.jpg`, `.jpeg`, `.png` o `.webp`) dentro de `datasets/ecoaccess35/bandeja_entrada/`.
2. Detectar archivos nuevos, verificar integridad, rechazar duplicados por SHA-256 y generar sugerencias:
   ```powershell
   powershell -ExecutionPolicy Bypass -File scripts\pruebas\vision\Incorporar-NuevasFotos35.ps1 -SugerirConOllama
   ```
3. Revisar manualmente las etiquetas en `datasets/ecoaccess35/anotaciones/pendientes_revision.json`, definir `codigos_esperados` y `combinacion_esperada`, y cambiar `"revisado_humano": false` a `true`.
4. Promover únicamente las imágenes aprobadas y ejecutar evaluación automática:
   ```powershell
   powershell -ExecutionPolicy Bypass -File scripts\pruebas\vision\Incorporar-NuevasFotos35.ps1 -AprobarRevisadas
   ```

---

## 4. Auditoría de Ollama, evaluación individual y comparativa A/B (`LFM3-VL` vs `Qwen3-VL`)

```powershell
# 1. Verificar versión de Ollama y capacidades reales ('vision') de cada modelo instalado
src\EcoAccess.P3.Bot\bin\Release\net48\EcoAccess.P3.Bot.exe --auditar-vision-ollama

# 2. Probar una sola imagen
src\EcoAccess.P3.Bot\bin\Release\net48\EcoAccess.P3.Bot.exe --probar-imagen35 assets\imagenes\residuos\verde\banano.jpg --modelo lfm3-vl:latest

# 3. Evaluar catálogo real o evaluación independiente multirresiduo
src\EcoAccess.P3.Bot\bin\Release\net48\EcoAccess.P3.Bot.exe --evaluar-catalogo35 datasets\ecoaccess35\evaluacion_independiente\manifiesto_evaluacion_independiente.json --modelo lfm3-vl:latest --salida reportes\vision35\eval_independiente_lfm3vl

# 4. Comparar LFM3-VL vs Qwen3-VL bajo exactamente las mismas imágenes y reglas (requiere qwen3-vl:4b instalado)
src\EcoAccess.P3.Bot\bin\Release\net48\EcoAccess.P3.Bot.exe --comparar-modelos35 datasets\ecoaccess35\evaluacion_independiente\manifiesto_evaluacion_independiente.json --modelo-a lfm3-vl:latest --modelo-b qwen3-vl:4b --salida reportes\vision35\comparativa_lfm3_vs_qwen3vl

# 5. Repetir únicamente los casos fallidos de una corrida anterior
src\EcoAccess.P3.Bot\bin\Release\net48\EcoAccess.P3.Bot.exe --evaluar-catalogo35 datasets\ecoaccess_35\manifiesto_real_catalogo.json --repetir-fallidos reportes\vision35\eval_real_catalogo_15_v3.json --salida reportes\vision35\eval_reintento
```

---

## 5. Seguridad de Oracle Cloud y hardware físico

- **Oracle Cloud (`ECOACCESS_P3_APP`):** protegido contra escrituras de pruebas automatizadas. Nunca subir contraseñas, tokens ni wallets (`Wallet_*.zip`, `cwallet.sso`, `ewallet.p12`). Las credenciales residen únicamente en User Secrets (`ecoaccess-p3-framework`) o `ECOACCESS_ConnectionStrings__OracleP3`.
- **Esquema aislado de pruebas (`ECOACCESS_P3_PRUEBAS`):** para ejecutar las 192 pruebas de `EcoAccess.P3.Datos.Tests`, crear primero un esquema local aislado con `scripts/pruebas/Crear-Esquema-Aislado-Local.ps1` (requiere Oracle FREE local en `localhost:1521/freepdb1` y autorización explícita).
- **Migración `V028`:** `database/migrations/V028_catalogo_35_prioritarios_y_multirresiduo.sql` y `V028_reversion.sql` están listas como plantilla y **no se aplican automáticamente a producción**.
- **Hardware físico (MODO 1 vs MODO 2):** las pruebas automáticas operan exclusivamente en **MODO 1 (Simulación)** con `GuardiaHardware` activo. En **MODO 2 (Hardware físico supervisado)**, cuando el ESP32 no cuenta con sensores de posición de tapa, el sistema registra explícitamente `TieneSensorFisico = false` y `ConfirmacionObservacionalRequerida = true`, sin afirmar apertura física confirmada por sensores inexistentes. El modo predeterminado es **Secuencial guiado**; el modo **Simultáneo supervisado** es opcional.

---

## 6. Comparativa real LFM3-VL vs Qwen3-VL y escalado automático de fotos de celular

Para instalar `qwen3-vl:4b` en otro computador con Ollama:

```powershell
ollama pull qwen3-vl:4b   # ≈ 3,34 GB (recomendado en GPUs con ≥6–8 GB VRAM para escenas multirresiduo)
```

- **Escalado seguro en memoria:** `ClienteVisionOllamaMultirresiduo` reduce automáticamente a `≤ 768 px` las fotos de alta resolución (p. ej., `1200×1600` de celular) antes de enviarlas a Ollama sin modificar los archivos originales.
- **Comparativa A/B real sobre los 3 casos base (`reportes/vision35/comparativa_3base_real_lfm3_vs_qwen3vl.md`):**
  - `lfm3-vl:latest` (predeterminado): **3/3 (100,0 %)**, 0 falsas aperturas, 0 errores, latencia media **9,02 s** (P95 19,41 s; 100 % GPU).
  - `qwen3-vl:4b` (alternativa configurable): **3/3 (100,0 %)**, 0 falsas aperturas, 0 errores, latencia media **84,78 s** (P95 103,70 s; `68% CPU / 32% GPU` en GTX 1650 de 4 GB VRAM).
# LFM3-VL: recuperación y optimización tras el formateo

Fecha: 2026-11-09 · Motor: **Ollama 0.40.15** (local, `127.0.0.1:11434`, `OLLAMA_NO_CLOUD=1`) · Modelo: **`lfm3-vl:latest`** (1,5 GB, digest verificado por Ollama) · Equipo: NVIDIA GeForce GTX 1650 (4 GB), 32 GB RAM, Windows 11.

Ollama es el **motor de inferencia** y LFM3-VL el **modelo**: se usan juntos; uno no reemplaza al otro.

## 1. Configuración conservada (código en `src/EcoAccess.P3.Bot/Vision/ClasificadorOllamaLocal.cs`)

| Parámetro | Valor | Motivo (ver `07_reconocimiento_visual.md`) |
|---|---|---|
| Lado mayor de la imagen | 512 px | suficiente para el objeto y barato de codificar |
| Contexto | 2048 | vocabulario cerrado cabe holgado |
| Temperatura | 0 | respuestas reproducibles |
| `think` | `false` | LFM3-VL razona por defecto y agota los tokens sin responder |
| Salida | JSON con esquema, validado en C# | nunca se confía en texto libre |
| Vocabulario | cerrado (catálogo de Oracle) | el modelo no inventa códigos |
| Una foto a la vez | `SemaphoreSlim(1,1)` | la GPU de 4 GB no admite dos inferencias simultáneas sin degradarse |
| Foto borrosa | `CalidadImagen.Insuficiente` no consulta el modelo | el modelo inventa antes que abstenerse |
| Confirmación del ciudadano | obligatoria | la propuesta no autoriza apertura |

## 2. Recuperación realizada

1. `winget install Ollama.Ollama` → 0.40.15.
2. Variable de usuario `OLLAMA_NO_CLOUD=1` y reinicio del servidor para que la herede.
3. `ollama pull lfm3-vl` → `lfm3-vl:latest`, verificado `sha256`.
4. User Secrets `ecoaccess-p3-framework`: `Vision:Modo=Local`, `Vision:Proveedor=OllamaLocal`, `Vision:Ollama:ServidorSinNubeVerificado=true`. No se configuró Groq (segundo motor de pago): presupuesto 0.
5. Corpus público TrashNet (MIT, revisión `6fa2b87…`) en `C:\ProgramData\EcoAccessP3Framework\Modelos\trashnet-6fa2b87` (2 527 fotos; hash de control coincide). Las fotos **no** se versionan.

## 3. Resultados medidos en este equipo

### 3.1 Pruebas del proyecto con visión real

| Prueba | Resultado |
|---|---|
| `VisionLocalRealTests` (3 fotos con SHA-256 fijado: papel, vidrio, cartón) | **3/3 aciertos**, 2,66–2,80 s, 0 fallos |
| `BridgeOracleTests.RecorridoCompletoDeEco_FotoReal…` (foto real + Oracle Cloud) | superada |
| `BridgeOracleTests.FotoVisionReal_…` | **no superada por requisito previo**: necesita la tarifa ficticia del piloto, que el script `05_tarifas_piloto_simulado.sql` solo permite en el esquema aislado de pruebas (correcto en la base central) |
| `VisionAmpliaRealTests` (24 fotos) | **no ejecutada**: el manifiesto de partición `ECOACCESS_PARTICION_VISION` no está en el repositorio |
| `EcoAccess.P3.Bot --comprobar-vision-local glass12.jpg` | `VIDRIO` identificado en 3,6 s; la admisión consultó Oracle y respondió que la caneca piloto no lo recibe (correcto) |

### 3.2 Medición propia: 60 fotos con el catálogo real de Oracle Cloud

10 fotos por clase de TrashNet (semilla 2026), catálogo de 40 materiales leído de Oracle Cloud. Evidencia: `resultados/lfm3vl_oracle_cloud_60.json`.

| Métrica | Valor |
|---|---|
| Fallos técnicos | **0 / 60** |
| Con código del catálogo | 41 |
| Sin código (objeto reconocido fuera del catálogo: 11; «sin residuo»: 8) → **Plan B** | 19 |
| Categoría correcta (de las 60) | **28** (47 %); de las 41 con código, 28 (68 %) |
| Código exacto en cartón, vidrio y papel | 19 / 30 |
| Por clase (categoría correcta) | cartón 8/10, vidrio 9/10, papel 4/10, plástico 4/10, metal 2/10, basura 1/10 |
| Latencia mediana / P95 / máximo | **3,21 s / 3,27 s** / 7,79 s (la primera foto incluye la carga del modelo) |
| Concurrencia: 5 fotos a la vez | se atienden en fila: 3,2 s, 6,4 s, 9,6 s, 12,8 s, 16,1 s |

Comparación honesta con la referencia histórica (38/50, P95 2,45 s): es **otro conjunto y otro catálogo** (40 materiales ampliados frente al vocabulario anterior) y otra GPU, así que no son comparables directamente. La latencia cumple el objetivo del proyecto (P95 < 5 s). La precisión **no** alcanza un nivel para abrir sin confirmación: por eso la persona siempre confirma y existe el Plan B.

## 4. Plan B (continuidad)

Si el modelo falla, tarda más de 45 s, no está instalado o no reconoce el objeto, el bot:
* informa que no pudo reconocer la foto (nunca lo presenta como clasificación visual);
* ofrece la **selección manual** por categorías del catálogo de Oracle;
* conserva las mismas validaciones (admisión, caneca en servicio, orden firmada) y el registro en Oracle;
* no abre nada sin confirmación.

La configuración `Vision:Proveedor=Desactivada` fuerza el Plan B para la demostración.

## 5. Concurrencia y despliegue

Una GPU de 4 GB atiende una foto a la vez; con varios ciudadanos enviando fotos, la cola crece ~3,2 s por foto. No se asume que una VM sin GPU logre estos tiempos: un servicio centralizado gratuito (por ejemplo Oracle Cloud Always Free, ARM sin GPU) debe medirse antes de usarse; mientras tanto la visión corre en el equipo con GPU y el Plan B cubre la ausencia del modelo.

# Informe de cierre: misión nocturna y misión final (12/11/2026)

> **NO SE REALIZARON MOVIMIENTOS FÍSICOS DURANTE LA MISIÓN NOCTURNA.** No se movió ningún servo, no se cargó firmware, no se
> reinició el ESP32 y no se inició ningún bridge. El bot que ya corría (`--polling`, copia `EcoAccess-P3`) no se tocó, y no
> había ningún puerto COM presente. Ningún resultado de este informe proviene de hardware real.

Rama de trabajo: `desarrollo/mision-nocturna-ia`. Se publicó por avance rápido (*fast-forward*) en `desarrollo/telegram-doble-ia`,
la rama del PR #11 abierto. Partió de `edcbc13` y conserva el diagnóstico de la orden 1683.

**Estados:**
- **Completada y validada en software:** probada con pruebas automatizadas y, cuando se indica, con Oracle (esquema aislado) o con el modelo real.
- **Completada con simulación:** probada solo con las canecas virtuales.
- **Pendiente de prueba física:** necesita la estación y al propietario presente.
- **Pendiente por limitación técnica:** no se pudo hacer con los recursos disponibles.

## 1. Inteligencia artificial

| Dato | Valor |
|---|---|
| Modelo | `lfm3-vl:latest` local (Ollama, 1,7 B, GGUF Q4_K_M). No se cambió de modelo |
| Imágenes reales recopiladas | 710 candidatas de Wikimedia Commons; **281 etiquetadas** (271 de un objeto y 10 con varios) |
| Entrenamiento / validación / prueba | **196 / 33 / 42**, separadas por autor |
| Ruta de disposición correcta, antes → después | prueba 76,2 % → **83,3 %**; validación 66,7 % → **75,8 %**; total 73,8 % → **79,0 %** |
| Falsas admisiones, antes → después | 11 → **5** (lista en `docs/vision/EVALUACION_BANCO_MISION_NOCTURNA.md`) |
| Varios residuos (10 fotos) | conjunto exacto 2/10; BLANCA F1 0,89; NEGRA recall 0,14 |
| Entrenamiento real | **No viable**: GGUF cuantizado, GTX 1650 de 4 GB, sin Python ni PyTorch. Se adaptaron reglas, catálogo y lenguaje, y se midieron |
| Meta de 330 fotos (400 de evaluación) | **No alcanzada**: 281. Sin cobertura suficiente: ropa, higiene, metales, multiobjeto y negativos |

Errores pendientes:
- 5 falsas admisiones: envases químicos leídos como PET, un medicamento como cartón, un aerosol como lata y un vaso
  de vidrio como botella;
- la caneca NEGRA casi nunca aparece en fotos con varios residuos.

## 2. Catálogo, matriz y reglas

| Funcionalidad | Estado |
|---|---|
| Descripción concreta («una manzana verde», «una cáscara de banano») | Completada y validada en software |
| Catálogo de **307 tipos**, 235 nuevos y ninguno con material, así que no pueden abrir | Completada y validada en software |
| Matriz de decisiones versionada (13 rutas) | Completada y validada en software |
| Electrónica → RAEE; ropa → donar; higiene: producto o envase; sanitarios → NEGRA; cortopunzantes | Completada y validada en software |
| Correcciones de seguridad del intérprete: «pill bottle», «pesticide container» y «coffee capsule» | Completada y validada en software |

Documentos: `docs/vision/CATALOGO_OBJETOS_COLOMBIA.md`, generado desde el código, y
`docs/operacion/SEIS_CONTENEDORES_Y_REGLAS_DE_DISPOSICION.md`.

## 3. Varios residuos y coordinador de aperturas

| Funcionalidad | Estado |
|---|---|
| Análisis de varios residuos (6 casos de la misión, escenarios B, D y E) | Completada y validada en software |
| En Telegram: una línea por residuo con su caneca o ruta; **no abre**, pide depositarlos de uno en uno | Completada y validada en software. Falta probarla en el Telegram real |
| Coordinador: agrupa, sin duplicados, en orden fijo, una tapa a la vez, fallos parciales, idempotente | **Completada con simulación** |
| Las 7 combinaciones; 126 operaciones concurrentes con fallas sin dos tapas a la vez | **Completada con simulación** |
| Apertura física de 2 o 3 canecas | **Pendiente de prueba física**. El código la impide: el coordinador rechaza ejecutores no simulados |

## 4. Contenedores (exactamente seis)

| Id | Configurado | Validado | Necesita supervisión física |
|---|---|---|---|
| REAL-BLANCA (1093, GPIO 19) | Sí, sin cambios | Estático: ids y GPIO | Sí |
| REAL-NEGRA (1401, GPIO 18) | Sí, sin cambios | Estático | Sí. Especial atención |
| REAL-VERDE (1400, GPIO 23) | Sí, sin cambios | Estático | Sí |
| VIRTUAL-BLANCA | Sí | Pruebas funcionales en el simulador | No |
| VIRTUAL-NEGRA | Sí | Pruebas funcionales en el simulador | No |
| VIRTUAL-VERDE | Sí | Pruebas funcionales en el simulador | No |

**La apertura de 90° no está confirmada:** se conservó la calibración validada (0°/60°).

## 5. Telegram

| Funcionalidad | Estado |
|---|---|
| Primera vez: «🌱 ¡Bienvenido a EcoAccess!» con «✨ Crear mi cuenta», «🔗 Ya tengo cuenta» y «🌱 Continuar como invitado» | Completada y validada en software |
| Registro en 3 pasos, en la página HTTPS de un solo uso; sin datos ni contraseñas en el chat; correo único; un enlace vencido no deja registros a medias | Completada y validada en software (reutiliza las validaciones existentes) |
| Un usuario existente no vuelve a registrarse | Completada y validada en software |
| Menú de 8 opciones; una foto directa se procesa sin pasar por el menú | Completada y validada en software |
| Lo no admitido recibe su ruta y dónde entregarlo en Valledupar | Completada y validada en software |
| Prueba en el Telegram real | **Pendiente**: el bot activo es una compilación anterior |

## 6. Valledupar

| Dato | Estado |
|---|---|
| Unicentro, Cra. 9 #6-42: listado por la Fundación Puntos Verdes Lito (RAEE, pilas, lámparas) | **Listado sin confirmar**: Eco lo muestra con advertencia |
| CORPOCESAR, punto amarillo de plaguicidas, con su dirección publicada | Listado sin confirmar |
| Punto Azul, Pilas con el Ambiente, Lúmina y Red Posconsumo | Solo programas, sin dirección |
| Puntos de donación de ropa, zapatos o gafas | **Pendiente de verificación**: Eco lo dice expresamente |
| Tabla `PUNTO_DISPOSICION` (V026 y R026) | Completada y validada **solo en el esquema aislado** (2/2 en dos corridas). **No aplicada en producción** |

## 7. Oracle y concurrencia

| Tema | Resultado |
|---|---|
| Esquema | Sin cambios en producción esta noche. V026 es aditiva y solo se aplicó en `ECOACCESS_P3_PRUEBAS` |
| Aislamiento simulado/real | El simulador no escribe en Oracle. Las pruebas de carga usan el esquema aislado. `GuardiaHardware` bloquea los COM y los comandos en pruebas |
| 408 con 30 registros simultáneos | Causa medida: 96 % de CPU con 30 PBKDF2 y 0 bytes leídos con hilos libres. Corrección: como mucho la mitad de los núcleos procesa registros. Sin tope, 1 de 2 pasadas falló; con tope, **3 de 3 pasadas completas sin fallos** (212 correctas, 0 fallidas, 7 omitidas). Tres pasadas son evidencia, no garantía |
| Integración del bridge entre procesos | Una regresión propia de la guardia rompió 13 recorridos. Se corrigió: la guardia solo restringe comandos |

## 8. Regresión final

| PRUEBA | ENTORNO | COMANDO | RESULTADO | COMMIT |
|---|---|---|---|---|
| Compilación de la solución | Windows, .NET Framework 4.8 | `dotnet build EcoAccess.P3.sln -c Release` | 0 errores | `7513ed0` |
| Unitarias | Local, sin hardware (guardia activa) | `dotnet test tests/unitarias/...` | **2074 superadas, 0 fallidas, 1 omitida** | `7513ed0` |
| Integración completa | Esquema aislado, Oracle real | `dotnet test tests/integracion/...` (3 pasadas) | 212 correctas, 0 fallidas, 7 omitidas en cada una | `f056afc` + `ea32c78` |
| V026 | Esquema aislado | filtro `PuntosDisposicion` | 2/2, en dos corridas | `1f4032a` |
| Visión, antes y después | Ollama local, 271 fotos | `--evaluar-objetos` (3 conjuntos × 2 versiones) | tabla del apartado 1 | `7513ed0` |
| Varios residuos | Ollama local, 10 fotos | `--evaluar-multiobjeto` | 2/10 exactas | `7513ed0` |
| Hardware | — | — | **No ejecutado: prohibido durante la ausencia** | — |

## 9. GitHub

Commits de la misión, del más antiguo al más reciente:

| Commit | Contenido |
|---|---|
| `5ad65b6` | Guardia de hardware |
| `4afecf7` | Descripción concreta y catálogo |
| `28fe16c` | Varios residuos y coordinador |
| `77cdbba` | Telegram |
| `f056afc` | Evaluación multiobjeto y documentos |
| `ea32c78` | 408 y guardia |
| `67a18a6` | Catálogo de 307 tipos, matriz, seis representaciones y Valledupar |
| `1f4032a` | V026 |
| `2781d36` | Documentación |
| `7513ed0` | Evaluación del banco |

Todos están en el PR #11 (rama `desarrollo/telegram-doble-ia`). No se creó otro PR ni se fusionó nada.

## 10. Lo que falta para habilitar las funciones nuevas sobre las canecas físicas

1. Seguir `docs/operacion/GUIA_PRUEBAS_SUPERVISADAS_13-11-2026.md` con el propietario presente: calibración, una tapa a la
   vez (especialmente la negra), fotos individuales, y comprobar que una foto con varios residuos no abre nada.
2. Corregir las 5 falsas admisiones, ajustando solo con las fotos de entrenamiento, y volver a medir la prueba.
3. Recién entonces decidir si se conecta el coordinador a la estación real: hoy lo impide el código, a propósito.
4. Aplicar V026 en producción y confirmar en sitio los puntos de Valledupar.
5. Reiniciar el bot de `EcoAccess-P3` con la versión nueva y probar el registro y el menú en el Telegram real.
6. Ampliar el banco (ropa, higiene, metales, multiobjeto) hacia las 400 fotos de evaluación.

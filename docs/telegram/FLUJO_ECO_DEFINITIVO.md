# Flujo definitivo de Eco

Fecha: 3 de noviembre de 2026. **No se ha confirmado desde un teléfono ni con el ESP32.** Las pruebas de este documento usan motores falsos, el modelo local real o la caneca simulada, según se indica.

## Antes y ahora

| Paso | Antes | Ahora |
|---|---|---|
| Saludo | Bienvenida larga y 12 botones | Una frase y `☰ Menú` |
| Foto | «¿Es correcto?» y dos botones | Si pasa las reglas, abre sin preguntar |
| Mientras analiza | Mensaje «Estoy identificando…» | «escribiendo…» de Telegram |
| Duda | Varios mensajes según la causa | `🤔 No pude identificarlo con seguridad.`, con `📸 Otra foto` y `✏️ Elegir material` |
| Ventana | 20 s | 10 s desde la apertura confirmada |
| Fin | Agradecimiento al detectar el depósito | Agradecimiento al cerrar, valoración opcional, despedida y menú |

## Conversación

```text
Persona: /start, Hola o Buenas, o directamente la foto
Eco:     🌱 ¡Hola, soy Eco! Envíame una foto de tu residuo. 📸       [☰ Menú]

Persona: foto
Eco:     ♻️ Detecté una lata.
         🔓 ¡Caneca abierta! Tienes 10 segundos para depositarla.

Al cerrar:
Eco:     🌱 ¡Gracias por cuidar nuestro planeta! 💚
         ⭐ ¿Cómo fue tu experiencia con Eco?                        [⭐ … ⭐⭐⭐⭐⭐] [Omitir]

Al valorar u omitir:
Eco:     💚 Pequeñas acciones, grandes cambios.
         🌿 ¿Te gustó EcoAccess? ¡Descubre todo lo que puedes hacer!
```

Botones finales: `📸 Depositar otro residuo`, `🪙 Mis ecopuntos`, `📊 Mi actividad`, `🌎 Explorar EcoAccess`, `🌿 Próximamente` y `☰ Menú completo`.

Con la caneca simulada, el aviso de apertura añade `🧪 Caneca simulada: no hay movimiento físico.` Se conservó a propósito: quitarlo presentaría una simulación como una apertura real.

## Riesgo de abrir sin confirmación

**Conclusión: LFM3-VL no permite abrir sin confirmación en todos los casos.** Se implementó la excepción mínima: abre solo si el material y el objeto coinciden en los pares con mejor evidencia. Lo demás pide un toque. La confianza declarada no interviene, porque casi siempre sale alta.

Pares con apertura automática: lata con aluminio, envase de vidrio con vidrio, caja o cartón con cartón, y botella con plástico. Además exige material aprovechable, admitido por la caneca y no visiblemente sucio.

Medido una vez con LFM3-VL real y el catálogo de la base del proyecto (`docs/vision/resultados/riesgo_apertura_*.json`):

| Conjunto | Fotos | Abriría sola | Material correcto | Material equivocado | Sanitario o peligroso abierto |
|---|---|---|---|---|---|
| Validación | 84 | 32 (38 %) | 28 | 4 | 0 |
| Corpus de comparación | 198 | 82 (41 %) | 76 | 6 | 0 |

Errores observados: papel por cartón (4), plástico por vidrio (3), vidrio por botella (1) y dos objetos fuera del catálogo tomados por lata.

Cómo leerlo:

- **Cero no significa imposible.** Fueron 0 casos en 114 aperturas automáticas, sin fotos en común entre ambos conjuntos. El límite superior aproximado al 95 % ronda el 3 %. Dos aperturas fueron objetos fuera del catálogo, y su etiqueta no dice si eran peligrosos.
- **No es una medición ciega.** La regla se eligió después de ver los errores de estos mismos conjuntos. Hace falta medirla con fotos nuevas.
- Son fotos limpias de bases públicas. Las fotos reales de un teléfono pueden dar peor resultado.
- Un material equivocado no otorga puntos: los ecopuntos siguen esperando la verificación del depósito.

### Botellas plásticas: pedirán un toque

LFM3-VL casi nunca nombra la botella: la clasifica como envase plástico rígido. Ese par tiene errores que incluyen residuos sanitarios: 8 aciertos y 4 errores en validación, entre ellos un condón y una servilleta usados. Por eso **hoy una botella plástica normalmente pide `✅ Sí, continuar`**. El ejemplo del mensaje `Detecté una botella plástica` solo aparece si el modelo reconoce la botella. Mejorar esto exige más fotos de botellas y medir de nuevo, sin relajar la regla.

## Condiciones para ordenar la apertura

| Condición | Cómo se comprueba |
|---|---|
| Sesión de depósito | Interacción vigente de esa persona, guardada en Oracle |
| Asociación con la caneca y presencia | Caneca física: QR escaneado hace menos de 5 minutos. Caneca simulada: no aplica |
| Material admitido | Catálogo y caneca en Oracle, otra vez al emitir la orden |
| Clasificación | `PoliticaAperturaAutomatica` |
| Dispositivo disponible | Puertos locales del bridge y del dispositivo; una comprobación fallida se trata como no disponible |
| Sin órdenes duplicadas | Orden vigente de la caneca, clave de idempotencia y update repetido |
| Orden no vencida | Vigencia de 2 minutos; el comando vuelve a validarse antes de actuar |
| Sin alertas físicas | Sin evento de fallo en 15 minutos que una apertura posterior no haya superado |

**Límite del QR:** es fijo, así que una foto suya puede reutilizarse a distancia. Demuestra menos presencia de la que parece. Antes de operar la caneca física sin supervisión hace falta un QR rotativo o un botón en la caneca. Tampoco se habilitan movimientos desatendidos: sigue vigente `docs/hardware/GUIA_VALIDACION_ESP32.md`.

**Foto sin QR en caneca física:** Eco guarda solo la referencia de Telegram durante 10 minutos, pide escanear y, al hacerlo, la analiza sin que se envíe de nuevo. La presencia vive en memoria: reiniciar el bot obliga a escanear otra vez.

## Corrección

| Momento | Qué pasa |
|---|---|
| Antes de abrir (recorrido de un toque) | Ofrece cambiar material, otra foto, segunda opinión externa y menú |
| Con la orden emitida | Responde que no se repite, deja constancia en `analisis_imagen` y ofrece otra foto. No crea otra orden |

La segunda opinión con Qwen solo se ofrece desde la corrección y solo sale con autorización. La incertidumbre nunca envía una foto a Groq por sí sola.

## Cambios técnicos

| Área | Cambio |
|---|---|
| Telegram | Saludo, menú de 10 opciones, despedida, secciones de Próximamente, «Otra foto» y «escribiendo…» |
| Máquina de estados | Sin estados nuevos. Reutiliza `CanecaIdentificada`, `EsperandoConfirmacion` y `OrdenEmitida`; la orden automática sigue el mismo camino que el botón |
| Oracle | **Sin migraciones.** Los avisos cambian de consulta: el cierre agradece y una orden vencida sin apertura se avisa. La traza visual queda activa salvo `Vision:RegistrarTraza=false` |
| Bridge | `EstadoCanecaLocal` comprueba disponibilidad. La espera del recibo de cierre pasa a 20 s |
| ESP32 | `ABIERTO_MS = 10000` en `configuracion_ejemplo.h`; las protecciones no cambian. Compila, pero **no se ha cargado a una placa** |
| Simulador | `Bridge:VentanaSimuladaSegundos = 10`, también en el archivo local de este equipo |

Un ESP32 cargado antes de este cambio seguiría abierto 20 s: el bridge lo daría por fallido al no recibir el cierre a tiempo. Hay que volver a cargar el firmware antes de usarlo, y su `configuracion_privada.h` debe decir también 10000.

**Los 10 segundos cuentan desde que el firmware aplica la orden PWM.** No hay sensor de posición, así que el tiempo real con la tapa abierta será algo menor, según lo que tarde en moverse. Hay que medirlo con cronómetro antes de decidir si necesita un ajuste, y comprobar que 10 s son seguros para la mano y para la tapa.

## Fallo encontrado en la prueba real del teléfono

El 3 de noviembre a las 05:09, hora de Colombia, la primera foto de una cuenta invitada se rechazó en 211 ms, sin llegar al modelo. La foto creaba la sesión solo en memoria, y la validación del invitado la buscaba en Oracle: «La solicitud no pertenece a este diálogo». La segunda foto sí habría pasado. Ahora la sesión se guarda antes de validar, y `PrimeraFotoDeUnInvitadoSinStartAbreLaCanecaSimulada` lo cubre. El texto que recibió el teléfono no se registró; se dedujo del código, del tiempo y del estado en Oracle.

## Actualización: reconocimiento general

Lo que no se admite ya no pasa por este recorrido: recibe una respuesta propia. Las botellas suelen abrir sin toque cuando dos lecturas coinciden, y una solicitud vencida ya no bloquea una foto nueva. Ver `docs/vision/RECONOCIMIENTO_GENERAL.md`.

## Resultados

Conjuntos sobre una instalación desde cero en un Oracle temporal, ya retirado, con el piloto simulado:

| Conjunto | Aprobadas | Fallidas | Omitidas |
|---|---|---|---|
| Unitarias | 1531 | 0 | 1 |
| Integración | 195 | 0 | 5 |
| Modelo real | 3 | 0 | 1 |

Tiempos del recorrido con LFM3-VL real, bridge y caneca simulada, **sin Telegram real**:

| Medida | Modelo cargado | Modelo sin cargar |
|---|---|---|
| Foto hasta orden | 3,1 s | 12,7 s |
| Foto hasta apertura | 5,3 s | 14,6 s |
| Apertura hasta cierre, visto desde Oracle | 9,6 s | 10,1 s |

La diferencia entre 9,6 y 10,1 s viene de consultar Oracle a intervalos: mide la ventana con esa resolución, no con un cronómetro. El bot carga el modelo al iniciar y lo mantiene 2 horas tras cada foto.

| # | Prueba | Estado | Evidencia |
|---|---|---|---|
| 1, 2 | `/start` y `Hola` | **automática** | `ElSaludoEsUnaFraseConUnSoloBotonDeMenu`. Falta el teléfono |
| 3 | Foto directa | **automática** | `PrimeraFotoDeUnInvitadoSinStartAbreLaCanecaSimulada`. Falta el teléfono |
| 4 | LFM3-VL | **real** | Lata de foto pública, `LATA_ALUMINIO`, 2,9 s; 282 fotos medidas |
| 5 | Qwen 3.8 | **real, una foto pública** | `LATA_ALUMINIO`, 3,1 s. La comparación sigue sin ganador |
| 6 | Oracle identifica el material | **integración** | Recorrido completo |
| 7 | Admitido abre solo | **integración** | Modelo real, caneca simulada |
| 8, 9 | Rechazado o ambiguo no abren | **automática** | `MaterialRechazado…`, `ImagenAmbigua…` |
| 10 | Una sola orden | **integración** | Recorrido y concurrencia con Oracle |
| 11, 12 | 10 s y cierre | **simulador** | Falta el ESP32 |
| 13–18 | Agradecimiento, valoración, omitir, eslogan, menú, Próximamente | **automática** | `FlujoEcoDefinitivoTests`. Falta el teléfono |
| 19–21 | Consecutivos, sigue respondiendo, correcciones | **automática** | `DepositosConsecutivos…`, `CorreccionTrasLaOrden…` |
| 22 | Otra cuenta | **pendiente** | Necesita a la persona |
| 23 | Pérdida de conexión | **parcial, real** | A las 11:08 UTC el bot perdió Telegram, reintentó solo y siguió atendiendo el sondeo. No se comprobó un mensaje enviado durante el corte |
| 24 | Integridad en Oracle | **integración** | Auditoría y pruebas de concurrencia |
| 25 | Funciones existentes | **automática** | Conjuntos anteriores. Se actualizaron 13 pruebas que describían el flujo viejo; ninguna se eliminó |

Un fallo observado durante las pruebas: `--comprobar-vision-local` respondió «no disponible» mientras otra inferencia ocupaba el modelo desde otro proceso. Repetida sola, identificó la lata. Dentro del bot las fotos hacen fila, así que dos personas no chocan.

## Límites

- **Valoración:** una por persona y día, regla ya existente. Una segunda el mismo día muestra la despedida y no cambia la primera. No es una por depósito.
- **Aviso al invitado:** antes, pulsar una estrella iba precedido por una explicación de que se guardaría con un seudónimo. El mensaje definitivo no la incluye; conviene ponerla en «Conoce EcoAccess» si se exige ese aviso legal.
- **Cuentas:** `Mi cuenta` no puede crear enlaces de vinculación hasta configurar una página HTTPS.
- **Otras personas al mismo tiempo:** si la caneca está ocupada, la segunda persona recibe `⏳` y vuelve a enviar la foto. No hay cola.

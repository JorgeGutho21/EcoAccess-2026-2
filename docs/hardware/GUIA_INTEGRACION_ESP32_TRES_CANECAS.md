# Guía de integración ESP32 — estación de tres canecas (11/11/2026)

Esta guía explica cómo conectar las tres canecas que construyó el equipo al software de EcoAccess P3 **sin rehacer la lógica**. Todo lo que dice sale del código y de Oracle Cloud, revisados el 11/11/2026; no se inventaron pines ni cableados.

## 0. Qué está probado y qué no

| Parte | Estado | Evidencia |
|---|---|---|
| Canecas físicas blanca, verde y negra | **Construidas, según el equipo**; no las vio ningún software | Información del equipo |
| Firmware de una tapa (`hardware/servosecoacces/servosecoacces.ino`) | IMPLEMENTADO SIN VALIDACIÓN física | Compila para `esp32:esp32:esp32`; pruebas de estado en `hardware/pruebas/` |
| Estación de tres tapas en software (`EstacionSimulada`) | VERIFICADO EN SIMULACIÓN | `tests/unitarias/.../EstacionSimuladaTests.cs`: una orden de la blanca nunca abre la verde ni la negra |
| Estación en Oracle (V024) | VERIFICADO REAL | Vista `VISTA_CANECAS_ESTACION`: 1093 blanca (izquierda), 1400 verde (centro), 1401 negra (derecha), con un ESP32 por caneca |
| Selección de caneca según la clasificación (`ResolutorDestino`) | VERIFICADO EN SIMULACIÓN | `EstacionTresCanecasTests.cs` y `EstacionTresCanecasOracleTests.cs` |
| Conexión de un ESP32 real con el Bridge (`--dispositivo-serial`) | IMPLEMENTADO SIN VALIDACIÓN | Código en `src/EcoAccess.P3.Bot/Dispositivos/`; en este PC no existe aún ningún `bridge.local.json` |
| Apertura física de cualquiera de las tres tapas | **PENDIENTE** | Ningún motor se ha movido desde el software |

**Estado actual en Oracle Cloud:** la blanca (1093) está **en servicio**. La verde (1400) y la negra (1401) están **fuera de servicio** a propósito (V024): mientras no estén validadas, Echo orienta hacia ellas («caneca verde», «caneca negra») pero **nunca emite una orden de apertura** para ellas.

## 1. Qué dispositivo controla cada caneca

Un ESP32 por caneca. Los identificadores ya están guardados en Oracle (`CONFIGURACION_CANECA`, migraciones V021 y V024):

| Caneca | `id_contenedor` | Color / categoría | Dispositivo (`identificador_dispositivo`) | Ubicación | Estado hoy |
|---|---|---|---|---|---|
| EcoAccess — Caneca piloto | 1093 | Blanca / 1 (aprovechable) | `esp32-piloto-blanca` | izquierda | En servicio |
| EcoAccess · Caneca verde | 1400 | Verde / 2 (orgánico aprovechable) | `esp32-piloto-verde` | centro | Fuera de servicio |
| EcoAccess · Caneca negra | 1401 | Negra / 3 (no aprovechable) | `esp32-piloto-negra` | derecha | Fuera de servicio |

Las tres comparten **el mismo QR oficial**, que identifica la estación. El QR no se regenera ni cambia.

## 2. Qué identificadores usa cada parte

- **`ID_CANECA`** (firmware) = `id_contenedor` de Oracle: 1093, 1400 o 1401.
- **`DISPOSITIVO`** (firmware) = `identificador_dispositivo`: `esp32-piloto-blanca`, `-verde` o `-negra`.
- **`VERSION_CLAVE`**: empieza en 1. Se sube solo cuando se rota la clave.
- **`CLAVE_BASE64`**: clave HMAC **propia de cada ESP32**, de 32 a 128 bytes. Nunca se comparte entre canecas ni se sube a Git.

Si alguno de estos cuatro valores no coincide entre el ESP32 y su Bridge, el ESP32 **rechaza** la orden: `idContenedor` distinto de `ID_CANECA`, otro `dispositivo` o una firma inválida.

## 3. Qué archivos configuran la conexión

| Archivo | Dónde | Qué contiene |
|---|---|---|
| `hardware/servosecoacces/configuracion_privada.h` | Copia de `configuracion_ejemplo.h`, **excluida de Git** | `DISPOSITIVO`, `ID_CANECA`, `VERSION_CLAVE`, `CLAVE_BASE64`, ángulos y calibración |
| `bridge-blanca.json`, `bridge-verde.json`, `bridge-negra.json` | Carpeta privada, por ejemplo `C:\ProgramData\EcoAccessP3Framework\Bridge\` | Configuración del Bridge de cada caneca (§4) |
| Clave HMAC de cada caneca | Archivo privado indicado en `Bridge:ArchivoClaveHmac` | Los mismos bytes que `CLAVE_BASE64` del ESP32 correspondiente |

Cada proceso del Bridge lee su propio archivo con la variable de entorno **`ECOACCESS_ARCHIVO_BRIDGE`** (`src/EcoAccess.P3.Bot/Program.cs`). La ruta debe ser absoluta y privada: `ConfiguracionBridge.ValidarRutaPrivada`.

## 4. Qué parámetros necesita cada Bridge

Claves que exige `ConfiguracionBridge.Leer` (`src/EcoAccess.P3.Bot/Dispositivos/ConfiguracionBridge.cs`). Ejemplo para la **blanca**; la verde y la negra cambian contenedor, dispositivo, puertos, carpeta y COM:

```json
{
  "Bridge": {
    "IdContenedor": "1093",
    "Dispositivo": "esp32-piloto-blanca",
    "VersionClave": "1",
    "ArchivoClaveHmac": "C:\\ProgramData\\EcoAccessP3Framework\\Bridge\\clave-blanca.bin",
    "Eventos": "http://127.0.0.1:8182/eventos/",
    "Comandos": "http://127.0.0.1:8183/comandos/",
    "DirectorioEstado": "C:\\ProgramData\\EcoAccessP3Framework\\Bridge\\estado-blanca",
    "PuertoSerial": "COM5"
  }
}
```

| Caneca | Eventos | Comandos | Carpeta de estado | COM |
|---|---|---|---|---|
| Blanca | 8182 | 8183 | `estado-blanca` | el que muestre Windows |
| Verde | 8184 | 8185 | `estado-verde` | el que muestre Windows |
| Negra | 8186 | 8187 | `estado-negra` | el que muestre Windows |

Reglas que el código hace cumplir:
- Los puertos de eventos y comandos deben ser locales y distintos entre sí.
- Las rutas deben terminar en `/eventos/` y `/comandos/`.
- El identificador del dispositivo solo admite letras, números, `-` y `_` (máximo 40 caracteres).

El número de COM no se fija en la guía: se toma del Administrador de dispositivos al conectar cada ESP32. Los puertos 8184–8187 son una propuesta: cualquiera libre y distinto sirve.

## 5. Cómo recibe una orden el ESP32

Recorrido completo, con las clases reales:

1. La persona escanea el QR oficial y envía la foto. Echo clasifica y `ResolutorDestino.Resolver` (`src/EcoAccess.P3.Logica/Servicios/DestinoEstacion.cs`) elige la caneca del color correcto **dentro de la estación**.
2. Solo si esa caneca está **en servicio** y el material está admitido, `ServicioInteracciones.AutorizarAperturaAsync` crea una `ORDEN_APERTURA` en Oracle, para **ese** `id_contenedor`.
3. El proceso `--bridge` de esa caneca (`BridgeLocal`) consulta `ObtenerOrdenVigenteAsync(IdContenedor)`. **Cada Bridge solo ve las órdenes de su contenedor.**
4. El Bridge la entrega por HTTP local al proceso `--dispositivo-serial` de la misma caneca (`DispositivoHttpConColas`).
5. `ActuacionSerial` abre el COM y hace el intercambio de desafío y respuesta con el ESP32 (§6).

## 6. Cómo valida el ESP32 que la orden le corresponde

En `servosecoacces.ino`, función `procesar`:

1. El Bridge pide un desafío. El ESP32 responde `{"ecoaccessDesafio":"<nonce>"}`, un número aleatorio de 16 bytes válido por poco tiempo (`VentanaDesafio`).
2. El Bridge envía la orden firmada con HMAC-SHA256 sobre `ecoaccess-p3/v1`, `DISPOSITIVO`, `VERSION_CLAVE`, el tipo y los datos.
3. El ESP32 acepta solo si se cumplen todas estas condiciones:
   - firma correcta;
   - `dispositivo` igual al suyo;
   - `idContenedor` igual a `ID_CANECA`;
   - el `desafio` es el último que emitió y sigue vigente;
   - la orden y su `claveIdempotencia` **no se usaron antes** (`reservar`, en memoria no volátil).

   Si algo falla, responde `rechazado` y **no mueve nada**.
4. Además no actúa si no está calibrado (`CALIBRACION_VERIFICADA = false`) o si quedó una ejecución pendiente tras un reinicio (`RECUPERACION_MANUAL_VERIFICADA`).

## 7. Cómo reconoce la ventana de diez segundos

- `ABIERTO_MS = 10000` en `configuracion_privada.h`.
- Tras aplicar el PWM de apertura, el ESP32 responde `pwm-apertura-aplicada`, espera `ABIERTO_MS` y cierra.
- La orden también trae `venceUtc`: el Bridge no la entrega vencida (`ServicioInteracciones.VencerOrdenesAsync`).
- No se cambia el valor para una demostración.

## 8. Cómo responde al software

- Cada respuesta es un sobre JSON firmado con el mismo HMAC, con `estado`: `pwm-apertura-aplicada`, `rechazado` o `incierto`.
- `ActuacionSerial` la verifica y el Bridge la convierte en eventos de apertura y cierre con clave `usb:<idOrden>:<sufijo>`.
- Un recibo confirma **la orden PWM**, no la posición real de la tapa ni que entró un residuo. No hay sensor.

## 9. Cómo registra Oracle el evento

- Los eventos llegan por `Bridge:Eventos` y `ServicioInteracciones.RegistrarEventoAsync` los guarda en `EVENTO_DISPOSITIVO`, con clave única: un reintento no duplica.
- La apertura confirmada crea el `DEPOSITO` con estado `declarada_sin_sensor` (`EstadoRegistroDeposito`). Queda **pendiente de verificación** por un operario antes de cualquier ecopunto confirmado.
- En la estación simulada esto ya está probado: `EstacionSimuladaTests` y `BridgeOracleTests`.

## 10. Cómo hacer una prueba segura (en este orden)

1. **Sin hardware:** iniciar el bot con `--dispositivo-simulado` y comprobar el recorrido completo. Ya está probado en software.
2. **Por cada ESP32, por separado:**
   1. Copiar `configuracion_ejemplo.h` a `configuracion_privada.h` con sus cuatro valores (§2) y `CALIBRACION_VERIFICADA = false`.
   2. Cargar el firmware **sin tapa ni servos conectados**.
3. Medir la fuente con los servos de esa caneca (`GUIA_VALIDACION_ESP32.md` §5). Una fuente de 5 V y 1 A **no está validada**.
4. Calibrar **sin tapa** los ángulos `CERRADO_*` y `ABIERTO_*`. Solo entonces poner `CALIBRACION_VERIFICADA = true`.
5. Crear `bridge-<color>.json`, iniciar ese par de procesos y enviar **una** orden de prueba **sin tapa**. Para la verde y la negra, ponerla en servicio solo durante la prueba y con autorización del administrador.
6. Con tapa: repetir un ciclo completo (abre, 10 s, cierra).
7. Revisar en Oracle el evento y el depósito (§9).
8. Pasar a la siguiente caneca. **Nunca se prueban las tres a la vez la primera vez.**

## 11. Cómo detectar fallos de conexión

| Síntoma | Causa probable | Dónde mirar |
|---|---|---|
| El Bridge no arranca: «Configura Bridge:IdContenedor…» | Falta una clave en el JSON | `ConfiguracionBridge.Leer` |
| «Falta puerto COM» | Falta `PuertoSerial` | JSON de esa caneca |
| El ESP32 responde `rechazado` | Clave, dispositivo, `ID_CANECA` o desafío distintos; orden ya usada | Comparar el `configuracion_privada.h` con el JSON |
| El ESP32 responde `incierto` | El desafío venció o la memoria no guardó la reserva | Reintentar con una orden nueva; revisar la alimentación |
| No pasa nada | ESP32 sin calibrar o en recuperación manual | Monitor serie a 115200 |
| La orden vence | El Bridge de esa caneca no está corriendo | Procesos `--bridge` y `--dispositivo-serial` de ese color |

## 12. Cómo comprobar que no se abre otra caneca

- Cada ESP32 compara `idContenedor` con su `ID_CANECA` y rechaza lo demás.
- Cada Bridge solo consulta órdenes de su `IdContenedor`.
- Prueba manual: con las tres conectadas y **sin tapas**, emitir una orden para la blanca y comprobar que solo su monitor serie muestra `pwm-apertura-aplicada`. La verde y la negra no deben recibir nada.
- Es la misma regla que ya prueba `EstacionSimuladaTests` en software.

## 13. Cómo detener el sistema ante un error

1. **Parar el bot sin que el vigilante lo reinicie:** crear `C:\ProgramData\EcoAccessP3Framework\Desarrollo\pausa-vigilante` y cerrar los procesos.
2. **Cortar la alimentación de los servos.** Es la parada física: el software no la reemplaza.
3. **Poner la caneca fuera de servicio** desde Windows Forms («Canecas y QR» → «Fuera de servicio»). Echo deja de emitir órdenes para ella.
4. **Si un ESP32 se reinició a mitad de ciclo**, queda bloqueado. Revisar la tapa a mano antes de poner `RECUPERACION_MANUAL_VERIFICADA = true`, y volver a `false` después.

## Pendiente del equipo antes de la primera prueba física

- **Confirmar cuántos servos tiene cada caneca y en qué pines.** El firmware actual usa **GPIO 18 y 19** para dos servos que mueven una tapa: `SERVO_1` y `SERVO_2` en `servosecoacces.ino`. Si las canecas nuevas usan otros pines u otra cantidad de servos, hay que confirmarlo y ajustarlo **antes** de cargar el firmware.
- Medir la fuente de cada caneca.
- Calibrar cada tapa.
- Generar las tres claves HMAC privadas, una por ESP32, con el procedimiento del Bridge privado.

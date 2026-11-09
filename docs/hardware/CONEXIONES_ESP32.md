# Conexiones de los ESP32 (tres canecas, alternativa B)

Cada caneca tiene **su propio ESP32**, sus dos servomotores y su fuente. El firmware es el mismo (`hardware/servosecoacces/servosecoacces.ino`); cambia solo `configuracion_privada.h` (excluido de Git).

## 1. Conexión de una caneca (se repite tres veces)

| Elemento | Conexión | Nota |
|---|---|---|
| Señal servo 1 | GPIO 18 | PWM 50 Hz por `ledcAttach(pin, 50, 16)` |
| Señal servo 2 | GPIO 19 | mueve la misma tapa que el servo 1 |
| Alimentación de servos (+) | fuente externa de 5–6 V | **nunca** el pin de 3,3 V ni el de 5 V del ESP32 |
| Tierra | **común** entre fuente, servos y ESP32 | sin tierra común la señal PWM no tiene referencia |
| ESP32 | USB al computador que ejecuta el bridge (`--dispositivo-serial`) | la misma conexión transporta los mensajes firmados |

## 2. Configuración privada por caneca

| Campo (`configuracion_privada.h`) | Blanca | Verde | Negra |
|---|---|---|---|
| `DISPOSITIVO` | `eco-blanca-01` | `eco-verde-01` | `eco-negra-01` |
| `ID_CANECA` | `id_contenedor` real de la blanca | … de la verde | … de la negra |
| `VERSION_CLAVE` | 1 | 1 | 1 |
| `CLAVE_BASE64` | clave HMAC **propia** | clave **distinta** | clave **distinta** |
| `CALIBRACION_VERIFICADA` | `false` hasta calibrar | ídem | ídem |

El `DISPOSITIVO` debe coincidir con `CONFIGURACION_CANECA.identificador_dispositivo` de esa caneca (restricción `UQ_CONFIGURACION_DISPOSITIVO`: no puede repetirse).

## 3. Alimentación

* La guía previa ya advertía que **una fuente de 5 V y 1 A no está validada** ni para los dos servos de una caneca bajo carga. No se asume suficiente para tres mecanismos.
* Antes de montar: medir con el servo bloqueado (corriente de arranque/bloqueo) y elegir la fuente con margen. Con la alternativa B cada fuente alimenta solo dos servos.
* Condensador electrolítico cerca de los servos para los picos, y fusible en la línea de 5 V.

## 4. Calibración (con verificación humana, sin tapa)

1. Cargar el firmware con `CALIBRACION_VERIFICADA = false`: **no mueve** los motores.
2. Sin tapa, verificar sentido de giro y topes de cada servo; ajustar `CERRADO_1/2` y `ABIERTO_1/2`.
3. Confirmar que los dos servos de la misma tapa no se oponen.
4. Solo entonces `CALIBRACION_VERIFICADA = true` y montar la tapa.
5. Repetir para cada caneca; nunca copiar la calibración de otra.

## 5. Seguridad física

* Al encender o reiniciar el firmware **no mueve** los motores; un reinicio a mitad de ciclo deja la caneca bloqueada hasta revisión (`RECUPERACION_MANUAL_VERIFICADA`).
* Ventana de depósito: `ABIERTO_MS = 10000` (10 s) y cierre automático.
* Mantener manos fuera del recorrido de la tapa durante las pruebas.

## 6. Estado

| Comprobación | Resultado |
|---|---|
| Firmware compila para `esp32:esp32:esp32` | registrado en `hardware/README.md` (325 159 bytes) en el equipo anterior; no recompilado aquí (Arduino IDE no instalado) |
| Conexión física de las tres canecas | **pendiente: no hay hardware conectado** |
| Calibración y consumo eléctrico medido | **pendiente** |

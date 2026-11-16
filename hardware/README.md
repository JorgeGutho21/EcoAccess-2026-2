# Caneca piloto: ESP32, dos servomotores, una tapa
> **Histórico (piloto de una caneca).** La estación actual tiene tres canecas, un servo por tapa: **BLANCA 1093 → GPIO 19**, **NEGRA 1401 → GPIO 18**, **VERDE 1400 → GPIO 23**, con el firmware `hardware/estacion_tres_canecas/`. No existe caneca azul. Ver `docs/hardware/INTEGRACION_FINAL_TRES_CANECAS_QR.md`.

El firmware está en `servosecoacces/`. Se comunica por USB, no guarda contraseñas Oracle ni tokens de Telegram y no inventa sensores.

## Conexiones

| Elemento | Conexión |
|---|---|
| Señal del servo 1 | GPIO 18 |
| Señal del servo 2 | GPIO 19 |
| Alimentación de los servos | fuente externa adecuada, nunca los 3,3 V del ESP32 |
| Tierra | común entre fuente, servos y ESP32 |

Los dos motores mueven la misma tapa. **La fuente de 5 V y 1 A no está validada** para dos motores bajo carga: mide arranque y bloqueo antes de montarlos.

## Preparación

1. Instala el paquete `esp32` de Espressif 3.x en Arduino IDE y selecciona la placa y el puerto COM reales.
2. Copia `configuracion_ejemplo.h` como `configuracion_privada.h` (excluido de Git) y escribe identidad, caneca, versión y clave HMAC del archivo privado del servicio de dispositivos.
3. Calibra sin tapa: comprueba sentido de giro, topes y que los motores no se opongan. Solo entonces pon `CALIBRACION_VERIFICADA = true`.
4. Compila y carga. Sin calibración el firmware no mueve los motores.
5. Añade `"PuertoSerial": "COMx"` a `bridge.local.json` e inicia el bot con `--bridge` y `--dispositivo-serial`. No inicies a la vez `--dispositivo-simulado`.

## Ciclo de una orden

1. El adaptador pide un desafío nuevo y confirma con Oracle que la orden sigue autorizada.
2. El ESP32 valida la firma HMAC, el desafío y la caneca, y reserva la orden en memoria persistente **antes** de mover nada.
3. Aplica la apertura y envía un recibo autenticado: EcoAccess registra la apertura y avisa al ciudadano.
4. Mantiene la ventana de 10 segundos (`ABIERTO_MS`), aplica el cierre y envía el segundo recibo.
5. Sin sensor, el depósito queda **pendiente de verificación** y no genera ecopuntos hasta que un operador lo confirme.

## Seguridad

- Una orden repetida o vencida no mueve la tapa; tampoco se reintenta una ejecución incierta.
- Al encender o reiniciar no se mueven los motores. Si se reinicia durante un ciclo, queda bloqueado hasta una revisión física (`RECUPERACION_MANUAL_VERIFICADA`).
- Un recibo confirma la orden PWM, **no** la posición física de la tapa ni el ingreso de un residuo.

## Estado (11/11/2026, con la placa conectada)

| Comprobación | Resultado |
|---|---|
| Placa | ESP32-D0WD-V3 rev. 3.1, 4 MB, COM3 (CP210x); placa `esp32:esp32:esp32` |
| Núcleo `esp32 by Espressif` | 3.0.7 instalado (el error «Missing FQBN» venía de no tenerlo) |
| Compilación de los cuatro sketches | correcta: estación 352 337 B, una tapa 344 913 B, calibración 315 169 B, diagnóstico 287 849 B |
| Carga en placa | correcta; `estacion_tres_canecas` queda cargado con las tres tapas fuera de servicio |
| Seguridad serie en la placa real | 11 de 11 (`docs/evidencias/esp32_fisico/`) |
| Banco nativo (`pruebas/*.cpp`) | se ejecuta en el CI; en este PC no hay compilador C++ de escritorio |
| Mapeo de pines, calibración, apertura física, 10 s medidos, consumo | **pendiente: requiere una persona presente; no se movió ningún motor** |

Sketches: `diagnostico_esp32` (identifica la placa, nunca mueve), `calibracion_seis_servos` (banco supervisado),
`estacion_tres_canecas` (producción, un ESP32 y tres tapas) y `servosecoacces` (variante de una tapa por ESP32).
Guía completa: `docs/hardware/GUIA_PASO_A_PASO_PROTOTIPO_FISICO.md`.

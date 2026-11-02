# Caneca piloto: ESP32, dos servomotores, una tapa

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
4. Mantiene la ventana de 20 segundos (`ABIERTO_MS`), aplica el cierre y envía el segundo recibo.
5. Sin sensor, el depósito queda **pendiente de verificación** y no genera ecopuntos hasta que un operador lo confirme.

## Seguridad

- Una orden repetida o vencida no mueve la tapa; tampoco se reintenta una ejecución incierta.
- Al encender o reiniciar no se mueven los motores. Si se reinicia durante un ciclo, queda bloqueado hasta una revisión física (`RECUPERACION_MANUAL_VERIFICADA`).
- Un recibo confirma la orden PWM, **no** la posición física de la tapa ni el ingreso de un residuo.

## Estado

| Comprobación | Resultado |
|---|---|
| Compilación para `esp32:esp32:esp32` con la ventana de 20 s | correcta: 325159 bytes |
| Banco nativo del estado (`pruebas/EstadoActuadorTests.cpp`) | no ejecutado en el equipo actual: falta compilador C++ de escritorio |
| Carga en placa, calibración, apertura física, 20 s medidos, desconexión, consumo | **pendiente: no hay hardware conectado** |
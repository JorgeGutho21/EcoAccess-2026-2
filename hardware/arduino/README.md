# Caneca piloto: dos servos, una tapa

Actualización31/10: el original de referencia sigue intacto. Para el firmware adaptado abre [hardware/servosecoacces/LEEME_PRIMERO.txt](../servosecoacces/LEEME_PRIMERO.txt) y su `.ino`. Recibe órdenes USB autorizadas, usa18/19 para la misma tapa y reserva órdenes en NVS. Se comprobó sintaxis con core3.3.12 y banco C++ nativo; falta enlazar/cargar/calibrar y comprobar hardware. El recibo de PWM no prueba posición ni ingreso; no se agregaron sensores ficticios. [Informe49](../../docs/modulos/49_ORACLE_QWEN_Y_ACTUACION_USB.md).

## Archivo recibido

Se recuperó `referencia/servosecoacces/servosecoacces.ino` del ZIP del usuario `servosecoacces.zip` el 31/10/2026, sin modificar sus 638 bytes. SHA-256 del ZIP: `94BEBDCFE0ADC985C818B93B310EB15A582934ABEE669E57DFFD6D9C2B75FB28`.

SHA-256 del `.ino` preservado: `194580A94BE57872F628D31A158587B834C57517560EC69BFD1D140844BC06BA`.

Abre el `.ino` desde Arduino IDE. La carpeta y el archivo conservan el mismo nombre. Es una **referencia de movimiento periódico**, no firmware integrado: al ejecutarse alterna ambos motores entre 0° y 90° cada dos segundos, sin esperar autorización. No lo cargues para una demostración de seguridad de EcoAccess.

## Lo comprobado en el código

| Dato | Valor del original | Validación física |
|---|---|---|
| Señal del servo 1 | GPIO 18 | Pendiente |
| Señal del servo 2 | GPIO 19 | Pendiente |
| PWM | 50 Hz, resolución de 16 bits | Pendiente |
| Pulsos | Mapea 0–180° a 500–2500 μs | Pendiente de especificación del servo |
| Dos servos | Reciben el mismo ángulo para una sola tapa | Calibrar orientación y esfuerzo |

No se han confirmado modelo de servo, placa exacta, sensor ni orientación mecánica. 0° no significa necesariamente cerrado en el montaje. No conectes la alimentación de los motores al pin de 3,3 V del ESP32. El montaje necesita fuente externa adecuada y referencia GND común; comprobar corriente de arranque y bloqueo de ambos servos. Los 5 V y 1 A indicados por el usuario pueden resultar insuficientes: medir antes de probar con carga.

El original usa `ledcAttach(pin, frecuencia, resolución)` y `ledcWrite(pin, duty)`, APIs del core Arduino-ESP32 3.x. Se encontró el core 3.3.12 instalado; **no se ha compilado, cargado ni probado este sketch**. Referencias oficiales: [LEDC](https://docs.espressif.com/projects/arduino-esp32/en/latest/api/ledc.html), [migración a 3.x](https://docs.espressif.com/projects/arduino-esp32/en/latest/migration_guides/2.x_to_3.0.html).

## Incorporación al sistema existente

Sigue [la guía del bridge](../../docs/manuales/GUIA_INTEGRACION_ESP32.md). Una orden representa una caneca y una apertura conjunta de ambos servos. No se crea una identidad por motor. El firmware definitivo debe autenticar el protocolo existente, comprobar vigencia mediante el bridge, reservar el identificador antes de actuar y conservarlo tras reiniciar. La pérdida de conexión o una ejecución incierta no debe provocar otra apertura.

No basta conectar este sketch directamente al bot: aún faltan el transporte físico, la provisión local de la clave y la adaptación del protocolo en el microcontrolador. El bridge actual sigue limitado a loopback; no se ha abierto ningún puerto de red. No ejecutar el simulador y el actuador físico sobre la misma orden.

Sin sensor confirmado no generar eventos de detección. Mandar un ángulo no demuestra apertura real. Los estados son distintos: autorización, entrega, movimiento solicitado, apertura confirmada, ingreso detectado, verificación del material y ecopuntos. Solo se informa lo realmente comprobado.

## Prueba del compañero, cuando esté listo

1. Confirmar modelo de placa, servos, alimentación, GPIO y posiciones mecánicas.
2. Separar primero los servos de la tapa y verificar consumo y sentido de giro con una prueba controlada.
3. Calibrar cerrado/abierto de cada motor; no forzar topes.
4. Incorporar el transporte autenticado antes de permitir aperturas desde EcoAccess.
5. Probar una orden autorizada, vencida, duplicada, desconexión y reinicio; el duplicado no debe mover la tapa.
6. Registrar evidencia de apertura/cierre y mantener detección/depósito pendientes si no hay sensor.

Esta carpeta conserva la fuente recibida y los requisitos verificables. No acredita firmware integrado ni hardware funcionando.

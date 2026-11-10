# Guía final de hardware — EcoAccess P3 (10/11/2026)

**Estado:** PENDIENTE O BLOQUEADO (prueba física).

- El firmware, el protocolo y el Bridge existen y están probados con el simulador.
- **Ningún motor se ha movido desde este software.** Esta misión tampoco envió órdenes al ESP32.
- El bot sigue en modo software (`SinAperturaFisica`). Por eso Telegram responde «Modo demostración: apertura física pendiente.».

Esta guía junta en una sola página lo que hay que hacer con el prototipo en la mano. El detalle de cada paso está en:

- [`GUIA_ARDUINO_PRINCIPIANTES.md`](GUIA_ARDUINO_PRINCIPIANTES.md): componentes, cableado, instalación del IDE y seguridad.
- [`GUIA_VALIDACION_ESP32.md`](GUIA_VALIDACION_ESP32.md): medición de la fuente, calibración, primera orden y verificación en Oracle.

## 1. Lo que existe físicamente hoy

| Elemento | Real | Nota |
|---|---|---|
| Caneca piloto | 1 | Oracle `contenedor` 1093 «EcoAccess — Caneca piloto», QR oficial sin cambios |
| Servomotores | 2 | Mueven **la misma tapa**; no deben oponerse |
| ESP32 | 1 | Núcleo clásico (`esp32:esp32:esp32`) |
| Sensor de depósito | 0 | Un recibo confirma la orden PWM, no que entró un residuo |
| Canecas 2 y 3 (verde, negra) | 0 | Diseño futuro: en Oracle es configuración, no hay hardware |

## 2. Cableado y GPIO (fijados en `hardware/servosecoacces/servosecoacces.ino`)

| Señal | Desde | Hacia |
|---|---|---|
| PWM servo 1 | GPIO **18** | Señal del servo 1 |
| PWM servo 2 | GPIO **19** | Señal del servo 2 |
| V+ servos | Fuente externa (+) | V+ de ambos servos, **nunca** el pin 3,3 V del ESP32 |
| Tierra | Fuente (−) | GND de ambos servos y GND del ESP32 (tierra común obligatoria) |
| Datos y 5 V de la placa | PC (USB de datos) | ESP32; puerto serie a 115200 baudios |

## 3. Fuente

- No está elegida, porque **una fuente de 5 V y 1 A no está validada**.
- Antes de elegirla se miden, con los dos servos sobre la tapa real:
  - la corriente en reposo;
  - el pico de arranque;
  - la corriente con el servo bloqueado (menos de 2 s);
  - la caída de tensión (no debe bajar de unos 4,5 V).
- La fuente debe superar con margen la suma de los dos picos.

## 4. Configuración y calibración

1. **Configuración privada**
   - Copiar `configuracion_ejemplo.h` a `configuracion_privada.h` (Git la excluye).
   - Completar `ID_CANECA = 1093`, `VERSION_CLAVE` y `CLAVE_BASE64` con la clave privada del Bridge.
   - Nunca publicar esa clave.
2. Cargar el firmware con `CALIBRACION_VERIFICADA = false`: así **no mueve** los motores.
3. **Sin tapa**, ajustar `CERRADO_1/2` y `ABIERTO_1/2`.
   - Valores de ejemplo: 0° y 90°.
   - Revisar el sentido de giro y los topes.
4. Solo después de calibrar, poner `CALIBRACION_VERIFICADA = true` y volver a cargar.
5. La ventana de depósito es `ABIERTO_MS = 10000` (10 s desde la apertura confirmada). No se cambia para la demostración.

## 5. Conexión al Bridge

- **Configuración:** en `bridge.local.json` agregar `"PuertoSerial": "COMx"`. Usar el puerto que muestre el Administrador de dispositivos; no se fija ningún número.
- **Arranque:** iniciar el bot con `--bridge` y `--dispositivo-serial`, nunca junto con `--dispositivo-simulado`.
- **Cada orden:**
  - va firmada con HMAC-SHA256;
  - tiene vigencia corta;
  - se rechaza si se repite, vence o trae una firma inválida.
- **Origen de las órdenes:** solo nacen de una admisión del catálogo contra Oracle. **El texto libre del modelo nunca puede activar el hardware.**

## 6. Protecciones que no se desactivan

- Al encender o reiniciar, los motores no se mueven.
- Si el ESP32 se reinicia a mitad de un ciclo, queda bloqueado hasta revisión física (`RECUPERACION_MANUAL_VERIFICADA`).
- Si no hay recibo, no hay depósito confirmado. Sin sensor, el depósito queda **pendiente de verificación**.

## 7. Pruebas físicas pendientes (en este orden)

| # | Prueba | Quién | Estado |
|---|---|---|---|
| 1 | Puerto COM visible y firmware cargado sin movimiento | Equipo con la placa | PENDIENTE |
| 2 | Medición de corriente y tensión de la fuente (§3) | Equipo con multímetro | PENDIENTE |
| 3 | Calibración sin tapa | Equipo | PENDIENTE |
| 4 | Primera orden firmada sin tapa, desde el Bridge | Equipo | PENDIENTE |
| 5 | Ciclo completo con tapa: abre, 10 s, cierra | Equipo | PENDIENTE |
| 6 | Orden repetida o vencida: la tapa no se mueve | Equipo | PENDIENTE |
| 7 | Reinicio a mitad de ciclo: queda bloqueado | Equipo | PENDIENTE |
| 8 | Eventos del ciclo visibles en Oracle y en el dashboard | Equipo | PENDIENTE |
| 9 | Foto por Telegram (iPhone) → admisión → apertura real | Equipo | PENDIENTE |

Ninguna de estas pruebas puede hacerse desde este PC sin la placa conectada. Este software no las simula ni las marca como hechas.

# 08. Hardware

Guía de montaje y carga: [hardware/README.md](../../hardware/README.md). Validación paso a paso: [GUIA_VALIDACION_ESP32.md](GUIA_VALIDACION_ESP32.md).

El firmware proviene del proyecto anterior y se adaptó: la ventana pasó de 2 a 20 y luego a 10 segundos y ahora envía dos recibos autenticados, al aplicar la apertura y al aplicar el cierre. Antes el único recibo llegaba al final, por lo que el aviso de apertura habría llegado con la tapa ya cerrada. El servicio de dispositivos también se corrigió: envía los eventos mientras la actuación sigue en curso.

| Comprobación | Estado |
|---|---|
| Compilación ESP32 con core 3.3.12 | **correcta** |
| Protocolo de dos recibos en el adaptador C# | **probado con enlace simulado**: 5 pruebas |
| Banco nativo de `EstadoActuador` | **no ejecutado en este equipo**: falta compilador C++ de escritorio |
| Carga, calibración, apertura física, 10 s reales, desconexión, reinicio y consumo | **pendientes: no hay hardware conectado** |

## Sensor de ingreso

`TipoEventoDispositivo.Deteccion` ya existe y crea un depósito con detección real. Un sensor futuro solo debe emitir ese evento; hoy se usa `CierreSinSensor`, que deja el depósito pendiente de verificación.

## Varias canecas

Cada ESP32 tiene su identidad, clave y contenedor; ver el documento 09.
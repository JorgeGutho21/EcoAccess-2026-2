# Prueba física de las tres canecas — 25/11/2026, noche (PC de Heyder como anfitrión)

ESP32 conectado por USB: Silicon Labs CP210x en **COM9** (`USB\VID_10C4&PID_EA60`). Panel corregido de la rama
`integracion/tres-canecas-fisicas` (Release) como único dueño del puerto, iniciado con `--puerto COM9`.

## Arranque
| Comprobación | Resultado |
|---|---|
| Detección del ESP32 | Conectado en COM9; firmware `ecoaccess-calibracion-v2` (`estacion_tres_canecas`) |
| Estado al arrancar, sin pulsar nada | **En servicio** (`enServicio=true`, `operativa=true`) |
| Calibraciones conservadas | Verde 0°/50°, blanca 0°/70°, negra 0°/40° (las mismas del informe anterior; no se tocaron) |
| Página del panel | Carga y se actualiza sola, sin errores en la consola (la corrección del SyntaxError funciona) |

## Aperturas reales (canal ciudadano, sin sesión supervisada)
El tiempo «fuera de reposo» va desde que el firmware sale de REPOSO hasta que vuelve a él: apertura, tiempo de la
política y cierre de unos 1–1,5 s. Lo midió el sondeo del estado cada 200 ms.

| Combinación | Respuesta del ESP32 | Duración pedida | Fuera de reposo medido | Cierre automático |
|---|---|---|---|---|
| Verde | 110 ms | 7 s | 8,4 s | Sí |
| Blanca | 98 ms | 7 s | 8,5 s | Sí |
| Negra | 92 ms | 7 s | 8,5 s | Sí |
| Verde + Blanca | 83 ms | 14 s | 15,0 / 16,0 s | Sí |
| Verde + Negra | 99 ms | 14 s | 16,0 / 16,0 s | Sí |
| Blanca + Negra | 86 ms | 14 s | 15,8 / 15,8 s | Sí |
| Verde + Blanca + Negra | 86 ms | 21 s | 22,4 / 22,4 / 23,5 s | Sí |

## Canal C# (app de escritorio y bot de Telegram)
Probado con el mismo `ClienteEstacionPanel` compilado: estado `conectado=True`, `en servicio=True`, `puerto=COM9`.
Apertura de la verde con `Aceptada=True`, `Simulada=False`, 7 000 ms.

## Protecciones
| Prueba | Resultado |
|---|---|
| Misma orden (misma clave) enviada dos veces | La primera abre; la segunda se rechaza con **409**, «Solicitud duplicada». No se repite la apertura |
| PONER EN PAUSA (motivo «prueba de mantenimiento») y pedir una apertura | **423** con el motivo; ninguna tapa se mueve |
| Reiniciar el panel estando en pausa | Sigue en pausa (`pausaDesde 25/11 20:43`) |
| REANUDAR SERVICIO | Vuelve a `enServicio=true`, `operativa=true` |

## No probado en esta sesión
- **Telegram de punta a punta (foto → apertura)**, ECOACCESS WEB y DOS CLICS a través de la pasarela: requieren el bot
  y el túnel de la estación. El bot de Telegram solo puede correr en un equipo (el de Jorge), y no se inició un segundo
  consumidor. La respuesta «Caneca abierta. Puedes desechar tu residuo» la cubren las pruebas unitarias del mismo
  resultado que devolvió la apertura real.
- Control desde un segundo computador por la red local (`Configurar-Anfitrion.ps1`): requiere otro equipo y, en este
  anfitrión, `Habilitar-PanelEnRed.ps1` como administrador.
- Depuración T003 en Oracle: pendiente de autorización.
- La posición física de cada tapa se confirma por observación: el hardware no tiene sensor de posición.

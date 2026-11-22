# Estación de tres canecas: apertura DEMO y apertura FÍSICA

Guía corta para que las canecas abran desde la aplicación de escritorio (y desde Telegram), con Oracle Cloud.

## 1. El error «La caneca no pudo recibir la orden (RN-16)»

**Qué pasaba (21/11/2026):** la aplicación emite una orden de apertura en Oracle y espera a que la caneca la atienda.
Si no había ninguna estación en marcha (ni simulada ni física), nadie atendía la orden y, como solo el proceso de la
estación vencía las órdenes viejas, la orden quedaba «Emitida» para siempre. Desde ese momento Oracle consideraba la
caneca ocupada y rechazaba todas las órdenes nuevas. En Oracle había tres órdenes atascadas: 1714 (blanca, desde el
18/11), 1836 (verde) y 1837 (negra).

**Corrección:**
- `ServicioInteracciones.ComprobarAperturaAsync` y `RegistrarAperturaManualAsync` vencen primero las órdenes de esa caneca
  cuyo plazo (2 minutos) ya pasó, con la misma regla que el bridge: la orden queda «Vencida» y la solicitud se cierra sin
  depósito. Una orden que sigue vigente **sí** bloquea (RN-16 se conserva). Pruebas:
  `Autorizar_OrdenAnteriorVencidaSinBridge_SeVenceYLaNuevaSeEmite` y `Autorizar_OrdenAnteriorAunVigente_SigueBloqueandoLaCaneca`.
- Las tres órdenes atascadas quedaron en estado 7 (Vencida) al iniciar la estación.

## 2. Cómo funciona una apertura

```
EcoAccess (escritorio o Telegram) ── orden ──► Oracle Cloud (ORDEN_APERTURA)
                                                   ▲
           ┌──── --bridge (una por caneca) ─────────┘   lee la orden de SU caneca y registra los eventos
           ▼
   --dispositivo-simulado (DEMO)  ó  --dispositivo-serial (FÍSICA, ESP32 por COM)
           │                                   firma HMAC propia de cada caneca
           └─► acuse → apertura (10 s) → cierre ─► depósito SIN verificar (no da ecopuntos hasta que lo verifique un Operario/Supervisor)
```

| Caneca | Oracle | Puertos locales | Dispositivo demo | Dispositivo físico |
|---|---|---|---|---|
| Blanca | 1093 | 8182 / 8183 | `simulado-blanca` | `esp32-piloto-blanca` |
| Verde | 1400 | 8184 / 8185 | `simulado-verde` | `esp32-piloto-verde` |
| Negra | 1401 | 8186 / 8187 | `simulado-negra` | `esp32-piloto-negra` |

## 3. Modo DEMOSTRACIÓN (sin hardware)

Doble clic en el acceso directo **«EcoAccess - Iniciar canecas DEMO»** del escritorio, o:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\estacion\EcoAccess-Estacion.ps1 -Accion Iniciar -Modo Demo
```

- La primera vez crea en `%ProgramData%\EcoAccessP3Framework\Bridge` (fuera de Git) un JSON, una clave HMAC aleatoria y
  una carpeta de estado por caneca. Nunca muestra las claves.
- Inicia 6 procesos ocultos (bridge + caneca simulada por cada color) y los registra en
  `%ProgramData%\EcoAccessP3Framework\Desarrollo\estacion.json`. La aplicación lo lee y muestra **«● Estación simulada (demo)»**.
- Luego, en EcoAccess: elige el residuo → «Depositar este residuo». La caneca acusa, abre 10 s, registra el depósito y cierra.
  Es una **simulación**: no se mueve ninguna tapa y el depósito queda sin verificar.

Ver estado y detener:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\estacion\EcoAccess-Estacion.ps1 -Accion Estado
```

```powershell
powershell -ExecutionPolicy Bypass -File scripts\estacion\EcoAccess-Estacion.ps1 -Accion Detener
```

El bot de Telegram sigue aparte: lo mantiene el vigilante (`scripts\desarrollo\Vigilar-Bot.ps1`) con su propio
`sesion.json`. Las dos cosas pueden estar activas a la vez.

## 4. Modo FÍSICO (ESP32 de tres tapas)

1. Conecta el ESP32 por USB y mira el puerto en el Administrador de dispositivos (en este equipo estaba en **COM3**).
2. Detén la demo si está activa (`-Accion Detener`): no pueden correr las dos a la vez porque usan los mismos puertos.
3. Inicia:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\estacion\EcoAccess-Estacion.ps1 -Accion Iniciar -Modo Fisica -PuertoSerial COM3
```

- Reutiliza la configuración física existente (`bridge-blanca.json`, `clave-blanca.bin`, `estado-blanca`, y lo mismo para
  verde y negra): son las claves grabadas en `configuracion_privada.h` del firmware y **no se regeneran**. Si cambias de
  COM, solo se actualiza el puerto.
- En un equipo nuevo sin esa configuración, el script la crea con claves nuevas; entonces hay que copiar cada clave en el
  firmware (`hardware/estacion_tres_canecas/configuracion_privada.h`, campo `claveBase64` de cada tapa) antes de conectar.
- La aplicación muestra **«● Bridge físico activo»**. Las reglas de seguridad del firmware y del puerto COM exclusivo
  están en [`INTEGRACION_FINAL_TRES_CANECAS_QR.md`](INTEGRACION_FINAL_TRES_CANECAS_QR.md).

## 5. Prueba con varios usuarios (21/11/2026, Oracle Cloud, modo demo)

`tests/integracion/PruebaEstacion` hace el mismo recorrido que el asistente de depósito con cuentas ficticias
(244, 1934, 2298 y 2299) y un invitado:

```powershell
dotnet build tests\integracion\PruebaEstacion -c Release -o $env:TEMP\prueba_estacion
& "$env:TEMP\prueba_estacion\PruebaEstacion.exe"
```

| Escenario | Resultado |
|---|---|
| 4 usuarios, uno tras otro, en blanca, verde, negra y blanca (periódico, residuo nuevo) | 4 de 4: abierta a los 4,4–6,4 s, cerrada a los 13,6–16,7 s |
| 3 usuarios a la vez, cada uno en una caneca | 3 de 3, sin interferencias |
| 2 usuarios a la vez en la misma caneca | el segundo recibe RN-16, espera su turno y deposita (abierta a los 15,1 s) |
| Reintento con la misma clave | devuelve la misma orden; no abre dos veces |
| Invitado en la verde | depósito registrado |
| Ecopuntos | los 11 depósitos quedan sin verificar: no se acredita nada sin verificación |

No se probó con el ESP32 conectado (no había ningún puerto COM en el equipo durante la prueba).

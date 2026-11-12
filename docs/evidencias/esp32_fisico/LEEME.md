# Evidencias con el ESP32 real (11/11/2026, este PC)

Todo lo de esta carpeta se obtuvo con la placa conectada por USB en COM3. **Ningún motor se movió**: las tres tapas
estaban fuera de servicio en el firmware (`SinPin`, sin calibrar).

## 1. Identificación (esptool 4.6, solo lectura)

```
Chip is ESP32-D0WD-V3 (revision v3.1)
Features: WiFi, BT, Dual Core, 240MHz, VRef calibration in efuse, Coding Scheme None
Crystal is 40MHz
Detected flash size: 4MB
```

La MAC completa no se publica. El diagnóstico muestra solo el sufijo.

## 2. Firmware de diagnóstico (`hardware/diagnostico_esp32`)

```
=== EcoAccess diagnostico ESP32 ===
modelo=ESP32-D0WD-V3 revision=301 nucleos=2
wifi=si bt=si ble=si
cpu_mhz=240 flash_bytes=4194304 psram_bytes=0 heap_libre=352828
arduino_core=3.0.7 idf=v5.1.4-972-g632e0c2a9f-dirty
reinicio=encendido
motores=sin PWM (este sketch nunca los mueve)
GPIO 17 = 0 · GPIO 16 = 0 · GPIO 21 = 0 · GPIO 23 = 0 · GPIO 18 = 0 · GPIO 19 = 0
ECO EcoAccess-115200
```

El valor de `heap_libre` corresponde a la primera versión del sketch; la versión publicada solo cambió el formato de `printf`.

## 3. Compilación y carga (arduino-cli 1.5.1, `esp32:esp32:esp32`, núcleo 3.0.7)

| Sketch | Bytes de programa | Carga |
|---|---|---|
| `diagnostico_esp32` | 287 849 | cargado, «Hash of data verified» |
| `estacion_tres_canecas` (con la cabecera privada) | 352 337 | cargado, «Hash of data verified»; **queda cargado en la placa** |
| `servosecoacces` | 344 913 | solo compilado |
| `calibracion_seis_servos` | 315 169 | solo compilado |

## 4. Seguridad del protocolo en la placa (`scripts/pruebas/Probar-ESP32-Seguridad.ps1`)

El resultado completo está en `seguridad_serial_20261111.json`. Se superaron **11 de 11** pruebas:

- el desafío tiene el formato correcto y cambia en cada petición;
- se rechazaron el texto basura, un sobre incompleto, una firma con clave falsa y una identidad desconocida;
- con la **clave real** de la blanca, se rechazaron:
  - un desafío inventado;
  - un desafío anterior;
  - una orden de la verde;
  - una orden correcta para una tapa fuera de servicio;
- una línea de 5000 bytes se descartó y la placa siguió respondiendo.

**Límite:** con las tapas fuera de servicio, todas las órdenes se rechazan antes de la verificación de la firma. Esto prueba
que nada se ejecuta sin una tapa habilitada. La aceptación de una orden válida, la apertura y el cierre se probarán con
una tapa calibrada y una persona presente (guía `docs/hardware/GUIA_PASO_A_PASO_PROTOTIPO_FISICO.md`, paso 18).

## 5. Sesión de calibración supervisada (11/11/2026, 22:26 a 22:36)

Banco `hardware/calibracion_seis_servos` cargado. Antes del primer movimiento, el propietario confirmó cinco condiciones:
- tapas desacopladas;
- servos alimentados por una fuente externa de 5 V;
- tierra común;
- fuente al alcance;
- recorrido libre.

| Hora | Orden (un pin a la vez) | Firmware | Observación física del propietario |
|---|---|---|---|
| 22:26:48 | GPIO 17 → 30°, luego sin PWM | MOVIDO | El servo **todavía no estaba conectado** |
| 22:28:55 | GPIO 17 → 90° durante 2 s, luego sin PWM | MOVIDO, sin reinicio | **«No se movió»** |
| 22:31:13 a 22:31:46 | Barrido 30° → 90° → 30° en 17, 16, 21, 23, 18 y 19, uno a la vez y con 3 s de pausa | 18 de 18 confirmados, sin reinicio | **Se movieron la negra y la verde (unos 30°); la blanca no.** No se sabe qué pin mueve cada una |
| 22:36:11 | `Mover-Servo.ps1`: GPIO 17, barrido | 3 de 3 confirmados, sin reinicio | **pendiente** |

**Conclusión:**
- El firmware genera el PWM en los pines indicados y la placa no se reinició: no hubo caída de tensión visible.
- Que el servo no se moviera apunta al circuito físico. Causas posibles: el servo no recibe +5 V, falta la tierra común, la señal está en otro pin o el servo está dañado.
- **Ninguna tapa queda verificada físicamente.**

Registro automático: `movimientos_servos.csv`, que genera `scripts/pruebas/Mover-Servo.ps1`.

## 6. Arduino IDE configurado (corrección de «Missing FQBN»)

- Se añadió la URL de Espressif al archivo de configuración del IDE (`%USERPROFILE%\.arduinoIDE\arduino-cli.yaml`). Hay una copia de respaldo de la versión anterior junto a él.
- Cada sketch de `hardware/` trae un `sketch.yaml` con `default_fqbn: esp32:esp32:esp32` y `default_port: serial://COM3`. `arduino-cli compile <sketch>` funciona sin `--fqbn`.
- En la IDE quedó seleccionado **«ESP32 Dev Module on COM3»** (*Tools → Board → esp32 → ESP32 Dev Module* y *Tools → Port → COM3*).
- El aviso de actualización de placas se pospuso: hay que mantener el núcleo 3.0.7, el mismo de la CI.

### Estado a las 22:38

| Caneca | Observación física | Estado |
|---|---|---|
| Negra | Su tapa se movió durante el barrido | **MOVIMIENTO OBSERVADO; falta saber con qué pines** |
| Verde | Su tapa se movió durante el barrido | **MOVIMIENTO OBSERVADO; falta saber con qué pines** |
| Blanca | No se movió con ninguno de los seis pines | **FALLO FÍSICO**: su servo no recibe señal, alimentación o tierra, o está conectado a un pin fuera de la lista |

- Para el mapeo, se intentó un barrido lento (30° → 60° → 30°, un pin cada 6 s).
- No llegó a ejecutarse: a las 22:38 el ESP32 desapareció de Windows y no había ningún puerto COM. La causa probable es un cable USB suelto o una caída de alimentación del puerto USB.
- El firmware nunca mueve dos tapas a la vez, y el banco solo mueve un pin por orden.

## 7. Mapeo confirmado y prueba del código original (22:53 a 22:54)

**Mapeo** (confirmado por el propietario, un servo por tapa):

| Caneca | GPIO |
|---|---|
| Blanca | 19 |
| Negra | 18 |
| Verde | 23 |

**Código original** (`servosecoacces (2).zip`):
- El SHA-256 es `DF1D3092…79805D3B`, el mismo de `hardware/referencia/seis_servos_companero_original.ino.txt`.
- Mueve los seis servos **a la vez** entre 0° y 60°, sin parar, desde el arranque.
- **No se cargó tal cual**, porque movería las tres tapas juntas sin supervisión.

**Prueba equivalente:** se repitió su recorrido (0° → 60° → 0°, 2 s en cada posición) **una tapa a la vez** con `Mover-Servo.ps1 -Desde 0 -Angulo 60`.

| Hora | Caneca | GPIO | Firmware | Observación física |
|---|---|---|---|---|
| 22:53:50 | Blanca | 19 | 3 de 3 confirmados, sin reinicio | **No se movió** |
| 22:55:40 y 22:55:50 | Blanca (repetición, dos veces) | 19 | 6 de 6 confirmados, sin reinicio | **Se movió** |
| 22:54:02 | Negra | 18 | 3 de 3 confirmados, sin reinicio | **Se movió** |
| 22:54:14 | Verde | 23 | 3 de 3 confirmados, sin reinicio | **Se movió** |

0° y 60° son los ángulos del código del compañero. **No se dan por buenos** como «cerrado» y «abierto» hasta que el propietario confirme que la tapa cierra del todo en 0° y abre lo suficiente en 60°, sin forzar.

**Estado a las 22:56: los tres mecanismos se movieron físicamente**, cada uno con su GPIO y uno a la vez.
- La blanca falló una vez y respondió en la repetición. La causa probable es un contacto intermitente en su cable o conector: hay que vigilarlo.
- Los ángulos de cerrado y abierto siguen **sin verificar**.

## 8. Firmware definitivo con HMAC: aperturas firmadas reales (23:04 a 23:05)

**Configuración:**
- Ángulos confirmados por el propietario: **cerrado 0° y abierto 60°**, iguales para las tres tapas. Son los del código original del compañero, observados tapa por tapa a las 23:01.
- `configuracion_privada.h`:
  - blanca `{19}`, verde `{23}` y negra `{18}`;
  - las tres calibradas;
  - claves fuera de Git.
- Cargado `estacion_tres_canecas` (352 337 B, hash verificado).

**Prueba:** `scripts/pruebas/Probar-Apertura-Firmada.ps1 -Canecas blanca,negra,verde -ProbarDuplicado`. El script hace de bridge con el mismo sobre HMAC.

| Hora | Caneca | Orden | Apertura | Cierre | Ventana medida | Orden repetida |
|---|---|---|---|---|---|---|
| 23:04:31 | Blanca (1093, GPIO 19) | 2000404285 | `pwm-apertura-aplicada`, firma válida | `pwm-completado-sin-sensor`, firma válida | **10,50 s** | rechazada, no abre |
| 23:04:49 | Negra (1401, GPIO 18) | 2000404286 | ídem | ídem | **10,53 s** | rechazada, no abre |
| 23:05:07 | Verde (1400, GPIO 23) | 2000404287 | ídem | ídem | **10,54 s** | rechazada, no abre |

- La ventana se mide entre los dos recibos: son 10 000 ms de `ABIERTO_MS` más los 500 ms que espera el firmware tras ordenar el cierre.
- Los identificadores ≥ 2 000 000 000 se reservaron en la memoria de la placa y no chocan con los de Oracle.
- El resultado completo está en `aperturas_firmadas_20261111.json`.
- **El recibo confirma el PWM.** La apertura y el cierre visibles de cada tapa los confirma el propietario.

## 9. Recorrido completo QR → Telegram → IA → Oracle → bridge → ESP32 (12/11/2026, 04:31 a 04:34 UTC)

**Montaje:**
- Estación lanzada con `scripts/desarrollo/Iniciar-Estacion-Fisica.ps1`: tres adaptadores USB, tres bridges y el bot en modo físico.
- Oracle Cloud: blanca 1093 **en servicio**; verde 1400 y negra 1401 fuera de servicio.

**Prueba:** el propietario escaneó el QR `EcoAccessCanecaPiloto1` y envió fotos de una botella PET.

| Paso | Evidencia |
|---|---|
| IA local | `objeto=plastic_bottle contexto=ResiduoAprovechable resultado=Identificado corroborada=True` (3 fotos; la primera tardó 15,6 s y las siguientes unos 7,3 s) |
| Orden solo para la blanca | órdenes 1681 y 1682, `ID_CONTENEDOR=1093`, interacciones 2380 y 2381, material confirmado «Botella plástica PET» |
| Bridge → ESP32 | «Orden 1681: Enviada», y el adaptador USB da «AccionCompletada» para las dos órdenes |
| Eventos en Oracle | orden 1681: acuse 04:33:17.15, apertura 04:33:17.47, cierre sin sensor 04:33:27.40 (**9,93 s**); orden 1682: 04:34:26.85, 04:34:27.10, 04:34:37.38 (**10,28 s**) |
| Depósito | 1221 y 1222: PET declarado, `SIN_SENSOR=1`, sin verificar y sin ecopuntos. **No se afirma ingreso físico** |
| Ninguna otra tapa | los logs de los bridges negra y verde no registran ninguna orden |

**Observaciones:**
- Las dos operaciones quedaron como **invitado** (sin usuario vinculado). Los ecopuntos requieren una cuenta vinculada y la verificación de un operador.
- Una respuesta de texto del bot tardó 21,2 s en enviarse a Telegram, a las 04:33:35Z, según el registro «envío 21205 ms». Hay que vigilarlo.
- La apertura y el cierre visibles de la tapa los confirma el propietario.

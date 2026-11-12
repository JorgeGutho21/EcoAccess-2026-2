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

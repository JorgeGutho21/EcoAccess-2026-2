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

# Prototipo físico paso a paso: un ESP32, tres canecas y un QR (11/11/2026)

Guía única, de principio a fin, para poner en marcha la estación física. Cada paso dice **qué se hizo hoy en este PC y con
qué resultado**, y qué debe hacer una persona frente al hardware. Complementa `INTEGRACION_FINAL_TRES_CANECAS_QR.md`
(arquitectura y pruebas de software) y `hardware/README.md`.

## Estado real al cerrar esta sesión

| Parte | Estado | Evidencia |
|---|---|---|
| ESP32 identificado: **ESP32-D0WD-V3, revisión 3.1**, doble núcleo, 240 MHz, 4 MB de flash, sin PSRAM (módulo tipo ESP32-WROOM-32) | VERIFICADO REAL | `esptool flash_id` y firmware de diagnóstico |
| Puerto: **COM3**, puente USB-UART Silicon Labs CP210x (`VID_10C4&PID_EA60`) | VERIFICADO REAL | Administrador de dispositivos y `arduino-cli board list` |
| Núcleo `esp32 by Espressif Systems` **3.0.7** (ESP-IDF 5.1.4), el mismo que fija el CI | INSTALADO Y VERIFICADO | `arduino_core=3.0.7` informado por la propia placa |
| Compilación de los cuatro sketches para `esp32:esp32:esp32` | VERIFICADO REAL | diagnóstico 287 849 B, estación 352 337 B, una tapa 344 913 B, calibración 315 169 B |
| Carga en la placa (diagnóstico y estación) | VERIFICADO REAL | «Hash of data verified» en las cuatro regiones |
| Comunicación serie a 115200 en ambos sentidos | VERIFICADO REAL | `INFO`, `PINES` y `ECO` respondieron |
| Seguridad del protocolo **en la placa real** | VERIFICADO REAL, 11 de 11 | `docs/evidencias/esp32_fisico/seguridad_serial_20261111.json` |
| Claves HMAC, bridges y cabecera privada de las tres canecas | PREPARADO EN ESTE PC | `scripts/instalacion/Preparar-Estacion-Fisica.ps1` |
| Aviso «Tienes N segundos» alineado con la apertura real | IMPLEMENTADO Y PROBADO EN SOFTWARE | `AvisoDeApertura_DiceLosSegundosQueRealmenteQuedan` |
| Cantidad, modelo y cableado de los servos; fuente | **PENDIENTE (requiere a una persona)** | No se puede ver desde el PC |
| Movimiento de cualquier motor, apertura, cierre, 10 s medidos | **PENDIENTE** | **No se movió ningún motor** en esta sesión |
| Tapas verde y negra | **PENDIENTE** | Fuera de servicio en Oracle y en el firmware hasta calibrarlas |

Las tres tapas están hoy **fuera de servicio en el firmware** (`SinPin`, sin calibrar): la placa rechaza cualquier orden y
no conecta PWM. Así quedó a propósito hasta que alguien supervise el primer movimiento.

## 1. Materiales necesarios

| Material | Cantidad | Nota |
|---|---|---|
| Placa ESP32 (la identificada: ESP32-D0WD-V3, 4 MB) | 1 | Ya conectada en COM3 |
| Cable USB **de datos** | 1 | Un cable solo de carga no crea el puerto COM |
| Servomotores | 6 para tres tapas (dos por tapa) | **Por confirmar**: modelo y cantidad real conectada |
| Fuente externa de 5 V para los servos | 1 | La de 5 V y 1 A **no está validada**: ver §15 |
| Bornera o protoboard y cables dupont | — | Tierra común obligatoria |
| Condensador electrolítico de 470 a 1000 µF, 10 V o más | 1 por fuente (recomendado) | Cerca de los servos, polaridad correcta |
| Multímetro | 1 | Para medir tensión y polaridad antes de conectar |
| PC con Windows, Arduino IDE 2.3.10 y EcoAccess | 1 | Este PC |

## 2. Identificación del ESP32

Hecho hoy, sin mover nada:

```powershell
$esptool = Get-ChildItem "$env:LOCALAPPDATA\Arduino15\packages\esp32\tools\esptool_py" -Recurse -Filter esptool.exe | Select-Object -First 1 -ExpandProperty FullName
& $esptool --port COM3 flash_id
```

Resultado: `Chip is ESP32-D0WD-V3 (revision v3.1)`, `Features: WiFi, BT, Dual Core, 240MHz`, `Detected flash size: 4MB`.
Por eso la placa correcta es **ESP32 Dev Module** (`esp32:esp32:esp32`), no ESP32-S2, S3 ni C3.

## 3. Instalación de Arduino IDE

Ya instalado: Arduino IDE 2.3.10 (incluye `arduino-cli` 1.5.1). En otro PC: descargarlo de arduino.cc e instalarlo
con las opciones por defecto.

## 4. Driver USB

La placa usa un puente **Silicon Labs CP210x**. En este PC el driver ya estaba instalado y Windows muestra
«Silicon Labs CP210x USB to UART Bridge (COM3)». En otro PC: instalar «CP210x Universal Windows Driver» de Silicon Labs.
Si aparece un CH340 en vez de un CP210x, el driver es el de WCH.

## 5. Instalación de `esp32 by Espressif Systems`

**Causa del error `Compilation error: Missing FQBN (Fully Qualified Board Name)`**: el paquete esp32 no estaba
instalado, así que el IDE no tenía ninguna placa ESP32 que seleccionar. Hoy se instaló la versión **3.0.7**:

```powershell
$cli = "$env:LOCALAPPDATA\Programs\Arduino IDE\resources\app\lib\backend\resources\arduino-cli.exe"
& $cli core update-index --additional-urls https://espressif.github.io/arduino-esp32/package_esp32_index.json
& $cli core install esp32:esp32@3.0.7 --additional-urls https://espressif.github.io/arduino-esp32/package_esp32_index.json
```

Desde el IDE es lo mismo: *Archivo → Preferencias → URLs adicionales del gestor de placas*:
`https://espressif.github.io/arduino-esp32/package_esp32_index.json`. Después, *Gestor de placas → esp32 → 3.0.7*.
No actualices a otra versión sin recompilar: el firmware usa la API `ledcAttach`/`ledcWrite(pin, …)` del núcleo 3.x
(no funciona con el 2.x).

## 6. Selección de placa y puerto COM

En el IDE: *Herramientas → Placa → esp32 → ESP32 Dev Module* y *Herramientas → Puerto → COM3*. Con eso desaparece el
error de FQBN. Con la línea de órdenes:

```powershell
& $cli board list   # COM3  serial  Serial Port (USB)
```

Si el número cambia (otro puerto USB), usa el que muestre Windows y vuelve a ejecutar el paso 11 con ese COM.

**Hecho en este PC (11/11/2026):**
- La URL de Espressif está en `%USERPROFILE%\.arduinoIDE\arduino-cli.yaml`.
- Cada sketch trae un `sketch.yaml` con la placa y el puerto.
- El IDE quedó en **«ESP32 Dev Module on COM3»**.

Si el IDE no muestra «esp32» en *Tools → Board*, ciérralo del todo (Ctrl+Q) y ábrelo otra vez: lee la URL al arrancar. Si ofrece «Updates are available», elige **Later**, porque hay que mantener el 3.0.7.

## 6 bis. Probar un servo por código

Con el banco `calibracion_seis_servos` cargado y el Monitor Serie del IDE **cerrado** (el COM admite un solo dueño):

```powershell
.\scripts\pruebas\Mover-Servo.ps1 -Puerto COM3 -Pin 17 -Angulo 90 -Barrido      # un servo: 30° → 90° → 30°
.\scripts\pruebas\Mover-Servo.ps1 -Puerto COM3 -Todos -Barrido -Angulo 90        # los seis, uno a la vez
```

- Cada movimiento se anota en `docs/evidencias/esp32_fisico/movimientos_servos.csv` con lo que confirmó el firmware.
- Si la placa se reinicia (caída de tensión), el script se detiene.
- La observación física (qué se movió) la añade la persona con `-Observacion`.

**Si el firmware confirma pero el servo no se mueve**, revisa con la fuente **apagada**:
1. el cable rojo del servo va al **+** de la fuente y el marrón o negro al **−**;
2. el **−** de la fuente está unido a un pin **GND** del ESP32;
3. el cable naranja o amarillo va al pin **17** (rotulado G17, D17 o IO17).

Después enciende la fuente y mide **5 V** entre rojo y marrón en el conector del servo.

## 7. Bibliotecas

No hace falta instalar ninguna. Todo lo que usa el firmware viene con el núcleo 3.0.7: `Preferences` (NVS), `cJSON`,
`mbedtls` (HMAC-SHA256 y Base64) y `esp_random`. No se usa la biblioteca `Servo` de Arduino UNO: el PWM de 50 Hz y
16 bits se genera con LEDC, que es lo propio del ESP32.

## 8. Conexiones eléctricas

Comprobado en el software: el firmware solo acepta los GPIO **16, 17, 18, 19, 21 y 23**. Rechaza los pines de arranque
(0, 2, 5, 12 y 15), los de la flash (6 a 11) y los que solo admiten entrada (34 a 39).

**Por confirmar físicamente:** qué par mueve cada tapa. El archivo del compañero usa 17, 16, 21, 23, 18 y 19, pero no dice
cuál es cuál.

| Cable del servo | Va a |
|---|---|
| Señal (naranja o amarillo) | un GPIO de la lista |
| Positivo (rojo) | **+5 V de la fuente externa**, nunca el pin 3V3 ni el 5V/VIN del ESP32 |
| Negativo (marrón o negro) | **GND de la fuente externa** |
| GND del ESP32 | unido al GND de la fuente (tierra común) |

## 9. Diagrama de cableado

```
                 Fuente externa 5 V
                 (+)            (−)
                  │              │
      ┌───────────┼──────┬───────┼──────────────────────────┐
      │  condensador ═╪═ │       │                          │
      │           │      │       │                          │
   Servo A1   Servo A2  ...   Servo C2   (rojo a +, marrón a −)
      │ señal     │ señal          │ señal
      │           │                │
   GPIO a1     GPIO a2   ...    GPIO c2     ESP32 (USB al PC)
                                              │
                                     GND ESP32 ┴── unido al (−) de la fuente
```

Tapa A, B y C = blanca, verde y negra en el orden que se confirme en el paso 18. Nunca alimentes un servo desde un GPIO.

## 10. Configuración de los GPIO

Se hace con el script del paso 11, **después** de mapear los pines (paso 18):

```powershell
.\scripts\instalacion\Preparar-Estacion-Fisica.ps1 -Puerto COM3 -RehacerCabecera `
  -Pines @{ blanca = '18,19' } -Angulos @{ blanca = '0,0;90,90' } -Calibradas blanca
```

Los valores del ejemplo **no están medidos**: reemplázalos por los que observes. Una tapa sin `-Pines` o sin `-Calibradas`
sigue fuera de servicio, y las demás funcionan igual.

## 11. Credenciales y comunicación (por qué no hay Wi-Fi)

**Mecanismo elegido: USB serie entre el ESP32 y un bridge en este PC; el bridge habla con Oracle Cloud por salida TLS.**
- El ESP32 no abre ningún puerto en la red ni necesita que Oracle o Telegram lleguen a una IP local.
- Tampoco guarda contraseñas de Wi-Fi, de Oracle ni tokens de Telegram.
- Cada orden va firmada con HMAC-SHA256 y lleva:
  - un desafío nuevo de la propia placa, válido menos de 5 s;
  - un identificador único de orden y una clave de idempotencia;
  - una vigencia.
- La placa reserva la orden en su memoria no volátil (NVS) **antes** de mover nada, así que una orden repetida no vuelve a abrir.
- **La «reconexión» corresponde al bridge**: abre el COM en cada orden y, si la placa no responde, registra el fallo sin reintentar.

Por eso **no se implementó Wi-Fi ni MQTT**: añadirían una superficie de ataque y credenciales sin aportar nada a este prototipo. Si en el futuro la estación queda lejos del PC, la opción sería HTTPS saliente del ESP32 hacia el backend.

Preparación de las credenciales, hecha hoy:

```powershell
.\scripts\instalacion\Preparar-Estacion-Fisica.ps1 -Puerto COM3
```

| Qué crea | Dónde | En Git |
|---|---|---|
| `clave-blanca.bin`, `clave-verde.bin` y `clave-negra.bin` (32 bytes aleatorios cada una) | `C:\ProgramData\EcoAccessP3Framework\Bridge\` | **No** |
| `bridge-blanca.json`, `bridge-verde.json` y `bridge-negra.json` (COM3; puertos 8182 a 8187) | la misma carpeta | **No** |
| `hardware/estacion_tres_canecas/configuracion_privada.h` (las mismas claves en Base64) | repositorio, excluida por `.gitignore` | **No** (el script lo comprueba) |

El script nunca imprime las claves y no sobrescribe nada que ya exista. **No copies esos archivos a un chat ni a un correo.**

## 12. Compilación del firmware

```powershell
& $cli compile --fqbn esp32:esp32:esp32 hardware\estacion_tres_canecas
```

Hoy: «El Sketch usa 352337 bytes (26%)». Sin avisos con `--warnings all`. En el IDE: abrir
`hardware/estacion_tres_canecas/estacion_tres_canecas.ino` y pulsar ✔.

## 13. Carga al ESP32

```powershell
& $cli upload --fqbn esp32:esp32:esp32 -p COM3 hardware\estacion_tres_canecas
```

Hoy: cuatro regiones con «Hash of data verified» y reinicio automático por RTS: no hizo falta pulsar BOOT. Si alguna vez
aparece `Failed to connect… Wrong boot mode`, mantén pulsado **BOOT** mientras empieza «Connecting…».

## 14. Verificación desde el monitor serie

1. Carga primero `hardware/diagnostico_esp32`: no mueve motores. En el monitor serie (115200, fin de línea «Nueva línea»)
   escribe `INFO`.
2. Resultado de hoy:
   - `modelo=ESP32-D0WD-V3`, `cpu_mhz=240`, `flash_bytes=4194304` y `arduino_core=3.0.7`;
   - `reinicio=encendido`: si alguna vez dice **BROWNOUT**, la alimentación se cae al mover los servos.
3. Escribe `PINES`: los seis GPIO dieron 0 en reposo.
4. Con `estacion_tres_canecas` cargado, escribe `?`. La respuesta debe ser `{"ecoaccessDesafio":"<32 hex>"}`. Cualquier
   otra cosa recibe `{"error":"rechazado"}`.
5. **Cierra el monitor serie antes de iniciar los bridges**: Windows abre el COM en exclusiva.

## 15. Encendido seguro

1. Con todo **desconectado**, mide la fuente con el multímetro: tensión y polaridad.
2. **Dimensionamiento:**
   - un SG90 consume entre 100 y 250 mA en movimiento y hasta unos 650 mA bloqueado;
   - un MG996R llega a unos 2,5 A bloqueado;
   - el firmware mueve **solo los dos servos de una tapa a la vez**, así que el pico es de dos servos;
   - aun así, con 5 V y 1 A, dos SG90 bloqueados ya están en el límite y dos MG996R lo superan.
   
   Usa al menos 5 V y 3 A para SG90, y bastante más para MG996R, después de confirmar el modelo.
3. Conecta primero la tierra común, luego las señales y por último el +5 V de los servos.
4. Conecta el USB. El firmware **no mueve nada al encender ni al reiniciar**.
5. Ten la fuente de los servos al alcance para cortarla en cualquier momento.

## 16. Integración con los servicios de EcoAccess

Una consola por proceso. Cada bridge lee su archivo con `ECOACCESS_ARCHIVO_BRIDGE`:

```powershell
$env:ECOACCESS_ARCHIVO_BRIDGE = 'C:\ProgramData\EcoAccessP3Framework\Bridge\bridge-blanca.json'
.\src\EcoAccess.P3.Bot\bin\Release\net48\EcoAccess.P3.Bot.exe --dispositivo-serial   # adaptador USB de la blanca
```

```powershell
$env:ECOACCESS_ARCHIVO_BRIDGE = 'C:\ProgramData\EcoAccessP3Framework\Bridge\bridge-blanca.json'
.\src\EcoAccess.P3.Bot\bin\Release\net48\EcoAccess.P3.Bot.exe --bridge                # despacho desde Oracle
```

- Hoy se comprobó que el adaptador de la blanca **arranca con `bridge-blanca.json`** y espera órdenes sin abrir el COM.
- Repite lo mismo con `bridge-verde.json` y `bridge-negra.json` solo cuando esas tapas estén calibradas y en servicio en Oracle.
- El bot de Telegram sale del «Modo demostración» cuando tiene configurados `Bridge:Eventos` y `Bridge:Comandos`, por ejemplo con `ECOACCESS_ARCHIVO_BRIDGE` apuntando al JSON de la blanca.

Para pasar el bot en vivo a modo físico hay que reiniciarlo, y eso lo decide el propietario.

## 17. Prueba del QR

El QR es **uno solo y compartido**: `EcoAccessCanecaPiloto1`, ya registrado en Oracle Cloud. No se generó ninguno nuevo.
El QR identifica la estación y al ciudadano; **la tapa que se abre la decide la clasificación del residuo**
(`ResolutorDestino`), nunca el QR. Prueba: escanéalo con el celular y comprueba que Eco responde con la estación y pide
la foto.

## 18. Prueba de cada caneca (supervisada, sin tapas acopladas)

1. Carga `hardware/calibracion_seis_servos`.
2. En el monitor serie escribe `ARMAR` y luego `S 17 30`. Anota qué servo se movió y de qué tapa.
3. Repite con 16, 21, 23, 18 y 19, uno a la vez. `PARAR` quita el PWM de todos.
4. Para cada tapa, busca los ángulos de cerrado y abierto sin que los dos servos se opongan ni choquen.
5. Ejecuta el paso 10 con los pines y ángulos medidos, solo para las tapas calibradas, y vuelve a cargar la estación.
6. Primera orden real: solo la **blanca** (la única en servicio en Oracle). Envía una foto clara de una botella PET y
   mide con cronómetro que la tapa quede abierta 10 s y cierre sola.
7. La verde y la negra siguen fuera de servicio en Oracle hasta repetir los pasos 1 a 6 con cada una y tener la
   autorización del propietario.

Cada prueba física se anota con fecha, persona que supervisa, fuente medida y un video o foto **sin datos personales**.

## 19. Errores comunes

| Síntoma | Causa | Solución |
|---|---|---|
| `Missing FQBN` | No hay placa seleccionada; faltaba el núcleo esp32 | Pasos 5 y 6 |
| No aparece el COM | Cable solo de carga o falta el driver CP210x | Otro cable; paso 4 |
| `Access is denied` o puerto ocupado | El monitor serie o un bridge tienen abierto el COM | Cierra el monitor; un solo dueño del COM a la vez |
| `Failed to connect … Wrong boot mode` | La placa no entró al cargador | Mantén BOOT al empezar «Connecting…» |
| `reinicio=BROWNOUT` o la placa se reinicia al mover | Fuente insuficiente o sin tierra común | §15 |
| Todo responde `{"error":"rechazado"}` | Tapa sin pines o sin calibrar, clave distinta de la del bridge, o desafío vencido | Pasos 10 y 11; no edites las claves a mano |
| Un servo vibra en el tope | Ángulo fuera del recorrido mecánico | Reduce el ángulo en el paso 18 |
| `estado=incierto` | Se reinició o falló durante un ciclo | §20 |

## 20. Reinicio y recuperación

- **Reinicio normal:** desenchufa el USB o pulsa EN. No se mueve nada y las reservas de órdenes se conservan.
- **Reinicio a mitad de un ciclo:** esa tapa queda bloqueada (`incierto`). Para recuperarla:
  1. revisa la tapa físicamente;
  2. en `configuracion_privada.h`, edita a mano la línea `RECUPERACION_MANUAL_VERIFICADA` y pon `true` solo en la posición de esa tapa;
  3. carga el firmware;
  4. vuelve a ponerla en `false` y carga otra vez.

  Nunca se repite una orden incierta.
- **Parada de emergencia:** corta la fuente de los servos. Desde el software, cierra los procesos `--dispositivo-serial`: sin ellos, ninguna orden llega a la placa.
- **No borres** `C:\ProgramData\EcoAccessP3Framework\Bridge\estado-*`: contienen el diario de órdenes.

## Qué debe proporcionar el propietario

1. Cuántos servos hay conectados, su modelo (SG90, MG90S, MG996R…) y una foto del cableado.
2. Tensión y corriente de la fuente.
3. Estar presente, con la fuente al alcance, durante el paso 18.
4. La decisión de poner en servicio la verde y la negra en Oracle, después de calibrarlas.

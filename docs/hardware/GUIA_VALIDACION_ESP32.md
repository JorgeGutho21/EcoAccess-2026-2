# Validación del ESP32 con dos servomotores

**Estado: pendiente.** Nada de esta guía se ha ejecutado con hardware. Sigue el orden y no avances si un paso falla. Hazlo con otra persona presente la primera vez y con la alimentación de los servos al alcance de la mano.

## 1. Antes de conectar

La fuente de 5 V y 1 A **no está validada** para dos servomotores bajo carga. Un servo pequeño puede consumir más de 1 A al arrancar o bloquearse, y una caída de tensión reinicia el ESP32 en plena apertura.

- Consigue un multímetro con medición de corriente o una fuente de banco con limitación.
- Nunca alimentes los servos desde los pines del ESP32.
- Une las tierras de la fuente, los servos y el ESP32.
- Señales: servo 1 en GPIO 18, servo 2 en GPIO 19.

## 2. Comprobar el puerto COM

Con EcoAccess detenido, conecta solo el ESP32 por USB, sin servos:

```powershell
[System.IO.Ports.SerialPort]::GetPortNames()
```

Anota el puerto que aparece al conectar y desaparece al desconectar. Si no aparece ninguno, instala el controlador USB de tu placa (CP210x o CH340).

## 3. Configurar la identidad

```powershell
Copy-Item hardware\servosecoacces\configuracion_ejemplo.h hardware\servosecoacces\configuracion_privada.h
```

`configuracion_privada.h` está excluido de Git. Lee la identidad y la caneca:

```powershell
(Get-Content C:\ProgramData\EcoAccessP3Framework\Bridge\bridge.local.json -Raw | ConvertFrom-Json).Bridge | Select-Object Dispositivo, IdContenedor, VersionClave
```

Pon esos valores en `DISPOSITIVO`, `ID_CANECA` y `VERSION_CLAVE`. Copia la clave al portapapeles, sin mostrarla, y pégala en `CLAVE_BASE64`:

```powershell
[Convert]::ToBase64String([IO.File]::ReadAllBytes('C:\ProgramData\EcoAccessP3Framework\Bridge\clave-hmac.bin')) | Set-Clipboard
```

Deja `CALIBRACION_VERIFICADA = false`. Con ese valor el firmware no mueve los motores.

## 4. Cargar el firmware sin movimiento

En Arduino IDE instala el paquete `esp32` de Espressif 3.x, abre `hardware\servosecoacces\servosecoacces.ino`, elige tu placa y el puerto, y pulsa **Cargar**. Abre el monitor serie a 115200 baudios: no debe moverse nada.

## 5. Validar la alimentación

Conecta **un solo servo, sin tapa**, a la fuente externa, con el amperímetro en serie.

| Medición | Dónde | Anota |
|---|---|---|
| Reposo | servo quieto | ___ A |
| Arranque | al moverlo | ___ A |
| Bloqueo | sujeta el brazo con cuidado, menos de 2 s | ___ A |

Repite con el otro. Tu fuente debe superar **la suma de los dos picos de bloqueo** con margen; si no llega, cámbiala antes de seguir. Comprueba también que la tensión no baje de unos 4,5 V durante el arranque.

## 6. Calibrar sin tapa

1. Con los servos sin tapa, ajusta `CERRADO_1`, `CERRADO_2`, `ABIERTO_1` y `ABIERTO_2`.
2. Revisa el sentido de giro y los topes. Los dos motores mueven la misma tapa: **no deben oponerse**.
3. Solo entonces pon `CALIBRACION_VERIFICADA = true` y vuelve a cargar.

## 7. Primera orden, sin tapa

Añade el puerto dentro de `Bridge` en `bridge.local.json`, por ejemplo `"PuertoSerial": "COM5"`, e inicia:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\desarrollo\Iniciar-Bot-Eco.ps1 -DispositivoSerial
```

Desde el teléfono envía una foto y confirma. Comprueba:

- El movimiento ocurre solo después de confirmar, nunca al enviar la foto.
- Repetir el botón no genera otro movimiento.
- Mide con cronómetro desde la apertura hasta el inicio del cierre: **20 s**, según `ABIERTO_MS`.

Anota la duración medida. Solo esa medición valida el temporizador físico.

## 8. Con la tapa

Monta la tapa solo si el paso 7 fue limpio. Repite tres aperturas, mide corriente y observa si algo roza o se atasca. Corta la alimentación si un motor forcejea. Un reinicio deja el firmware bloqueado hasta la revisión física; `RECUPERACION_MANUAL_VERIFICADA` se usa solo tras revisar y luego vuelve a `false`.

## 9. Validar los eventos en Oracle

```powershell
@"
ALTER SESSION SET CONTAINER=PRUEBAP3;
ALTER SESSION SET CURRENT_SCHEMA=ECOACCESS_P3_APP;
SET LINESIZE 150 PAGESIZE 50
SELECT id_orden, estado, fecha_emision_utc, fecha_cierre_utc FROM orden_apertura ORDER BY id_orden DESC FETCH FIRST 3 ROWS ONLY;
SELECT id_orden, tipo, resultado, fecha_dispositivo_utc, fecha_recepcion_utc FROM evento_dispositivo ORDER BY id_evento DESC FETCH FIRST 6 ROWS ONLY;
EXIT
"@ | docker exec -i oracle-free-p3 bash -c "sed '1s/^\xEF\xBB\xBF//' | sqlplus -s / as sysdba"
```

Cada orden debe tener un solo ciclo de eventos. Los recibos confirman la orden PWM, **no** la posición de la tapa ni el ingreso del residuo. Sin sensor, el depósito queda pendiente de verificación y no genera ecopuntos hasta que un operador lo confirme.

## 10. Registro de resultados

| Comprobación | Resultado | Fecha |
|---|---|---|
| Puerto COM | | |
| Corriente en reposo, arranque y bloqueo | | |
| Fuente suficiente | | |
| Calibración sin tapa | | |
| Una sola apertura por orden | | |
| 20 s medidos con cronómetro | | |
| Tres ciclos con tapa | | |
| Eventos en Oracle | | |
| Desconexión de USB durante un ciclo | | |

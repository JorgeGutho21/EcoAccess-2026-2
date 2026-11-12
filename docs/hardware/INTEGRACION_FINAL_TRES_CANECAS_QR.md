# Integración final: tres canecas, un QR y el ESP32 (11/11/2026)

Esta guía explica desde cero cómo una foto en Telegram termina abriendo **una sola** tapa. También dice qué está probado y qué falta antes de la primera prueba física.

Complementa a `GUIA_INTEGRACION_ESP32_TRES_CANECAS.md` (la variante de tres ESP32) y a `hardware/README.md`.

## 0. Estado real

| Parte | Estado | Evidencia |
|---|---|---|
| QR oficial compartido `EcoAccessCanecaPiloto1` | VERIFICADO REAL | Oracle Cloud; no se reemplazó ni se duplicó |
| Canecas en Oracle: 1093 blanca (en servicio), 1400 verde y 1401 negra (fuera de servicio) | VERIFICADO REAL | Consulta del 11/11/2026 |
| Selección de caneca por material (`ResolutorDestino`) | VERIFICADO EN SIMULACIÓN | `EstacionTresCanecasTests`, `EstacionTresCanecasOracleTests` |
| Estación de **un ESP32 con tres tapas** (`EstacionTresTapasTests`) | VERIFICADO EN SIMULACIÓN | 16 de 16: cada orden escribe solo los dos canales de su tapa; nunca hay dos tapas en movimiento |
| Lógica del firmware (`EstacionCanecas.h`) | VERIFICADO EN SIMULACIÓN | Banco nativo `hardware/pruebas/EstacionCanecasTests.cpp`, aprobado en el CI (job `hardware`) |
| Compilación para `esp32:esp32:esp32` (núcleo 3.0.7) | VERIFICADO (compilación) | CI, commit `a1af3e6`: estación 352 089 B, una tapa 344 809 B, calibración 315 065 B |
| Carga en placa, calibración, apertura, 10 s medidos, consumo | **PENDIENTE** | No se movió ningún motor |
| Mapeo de cada color a su par de GPIO | **PENDIENTE** | El archivo del compañero no lo dice |

## 1. El archivo de seis servos del compañero

`hardware/referencia/seis_servos_companero_original.ino.txt` es la copia sin cambios (SHA-256 del ZIP `df1d3092…805d3b`). Lo que hace:

- Configura seis servos en GPIO **17, 16, 21, 23, 18 y 19**, con PWM de 50 Hz y 16 bits (API `ledcAttach` del núcleo 3.x).
- Los lleva a 0° y después, **los seis a la vez**, los mueve a 60° (el comentario dice 90°, pero el código usa 60) durante 2 s. Repite sin fin.
- **No** tiene identidad, QR, HMAC, órdenes por caneca, ventana de 10 s ni estado seguro. **No debe cargarse con las tapas puestas.**
- Mover seis servos a la vez multiplica el pico de corriente. La fuente de 5 V y 1 A **no** está validada para eso.

**Topología deducida:** un ESP32 con seis servos, es decir, tres tapas de dos servos. Los pines 18 y 19 coinciden con el firmware seguro de una tapa. Que la blanca sea 17 y 16 (u otro par) **no** está confirmado.

## 2. Arquitectura elegida

```
Telegram ─► Bot (ServicioConversacionTelegram) ─► Oracle (INTERACCION, ORDEN_APERTURA)
                                                     │
              ┌──────────────────────────────────────┼──────────────────────────────┐
              ▼                                      ▼                              ▼
   Bridge blanca (1093)                  Bridge verde (1400)               Bridge negra (1401)
   clave y dispositivo propios           clave y dispositivo propios       clave y dispositivo propios
              └────────────── un solo COM, abierto en exclusiva ───────────────────┘
                                                     ▼
                                   ESP32 — estacion_tres_canecas.ino
                     tapa 0 (2 servos) · tapa 1 (2 servos) · tapa 2 (2 servos)
```

- **Por qué tres bridges y no uno:** cada caneca conserva su identidad, su clave HMAC, sus puertos locales, su carpeta de estado y su registro en Oracle. No se debilita ni se reescribe el protocolo existente.
- **Por qué es seguro compartir el COM:** Windows abre un puerto serie en exclusiva. Si la verde está en su ciclo de 10 s, el bridge de la blanca recibe «acceso denegado» y `ActuacionSerial.AbrirEsperandoTurnoAsync` **espera su turno** mientras la orden siga vigente (con 2 s de margen). Después vuelve a preguntar a Oracle si la orden sigue autorizada (`SigueAutorizadoComandoAsync`). Así nunca hay dos tapas moviéndose, y el pico de corriente queda en dos servos.
- **Alternativa de tres ESP32:** sigue siendo válida con el firmware `hardware/servosecoacces` (uno por tapa, cada uno con su COM). Ver la guía anterior.

## 3. Recorrido completo (de la foto a Oracle)

| Paso | Qué ocurre | Clase y método |
|---|---|---|
| 1 | La persona escanea el QR compartido; Telegram abre el bot con el identificador de la estación | `EnlaceTelegram.ParaCaneca`, `CodigoQrCaneca` |
| 2 | Eco identifica la estación y abre una interacción | `ServicioInteracciones.IniciarAsync` / `IniciarVisitanteAsync` |
| 3–5 | Llega la foto; la IA local la clasifica y la lógica fija el material (vocabulario cerrado y objetos confundibles) | `ServicioClasificacionImagen`, `InterpretadorNombreObjeto` |
| 6 | El resolutor elige la caneca por la categoría del material | `ResolutorDestino.PorCategoria` y `ResolutorDestino.Resolver` (`DestinoEstacion.cs`) |
| 7 | Comprueba que esa caneca esté en servicio. Si está fuera de servicio, **orienta pero no abre** | `MensajesInteracciones.CanecaNoEnServicio` |
| 8 | Se crea una orden **solo** para esa caneca, válida 2 minutos y con clave de idempotencia | `ServicioInteracciones.AutorizarAperturaAsync` (`VigenciaOrden`) |
| 9 | El bridge de esa caneca la toma, pide un desafío al ESP32 y le envía el permiso firmado con su clave | `DispositivoHttpConColas`, `ActuacionSerial.EjecutarAsync` |
| 10 | El ESP32 busca la tapa por la identidad firmada, verifica el HMAC, comprueba que la orden sea de su caneca y reserva la orden en NVS **antes** de mover | `autenticar`, `OrdenParaTapa` y `reservar` (`estacion_tres_canecas.ino`) |
| 11 | Abre **solo** sus dos servos y mantiene la ventana de 10 s | `ServosEstacion::Posicionar`, `ABIERTO_MS = 10000` |
| 12 | Cierra y envía un recibo firmado | `responder(..., "pwm-completado-sin-sensor")` |
| 13 | El bridge registra en Oracle los eventos de acuse, apertura y cierre sin sensor | `AplicacionBridge`, `TipoEventoDispositivo` |
| 14 | El depósito queda **declarado, sin verificación física** | `ServicioInteracciones` |
| 15 | Los ecopuntos solo se otorgan cuando un Operario o Supervisor verifica las unidades | `ServicioEcopuntos` |

## 4. Configuración del firmware de tres tapas

1. Instalar en Arduino IDE el paquete **esp32 de Espressif 3.x** (el CI usa la 3.0.7) y elegir la placa y el COM reales.
2. Copiar `hardware/estacion_tres_canecas/configuracion_ejemplo.h` como `configuracion_privada.h`. Ese archivo está excluido de Git.
3. Llenar una fila por tapa, con los datos del JSON privado del bridge de esa caneca:

| Campo | Blanca | Verde | Negra |
|---|---|---|---|
| `dispositivo` | `esp32-piloto-blanca` | `esp32-piloto-verde` | `esp32-piloto-negra` |
| `idCaneca` | 1093 | 1400 | 1401 |
| `versionClave` y `claveBase64` | los de su bridge | los de su bridge | los de su bridge |
| `pin` | **por confirmar** | **por confirmar** | **por confirmar** |
| `cerrado`, `abierto` | ángulos medidos | ángulos medidos | ángulos medidos |
| `calibracionVerificada` | `true` solo tras calibrar | ídem | ídem |

4. Reglas que el firmware hace cumplir:
   - Si un pin no está entre 16, 17, 18, 19, 21 y 23, o si se repite, o si una identidad o caneca está duplicada, **toda** la estación queda deshabilitada (`Validar`).
   - Una tapa sin pines, sin calibrar o sin clave queda fuera de servicio (`Habilitada`), y las demás siguen funcionando.
5. Al encender, el firmware no conecta PWM ni mueve nada. Si se reinicia a mitad de un ciclo, **esa** tapa queda bloqueada hasta `RECUPERACION_MANUAL_VERIFICADA[i] = true`, que se pone después de revisarla físicamente.

## 5. Configuración de los tres bridges

Igual que en la guía anterior (puertos 8182 a 8187 y una carpeta de estado por caneca), con una diferencia: **los tres JSON llevan el mismo `PuertoSerial`**.

Se inicia un proceso `--dispositivo-serial` por caneca, cada uno con su `ECOACCESS_ARCHIVO_BRIDGE`. No se ejecuta `--dispositivo-simulado` al mismo tiempo.

## 6. Preparación física, en este orden y supervisada

1. **Inventario:**
   - modelo exacto de la placa ESP32 y número de placas;
   - modelo de los servos (por ejemplo, SG90 o MG996R) y su corriente de bloqueo;
   - fuente: tensión medida y corriente máxima.
2. **Alimentación:**
   - los servos se alimentan de una fuente externa, **nunca** de los 3,3 V ni de los GPIO del ESP32;
   - tierra común entre la fuente, los servos y el ESP32;
   - comprobar la polaridad y los conectores;
   - para seis servos (y aunque el firmware mueva solo dos a la vez), dimensionar con margen el pico de dos servos en bloqueo.
3. **Mapeo, sin tapas puestas:**
   - cargar `hardware/calibracion_seis_servos`;
   - en el Monitor Serie escribir `ARMAR` y luego `S 17 30`, uno a uno;
   - anotar qué servo se mueve y de qué tapa;
   - `PARAR` quita el PWM.
4. **Límites:** con el mismo banco, buscar los ángulos de cerrado y abierto sin que el servo choque ni se oponga al otro de su tapa.
5. **Firmware de estación:** llenar `configuracion_privada.h` con los pines y ángulos medidos, poner `calibracionVerificada = true` **solo** en las tapas calibradas, compilar y cargar.
6. **Primera orden:** solo la blanca (la única en servicio), con alguien junto a la caneca y la fuente a mano para cortarla.
7. **Verde y negra:** siguen **fuera de servicio en Oracle** hasta completar los pasos anteriores y recibir la autorización del propietario.

## 7. Pruebas de software disponibles

| Prueba | Archivo | Qué demuestra |
|---|---|---|
| Selección blanca, verde y negra; apertura exclusiva | `EstacionTresTapasTests.CadaCanecaAbreSoloSusDosServos_YCierraTrasLaVentana` | Cuatro escrituras PWM, solo en los pines de la tapa elegida |
| Caneca equivocada | `OrdenParaOtraCaneca_ElBridgeNoLaEnvia_YLaEstacionLaRechazaSiLlega` | Ni el bridge ni la estación la ejecutan |
| Dispositivo equivocado o firma inválida | `DispositivoDesconocidoOFirmaFalsa_NoMuevenNada` | Rechazo sin movimiento |
| Orden caducada | `OrdenCaducada_NoSeEnvia_YValidezCortaSeRechaza` | — |
| Orden duplicada | `OrdenDuplicada_EnCualquierTapa_NoReabre` | La reserva es global |
| Desafío repetido | `DesafioRepetido_SeConsumeAunqueLaOrdenSeaNueva` | — |
| Reinicio durante la apertura | `ReinicioDuranteLaApertura_BloqueaSoloEsaTapaHastaRecuperacionManual` | Sin PWM al encender |
| Desconexión o fallo de comunicación | `DesconexionAntesDelCierre_FallaSinReintentar` | — |
| Fallo de servo | `FalloDeServo_NoAvisaApertura_YDejaLaTapaFueraDeServicio` | — |
| Tapa fuera de servicio | `TapaFueraDeServicio_NoSeMueve_YLasDemasSi` | — |
| Concurrencia | `TresBridgesALaVez_ComparteElPuerto_YNuncaSeMuevenDosTapas` | Como máximo una tapa en movimiento |
| Puerto ocupado | `PuertoOcupado_EsperaTurno_YRevalidaLaOrdenAlObtenerlo` y `PuertoOcupadoHastaCasiVencer_NoEsperaMasAllaDeLaOrden` | — |
| Ventana de 10 s | `LaVentanaDelBridgeCubreLosDiezSegundosDelFirmware` y `Validar` (de 1 a 30 s) | — |
| Registro en Oracle, QR, ecopuntos | `BridgeOracleTests`, `EstacionTresCanecasOracleTests`, `CargaUsuariosOracleTests` (integración) | — |

Todas son **pruebas de software**. Una prueba física se registra aparte, con fecha, persona que supervisa, fuente medida y video o foto.

## 8. Parada segura

- Desde el software: cerrar los procesos `--dispositivo-serial`. Sin bridge, ninguna orden llega a la placa.
- En la placa: el firmware nunca mueve al encender.
- Banco de calibración: `PARAR` quita el PWM de los seis pines.
- Física: cortar la fuente de los servos. Siempre debe estar al alcance durante las pruebas.

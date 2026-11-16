# Fase de software de EcoAccess P3 (sin ESP32) — 09/11/2026

EcoAccess P3 · Universidad Popular del Cesar · Programación de Computadores III · Jorge Gutierrez Thomas y Heyder Canchila.
Esta fase deja el sistema usable sin hardware: QR oficial, Telegram, registro y vinculación, reconocimiento visual y
destino ambiental contra Oracle Cloud. Actualiza `ESTADO_REAL_09-11-2026.md` en los puntos 2, 6.1 y 6.2.

## 1. QR oficial

- **Causa del «No reconozco ese código»:** el QR oficial (`EcoAccessCanecaPiloto1`) no tenía caneca en Oracle Cloud; el
  script que la creaba solo corre en la base local `PRUEBAP3`.
- **Solución autorizada:** `database/03_datos_prueba/08_piloto_qr_oficial.sql`, idempotente y en una transacción. Creó la
  estación «EcoAccess P3 — Estación piloto» y la caneca 1093 «EcoAccess — Caneca piloto» con el QR oficial y los 11
  aprovechables reales. La segunda ejecución no agregó nada. Cambios en Oracle: +1 estación, +1 caneca, +11 asociaciones;
  ningún registro de V2 ni de ciudadanos. La caneca dice en su ubicación que no hay ESP32 ni aperturas verificadas.
- **Verificado:** prueba `QrOficialIdentificaLaEstacionPilotoSinCrearSolicitudesEnModoSoftware` contra Oracle Cloud:
  `/start` saluda, `/start EcoAccessCanecaPiloto1` identifica la estación, un QR falso se rechaza, 0 interacciones y 0
  órdenes nuevas. Windows Forms muestra «La caneca piloto recibe este material» (`docs/evidencias/ejecucion_real/05_…`).
  El archivo del QR no cambió (`QrOficialTests`).

## 2. Un solo bot: P3

P3 y V2 usan el mismo token. Se detuvo el proceso de V2 (sin tocar su código, datos ni credenciales) y se inició P3.
`--comprobar-telegram`: `@EcoAccessP3_JG_bot`, sin webhook, 0 pendientes. El lanzador se niega a iniciar un segundo
consumidor; con `-DetenerOtroBot` detiene el de otra copia. Para usar ambos proyectos a la vez se necesitan dos bots
(dos tokens) o un único receptor que reparta los mensajes.

## 3. Modo software (sin ESP32)

Sin simulador ni `Bridge:Eventos/Comandos`, el bot activa `SinAperturaFisica`:

- El QR identifica la estación y pide la foto, sin crear solicitud (tampoco de invitado).
- Una foto admitida responde, por ejemplo: «Botella plástica PET. / Caneca blanca. / Modo demostración: apertura física
  pendiente.» Verde y negra usan la misma regla de Oracle (`GuiaSeparacion`), cubierta por `ModoSoftwareTests`.
- No se emiten órdenes, no se registran aperturas ni se otorgan ecopuntos. El protocolo HMAC y el firmware no cambian.

## 4. Registro y vinculación por HTTPS

- Tailscale 1.104.5 instalado (winget, hash verificado); la sesión de la cuenta ya estaba activa.
- `Publicar-Registro-HTTPS.ps1` ahora publica con el bot escuchando solo en `127.0.0.1:8081`: no necesita administrador
  ni abre el formulario a la red local. Funnel publica únicamente `/registro`
  (`https://gutiome.tailec56db.ts.net/registro/`); cualquier otra ruta responde 404. Oracle, Ollama y el bridge no se
  exponen.
- Comprobado: local 8081 → HTTP 400 sin token; pública → HTTP 400 con certificado válido (lo esperado sin token).
- **Limitación:** Funnel depende de que este computador esté encendido. No es alojamiento 24/7.
- Contra Oracle Cloud, 14/14 (`CargaUsuariosOracleTests`, `RegistroCiudadanosOracleTests`) sin dejar datos: cuentas nuevas
  (10 y 20 simultáneas) que sobreviven al reinicio, cuentas existentes vinculadas sin duplicarse, mismo correo → una
  cuenta, enlace usado no sirve dos veces, cuenta vinculada no pasa a otro Telegram, CSRF y token por formulario.

## 5. Reconocimiento visual del motor de P3

Mismas 50 fotos TrashNet (MIT) que la evaluación de V2, ahora con el motor de P3 (LFM3-VL local, sin enviar fotos fuera),
contra la caneca piloto real. Resultado: `docs/vision/resultados/objetos_p3_trashnet50.json`.

| Medida | P3 |
|---|---|
| Objeto correcto | 32/50 (64 %) |
| Contexto correcto | 42/50 |
| Destino ambiental correcto (blanca) | 42/50; 0 enviados a otra caneca |
| Sin material confirmado (pide otra foto o confirmación) | 8 (2 inciertas, 3 «comida servida», 3 «envoltorio») |
| Material equivocado admitido | 1 (lata tomada como vidrio; ambos van a la blanca) |
| Fallos técnicos | 0 |
| Latencia p50 / p95 | 3,3 s / 3,5 s (máx. 10,3 s, primera foto) |

Por clase: vidrio 8/10, papel 9/10, cartón 7/10, PET 6/10, lata 2/10. El 0/10 de papel era de V2 y no se repite en P3. Las
latas son el punto débil (las confunde con envoltorios). Las 217 fotos de Wikimedia de la evaluación 192/217 no están en
este equipo; ese resultado no se repitió. Faltan fotos de teléfono reales: dependen de que el propietario las envíe.

## 6. Iniciar y detener

```bash
pwsh -File scripts/desarrollo/Iniciar-EcoAccess-Nube.ps1 -ConEscritorio
```

```bash
pwsh -File scripts/desarrollo/Detener-Demostracion.ps1
```

El lanzador comprueba el equipo, consulta Oracle, verifica la identidad del bot y el motor visual, inicia el bot y prueba
la página local y pública. Tras reiniciar el computador: abrir Tailscale (Funnel se mantiene configurado) y ejecutar el
lanzador. Para la instalación en otros equipos: `docs/instalacion/INSTALACION_COMPANEROS.md`; ellos no inician el bot.

## 7. Pendiente de prueba manual

Desde el iPhone (datos móviles, no wifi de la casa): escanear el QR oficial, `/vincular` → crear cuenta, volver al bot,
enviar `/start` de nuevo y una foto. Hasta tener esa evidencia, el recorrido desde el teléfono no se declara comprobado.
Hardware (ESP32, servos, aperturas): fuera de esta fase.

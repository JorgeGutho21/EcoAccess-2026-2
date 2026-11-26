# Centro de control ECOACCESS ESP32 (panel local, `http://localhost:8090`)

Página: `src\EcoAccess.P3.PanelCanecas\wwwroot\index.html`. Usa solo las APIs existentes de `Program.cs` (sin cambios de
contrato) y está pensada para el PC de la estación (1366×768 en adelante).

## Qué muestra

- **Barra fija**: USB, puerto COM, ESP32, Oracle (según el pulso del bot), bot de Telegram y pasarela web; ACTIVAR
  CONTROL (sesión supervisada de 2 h, solo desde este equipo) y **DETENER TODO**, siempre visible al desplazarse.
- **Banco de la estación**: PC → cable USB → placa ESP32 dibujada (firmware informado por el dispositivo) → bus de
  cobre hacia los tres canales (verde GPIO 23, blanca GPIO 19, negra GPIO 18). Un canal se ilumina cuando el ESP32
  informa ese canal fuera de reposo («PWM comandado»).
- **Canecas** con sus capas reales (la tapa gira sobre la bisagra), estado **comandado, no medido**, cuenta regresiva
  del ciclo, ángulos y tiempo calibrados, y botones grandes ABRIR, CERRAR y DETENER.
- **Apartados desplegables**: combinaciones (política 7/14/21 s, simultánea escalonada o secuencial), calibración
  (guardado verificado releyendo el ESP32), diagnóstico (se verifica al abrirlo; solo GET) e historial técnico.
  `#apCombos+seccionCal+diag+apHist` en la dirección abre esos apartados; la página recuerda cuáles dejaste abiertos.

## Seguridad

- Al cargar solo se hacen GET (`/api/estacion/estado` cada segundo, `/api/historial` cada 2,5 s y `/api/diagnostico`
  al abrir Diagnóstico). Ninguna orden sale sin un clic, y el servidor sigue exigiendo la sesión supervisada para mover.
- La posición de las tapas es la **comandada**: no hay sensores de posición ni de corriente.
- Pruebas automáticas: `tests\unitarias\EcoAccess.P3.Tests\PanelCanecas\PaginaPanelCanecasTests.cs` (rutas del
  contrato, solo GET al cargar, DETENER TODO en la barra fija sin depender de la sesión, textos de sesión supervisada).

## Capturas (`capturas_panel/`)

Hechas el 25/11/2026 con Edge sin interfaz. Las de `127.0.0.1:8195` usan **datos de ejemplo** de un servidor de vista
previa que rechaza toda orden; `panel_real_8191_…` es la compilación de este cambio en el puerto 8191 con
`--puerto COM99 --solo-puerto` (sin dispositivo: nunca abrió COM3 ni envió órdenes; su historial solo registró el inicio).

| Archivo | Qué muestra |
|---|---|
| `panel_1366x768_motores_bloqueados.png` | ESP32 conectado, sesión supervisada inactiva |
| `panel_1366x768_ciclo_negra.png` | Sesión activa, negra en ciclo con cuenta regresiva (ejemplo) |
| `panel_1920x1080_ciclo_negra.png` | Lo mismo a 1920×1080 |
| `panel_1366_apartados_abiertos.png`, `panel_1920_apartados_abiertos.png` | Página completa con los apartados abiertos |
| `panel_1366x768_usb_desconectado.png` | USB desconectado: posición desconocida, movimiento bloqueado |
| `panel_real_8191_sin_dispositivo_1920x1080.png` | Servicio real compilado, sin dispositivo |

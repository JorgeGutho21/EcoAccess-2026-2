Se apila sobre #28 (`integracion/ecoaccess-final`): conviene fusionar primero #28 y después este PR, o fusionarlo dentro de #28.

## Causa real de «la estación está en pausa»
Telegram reconocía bien la cáscara de banano, pero el panel ECOACCESS ESP32 rechazaba la apertura con 423 porque el modo «ESTACIÓN EN SERVICIO» **caducaba a las 23:59 y nunca se restauraba al iniciar Windows**. Después de cada inicio normal, todas las aperturas ciudadanas quedaban en pausa hasta que alguien pulsara el botón otra vez. Además, el commit ddfd5cb dejó en `wwwroot/index.html` un `confirm("…")` con saltos de línea reales: es un **SyntaxError que inutilizaba todo el script del panel** (comprobado con `node --check`).

## Cambios
- **Panel (servicio central único de hardware):** la estación queda en servicio de forma persistente. Arranca en servicio; solo **PONER EN PAUSA** (con motivo) o **DETENER TODO** la pausan, y esa pausa se mantiene tras reiniciar hasta **REANUDAR SERVICIO**. La apertura sigue exigiendo la confirmación del ESP32; la calibración y las pruebas técnicas siguen pidiendo la sesión supervisada. `/api/estacion/estado` expone `enServicio`, `pausa`, `pausaDesde` y `operativa`.
- **App C# y Telegram:** `AnfitrionEstacion` localiza el panel del anfitrión: variables `ECOACCESS_PANEL_URL`/`ECOACCESS_PANEL_CLAVE`, `%LOCALAPPDATA%\EcoAccessP3\anfitrion.txt` o el panel local. Si hay un anfitrión de red configurado, nunca cae a simulación. Con la estación en pausa, Telegram explica que está en mantenimiento y nunca dice «Caneca abierta».
- **Oracle (operación manual T003, no aplicada todavía):** deja solo la Estación Oficial EcoAccess con «EcoAccess — Caneca verde/blanca/negra». Localiza las canecas por su QR, así que `EcoAccessCanecaPiloto1` y el QR impreso no cambian. Elimina el lote sintético SIM-2026-11-CIERRE. Antes de borrar, respalda en `ECO_RESP_T003_*` y comprueba precondiciones; al final verifica que quedan 1 estación y 3 canecas, y si no, revierte. La reversión está en `R_T003`. Se ejecuta con `scripts\instalacion\Depurar-EstacionOficial.ps1 -Aplicar`.
- **Instalación:** quedan exactamente cuatro accesos en todos los equipos: ECOACCESS P3, ECOACCESS ESP32, ECOACCESS DOS CLICS y ECOACCESS WEB. En el equipo de un compañero, ESP32 abre el panel del anfitrión configurado con el nuevo `Configurar-Anfitrion.ps1`, que comprueba la conexión sin mover tapas. Se corrigió la ruta de iconos del paquete, que tenía un carácter BEL. El verificador y la guía de compañeros están actualizados.
- Los textos visibles «catálogo del piloto» y «Estación piloto» pasan a «Estación Oficial EcoAccess».

## Verificación (equipo de Heyder, sin ESP32)
- `dotnet build` Debug y Release: 0 errores.
- Pruebas unitarias: 2635/2635 en dos ejecuciones repetidas. Una ejecución simultánea con la instalación tuvo 1 fallo aislado que no se reprodujo.
- Pruebas de datos: 65 superadas y 192 omitidas por diseño, porque solo corren en un esquema aislado de pruebas.
- Web (`node --test`): 15 superadas, 0 fallos. La prueba de integración web→pasarela→panel **no se ejecutó** porque requiere Python, que no está instalado en este equipo.
- Script del panel validado con `node --check`.
- Oracle, en solo lectura: la Estación Oficial 1015 tiene las canecas 1093, 1400 y 1401 en servicio; no existen canecas DEMO; la estación SIMULACIÓN 1327 está inactiva.
- Instalación local actualizada con los 4 accesos y sus iconos. GitHub Pages `/`, `/dos-clics/` y `/juego/` responden 200. ECOACCESS P3 abre la pantalla de inicio de sesión.

## Pendiente (requiere el anfitrión con el ESP32 o una autorización)
- Pruebas físicas de apertura y cierre de las tres canecas desde cada canal (7/14/21 s) y desde otro computador autorizado: **no realizadas**, porque el ESP32 no está conectado a este equipo.
- Ejecutar T003 en Oracle cuando el equipo lo autorice.
- Actualizar el anfitrión (`git pull`, reinstalar, `Iniciar-ServiciosEcoAccess.ps1`) para que el panel corregido entre en servicio, y volver a publicar GitHub Pages cuando esto llegue a `main`.

🤖 Generated with [Claude Code](https://claude.com/claude-code)

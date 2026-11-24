# EcoAccess P3 · estado de la entrega (24/11/2026)

Resumen corto y verificable. El detalle técnico está en los informes de esta carpeta.

| Tema | Estado |
|---|---|
| Carpeta oficial | `OneDrive - unicesar.edu.co\Documentos\02_PROYECTOS_PERSONALES\01_EcoAccess\01_ProyectoDeAula_EcoAccess\EcoAccess-P3` (solución `EcoAccess.P3.sln`) |
| Versión | `3.1.0-exposoftware`, rama `main` después de fusionar el PR #23. El commit exacto está en `VERSION.txt` del paquete |
| Compilación | Release, 0 errores |
| Pruebas unitarias | 2.407 aprobadas, 0 fallidas, 1 omitida (enlaces simbólicos de Windows sin privilegio) |
| Oracle Cloud | `ECOACCESS_P3_APP` conectado (el bot lee 41 materiales habilitados) |
| Oracle local | `FREEPDB1` en `localhost:1521`, esquema de pruebas `ECOACCESS_P3_PRUEBAS` |
| Telegram | `@EcoAccessP3_JG_bot`, un único proceso en polling en la estación, en modo físico supervisado (2 h por arranque) |
| ESP32 | COM3, firmware `estacion_tres_canecas` (responde al desafío firmado). Blanca 1093 → GPIO 19, verde 1400 → GPIO 23, negra 1401 → GPIO 18; cerrada 0°, abierta 90°, 10 s |

## Pruebas físicas supervisadas (24/11/2026, 15:13–15:16)

Ejecutadas con `EcoAccess.P3.Bot.exe --probar-estacion-fisica <canecas> --confirmar-presencia-supervisada`, con Jorge Gutiérrez presente. Una tapa a la vez.

| Prueba | Recibo firmado del ESP32 | Lo que se observó |
|---|---|---|
| C01 verde | apertura y cierre | **no se movió** |
| C02 blanca | apertura y cierre | abrió y cerró |
| C03 negra | apertura y cierre | abrió y cerró |
| C04 verde + blanca | apertura y cierre de ambas | blanca sí, verde no |
| C05 verde + negra | apertura y cierre de ambas | negra sí, verde no |
| C06 blanca + negra | apertura y cierre de ambas | ambas |
| C07 las tres | apertura y cierre de las tres | blanca y negra sí, verde no |

- El recibo solo prueba que el ESP32 aplicó el PWM; no hay sensores de posición. El movimiento lo confirma la observación presencial.
- **Verde:** recibe la misma orden que las otras con el mismo firmware. El fallo es de su conexión física: el cable de señal en D23 (junto a D22) o la alimentación de ese servo.
- **Alimentación:** con los servos tomando corriente del pin VIN/5V del ESP32 alimentado por USB, ninguna tapa se movió. Necesitan una fuente externa de 5 V con GND común.

## Cambios de esta entrega

- **Numeración de órdenes.** El firmware guarda cada número de orden usado y rechaza los repetidos, y el coordinador PC–Telegram empezaba en 1 en cada arranque. La segunda prueba física, o el bot tras reiniciarse, habría recibido «rechazado». Ahora la numeración parte de 1 000 000 000 más el tiempo transcurrido.
- **Estación física.** El bot arranca con la sesión supervisada y con las cuentas autorizadas de `ProgramData\EcoAccessP3Framework\Bridge\telegram-autorizados.txt`, un ID numérico por línea. Ese archivo es privado y no está en Git.
- **Acceso directo.** El nuevo `scripts/desarrollo/Abrir-EcoAccess.ps1` es lo que ejecuta «ECOACCESS P3» en la estación. Inicia la estación si no está activa (o el bot en modo orientación si falta el ESP32), no duplica procesos, no abre tapas y luego abre la aplicación.
- **Limpieza.** Se retiró `.claude/` del repositorio (respaldada fuera) y `.gitignore` bloquea las configuraciones de asistentes. Los tres informes de la raíz pasaron a `docs/informes/`.

## Limitaciones reales

- La tapa verde no se movió en la prueba presencial: hay que revisar su cableado.
- Los comandos `/verde`, `/blanca`, `/negra` y `/abrir` de Telegram abren solo para las cuentas autorizadas. Su registro queda en el historial de la sesión del bot, no en Oracle. El flujo normal (foto o material → orden → depósito → ecopuntos) sí se registra en Oracle.
- Siguen sin fotografía real confirmada: B03, B13, N10, N11 y N12. El reconocimiento puede equivocarse y lo dudoso pide confirmación.
- Sin sensores de posición, el cierre de una tapa se confirma mirándola.

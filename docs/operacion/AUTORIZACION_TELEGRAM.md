# Cuentas de Telegram autorizadas para abrir las canecas

El lanzador de la estación informa, al iniciar el bot, «cuentas autorizadas para abrir desde Telegram: N». Este
documento explica de dónde sale ese número y cómo autorizar **una** cuenta verificada sin debilitar el control de acceso.

## Mecanismo real

1. **Lista**: `%ProgramData%\EcoAccessP3Framework\Bridge\telegram-autorizados.txt`, un ID numérico por línea.
   Hoy no existe, por eso el lanzador informa **0**.
2. **Carga**: `scripts\desarrollo\Iniciar-EstacionExpo.ps1`, al iniciar el bot, toma solo las líneas de 4 a 19 dígitos
   y las pasa al proceso en la variable `ECOACCESS_TELEGRAM_CHATS_AUTORIZADOS` (solo para ese proceso; no queda en el
   entorno del usuario). La lista se lee **al arrancar el bot**: un cambio necesita reiniciar el bot.
3. **Decisión**: `CoordinadorEstacionUnificado.EsChatAutorizado(chat, usuario)` (`src\EcoAccess.P3.Logica\Vision35`)
   acepta el chat o el usuario de la lista. En Modo Expo (`ServicioConversacionTelegram.Expo.cs`) se puede abrir si la
   cuenta está autorizada **o** si escaneó el QR de la estación (presencia temporal). El resto recibe «escanea el QR
   de la estación o pide a un supervisor que autorice este chat».
4. **Siempre**, además: el panel exige la **sesión supervisada** (ACTIVAR CONTROL en el PC de la estación) para mover
   un motor. Autorizar una cuenta no la habilita ni la sustituye.

Consultas, clasificación de fotos y `/miid` no requieren autorización.

## Procedimiento para autorizar una cuenta

1. La persona escribe **`/miid`** al bot desde **su** cuenta. El bot responde solo a ella con su ID numérico.
2. El supervisor lo comprueba **en persona**, mirando la respuesta en el teléfono de esa persona. No se aceptan IDs
   enviados por terceros, capturas reenviadas ni texto copiado de un chat.
3. En el PC de la estación (sin privilegios elevados):

   ```powershell
   # Ver qué haría, sin escribir nada:
   powershell -ExecutionPolicy Bypass -File scripts\arranque\Autorizar-CuentaTelegram.ps1 -Identificador <ID> -WhatIf
   # Autorizar (pide confirmación):
   powershell -ExecutionPolicy Bypass -File scripts\arranque\Autorizar-CuentaTelegram.ps1 -Identificador <ID>
   ```

   El script valida la forma del ID (4 a 19 dígitos, positivo), no lo duplica, lo añade en ASCII (una marca BOM
   invalidaría la primera línea), deja constancia **enmascarada** en `Sesiones\arranque-ecoaccess.log` y no reinicia
   nada.
4. Aplicar el cambio reiniciando **solo el bot** (un único consumidor del token):

   ```powershell
   powershell -ExecutionPolicy Bypass -File scripts\desarrollo\Iniciar-EstacionExpo.ps1 -ReiniciarBot
   ```

   o esperar al próximo inicio de sesión (tarea `EcoAccess P3 - arranque de la estación`).
5. Comprobar: el lanzador informa el nuevo total, `Estado-ServiciosEcoAccess.ps1` lo muestra, y `/miid` en el teléfono
   de la persona responde «Este chat ya está autorizado…».

## Revocar

Borrar la línea del archivo `telegram-autorizados.txt` y reiniciar el bot como en el paso 4.

## Lo que este procedimiento no hace

- No se ejecutó con ningún ID: la lista sigue vacía (0 cuentas) hasta que el usuario dé un ID verificado.
- No desactiva el QR, la sesión supervisada ni la idempotencia de las aperturas.
- No acepta chats de grupo (IDs negativos) ni comodines.

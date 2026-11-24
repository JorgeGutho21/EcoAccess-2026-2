# INFORME FINAL EXPOSOFTWARE — ECOACCESS P3

- **Responsable:** Jorge Gutiérrez (`Jorge Gutierrez <263990811+JorgeGutho21@users.noreply.github.com>`)
- **Fecha de verificación:** 2026-11-24
- **Repositorio oficial único:** `C:\Users\Usuario\OneDrive - unicesar.edu.co\Documentos\02_PROYECTOS_PERSONALES\01_EcoAccess\01_ProyectoDeAula_EcoAccess\EcoAccess-P3`
- **Remoto GitHub:** `https://github.com/JorgeGutho21/EcoAccess-2026-2`
- **Rama activa y Pull Request:** `feat/ecoaccess-vision-integracion-final` → Draft PR #23 (`https://github.com/JorgeGutho21/EcoAccess-2026-2/pull/23`)
- **Solución principal:** `EcoAccess.P3.sln` (`.NET Framework 4.8`, `C# 9.0`)

---

## 1. Resumen Ejecutivo

ECOACCESS P3 se encuentra **unificado, compilado sin errores en `Debug` y `Release`, validado con 2.404 pruebas unitarias aprobadas (0 fallos)** y con todos los subsistemas reales verificados en vivo (cámara USB, Ollama local con `lfm3-vl:latest` y `qwen3-vl:4b`, bot de Telegram `@EcoAccessP3_JG_bot`, Oracle Cloud en solo lectura con 41 materiales habilitados y controlador ESP32 en `COM3` respondiendo con desafíos HMAC del firmware `estacion_tres_canecas`).

El único paso pendiente antes de la demostración presencial es la **ejecución de las 7 pruebas físicas supervisadas de apertura sobre `COM3`**, bloqueadas deliberadamente por el mecanismo de seguridad (`--confirmar-presencia-supervisada`) hasta recibir la confirmación explícita presencial de Jorge Gutiérrez.

---

## 2. Arquitectura Unificada (Windows PC + Telegram + Visión + Manual + Hardware)

Todos los canales convergen en una única solución (`EcoAccess.P3.sln`) y en reglas deterministas compartidas:

| Canal | Componente principal | Reglas compartidas | Acceso al hardware |
|---|---|---|---|
| Aplicación Windows (Visión + Demo) | `FormReconocimientoVisual.cs` + `GestorSesionMultirresiduo.cs` | `MotorReglasMultirresiduo` + `MotorCombinacionesCanecas` (7 combinaciones) | Modo Demo seguro por omisión |
| Registro Manual | `FormRegistroManual.cs` + `FormCatalogoMateriales.cs` | Catálogo 35 materiales + `GESTION_ESPECIAL` | Coherente con selector de canecas |
| Chatbot de Telegram | `ServicioConversacionTelegram.cs` + `ServicioConversacionTelegram.CanecasUnificadas.cs` | `CoordinadorEstacionUnificado` + `ServicioInteracciones` | Serializado por `SemaphoreSlim(1,1)` |
| Controlador físico ESP32 (`COM3`) | `EjecutorEstacionSerialHmac.cs` + `ActuacionSerial.cs` + `ProtocoloDispositivoHmac.cs` | Reutiliza `bridge-verde.json`, `bridge-blanca.json`, `bridge-negra.json` | Exclusivo por comando (sin competencia por `COM3`), protegido por `GuardiaHardware` |
| Diagnóstico y Lanzador ExpoSoftware | `scripts/desarrollo/Iniciar-Modo-ExpoSoftware.ps1` | Comprobación real de cada subsistema, nunca abre canecas al arrancar | Requiere `-HabilitarModoFisico` explícito |

---

## 3. Diagnóstico Real Ejecutado (Solo Lectura — 0 Movimientos Físicos)

Resultado real obtenido mediante `scripts/desarrollo/Iniciar-Modo-ExpoSoftware.ps1 -SoloDiagnostico`:

| Subsistema | Estado verificado | Evidencia |
|---|---|---|
| Aplicación Windows (`Release`) | **LISTA** | `EcoAccess.P3.Presentacion.exe` y `EcoAccess.P3.Bot.exe` compilados en `Release` (0 errores) |
| Cámara USB | **CONECTADA** | `USB2.0 HD UVC WebCam` (`Status = OK`) |
| Ollama local | **CONECTADO** | `v0.40.16` en `127.0.0.1:11434` (`ListoParaInferencia = true`) |
| Modelos visuales | **LISTOS** | `lfm3-vl:latest` (predeterminado rápido) y `qwen3-vl:4b` (configurable) |
| Chatbot Telegram | **CONECTADO** | `@EcoAccessP3_JG_bot` verificado por `getMe` y `--comprobar-telegram` (0 updates pendientes, sin webhook) |
| Controlador ESP32 | **CONECTADO (`COM3`)** | `Silicon Labs CP210x USB to UART Bridge (COM3)` responde al desafío `?` con `{"ecoaccessDesafio":"<32 hex>"}` (`estacion_tres_canecas OK`) |
| Caneca Verde (Orgánicos) | **DISPONIBLE** | ID `1400` · Pin `23` · `bridge-verde.json` (config HMAC lista) |
| Caneca Blanca (Aprovechables) | **DISPONIBLE** | ID `1093` · Pin `19` · `bridge-blanca.json` (config HMAC lista) |
| Caneca Negra (No aprovechables) | **DISPONIBLE** | ID `1401` · Pin `18` · `bridge-negra.json` (config HMAC lista) |
| Oracle Cloud (`--comprobar-oracle`) | **CONECTADO (solo lectura)** | 41 materiales habilitados leídos correctamente; **0 escrituras** sobre producción |
| Procesos residuales detectados | **ALERTA CONTROLADA** | 7 procesos antiguos de `EcoAccess.P3.Bot.exe` fuera de la carpeta oficial (6 en `ProgramData\...\Estacion\bin` y 1 PID `39920` en el clon universitario previo). Limpiables automáticamente con `Iniciar-Modo-ExpoSoftware.ps1 -DetenerTodo` antes de iniciar modo físico |
| Modo físico al arrancar | **DESHABILITADO (MODO DEMO SEGURO)** | Nunca energiza servos sin confirmación supervisada |

---

## 4. Comandos Unificados de Telegram y Seguridad

Implementados en `ServicioConversacionTelegram.CanecasUnificadas.cs` y enlazados en `EcoAccess.P3.Bot/Program.cs`:

- **Consulta de estado:** `/canecas`, `/modo`, `/historial` (integrado con `/depositos`).
- **Apertura individual (3):** `/verde`, `/blanca`, `/negra`.
- **Apertura combinada (4):**
  - `/abrir verde blanca` (`C04_VerdeBlanca`)
  - `/abrir verde negra` (`C05_VerdeNegra`)
  - `/abrir blanca negra` (`C06_BlancaNegra`)
  - `/abrir verde blanca negra` (`C07_VerdeBlancaNegra`)
- **Controles de seguridad activos:**
  1. **Autorización estricta de chats:** solo los chats listados en `ECOACCESS_TELEGRAM_CHATS_AUTORIZADOS` o supervisores autorizados pueden accionar canecas; cualquier otro chat recibe denegación auditada.
  2. **Idempotencia por update:** clave `tg:<chatId>:<updateId>` bloquea aperturas duplicadas ante reenvíos de Telegram.
  3. **Serialización de `COM3`:** `CoordinadorEstacionUnificado` y `EjecutorEstacionSerialHmac` usan `SemaphoreSlim(1, 1)` para garantizar concurrencia máxima = 1 (nunca compiten PC y Telegram por el puerto serial).
  4. **Sesión supervisada con vigencia y parada inmediata:** el modo físico solo se activa con `ECOACCESS_MODO_FISICO_SUPERVISADO=1` o `--confirmar-presencia-supervisada`; `DeshabilitarSesionFisica()` revierte de inmediato a simulación.
  5. **Honestidad sin sensores físicos:** las respuestas diferencian explícitamente simulación (`MODO DEMO`) de confirmación PWM del controlador ESP32 (`pwm-apertura-aplicada` / `pwm-completado-sin-sensor`, requiriendo observación presencial).

---

## 5. Compilación y Pruebas Automatizadas Verificadas

| Suite / Comando | Resultado | Detalle |
|---|---|---|
| `dotnet build EcoAccess.P3.sln -c Debug` | **APROBADA** | **0 errores** (9.13 s) |
| `dotnet build EcoAccess.P3.sln -c Release` | **APROBADA** | **0 errores** (10.67 s) |
| `dotnet test EcoAccess.P3.Tests` (`Debug`) | **APROBADA** | **2.404 superadas, 0 fallidas, 1 omitida** (prueba de enlace simbólico en Windows sin privilegios elevados), total 2.405 (8 s) |
| `PruebasCoordinadorYTelegramComandosTests` | **APROBADA** | 7 combinaciones, rechazo no autorizado, bloqueo sin sesión física, serialización PC+Telegram (concurrencia máx. = 1), parada inmediata |
| `ServicioConversacionTelegramTests.CanecasUnificadas` | **APROBADA** | `/modo`, `/canecas`, `/historial`, las 7 combinaciones en `ServicioConversacionTelegram`, rechazo de chat no autorizado y formato inválido |
| `EcoAccess.P3.Bot.exe --probar-estacion-fisica verde blanca negra --simulado` | **APROBADA** | Exit 0 · `C07_VerdeBlancaNegra` · `Simulacion` · 22 ms · VERDE (1400), BLANCA (1093) y NEGRA (1401) `Completada` |
| `EcoAccess.P3.Bot.exe --probar-estacion-fisica verde` (sin flag de presencia) | **BLOQUEADA CORRECTAMENTE** | Exit 1 · `SEGURIDAD: Se requiere --confirmar-presencia-supervisada...` · 0 tráfico serial |
| Auditoría visual real previa (41 fotos reales) | **VERIFICADA** | 41/41 procesadas (87,5 % individuales exactas, 78,0 % global, 5,99 s promedio con `lfm3-vl:latest`); Bug #4 (`charging cable box` → BLANCA, `USB cable` / `medicamentos` → `GESTION_ESPECIAL`) verificado |
| Oracle aislado (`ECOACCESS_P3_PRUEBAS@localhost:1521/freepdb1`) | **VERIFICADO EN FASE PREVIA** | 247/254 pruebas `EcoAccess.P3.Datos.Tests` aprobadas + migración `V028` y reversión `V028_reversion.sql` validadas sin tocar producción |

---

## 6. Matriz de Validación Física Pendiente (Requiere Confirmación Presencial)

Las siguientes 7 pruebas están listas para ejecutarse secuencialmente con `EcoAccess.P3.Bot.exe --probar-estacion-fisica <combinacion> --confirmar-presencia-supervisada` **únicamente después de que Jorge confirme presencia física y despeje mecánico**:

| # | Prueba | Combinación | Canecas / IDs / Pines ESP32 | Estado actual |
|---|---|---|---|---|
| 1 | Individual 1 | `C01_Verde` | Verde (`1400`, pin 23) | **PENDIENTE CONFIRMACIÓN PRESENCIAL** |
| 2 | Individual 2 | `C02_Blanca` | Blanca (`1093`, pin 19) | **PENDIENTE CONFIRMACIÓN PRESENCIAL** |
| 3 | Individual 3 | `C03_Negra` | Negra (`1401`, pin 18) | **PENDIENTE CONFIRMACIÓN PRESENCIAL** |
| 4 | Combinada 2A | `C04_VerdeBlanca` | Verde (`1400`) → Blanca (`1093`) | **PENDIENTE CONFIRMACIÓN PRESENCIAL** |
| 5 | Combinada 2B | `C05_VerdeNegra` | Verde (`1400`) → Negra (`1401`) | **PENDIENTE CONFIRMACIÓN PRESENCIAL** |
| 6 | Combinada 2C | `C06_BlancaNegra` | Blanca (`1093`) → Negra (`1401`) | **PENDIENTE CONFIRMACIÓN PRESENCIAL** |
| 7 | Combinada 3 | `C07_VerdeBlancaNegra` | Verde (`1400`) → Blanca (`1093`) → Negra (`1401`) | **PENDIENTE CONFIRMACIÓN PRESENCIAL** |

---

## 7. Limitaciones Honestas y Reglas de Seguridad Mantenidas

1. **Sin sensores físicos de apertura/cierre en las tapas:** el ESP32 devuelve recibos autenticados HMAC de aplicación PWM (`pwm-apertura-aplicada` y `pwm-completado-sin-sensor`); el sistema nunca afirma verificación por sensor inexistente.
2. **Apertura secuencial segura sobre `COM3`:** los tres puentes comparten un único puerto serial; las aperturas de 2 y 3 canecas se ejecutan una tras otra con `SemaphoreSlim(1,1)` para evitar colisiones USB y picos simultáneos de corriente en los servos.
3. **Procesos heredados en memoria:** existen 7 procesos antiguos de `EcoAccess.P3.Bot.exe` (incluyendo PID `39920` del clon universitario) que deben detenerse con `Iniciar-Modo-ExpoSoftware.ps1 -DetenerTodo` antes de iniciar la estación física oficial.
4. **Cero cambios en producción Oracle Cloud y cero credenciales expuestas:** ninguna clave HMAC, token de Telegram, contraseña ni wallet Oracle fue impreso ni versionado en Git.
5. **PR #23 mantenido en borrador (sin fusión automática):** los cambios quedan publicados en la rama `feat/ecoaccess-vision-integracion-final` listos para revisión final de Jorge Gutiérrez.

# Guía completa del bot Eco (Telegram) — recuperación con Oracle Cloud

Bot **@EcoAccessP3_JG_bot** · proyecto `src/EcoAccess.P3.Bot` (.NET Framework 4.8) · base Oracle Cloud (`ECOACCESS_P3_APP`) · visión LFM3-VL local. Documentación funcional previa del proyecto: [FLUJO_ECO_DEFINITIVO.md](FLUJO_ECO_DEFINITIVO.md), [GUIA_TELEGRAM_DOBLE_IA.md](GUIA_TELEGRAM_DOBLE_IA.md), [VINCULACION_HTTPS.md](VINCULACION_HTTPS.md), [DIAGNOSTICO_TELEGRAM.md](DIAGNOSTICO_TELEGRAM.md).

## 1. Arquitectura

```
Telegram ──polling (getUpdates) o webhook HTTPS──► EcoAccess.P3.Bot (raíz de composición: Program.cs)
   ProcesadorUpdates → ServicioConversacionTelegram (Lógica: estados, intenciones, reglas)
        ├─ Repositorios Oracle (Datos): conversación con versión optimista, UPDATE_TELEGRAM, vínculos, interacciones
        ├─ Visión: SelectorVision → ClasificadorOllamaLocal (LFM3-VL) | Plan B (selección manual)
        ├─ ServicioTresCanecas (destino por material) y ServicioDespachoOrdenes (orden firmada)
        └─ Bridge / ESP32 (solo con dispositivo configurado)
```

* Cada update se registra en `UPDATE_TELEGRAM`: un mensaje repetido no se procesa dos veces.
* El estado del diálogo vive en `CONVERSACION_TELEGRAM` con columna `version`: sobrevive a reinicios y no se pisa con mensajes simultáneos.
* Las fotos se atienden una a la vez en la GPU; el bot sigue recibiendo mensajes mientras tanto.
* Un bloqueo local impide dos consumidores en el mismo equipo; con webhook activo el polling no arranca.

## 2. Configuración (User Secrets `ecoaccess-p3-framework`; nunca en Git)

| Clave | Estado en este equipo |
|---|---|
| `Telegram:TokenBot`, `Telegram:NombreBot` | recuperados con BotFather y verificados con `getMe` |
| `ConnectionStrings:OracleP3` | Oracle Cloud con wallet |
| `Telegram:QrDemo` | QR público DEMO `DEMO_qr_ficticio_0001` |
| `Vision:Modo=Local`, `Vision:Proveedor=OllamaLocal`, `Vision:Ollama:ServidorSinNubeVerificado=true` | configurados |
| `Telegram:PaginaRegistro` | **sin configurar**: `/vincular` informa que el registro no está disponible (ver §5) |
| `Vision:Groq:*` | sin configurar a propósito (servicio de pago; presupuesto 0) |

Recuperar o rotar el token: @BotFather → `/mybots` → @EcoAccessP3_JG_bot → **API Token** (o **Revoke current token**). Guardarlo sin mostrarlo:

```bash
pwsh -NoProfile -STA -File scripts\Configurar-TelegramLocal.ps1
```

(la ventana llama a `getMe` y guarda en `ecoaccess-p3` solo si Telegram confirma el bot; para este repositorio copiar las claves a `ecoaccess-p3-framework` o usar `scripts/instalacion/Configurar-Telegram.ps1`).

## 3. Ejecutar

```bash
src\EcoAccess.P3.Bot\bin\Release\net48\EcoAccess.P3.Bot.exe --comprobar-telegram
```

```bash
src\EcoAccess.P3.Bot\bin\Release\net48\EcoAccess.P3.Bot.exe --comprobar-oracle
```

```bash
src\EcoAccess.P3.Bot\bin\Release\net48\EcoAccess.P3.Bot.exe --polling
```

Otros modos: `--simulado` (chat de consola), `--webhook`, `--bridge`, `--dispositivo-serial`, `--comprobar-vision-local <foto>`, `--estado-vision`, `--supervisar`. **Un solo consumidor**: no ejecutar polling en dos equipos con el mismo token.

## 4. Pruebas de esta recuperación (2026-11-09)

| Prueba | Tipo | Resultado |
|---|---|---|
| `--comprobar-telegram` | servicio real | @EcoAccessP3_JG_bot, sin webhook, 0 pendientes |
| `--comprobar-oracle` | servicio real | 40 materiales del catálogo en Oracle Cloud |
| `--comprobar-vision-local glass12.jpg` | servicio real (modelo local) | `VIDRIO` en 3,6 s; admisión consultada en Oracle |
| `--polling` | servicio real | identidad comprobada, consumidor exclusivo, en marcha |
| Conversación desde la cuenta real del propietario | **servicio real, cuenta real** | verificada con el bot de la línea anterior sobre la misma base: 6 mensajes y botones sin errores, atención 104–239 ms, envío 297–646 ms. **Con el bot de EcoAccess-P3 aún no se envió un mensaje desde el teléfono** |
| Flujo, intenciones, vinculación, invitados, visión simulada | unitarias/simuladas | dentro de las 1 638 de `EcoAccess.P3.Tests` |
| Bridge, invitado por Telegram, trazas de visión con Oracle | integración | dentro de las 225 contra Oracle Cloud |

## 5. Pendiente para no depender del computador personal

1. **Página HTTPS de registro y vinculación** (`Telegram:PaginaRegistro`, `scripts/instalacion/Publicar-Registro-HTTPS.ps1`).
2. **Despliegue gratuito**: el bot es .NET Framework 4.8 (Windows). Para una VM Linux Always Free de Oracle Cloud habría que migrar el bot a .NET moderno (la línea EcoAccess_P3 muestra que el código compila en .NET 10) o usar una VM Windows (no Always Free). Decisión documentada, no ejecutada.
3. **Visión centralizada**: una VM sin GPU no garantiza los 3,2 s medidos; medir antes. El Plan B cubre la ausencia del modelo.

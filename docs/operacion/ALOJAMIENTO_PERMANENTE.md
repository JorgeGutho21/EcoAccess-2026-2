# Alojamiento permanente del bot de EcoAccess P3 (10/11/2026)

Objetivo: que `@EcoAccessP3_JG_bot` responda sin depender de que alguien abra una consola. **No se contrató ni se desplegó
nada en la nube**; esta es la recomendación con sus costos y riesgos, más lo que ya quedó listo en el PC.

## Qué necesita el bot

| Pieza | Hoy | Requisito para moverla |
|---|---|---|
| Bot (`EcoAccess.P3.Bot.exe --polling`) | .NET Framework 4.8, solo Windows | Windows, o portar Bot, Lógica, Datos y Entidades a .NET moderno |
| Oracle Autonomous Database | Ya está en la nube (wallet mTLS) | Nada: cualquier equipo con el wallet se conecta |
| Telegram | Long polling: solo conexiones **salientes** | Nada: no abre puertos |
| Visión (Ollama + LFM3-VL) | Local, en el PC (p50 ≈ 3,3 s) | CPU/GPU del servidor; sin él, Eco pide la categoría |
| Página de registro (`/registro`, puerto 8081) | Tailscale Funnel desde el PC | El mismo Funnel en el servidor nuevo |
| ESP32 | Fase futura, USB al PC de la estación | El bridge **siempre** queda en el PC junto a la caneca |

## Opciones comparadas

| Opción | Costo | 24/7 real | Trabajo | Riesgo principal |
|---|---|---|---|---|
| **A. PC del propietario + vigilante** (listo) | 0 | No: depende de que el PC esté encendido, despierto y con sesión | Ninguno (scripts hechos) | Suspensión del PC o corte de red |
| **B. VM Always Free de Oracle (Ampere A1, Linux) + bot portado a .NET 8/10** (recomendada) | 0 en recursos Always Free | Sí | Medio: portar 4 proyectos y empaquetar | Oracle puede **recuperar** instancias Always Free ociosas |
| C. VM Windows en Oracle o Azure | De pago (Windows no es Always Free; la licencia se cobra aparte) | Sí | Bajo | Costo mensual: requiere autorización |
| D. Mono en Linux con el .exe actual | 0 | Sí | Bajo, pero incierto | ODP.NET, SkiaSharp nativo y TLS no están validados en Mono: no recomendado |

### Por qué B

- Oracle ya aloja la base de datos en la misma cuenta, así que no hace falta otro proveedor.
- Los recursos Always Free incluyen VMs Ampere A1 (ARM). La asignación vigente se confirma en la consola, porque Oracle
  la ha cambiado.
- El bot solo hace conexiones salientes a Telegram y a Oracle, así que la VM no necesita puertos públicos. La página de
  registro se publica con Tailscale Funnel, igual que hoy.
- **Riesgo de recuperación.** Según la documentación de Oracle, una instancia Always Free se considera ociosa si en 7 días
  el percentil 95 de CPU, de red y (en A1) de memoria queda por debajo del 20 %. En ese caso Oracle puede recuperarla. Un
  bot con pocos usuarios puede caer en ese umbral. Mitigación legítima: pasar la cuenta a *Pay As You Go*, que conserva
  gratis los recursos Always Free. Es una decisión de facturación del propietario y exige tarjeta. **No** se usan scripts
  que generen carga artificial.

### Qué hay que portar (opción B)

Las cuatro capas usan `net48` desde `Directory.Build.props`. El cambio sería compilar Entidades, Lógica, Datos y Bot
también para `net8.0` (multi-target), manteniendo `net48` para WinForms:

1. Datos: `Oracle.ManagedDataAccess` → `Oracle.ManagedDataAccess.Core` en `net8.0` (mismo API y mismo wallet).
2. Bot: `SkiaSharp.NativeAssets.Win32` → agregar `SkiaSharp.NativeAssets.Linux` para el destino Linux.
3. Revisar lo que solo existe en Windows (`--supervisar-en-segundo-plano`, rutas `C:\ProgramData`, puerto serie: el
   bridge se queda en Windows).
4. Ejecutar las 1649 pruebas unitarias y las de integración en ambos destinos antes de mover nada.
5. En la VM: servicio `systemd` con reinicio automático, secretos en variables de entorno `ECOACCESS_` con permisos solo
   del usuario del servicio, wallet copiado con permisos 600, y **detener el bot del PC** para que haya un solo consumidor.

Visión en la VM: LFM3-VL en CPU ARM no se ha medido. Si es lento, se deja `Vision:Modo` en el PC del propietario
(Ollama no se expone a Internet) y el bot de la VM pide la categoría cuando no hay motor. Hay que medirlo antes de
decidir.

## Lo que ya quedó listo en el PC (opción A)

- `scripts/desarrollo/Vigilar-Bot.ps1`: si el bot de esta copia no corre, lo inicia con `Iniciar-EcoAccess-Nube.ps1`.
  - Si corre el bot de otra copia (V2), no lo detiene ni compite por el token.
  - Respeta el archivo de pausa `C:\ProgramData\EcoAccessP3Framework\Desarrollo\pausa-vigilante`.
- `scripts/desarrollo/Instalar-Arranque-Automatico.ps1`: registra una tarea programada del usuario, sin administrador,
  que ejecuta el vigilante al iniciar sesión y cada 5 minutos. Con `-Quitar` la borra. **Instalada el 10/11/2026 a
  pedido del propietario.** Se probó así:
  - Con el bot en marcha, la tarea terminó con resultado 0 y no inició nada.
  - Con el bot detenido a propósito, la tarea lo relanzó sola y estaba en polling a los 11 s, con un solo proceso y la
    página pública respondiendo HTTP 400 sin token.
- Tailscale Funnel ya queda configurado en segundo plano y se mantiene tras reiniciar mientras Tailscale esté activo.

**Prueba real del 10/11/2026.** Con el bot en marcha, el vigilante no hizo nada (correcto). Luego se detuvo el bot a
propósito y se ejecutó el vigilante:

- La comprobación de Oracle tardó **20 minutos** (05:51 → 06:11) y Telegram no respondió, así que el arranque falló.
- Un minuto después Telegram respondía en 2 s. Lo más probable es que el PC se suspendiera o perdiera la red.

Esto confirma el límite de la opción A: **sin evitar la suspensión del PC no hay disponibilidad continua**. La
configuración de energía la cambia el propietario (Configuración → Sistema → Energía → «Suspender» en «Nunca» cuando
está enchufado). Tras esa prueba, el vigilante ya no espera la salida del lanzador por tubería: un bot recién iniciado
heredaba la tubería y lo dejaba bloqueado. Si el inicio falla, el siguiente ciclo lo reintenta.

Segundo fallo real, del mismo día: el vigilante y el lanzador contaban como «otro bot» cualquier `EcoAccess.P3.Bot.exe`,
incluida una evaluación de visión. Por eso el bot quedó caído unos 5 minutos. Ahora solo cuentan los procesos en
`--polling` o `--webhook`. Prueba: con una evaluación en marcha, el vigilante relanzó el bot.

## Fuentes

- Oracle, *Always Free Resources* (límites y reglas de recuperación por inactividad): https://docs.oracle.com/en-us/iaas/Content/FreeTier/resourceref.htm
- Oracle, *Microsoft Licensing on OCI* (la licencia de Windows Server se cobra aparte del cómputo): https://docs.oracle.com/en-us/iaas/Content/Compute/References/microsoftlicensing.htm

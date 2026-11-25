# ECOACCESS P3 · Modo ExpoSoftware (versión definitiva)

ECOACCESS: diseño e integración de un prototipo inteligente para orientar y registrar la disposición de residuos
aprovechables en Valledupar. Integrantes: Jorge Gutiérrez Thomas, Heyder Canchila De Angel y Saray Cadena Palomino.

Este documento describe lo que funciona en la demostración, cómo se inicia, qué se midió y qué falta. Todo lo que
dice «verificado» se comprobó con el comando indicado; lo físico se marca como pendiente hasta observarlo.

## 1. Arquitectura (un solo dueño del ESP32)

```
PC (FormEstacionExpo) ─┐
Telegram (@EcoAccessP3_JG_bot) ─┼─► ServicioEstacionExpo ─► Panel local :8090 ─► ESP32 (COM3) ─► 3 servos
Web de control (:8090) ─┘        │  (política 5/7/10 s,     (único dueño     firmware CalibracionTresCanecas v2
                                 │   idempotencia)           del puerto)
                                 └─► OPERACION_ESTACION (Oracle Cloud) ◄─ cola local si Oracle no responde
Foto ─► ReconocedorExpo9 (Ollama local, lfm3-vl, 1 inferencia, 512 px) ─► reglas C# ─► canecas
```

- El **panel** (`src/EcoAccess.P3.PanelCanecas`) es el único proceso que abre el puerto del ESP32. El bot y el PC le
  piden las aperturas por HTTP local con la clave de la estación (`%ProgramData%\EcoAccessP3Framework\Panel\clave.txt`).
- Nada se mueve sin **ACTIVAR CONTROL** en el panel (sesión supervisada de 2 h, solo desde la estación anfitriona).
  **DETENER TODO** cancela, cierra y desactiva el control.
- El firmware guarda la calibración de cada tapa (NVS): verde 0→50°, blanca 0→70°, negra 0→40° (valores vigentes el
  25/11/2026, conservados al cargar la v2). Al encender no mueve ningún motor.
- Respaldo y restauración del firmware: `Documents\01_UNIVERSIDAD\2026-2\_Respaldos_EcoAccess\Firmware_ESP32_2026-11-24\`
  (`calibracion_v1\RESTAURAR.txt` y el volcado completo del firmware HMAC oficial).

## 2. Nueve materiales (Modo ExpoSoftware)

| Código | Material | Caneca | Código del catálogo de 35 | Material Oracle |
|---|---|---|---|---|
| E01 | Media manzana | VERDE | — (no existía) | ORGANICO |
| E02 | Cáscara de banano | VERDE | V01 | ORGANICO |
| E03 | Papa o media papa | VERDE | V03 | ORGANICO |
| E04 | Bolsa de papel kraft o cartón arrugado, limpio y seco | BLANCA | B09 | CARTON |
| E05 | Botella plástica pequeña, vacía y limpia | BLANCA | B01 | PET |
| E06 | Frasco pequeño de vidrio, vacío y limpio | BLANCA | B06 | VIDRIO |
| E07 | Papel higiénico usado (muestra higiénica) | NEGRA | N02 | SERVILLETA_USADA |
| E08 | Servilleta usada o contaminada | NEGRA | N01 | SERVILLETA_USADA |
| E09 | Envoltura de mecato metalizada | NEGRA | N08 | PLASTICO_SIN_ID |

El modelo nombra libremente lo que ve en una sola inferencia; `CatalogoExpo9` (C#) lo traduce. Reglas de seguridad:
- Cables, pilas, medicamentos, cuchillas, vidrio roto, personas → **gestión especial: no se abre ninguna caneca**.
- Papel o «tissue» blanco sin señal de uso y papel aluminio → **por confirmar** (un toque: «Es servilleta o papel
  higiénico usado»). Se midió que una bola de papel cuadriculado y una bolsa plástica se leían como «tissue paper».
- Varios residuos de una misma categoría abren esa caneca **una sola vez**; el orden en la foto no importa.
- El catálogo completo de 35 materiales sigue disponible (Mayús + clic en «Reconocer foto» abre la pantalla avanzada).

## 3. Tiempos y aperturas

| Canecas distintas | Tiempo abierta | Modalidad predeterminada |
|---|---|---|
| 1 | 5 s | — |
| 2 | 7 s | simultánea (cada motor arranca 0,25 s después del anterior) |
| 3 | 10 s | simultánea (0,25 s de escalón) |

El temporizador corre en el firmware (orden `GRUPO`), no en el navegador. Modalidad secuencial (una tapa tras otra, cada
una con el tiempo del grupo) con `ECOACCESS_APERTURA_MODALIDAD=secuencial` o el selector del panel. Las pruebas manuales
de calibración usan el tiempo configurable de cada tapa.

## 4. Inicio

Estación anfitriona (equipo con el ESP32), acceso directo **ECOACCESS P3** o:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\desarrollo\Iniciar-EstacionExpo.ps1 -AbrirPanel
```

Inicia Ollama (si hace falta), el panel `http://localhost:8090` y **un solo** bot en polling (no crea un segundo
consumidor si ya hay uno). Acceso directo **ECOACCESS — Control de Canecas**: solo el panel. Ninguno mueve motores.

Cuentas de Telegram que pueden abrir sin escanear el QR: un ID numérico por línea en
`%ProgramData%\EcoAccessP3Framework\Bridge\telegram-autorizados.txt` (lo crea el supervisor; no se versiona).

## 5. Telegram

Menú: Identificar residuo · Abrir caneca · Mi historial · Mis ecopuntos · Estado de la estación · Escanear estación ·
Mi cuenta · Ayuda. Comandos: `/verde`, `/blanca`, `/negra`, `/abrir verde blanca`, `/abrir verde negra`,
`/abrir blanca negra`, `/abrir verde blanca negra`, `/canecas`, `/modo`, `/historial`.

Flujo de foto: aviso inmediato «📸 Fotografía recibida. Identificando residuos…» → resultado (materiales, categorías,
canecas, tiempo) → apertura si el chat está autorizado o escaneó el QR y el control está activo → al cerrar,
«Ciclo terminado» con el estado del registro en Oracle y el botón «Sí, deposité» (disposición **declarada**; las tapas
no tienen sensores). Las pruebas técnicas y aperturas no otorgan ecopuntos.

## 6. Oracle Cloud

- Migración **V030** `OPERACION_ESTACION` + vista `EA_R_OPERACIONES_ESTACION` (reversión `reversion/R030`).
  Reemplaza la V028 multirresiduo propuesta, que nunca se aplicó en producción (verificado: producción en V029).
- Estados: RECIBIDA, CLASIFICADA, PENDIENTE_APERTURA, EN_EJECUCION, CICLO_COMPLETADO, DISPOSICION_CONFIRMADA,
  REGISTRADA, ERROR, PENDIENTE_SINCRONIZACION. MERGE por clave de idempotencia (`tg:<update>`, `pc:<guid>`, `web:<clave>`):
  una actualización repetida de Telegram o un doble toque no duplica ni la apertura ni la fila.
- Sin Oracle: la operación queda en `%ProgramData%\EcoAccessP3Framework\Estacion\operaciones-pendientes.jsonl` y el bot
  la sube cada 30 s cuando vuelve la conexión (sin repetir la apertura). Las aperturas de la web se suben con canal WEB.
- **Aplicada en producción el 25/11/2026** con autorización, seguida de `05_conceder_permisos_roles.sql` (la tabla queda
  de solo lectura para los roles). Evidencia: `docs/database/evidencias/v030_produccion_20261125.md`.

## 7. Equipos de los compañeros

`scripts\instalacion\Instalar-EcoAccess.ps1 -Perfil EstacionCompanero -Wallet <Wallet.zip>`: aplicación + Oracle
(wallet), reconocimiento visual si tienen Ollama, historial compartido. Sin ESP32, la pantalla «Estación de tres canecas»
trabaja en **simulación declarada**. No deben iniciar otro bot (un solo polling por token).
Control remoto de la estación anfitriona solo en la misma red local: en el anfitrión, como administrador,
`netsh http add urlacl url=http://+:8090/ user=<usuario>` y una regla de firewall para el puerto 8090; en el equipo del
compañero `ECOACCESS_PANEL_URL=http://<ip-anfitrión>:8090` y la clave de la estación. Nunca se expone a Internet.

## 8. Página web

- Panel real: `http://localhost:8090` (código en `src/EcoAccess.P3.PanelCanecas`).
- Demostración pública: `web/panel-demo` (mismo HTML + `demo.js`, estación simulada en el navegador, aviso visible).
  Se regenera con `python scripts/web/generar_panel_demo.py`. Publicación: `.github/workflows/pages.yml`.
  **GitHub Pages no está habilitado**: el repositorio es privado y el plan actual no lo permite (HTTP 422). Para
  publicarla: hacer el repositorio público o usar un plan que lo permita, y crear la variable `PAGES_HABILITADO=true`.

## 9. Mediciones (sin hardware)

- Conjunto activo `datasets/exposoftware9`: **177 imágenes** (límite 200): 90 referencias (20 originales + 70 aumentadas
  etiquetadas como tales), 22 de evaluación independiente, 50 multirresiduo (4 reales + 46 compuestas), 15 negativas.
  Faltan fotografías reales de banano, botella, frasco de vidrio, papel higiénico y envoltura (`manifiesto.json` → `faltantes`).
- Evaluación (`EcoAccess.P3.Bot.exe --evaluar-expo9`, informes en `docs/informes/expo9/`), 84 casos independientes:

| Lado | Canecas correctas | Individual | Multirresiduo | Negativas sin apertura | Falsas aperturas | Inferencia mediana / P95 |
|---|---|---|---|---|---|---|
| 384 px | 56 % | 68 % | 39 % | 13/13 | 0 | 1,48 s / 2,12 s |
| **512 px (elegido)** | **63 %** | **73 %** | **49 %** | **13/13** | **0** | **1,69 s / 2,56 s** |
| 640 px | 64 % | 73 % | 51 % | 13/13 | 0 | 2,05 s / 3,21 s |

- Latencia antes/después en las mismas 22 fotos (`--comparar-latencia-expo9`): recorrido anterior de Telegram
  mediana **7,3 s** (P95 13,9 s) → una inferencia mediana **2,1 s** (P95 2,7 s). Carga en frío del modelo: ~14 s
  (por eso el bot y la app lo precalientan al iniciar y lo mantienen en memoria).
- Los errores restantes son omisiones (casi siempre escenas compuestas de 3–4 ilustraciones pequeñas y la manzana
  verde entera, que el modelo llama «lime»), no aperturas indebidas. Para la demostración: usar media manzana roja o
  cortada y separar los residuos en la foto.

## 10. Pruebas físicas pendientes (una sesión presencial supervisada)

Para cada canal (PC manual, PC foto, Telegram manual, Telegram foto, panel web): C01 verde 5 s, C02 blanca 5 s,
C03 negra 5 s, C04 verde+blanca 7 s, C05 verde+negra 7 s, C06 blanca+negra 7 s, C07 las tres 10 s.
Anotar: caneca correcta, ángulo, tiempo, apertura y cierre **observados**, fila en Oracle y consulta posterior.
El panel tiene «Prueba C01–C07 (5/7/10 s)» en Diagnóstico para la parte web.

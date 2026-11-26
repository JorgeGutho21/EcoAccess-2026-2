# EcoAccess Challenge

Juego oficial de EcoAccess para ExpoSoftware 2026 (Universidad Popular del Cesar). Siete preguntas (o diez en la ronda de diez), 100 puntos por acierto,
desempate por el tiempo medido en el servidor y premio para los tres primeros lugares.

| Página | URL |
|---|---|
| Juego (QR) | https://jorgegutho21.github.io/EcoAccess-2026-2/juego/ |
| Clasificación pública | https://jorgegutho21.github.io/EcoAccess-2026-2/juego/clasificacion/ |
| Vista para proyector | https://jorgegutho21.github.io/EcoAccess-2026-2/juego/clasificacion/?proyector |
| Panel administrativo | https://jorgegutho21.github.io/EcoAccess-2026-2/juego/admin/ (exige la clave) |

## Arquitectura

```
Celular / PC (GitHub Pages, HTTPS)  ──►  Oracle Autonomous Database (Bogotá) · ORDS  /ords/ecoaccess/juego/v1/
   HTML + CSS + JS sin dependencias          PKG_ECO_JUEGO (PL/SQL) ──► tablas ECO_JUEGO_* (V031, V032)
```

- **Sin servidor intermedio**: la base de datos publica sus propios servicios REST (ORDS), con URL HTTPS fija. Funciona
  desde cualquier red (Wi-Fi del evento o datos móviles) y no depende de que un portátil esté encendido.
- **Todo lo decide Oracle**: puntaje, aciertos y tiempo (SYSTIMESTAMP de inicio y fin). El navegador nunca recibe qué
  opción es correcta mientras el concurso está activo; la revisión educativa solo existe con la competencia FINALIZADA.
- **Sesión de juego**: secreto aleatorio de 256 bits generado por el navegador (`crypto.getRandomValues`); Oracle guarda solo
  su SHA-256. Recargar la página reanuda el mismo intento y el mismo cronómetro. Un participante = un intento.
- **Separado del ESP32**: el juego no comparte rutas, tablas ni procesos con la estación física. Responder nunca abre
  canecas ni registra disposiciones de residuos. Los participantes no se mezclan con usuarios ni invitados de EcoAccess.
- **Datos personales**: el nombre completo solo lo ve el panel administrativo; la clasificación pública muestra el alias.
- **Clave de respuestas** y **clave de administración**: fuera del repositorio (público), en
  `%LOCALAPPDATA%\EcoAccessP3\juego\` del equipo anfitrión. En Oracle la clave de administración se guarda como SHA-256.

### Servicios REST (`/ords/ecoaccess/juego/v1/`)
| Método | Ruta | Uso |
|---|---|---|
| GET | `estado` | Competencia vigente y si el registro está abierto |
| POST | `registro` | Registra al participante y crea su intento (`nombre`, `alias`, `acepta`, `sesion`) |
| POST | `intento/inicio` | Inicia el cronómetro oficial (una sola vez) |
| GET | `intento` | Estado del intento y pregunta actual (reanudación) |
| POST | `intento/respuesta` | Confirma una respuesta (`pregunta`, `opcion`); inmutable |
| GET | `intento/revision` | Respuestas correctas, solo con el concurso finalizado |
| GET | `clasificacion` | Cinco primeros, provisional o definitiva |
| POST | `admin/{competencias,crear,abrir,cerrar_registro,finalizar,participantes,eliminar_prueba}` | Panel (encabezado `X-Eco-Admin`) |
| GET | `admin/exportacion?competencia=N` | CSV con todos los resultados |

CORS limitado a `https://jorgegutho21.github.io` y al servidor local de desarrollo `localhost:5173`.

## Cómo usarlo en la exposición
1. Abre el panel administrativo, escribe la clave y comprueba que la ronda oficial esté **Abierta**. Para empezar de cero
   el día del evento, pulsa **Crear nueva ronda oficial** y luego **Abrir** (el historial anterior se conserva).
2. Proyecta `…/juego/clasificacion/?proyector` y entrega el QR de `…/juego/`.
3. Para terminar: **Cerrar registro** (quien ya se registró puede terminar) y después **Finalizar concurso**. La
   clasificación pasa a «Resultados definitivos» y se habilita la revisión de respuestas.
4. **Ver participantes** muestra nombre completo, alias, respuestas y los tres ganadores; **Exportar** descarga el CSV.

## Rondas de 7 y de 10 preguntas (V034 y V035)
- **V034** crea los bancos de preguntas (`ECO_JUEGO_BANCOS`, `ECO_JUEGO_BANCO_PREGUNTAS`) y la columna
  `ECO_JUEGO_COMPETENCIAS.BANCO_ID`. Nulo = banco 1 «CLASICO-7» (las 7 preguntas de V031): la ronda oficial abierta no cambia.
  El banco 2 «EXPO2026-10» tiene 10 preguntas nuevas (propósito de EcoAccess, residuos, ESP32, servomotores, SOLID, grafos,
  Oracle Cloud, Wallet, arquitectura web y política 7/14/21 s), cada una con su explicación. Deja preparada la competencia
  oficial `RONDA-10-EXPO2026` (PREPARADA). ES_CORRECTA queda en 'N'; la clave va en
  `%LOCALAPPDATA%\EcoAccessP3\juego\cargar-clave-respuestas-ronda10.sql` (privado).
- **V035** reemplaza solo el paquete: el total (7 o 10) y el orden salen del banco. Rutas, JSON, 100 puntos por acierto,
  desempate, cronómetro, idempotencia, SHA-256 de la sesión e inmutabilidad no cambian; el módulo ORDS no se redefine.
  Panel: «crear» acepta `{"banco":"EXPO2026-10"}`.
- Reversión: `reversion/R035…` (paquete exacto de V032) y luego `reversion/R034…` (se detiene si una ronda oficial de 10 ya
  tiene participantes).

### Aplicar en producción (pendiente; no aplicado)
1. Hacerlo sin nadie jugando (la ronda oficial abierta tiene 0 participantes a la fecha). V034 cambia tablas y Oracle
   recompila el paquete en la primera llamada siguiente.
2. Con sqlplus y el Wallet, como `ECOACCESS_P3_APP`: `@database/06_migraciones/V034__juego_bancos_ronda10.sql` y, enseguida,
   `@database/06_migraciones/V035__juego_paquete_bancos.sql` (o `EcoAccess.P3.Instalador`, que aplica V034 y V035 en orden).
   Entre ambas, el paquete de V032 mostraría también las 10 preguntas nuevas en la revisión de rondas finalizadas.
3. Comprobar: `PKG_ECO_JUEGO` VALID (especificación y cuerpo); `RONDA-261125155629-UIE8ZY` sigue ABIERTA con `BANCO_ID` nulo;
   `GET estado` responde `preguntas: 7` y `puntajeMaximo: 700`; `node --test web/juego/pruebas/api.test.mjs` 8/8.
4. Cargar la clave privada: `@%LOCALAPPDATA%\EcoAccessP3\juego\cargar-clave-respuestas-ronda10.sql` (verifica 10 de 10).
5. Opcional: `node web/juego/pruebas/carga.mjs 10 300-1500 0 EXPO2026-10` (ronda de PRUEBA de 10 que se elimina).
6. Publicar la página (el cliente funciona igual con V032: si falta un dato, usa 7).
7. Para jugar la de 10: panel → **Finalizar** la ronda oficial actual → **Abrir** `RONDA-10-EXPO2026`.

## Límite real de la infraestructura y comportamiento del cliente
ORDS responde HTTP 429 (página HTML, **sin** `Retry-After` y **sin** cabeceras CORS: el navegador lo ve como error de red)
cuando decenas de personas llegan en el mismo segundo. El cliente:
- reintenta 429/5xx/red caída hasta 8 veces con espera exponencial y jitter (700 ms × 2ⁿ, tope 12 s, máx. 90 s en total),
  respetando `Retry-After` si algún día llega; un 429 con JSON del juego (límite de registros) no se reintenta;
- muestra «Muchos jugadores a la vez, reintentando… Tu progreso está guardado» mientras tanto; todas las operaciones son
  idempotentes, nada se duplica;
- espera al azar 0–1,2 s antes de registrar y de iniciar (el cronómetro arranca en el servidor, después);
- la clasificación consulta cada 10 s (5 s en el proyector), alarga el intervalo ×1,5 sin cambios (hasta 30/20 s), lo
  duplica ante 429 (hasta 2 min), se detiene con la pestaña oculta y con los resultados definitivos.
La política vive en `js/reintentos.js` y la usan igual el navegador y `pruebas/carga.mjs`. Mediciones reales del
25/11/2026 en rondas de prueba (7 preguntas; latencia por solicitud incluyendo reintentos):

| Carga | Completos | 429 de ORDS | Mediana | P95 | P99 |
|---|---|---|---|---|---|
| 10 simultáneos | 10/10 | 0 | 37 ms | 91 ms | 287 ms |
| 25 simultáneos | 25/25 | 33 | 36 ms | 680 ms | 4,1 s |
| 50 simultáneos | 50/50 | 255 | 36 ms | 8,5 s | 18,2 s |
| 100 simultáneos | 100/100 | 988 | 37 ms | 19,3 s | 35,1 s |
| 100 escalonados (400 ms) | 100/100 | 3 | 38 ms | 52 ms | 86 ms |

Con 100 en el mismo segundo nadie pierde su partida, pero una de cada veinte solicitudes tarda 19 s o más: el límite es
de conexiones de ORDS, no del paquete. Escalonar la llegada (QR por grupos) lo evita.

## Pruebas
```powershell
node --test web/juego/pruebas/api.test.mjs           # pruebas reales contra Oracle en una ronda de prueba que se elimina
node --test web/juego/pruebas/reintentos.test.mjs    # política de reintentos (sin red)
node web/juego/pruebas/carga.mjs 100                 # 100 participantes a la vez
node web/juego/pruebas/carga.mjs 100 3000-8000 400   # 100 participantes, llegada escalonada
```
Requieren las claves locales del equipo anfitrión. `scripts/web/preparar_imagenes_juego.py` regenera las imágenes a partir
de las fotografías originales (no las modifica).

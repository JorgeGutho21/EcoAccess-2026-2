# EcoAccess Challenge

Juego oficial de EcoAccess para ExpoSoftware 2026 (Universidad Popular del Cesar). Siete preguntas, 100 puntos por acierto,
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

## Límite real de la infraestructura
La base Autonomous responde rápido con el uso normal de una exposición (prueba con 100 participantes llegando en 40 s:
0 fallos, p95 de 60 ms). Si decenas de personas registran e inician **en el mismo segundo**, Oracle responde
temporalmente «429» o «Database Connection Error» por su límite de conexiones. El navegador reintenta con espera
exponencial durante un minuto y muestra «Reintentando…»; como todas las operaciones son idempotentes, nada se duplica ni
se pierde. Ver `docs/informes/juego/README.md`.

## Pruebas
```powershell
node --test web/juego/pruebas/api.test.mjs      # pruebas reales contra Oracle en una ronda de prueba que se elimina
node web/juego/pruebas/carga.mjs 100 3000-8000 400   # 100 participantes, llegada escalonada
```
Requieren las claves locales del equipo anfitrión. `scripts/web/preparar_imagenes_juego.py` regenera las imágenes a partir
de las fotografías originales (no las modifica).

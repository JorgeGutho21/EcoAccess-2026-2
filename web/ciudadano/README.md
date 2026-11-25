# EcoAccess, interfaz ciudadana

Página pública de una sola pantalla: la persona **escribe o dice** qué residuo va a desechar, EcoAccess identifica la
categoría, muestra la caneca correcta con una animación realista de la tapa y, si está frente a la estación (QR
presencial), abre la caneca física con una cuenta regresiva sincronizada.

- HTML, CSS y JavaScript sin dependencias ni compilación (módulos ES). Carga inicial ≈ 520 KB con imágenes WebP y fuentes.
- Diseño (`docs/diseno/SISTEMA_DISENO_ECOACCESS.md`): la página es la estación. Pared mineral `#EEF3EF`, un piso continuo
  `#E0E8E2` y las tres canecas reales de pie sobre él con una única sombra de contacto; Bricolage Grotesque en titulares
  y Atkinson Hyperlegible en el texto. En móvil la consulta vive en un dock inferior fijo y las tres canecas quedan en la
  primera pantalla; en escritorio la pregunta y la consola de consulta van a la izquierda y el escenario ocupa el resto.
- Tapa por capas en proyección ortográfica: la imagen de la tapa es un plano inclinado 54° y estirado 1/cos 54°, así que
  cerrada calza al píxel con el cuerpo; abrir es girar ese plano sobre la bisagra del borde trasero (pasa de canto, se
  levanta y muestra su cara interna y la boca). El cuerpo nunca se mueve.
- ECO muestra su estado con punto + palabra: Listo para escuchar, Escuchando (ondas), Interpretando, Residuo identificado,
  Caneca seleccionada, Operación completada, No comprendido, Necesito un detalle y Residuo especial.
- Responsive y verificado (sin desbordamiento horizontal) en 320×568, 360×800, 390×844, 430×932, 768×1024, 1024×768, 1366×768 y 1920×1080.
- Accesible: navegación por teclado (`/` enfoca el buscador, `Esc` vuelve al inicio), ARIA, estados con texto (no solo
  color), `prefers-reduced-motion` y respuestas habladas opcionales.

## Arquitectura

```
Página pública (GitHub Pages, HTTPS)
   │  consulta: 100 % en el navegador (motor determinista + catálogo JSON), sin red ni Ollama
   │  apertura física: solo con el QR presencial  →  #e=<pasarela>&t=<permiso temporal>
   ▼
Túnel HTTPS (cloudflared)  ──►  EcoAccess.P3.PasarelaCiudadana  127.0.0.1:8092  (solo /api/v1/…)
                                     │  operador: http://localhost:8093 (solo este equipo; nunca por el túnel)
                                     ▼
                     EcoAccess.P3.PanelCanecas 127.0.0.1:8090  ──►  ESP32 (COM3)  ──►  servos
                     (único dueño del puerto serie; sesión supervisada; calibraciones)
```

| Modo | Cuándo | Qué pasa |
|---|---|---|
| **Consulta** | Por defecto | Clasifica y muestra la caneca. Animación rotulada «vista ilustrativa». Se guarda como consulta educativa (sin ecopuntos). |
| **Físico** | Tras escanear el QR de la estación | Solicita la apertura; la tapa y la cuenta regresiva empiezan cuando la pasarela confirma la orden **comandada** y pasa el recorrido del servo. |
| **Demostración** | `?demo=1` | Igual que el físico, simulado y rotulado «sin movimiento físico». |

Sin sensores de posición, la interfaz distingue **orden comandada** de **movimiento comprobado**: nunca afirma que la tapa
se movió ni que el residuo se depositó; la persona puede confirmar su depósito con un toque.

### Tiempos
Con el panel y firmware v2 (`GRUPO`) se aplica la política 1 caneca 5 s · 2 canecas 7 s · 3 canecas 10 s (arranque
escalonado de 250 ms). Con firmware v1 la pasarela usa `CICLO`/`SEC` y el tiempo calibrado en el ESP32, sin modificar
ninguna calibración. La cuenta regresiva siempre muestra el tiempo que informa la estación.

### Seguridad de la apertura remota
- Permiso presencial de 2 h (32 bytes aleatorios, solo se guarda su huella SHA-256). Un QR nuevo o «Terminar» invalida el anterior.
- El fragmento `#…` no viaja al servidor de Pages ni en el Referer; la página lo borra de la URL al cargarla.
- CORS restringido a `https://jorgegutho21.github.io` y `localhost:5173`. Cuerpo ≤ 4 KB.
- Idempotencia por clave (un doble clic o reintento devuelve el resultado original sin reabrir), solicitudes de más de
  60 s rechazadas, 1 apertura cada 6 s y 40 cada 10 min, una operación a la vez.
- Requiere además la sesión supervisada del panel. Oracle, Ollama, el COM y el panel nunca se exponen.
- La detención por software no sustituye el corte físico de alimentación.

### Registro
Cada apertura y cada confirmación se anotan en `%ProgramData%\EcoAccessP3Framework\Ciudadano\operaciones-ciudadanas.jsonl`
(una línea por evento, `idOperacion` única, canal `WEB`, tipo `OPERACION_AUTOMATICA` o `DISPOSICION_CONFIRMADA_CIUDADANO`,
contenedores 1400/1093/1401, `estadoRegistro = PENDIENTE_ORACLE`). La sincronización con Oracle reprocesa esa bitácora
sin volver a mover nada. Las consultas educativas se guardan solo en el historial local del navegador.

## Catálogo de 90 residuos
`datos/catalogo-residuos.json`: 30 verdes, 30 blancas y 30 negras (Resolución 2184 de 2019), cada uno con alias
colombianos y un código `MATERIAL.CODIGO` existente en Oracle (no se crean filas por alias). Incluye términos genéricos que
piden aclaración («papel», «caja», «icopor», «envase»…) y residuos especiales (pilas, medicamentos, cortopunzantes, RAEE,
bombillos, químicos, aceite, biosanitarios) que **nunca** abren una caneca.

## Uso

Página local:
```powershell
python -m http.server 5173 --bind 127.0.0.1 --directory web/ciudadano
```

Estación física (equipo anfitrión, con el panel de canecas en ejecución):
```powershell
powershell -ExecutionPolicy Bypass -File scripts\estacion\Iniciar-Estacion-Ciudadana.ps1
```
En `http://localhost:8093` pulsa **Habilitar 2 horas y generar QR** y deja el código visible junto a las canecas.

## Pruebas
```powershell
node --test web/ciudadano/pruebas/motor.test.mjs          # 1 312 casos del catálogo (0 fallos)
node --test web/ciudadano/pruebas/integracion.test.mjs    # web → pasarela → panel simulado (14 casos)
node --test web/ciudadano/pruebas/interfaz.test.mjs       # navegador real: texto, voz simulada, ambigüedad, peligrosos,
                                                          # 5/7/10 s, modo físico con pasarela interceptada, 320 px (22 casos)
dotnet test tests/unitarias/EcoAccess.P3.Tests --filter FullyQualifiedName~PruebasPasarelaCiudadana
```
`pruebas/panel-simulado.py` imita el contrato HTTP del panel sin tocar el COM. `interfaz.test.mjs` usa Playwright desde
`ECOACCESS_PLAYWRIGHT` (carpeta `node_modules`) y Chrome desde `ECOACCESS_CHROME`; si no los encuentra, se omite.

## Recursos gráficos
Las capas `assets/canecas/*-{cuerpo,tapa,tapa-interior,boca}.webp` salen de las canecas originales con
`scripts/web/separar_capas_canecas.py estacion-web/dist/assets/canecas web/ciudadano/assets/canecas` (unión tapa/cuerpo
detectada por columna, bisagra en el borde trasero de la tapa, boca con labio, pared interna y fondo, y cara interior de la
tapa con su hueco real). Los valores `bisagra_pct` que imprime van en el `--bisagra` de cada caneca en `index.html`.
ECO se sirve en 200 px (`assets/marca/eco-*-200.webp`).

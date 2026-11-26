---
name: eco-diseno-web
description: Regla OBLIGATORIA para todo diseño o rediseño visual de EcoAccess (sitio web ECOACCESS WEB en web/ciudadano, sus secciones #/…, la app de escritorio y cualquier pantalla nueva). Cubre lo prohibido (no tocar Dos clics sin pedido expreso), cómo leer las maquetas de referencia, cómo usar los recursos originales, cómo reemplazar datos de maqueta que no concuerdan con la lógica real por elementos interactivos con datos verdaderos, el sistema de diseño, la caché de GitHub Pages y la verificación en el sitio publicado. Úsala SIEMPRE antes de diseñar, maquetar, rediseñar o "mejorar la interfaz" de EcoAccess, y antes de dar por terminado o publicar un cambio visual.
---

# EcoAccess · Regla obligatoria de diseño

Esta regla manda sobre cualquier maqueta, prompt o preferencia de estilo. Si algo de una maqueta choca con
esta regla, gana la regla y se explica al usuario qué se adaptó y por qué.

## 1. Prohibido

- **No modificar `web/ciudadano/dos-clics/`** (HTML, CSS, JS, pruebas) salvo que el usuario lo pida
  explícitamente en ese mensaje. Rediseñar otra página no autoriza a tocar Dos clics. Solo se enlaza a
  `./dos-clics/`.
- No tocar firmware del ESP32, calibraciones, puertos, Oracle, reconocimiento visual, bot de Telegram,
  servicios de arranque, anfitrión, roles, QR oficial, reglas de apertura ni los cuatro accesos directos por
  razones estéticas.
- No reintroducir estaciones piloto, DEMO ni SIMULACIÓN, ni textos visibles como «vista ilustrativa»,
  «sin movimiento físico» o «versión» como adorno.
- No inventar cifras, usuarios, rankings, enlaces ni estados. Las cifras de una maqueta (p. ej. «+12.540
  residuos», «3.280 usuarios») son de relleno: **nunca** se copian.
- No usar una captura completa de una maqueta como fondo de página. Solo se recortan elementos sueltos
  (personaje, canecas, ilustración del hero) y el resto se construye en HTML/CSS/SVG.
- No descargar fuentes ni librerías externas sin permiso (el sitio no usa CDN).

## 2. Cuando la maqueta no concuerda con la lógica real

La lógica existente es importante y se conserva. Interpreta toda la maqueta y mejórala, pero:

1. Si un bloque muestra datos que el sistema no tiene, **conviértelo en un elemento interactivo con datos
   verdaderos** del propio proyecto:
   - catálogo real (`datos/catalogo-residuos.json`: residuos por caneca, ejemplos),
   - historial de este dispositivo (`js/historial.js`),
   - estado real de la estación (`js/estacion.js`, nunca simulado),
   - enlaces verificados en el repositorio (bot `https://t.me/EcoAccessP3_JG_bot`, `./dos-clics/`, `#/…`).
2. Si la maqueta omite una función que ya existe (buscador, voz, estado de la estación, Mi historial,
   apertura por QR), **no se elimina**: se integra en la composición nueva con los mismos `id` que usan
   `js/app.js`, `js/rutas.js` y `js/estacion.js`.
3. Cuando la página llega vinculada a la estación (QR `#e=…&t=…` o acceso del anfitrión), la parte operativa
   va primero; la presentación queda después.
4. Al terminar, di al usuario qué partes de la maqueta se adaptaron y con qué dato real.

## 3. Lectura de referencias y recursos

- Analiza cada referencia: composición, jerarquía, color, tipografía, espaciado, botones, tarjetas,
  decoración, integración de Eco y de las canecas.
- Recursos originales: `C:\Users\heyhc\Downloads\05_Imagenes_EcoAccess (2)\05_Imagenes_EcoAccess` (162
  archivos; no se modifican) y los recortes que el usuario envíe. Lo optimizado va a `web/ciudadano/assets/…`
  en WebP, con `width`/`height` declarados y `loading="lazy"` fuera de la primera pantalla.
- Eco y las canecas son siempre las imágenes originales del equipo; nunca iconos genéricos ni otro robot.

## 4. Sistema de diseño

Tokens en `web/ciudadano/css/portada.css` (`--eco-*`). Paleta del kit de UI v1.0:

| Token | Valor | Uso |
|---|---|---|
| `--eco-forest` | `#073E23` | titulares, botón principal |
| `--eco-primary` | `#0B5E34` | verde principal |
| `--eco-secondary` | `#16A34A` | iconos, acentos |
| `--eco-light` | `#A7E27D` | detalles claros |
| `--eco-lime` | `#D9F99D` | brillos, subrayados |
| `--eco-soft` | `#F2FBF3` | fondo suave |
| `--eco-text` | `#0F172A` | texto |
| `--eco-text-2` | `#475569` | texto secundario |
| `--eco-border` | `#E2E8F0` | bordes |
| éxito / advertencia / error / info | `#22C55E` / `#F59E0B` / `#EF4444` / `#0EA5E9` | estados |

- Tipografía: Montserrat si está instalada; si no, la pila del sistema (`Segoe UI Variable`, `Segoe UI`,
  `system-ui`). Escala del kit: H1 48 · H2 36 · H3 28 · H4 20 · texto 16 · secundario 14 · notas 12 (fluida
  con `clamp`).
- Radios 12–28 px, tarjetas blancas con sombra suave, botón principal verde bosque relleno, secundario con
  borde, terciario de texto; iconos de trazo 2 px redondeado en contenedor circular verde claro.
- Decoración (hojas, circuitos, ondas) solo en bordes y transiciones, `aria-hidden`, sin eventos.
- Animaciones 150–250 ms con `transform`/`opacity`; revelado suave al hacer scroll; `prefers-reduced-motion`
  lo desactiva todo.

## 5. Caché y publicación (para que la versión publicada sea la misma que la local)

- `index.html` pide sus CSS/JS con `?v=AAAAMMDD[letra]`; súbelo en cada cambio de estilos o scripts.
- `sw.js`: HTML/CSS/JS/JSON «red primero». Si reemplazas una imagen con el **mismo nombre**, sube `VERSION`;
  mejor usa nombres nuevos.
- GitHub Pages publica desde `main` (workflow `web-ciudadana`). Publicar en `main` solo con permiso del
  usuario.

## 6. Verificación obligatoria (antes de decir «listo»)

1. Navegador real en 320, 360, 375, 390, 430, 768, 1024, 1280, 1440 y 1920 px de ancho (y celular
   horizontal): sin desplazamiento horizontal, sin solapes, botones ≥ 44 px, Dos clics visible desde el
   primer momento en celular.
2. Consola sin errores; enlaces verificados; menú móvil; teclado y foco visible; contraste AA; un solo `h1`.
3. Funciones intactas: buscador, voz, Mi historial, estado de la estación, enlace a Dos clics.
4. Pruebas: `node --test pruebas/*.test.mjs` en `web/ciudadano`.
5. Tras publicar: abrir la URL **publicada** (no solo localhost), comprobar `version.json`, y revisarla
   también en un navegador que ya tenía el sitio en caché.

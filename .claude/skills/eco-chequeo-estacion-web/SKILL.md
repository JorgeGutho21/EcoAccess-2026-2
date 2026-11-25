---
name: eco-chequeo-estacion-web
description: Regla de chequeo del diseño y la funcionalidad de la estación web de canecas de EcoAccess (estacion-web). Verifica el diseño final (banner blanco con ondas, pastilla EcoAccess, buscador de residuos, micrófono abierto, tres tarjetas verde/blanca/negra), los tokens de TemaEcoAccess, el flujo residuo → caneca → apertura automática y la ventana de 7 s. Úsala SIEMPRE al terminar un cambio en estacion-web, antes de dar el trabajo por hecho o hacer commit, y cuando pidan "revisar el diseño de la estación", "chequear la web de canecas" o "validar la estación web".
---

# EcoAccess · Chequeo de la estación web

Un cambio en `estacion-web` **solo está terminado** cuando pasa las dos partes:
el chequeo automático (A) sin errores y la lista visual (B) completa.

## A. Chequeo automático

Desde la raíz del repositorio:

```powershell
node .claude/skills/eco-chequeo-estacion-web/scripts/verificar-estacion-web.js
```

`--estricto` hace que las advertencias también fallen. Devuelve código 1 si no cumple.

| Regla | Nivel | Qué exige |
|---|---|---|
| D1 | Error | Banner blanco redondeado (`header.banner`) con la pastilla «EcoAccess» y al menos dos ondas verdes |
| D2 | Error | Título exacto «¿Dónde vas a *depositar?*» con el brote verde a la izquierda |
| D3 | Error | Buscador de residuos (`#formResiduo`, `#residuo`) con `<label>` accesible |
| D4 | Error | Micrófono verde redondo `#botonVoz.microfono`, texto «Dictado por voz» y pastilla de estado `#estadoVoz` |
| D5 | Error | Pastilla de ayuda con «caneca verde», «caneca blanca» y «caneca negra» |
| D6 | Error | Tarjetas en orden verde, blanca, negra |
| D7 | Error | Títulos Orgánicos / Aprovechables / No aprovechables, sin la palabra del color |
| D8 | Error | Cada tarjeta con la caneca moderna partida en cuerpo y tapa, y la flecha circular |
| D9 | Error | Nada de «modo demostración» visible |
| D10 | Error | Minimalista: sin franja de pasos ni fotos de residuos |
| T1 | Advertencia | Colores de la paleta EcoAccess (si se agrega uno, justificarlo y sumarlo a `PALETA` del script) |
| T2 | Error | Tokens de `TemaEcoAccess` intactos (`--verde-oscuro #105C3B`, `--verde #168A55`, `--lima #7AC143`…) |
| T3 | Error | Sin fuentes, CDN ni imágenes remotas: la estación funciona sin internet |
| T4 | Error | Botones grandes: micrófono ≥ 58 px, botón del buscador ≥ 44 px |
| T5 | Advertencia | Ningún texto por debajo de 14 px |
| T6 | Error | Consultas adaptables: vertical en filas, banner en dos filas (≤ 980 px) y celular (≤ 600 px) |
| T7 | Error | `prefers-reduced-motion` respetado |
| T8 | Error | `styles.css?v=N` y `app.js?v=N` con la misma versión (subirla en cada cambio) |
| F1 | Error | El micrófono arranca abierto en cada visita |
| F2 | Error | Residuo → caneca destacada → apertura automática, sin pedir «sí» |
| F3 | Error | El momento de depósito dice «Tu caneca es la …» |
| F4 | Error | Ventana de 7 s en el reloj, en la simulación del navegador y en `server-local.js` |
| F5 | Error | La apertura exige `X-EcoAccess: estacion` |
| F6 | Error | Sin secretos (HMAC, wallets, contraseñas, claves) en la web |
| F7 | Error | Nada de `innerHTML` con lo que escribe o dice la persona |
| F8 | Error | `app.js` y `server-local.js` sin errores de sintaxis |

## B. Lista visual (navegador, después de Ctrl+F5)

Abrir `http://localhost:4173` (o el puerto en uso) y revisar en **1366×768, 1920×1080, 768×1024, 375×812 y 812×375**:

1. El banner se ve como el diseño final:
   - La pastilla verde «EcoAccess» monta sobre el borde superior izquierdo.
   - Hay ondas verde lima en las esquinas.
   - En escritorio el título va en una sola línea.
2. En escritorio, una sola fila: título y buscador, divisor, micrófono con «Dictado por voz» y «Escuchando…», y la pastilla de ayuda (esta se oculta bajo 1240 px).
3. Las tres tarjetas:
   - Caben sin desplazarse en 1366×768.
   - La caneca se ve completa y el título no se corta ni se parte a la mitad de una palabra.
   - La flecha circular es lima claro en la verde, verde suave en la blanca y blanca en la negra.
4. Celular y tableta de pie:
   - El banner va en dos filas: título y buscador arriba, micrófono abajo.
   - Hay una fila por caneca.
   - No hay barra de desplazamiento horizontal.
5. Flujo: escribir «botella de agua» y pulsar Enter.
   - La caneca blanca se destaca, aparece la etiqueta «♻ Botella de plástico» y las otras se apagan.
   - Luego se abre el momento con «Tu caneca es la blanca», «¡Deposita ahora!» y la cuenta 7 → 0.
6. Pilas o medicamentos: no se abre ninguna caneca; sale el aviso con a dónde llevarlos.
7. Texto que no se reconoce: el buscador se sacude y aparece «No reconocí…».
8. Con Chrome o Edge y el micrófono permitido:
   - El estado dice «Escuchando…» sin tocar nada.
   - Decir «botella de agua» abre la blanca.
9. A+ agranda el texto sin romper el banner.
10. La consola del navegador no muestra errores.

## Qué no cambiar sin acuerdo

- No volver a la franja oscura de encabezado ni a «modo demostración».
- La estación no habla con el ESP32: la ruta es Navegador → `server-local.js` (127.0.0.1) → `EcoAccess.P3.Bot --estacion-web` → Oracle → bridge → ESP32 (HMAC).
- La ventana es de 7 s en todo el sistema (`MensajesTelegram.VentanaDepositoSegundos = 7`, `ABIERTO_MS = 7000`). En modo `bot` la web muestra la que informa el bot; si un ESP32 sigue con el firmware viejo (10 s), hay que volver a cargarlo.

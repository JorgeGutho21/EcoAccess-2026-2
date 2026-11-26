# Sistema de diseño ECOACCESS (web ciudadana y centro de control ESP32)

Una marca, dos productos con temperamento distinto:

- **ECOACCESS WEB** es la estación vista por cualquier persona en el campus: clara, mineral, al aire libre, con las tres
  canecas reales como protagonistas. Se usa con una mano, de pie, a veces con ruido.
- **ECOACCESS ESP32** es la sala de control del operador: un tablero oscuro de bosque profundo, con lectura rápida,
  números tabulares y señales de estado inequívocas. Se usa en escritorio, sentado, con el riesgo real de mover motores.

## 1. Color

Base común (valores de `TemaEcoAccess.cs`, la app de escritorio):

| Token | Hex | Uso |
|---|---|---|
| `--eco-bosque` | `#105C3B` | Verde institucional: acción principal, marca |
| `--eco-hoja` | `#168A55` | Foco, progreso, éxito |
| `--eco-brote` | `#B7E4CB` | Texto claro sobre verde, rellenos suaves |
| `--caneca-verde` | `#3C9A2E` | Caneca de orgánicos (tono de la caneca real) |
| `--caneca-blanca` | `#DCE3E6` | Caneca de aprovechables (borde `#9AA8AE` sobre fondos claros) |
| `--caneca-negra` | `#1F2629` | Caneca de no aprovechables |
| `--alerta` | `#C27C0E` | Espera, estado incierto |
| `--peligro` | `#C0362C` | Error, desconexión, DETENER |

Web (clara, «cal de campus»): fondo `#EEF3EF` (gris verdoso mineral, no crema), superficie `#FFFFFF`, tinta `#14231C`,
tinta secundaria `#4B5D55`. Panel (oscuro, «bosque nocturno»): fondo `#0C1F18`, superficie `#132B22`, superficie 2
`#1A372C`, línea `#24473A`, tinta `#E8F2EC`, tinta secundaria `#9DB8AA`. No se usa negro puro ni gris neutro: todo
neutro lleva un poco de verde. Los colores de estado son siempre punto + palabra (nunca solo color).

## 2. Tipografía

| Producto | Títulos | Texto | Por qué |
|---|---|---|---|
| Web | **Bricolage Grotesque** (600–800) | **Atkinson Hyperlegible** (400/700) | Titular con carácter propio para la pregunta principal; texto diseñado para baja visión y lectura rápida en público |
| Panel | **Barlow Semi Condensed** (600–700) | **Barlow** (400–600), cifras tabulares | Familia de señalética industrial: etiquetas compactas, números de grados y segundos alineados |

Escala web (base 17 px móvil / 18 px escritorio, razón 1,25): 14 · 17 · 21 · 27 · 34 · 42 · 53 (`clamp()` entre
tamaños). Escala panel (base 15 px, razón 1,2): 13 · 15 · 18 · 22 · 26 · 31 · 44 (temporizadores). Nunca menos de 13 px.
Sentence case en todo; sin etiquetas en mayúsculas sostenidas salvo los botones de seguridad del panel
(ABRIR, CERRAR, DETENER, ACTIVAR CONTROL, DETENER TODO), que son señales operativas.

## 3. Forma, sombra y movimiento

- Radios por jerarquía (no uno para todo): escenario y diálogos 28 px; controles 14 px; pastillas 999 px; panel 10 px
  (más técnico).
- Sombra única de contacto bajo las canecas (elipse difusa) y una sombra de elevación para el dock de consulta. Nada
  de sombras grises repetidas bajo cada tarjeta.
- Movimiento solo como respuesta: la tapa gira sobre su bisagra (`transform-origin` en el borde trasero, `rotateX`,
  `perspective`), el cuerpo nunca se mueve; el anillo del temporizador se vacía con `stroke-dashoffset`. 180–260 ms
  para la interfaz, la duración real del servo (≈ 700 ms) para la tapa. `prefers-reduced-motion`: la tapa cambia de
  estado sin girar y el anillo se reemplaza por el número.
- Las animaciones de la caneca reflejan el estado real (comandado / estimado); en modo consulta llevan el rótulo
  «Vista ilustrativa».

## 4. Componentes compartidos (misma semántica en ambos productos)

| Componente | Web | Panel |
|---|---|---|
| Caneca por capas (cuerpo, boca, tapa, cara interna) | Escenario protagonista | Tarjeta de cada caneca y miniaturas de combinaciones |
| Temporizador circular | Grande sobre la caneca elegida | En cada tarjeta y en la secuencia |
| Indicador de estado (punto + palabra + antigüedad) | Disponibilidad de la estación | Barra de servicios (USB, ESP32, COM, estación, Oracle, Telegram, pasarela) |
| Botón principal / secundario / peligro | Identificar · Hablar | ABRIR · CERRAR · DETENER |

Recursos: `web/ciudadano/assets/canecas/*-{cuerpo,boca,tapa,tapa-interior}.webp` (separadas de las imágenes originales
con `scripts/web/separar_capas_canecas.py`), personaje ECO en `web/ciudadano/assets/marca/`, logotipo oficial.

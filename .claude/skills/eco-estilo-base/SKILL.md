---
name: eco-estilo-base
description: Sistema de diseño de EcoAccess (WinForms) extraído de las pantallas de ciudadano e invitado — paleta, tipografía, radios, cajas de texto, botones, tarjetas, pastillas, tablas y navegación. Úsala ANTES de crear o modificar cualquier pantalla, módulo, diálogo o control de src/EcoAccess.P3.Presentacion, y como base de eco-ui-operario y eco-ui-supervisor.
---

# EcoAccess · Estilo base

Esta skill describe la estética que ya tienen las pantallas públicas (inicio de sesión, explorar como invitado,
registro, asistente de depósito, inicio del ciudadano, catálogo por caneca). Las áreas **Operario** y
**Supervisor** deben verse como la misma aplicación: mismos tokens y mismos controles, **sin ilustraciones**
(ver `eco-ui-supervisor`).

Fuente de verdad del código: `src/EcoAccess.P3.Presentacion/TemaEcoAccess.cs`. Nunca copies un valor
hexadecimal: usa siempre la constante de `TemaEcoAccess`.

## 1. Lenguaje visual (lo que se percibe en las capturas)

- Fondo gris verdoso muy claro, contenido en **tarjetas blancas redondeadas con sombra suave**.
- Un solo color de acción: **verde oscuro** (`VerdeOscuro`). Los botones secundarios son blancos con borde gris
  y texto verde oscuro. Nada de azules de Windows, grises 3D ni bordes negros.
- Bordes finos de 1 px en `Borde`; con foco pasan a **2 px en `Verde`**; con error, 2 px en `Error`.
- Iconos **FontAwesome sólidos** (paquete `FontAwesome.Sharp`) en gris `Suave` o verde; nunca emojis ni PNG sueltos.
- Textos cortos, en español, en segunda persona («Ingresa tu correo», «Escribe un residuo…»).
- Mucho aire: rellenos de 14–20 px, separación de 8–12 px entre controles.
- Estados con **pastilla** (punto + texto sobre fondo suave del mismo tono): «● Oracle conectado».

## 2. Tokens

### Color (`TemaEcoAccess`)

| Token | Hex | Uso |
|---|---|---|
| `Fondo` | #F6F8F7 | Fondo de formularios y del área de contenido |
| `Superficie` | #FFFFFF | Tarjetas, cajas de texto, barra lateral, cabecera |
| `Borde` | #E2E8E5 | Bordes de 1 px, separadores, botón deshabilitado |
| `VerdeOscuro` | #105C3B | Botón principal, texto de botón secundario, títulos de marca, enlaces activos |
| `Hover` | #0C4A2F | Hover/pulsado del botón principal |
| `Verde` | #168A55 | Foco (anillo 2 px), iconos activos, cifras grandes. **No** para texto pequeño (4,4:1) |
| `VerdeSuave` | #E8F6EE | Hover de secundarios, fila seleccionada, ítem de menú activo, pastilla OK |
| `VerdeHoja` | #B7E4CB | Pulsado de secundarios, texto claro sobre verde oscuro |
| `Texto` | #1E293B | Texto principal y etiquetas |
| `Suave` | #556274 | Ayudas, subtítulos, iconos en reposo, encabezados de tabla |
| `Error` / `ErrorSuave` | #B42318 / #FDECEA | Mensajes y bordes de error / fondo de pastilla |
| `Advertencia` / `AdvertenciaSuave` | #925A07 / #FEF4E2 | Avisos |
| `Info` / `InfoSuave` | #1D4ED8 / #E8EFFD | Información neutra |

Colores de caneca (solo para representar categorías, vía `EstiloCategoria.Para(...)`): verde orgánicos,
blanco/gris pizarra aprovechables, negro no aprovechables.

### Tipografía (Segoe UI, nunca menor de 9 pt)

| Token | Fuente | Uso |
|---|---|---|
| `Titulo` | Segoe UI Semibold 20 | Título de pantalla / saludo |
| `TituloSeccion` | Segoe UI Semibold 14 | Título del módulo en la cabecera, títulos de tarjeta |
| `Subtitulo` | Segoe UI Semibold 11 | Etiquetas de campo, texto de botones |
| `Valor` | Segoe UI Semibold 24 | Cifras de indicadores (ecopuntos, depósitos) |
| `Entrada` | Segoe UI 11 | Texto dentro de cajas de texto |
| `Cuerpo` | Segoe UI 10 | Texto general, celdas de tabla, menú |
| `Pequena` | Segoe UI 9 | Ayudas bajo campos, notas, rótulo «ÁREA SUPERVISOR» |

### Forma y medida (en unidades lógicas; escalar SIEMPRE con `TemaEcoAccess.Px(control, n)`)

- Radio de tarjeta: `TemaEcoAccess.Radio` = 12. Radio de caja de texto y botón: 10. Pastilla: alto/2.
- Caja de texto: alto 46 (40 en modo compacto). Icono a 15 px del borde, texto desde 44 px.
- Botón: alto mínimo 36, relleno 14×6. Ítem de menú lateral: alto 40.
- Tabla: encabezado 40, filas 38, relleno de celda 8.
- Tarjeta: relleno 20 + sombra 6, margen 0,0,12,12.

## 3. Inventario de controles (usar estos, no inventar otros)

| Elemento | Control existente | Notas |
|---|---|---|
| Caja de texto con etiqueta | `CampoEco(etiqueta, IconChar, TextBox, contrasena)` | Etiqueta arriba en `Subtitulo`, caja redondeada con icono a la izquierda, ojo para contraseñas, mensaje debajo (`Ayuda`, `MostrarError`, `MostrarValido`). El `TextBox` va sin borde. |
| Texto de ayuda dentro de la caja | `CajaTexto.PlaceholderText` o `CajaTexto.Sugerir(caja, "…")` | Frase en segunda persona: «Ingresa tu correo». |
| Búsqueda | `CajaBusqueda` | Lupa a la izquierda, borde verde al enfocar, radio 12. |
| Botón principal | `TemaEcoAccess.EstilizarAccion(kryptonButton, principal: true)` o `Button` llamado `…Guardar/Registrar/Entrar` + `TemaEcoAccess.Aplicar` | Fondo `VerdeOscuro`, texto blanco, una sola por grupo de acciones. |
| Botón secundario | `EstilizarAccion(boton)` / `EstilizarBoton(boton)` | Blanco, borde `Borde`, texto `VerdeOscuro`, hover `VerdeSuave`. |
| Botón grande de portada | `BotonEco` (`Principal`, `Secundario`, `Suave`, `Enlace`) | Solo pantallas públicas; en gestión se prefiere Krypton/Button estilizado. |
| Botón con icono | `kryptonButton.Values.Image = CacheIconos.Obtener(IconChar.X, Px(16), color)` | Icono 16 px del color del texto (blanco en principal, `Verde`/`VerdeOscuro` en secundario). |
| Selector de opción (rol, filtro) | Botones secundarios en fila; el elegido con borde `VerdeOscuro` 2 px y fondo blanco | Como «Ciudadano / Operario / Supervisor» del login. |
| Tarjeta | `TarjetaEco` (`Relleno`, `ColorBorde`) | Blanca; la de bienvenida puede ir en `VerdeOscuro` con texto blanco/`VerdeHoja`. |
| Indicador (KPI) | `TarjetaDato` o tarjeta con título `Subtitulo`, cifra `Valor` en `VerdeOscuro`, nota `Pequena` en `Suave` + icono en cuadro `VerdeSuave` | «Mis ecopuntos · 0 · Verificados». |
| Estado | `PastillaEstado.Mostrar(texto, tono, fondoSuave)` | Nunca solo color: siempre punto + palabra. |
| Tabla | `DataGridView` + `TemaEcoAccess.EstilizarTabla` (o `DisenoFormularios.PrepararTabla`) | Sin cabeceras de fila, separadores horizontales, selección `VerdeSuave`. |
| Navegación lateral | `BotonNavegacion(texto, IconChar)` | Activo: fondo `VerdeSuave`, icono y texto `VerdeOscuro` semibold. |
| Avatar | `AvatarIniciales(nombre)` | Iniciales, nunca foto. |
| Pasos de asistente | `IndicadorPasos` | Hechos con check, actual resaltado, pendientes en gris. |
| Columna de formulario | `DisenoFormularios.Columna()` + `Campo(...)` + `Texto(...)` + `Accion(...)` | `Campo` ya produce etiqueta semibold + caja de 46 px con icono; `Texto(…, true)` es subtítulo de sección; `Accion` y `Agregar(botón)` dan el botón Eco con icono. |
| Estructura de módulo de gestión | `ModuloEco` (`Iniciar`, `Seccion`, `Campo`, `Boton`, `DosColumnas`, `Columna`, `Terminar`) | Encabezado con icono, tarjetas con título y campos con icono. Ver `eco-ui-supervisor`, sección 3. |
| Caja de entrada con icono | `MarcoEntrada(control, IconChar)` | Aloja TextBox, NumericUpDown, ComboBox o DateTimePicker con el mismo marco de 46 px del inicio de sesión. |

### Controles estándar del Designer (automático)

`TemaEcoAccess.Aplicar(form)` convierte los controles estándar con `EstiloControlesEco.cs`, sin tocar el Designer
ni los eventos. Por eso un formulario de gestión **no necesita** rehacerse a mano; basta con que pase por `Aplicar`:

| Control estándar | Se ve como |
|---|---|
| `TextBox` (con borde) y `NumericUpDown` | `MarcoEntrada`: caja blanca redondeada, alto ≈ 4/3 del natural, foco 2 px `Verde`, gris `Fondo` si es de solo lectura |
| `ComboBox` lista | Caja redondeada con chevron, opciones con selección `VerdeSuave` |
| `DateTimePicker` | Borde redondeado e icono de calendario Eco sobre el dibujo nativo |
| `Button` | Redondeado; tipo por nombre o texto: **principal** (Registrar, Guardar, Buscar, Consultar, Verificar, Admitir, Asignar, Crear, Enviar…), **peligro** (Dar de baja, Deshabilitar, Desactivar, Quitar, Fuera de servicio, Eliminar, Rechazar), el resto **secundario** |
| `GroupBox` | Tarjeta blanca radio 10 con el título en `VerdeOscuro` |
| `TabControl` | Pestañas planas: elegida en `VerdeOscuro` semibold con subrayado `Verde` de 3 px |
| `DataGridView` | `EstilizarTabla` + borde fino `Borde` |
| `ListBox`, `CheckBox`, `RadioButton` | Borde suave redondeado / casilla plana |

Para que un botón nuevo salga principal o de peligro, nómbralo con esas palabras (`botonGuardar`, `botonDarDeBaja`).
Un control con `Tag = TemaEcoAccess.SinEstilo` (botones) o `BorderStyle.None` (cajas) se deja como está.

## 4. Reglas que siempre se cumplen

1. Todo `Form` nuevo o abierto con `ShowDialog` pasa por `TemaEcoAccess.Aplicar(form)`.
2. Colores y fuentes solo desde `TemaEcoAccess` (salvo `Color.White` y `Color.Transparent`).
3. Medidas en píxeles siempre con `TemaEcoAccess.Px(...)` (pantallas al 125 % y 1366×768).
4. Foco visible: anillo de 2 px en `Verde`. Todo control interactivo tiene `AccessibleName`.
5. Errores: borde rojo **y** texto debajo del campo (no depender solo del color).
6. Botones `FlatStyle.Flat`, nunca `Standard`/`System`; cajas sin `BorderStyle.Fixed3D`.
7. Un único botón principal por bloque de acciones; el resto secundarios.
8. Textos de interfaz en español, en tono cercano y claro, sin tecnicismos de base de datos.

Para comprobar que una pantalla cumple, usa la skill `eco-chequeo-estetica`.

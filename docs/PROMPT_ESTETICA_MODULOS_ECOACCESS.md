# Prompt maestro — Estética de los módulos de gestión de EcoAccess (Supervisor y Operario)

> **Cómo usarlo:** copia todo lo que está debajo de la línea y pégalo como primer mensaje (o instrucciones del sistema) a la
> IA que vaya a rediseñar un módulo. Al final escribe qué módulo quieres («Rediseña el módulo Puntos de instalación») y, si
> tienes, adjunta las imágenes de referencia. Este documento describe con exactitud lo que ya se hizo en **Dashboard,
> barra lateral, Usuarios y roles, Materiales y Canecas y QR** para que cualquier módulo nuevo quede idéntico en estética.
> Complementa a `docs/PROMPT_CONTEXTO_IA_ECOACCESS.md` (contexto general del proyecto).

---

## 0. Tu misión y las reglas que no se negocian

Eres el encargado de la **estética** de un módulo de la aplicación de escritorio **EcoAccess** (C# WinForms, .NET Framework
4.8, C# 9, `nullable` habilitado). Tu trabajo es que el módulo que te pidan se vea **exactamente como los módulos ya
aprobados** (Usuarios y roles, Materiales, Canecas y QR) y como el Dashboard, usando **las mismas piezas de código**.

Reglas absolutas (el equipo las exigió de forma explícita):

1. **No modificas la lógica. Nada.** Solo mejoras la estética: botones, cajas, textos, tablas, pestañas, tarjetas, banner,
   iconos, avisos. Al terminar, `git status` debe mostrar **sin cambios** el `FormX.cs` (lógica) y el `FormX.Designer.cs`
   del módulo. Todo tu trabajo vive en `FormX.Diseno.cs` (archivo parcial de presentación) y, si hace falta, en piezas
   reutilizables (`ComponentesGestion.cs`, `ModuloEco.cs`, `EstiloControlesEco.cs`, `ComponentesDashboard.cs`).
2. **Reutiliza los controles del Designer**: los mismos objetos (`botonRegistrar`, `tablaX`, `comboX`, `textoX`,
   `panelAcciones`, `etiquetaEstado`…), así los eventos y validaciones siguen funcionando. Nunca crees un botón nuevo que
   duplique uno del Designer; nunca cambies el texto de un botón si la lógica lo usa para clasificarlo.
3. **No inventes comportamiento** en los textos de ayuda/avisos. Si un aviso afirma algo («el QR deja de funcionar»), debe
   estar confirmado en el código; si no, redáctalo de forma que solo diga lo que es seguro (p. ej. el texto del propio
   botón: «Dar de baja (definitivo)»).
4. **Todo en español** (textos de la UI, comentarios, nombres nuevos, mensajes de commit).
5. **Colores y fuentes solo desde `TemaEcoAccess`**. Un literal solo se permite con el comentario en la misma línea
   `// eco-estetica: permitido <motivo>`.
6. **Nada de imágenes** en gestión, salvo la excepción aprobada (banner del Dashboard y `BannerModulo` de los módulos que
   el equipo apruebe; ver §5). Iconos siempre FontAwesome.
7. **Accesibilidad**: estados con texto además de color; `AccessibleName` en controles de solo icono; foco visible con
   teclado; tooltips en botones de solo icono.
8. Antes de entregar: compilar, capturar, mirar las capturas, pasar el chequeo de estética en modo estricto (0 errores,
   0 advertencias) y las pruebas unitarias (todas en verde). Ver §13.
9. **No hagas commit** si no te lo piden. Si te lo piden, agrega solo los archivos de tu trabajo (nunca
   `BaseDeDatos_Amilkar.zip`, `BaseDeDatos_Amilkar/`, `docs/GUIA_ESTUDIO_COMPACTA_ECOACCESS.md`, `sites/`).
10. Si el equipo manda imágenes de referencia, **replícalas** (disposición, iconos, textos, colores). Si no manda, aplica
    el patrón aprobado de este documento. Si falta algo (una imagen, una decisión), **pregunta** con opciones concretas.

---

## 1. Tokens de diseño (`TemaEcoAccess`)

| Token | Hex | Uso |
|---|---|---|
| `Fondo` | `#F6F8F7` | fondo del área de contenido y de botones deshabilitados |
| `Superficie` | `#FFFFFF` | tarjetas, barra lateral, cabecera, páginas de pestañas |
| `Borde` | `#E2E8E5` | bordes de 1 px, separadores, pastillas grises |
| `Verde` | `#168A55` | acento: iconos de títulos, icono activo, foco, puntos de estado |
| `VerdeOscuro` | `#105C3B` | títulos, botón principal, píldora elegida, texto de pastillas verdes |
| `VerdeSuave` | `#E8F6EE` | fondos suaves: fichas, avisos, botón suave, encabezado de tabla, fila elegida |
| `VerdeHoja` | `#B7E4CB` | bordes de fichas/avisos/pastillas verdes, hover del botón suave |
| `Hover` | `#0C4A2F` | hover/pulsado del botón principal |
| `Texto` | `#1E293B` | texto normal, texto del botón suave |
| `Suave` | `#556274` | texto secundario, deshabilitado, pastillas grises |
| `Error` / `ErrorSuave` | `#B42318` / `#FDECEA` | botón de peligro, mensajes de error |
| `Advertencia` / `AdvertenciaSuave` | `#925A07` / `#FEF4E2` | pastillas ámbar («Fuera de servicio», «Pendiente») |
| `Info` / `InfoSuave` | `#1D4ED8` / `#E8EFFD` | (poco usado) |

Fuentes (Segoe UI; nunca por debajo de 9 pt): `Titulo` semibold 20 · `TituloSeccion` semibold 14 · `Subtitulo` semibold 11
· `Valor` semibold 24 · `Entrada` 11 · `Cuerpo` 10 · `Pequena` 9.
Para fuentes en **píxeles** dibujadas a mano: `DibujoDashboard.Fuente(control, px)` y `DibujoDashboard.Semibold(control, px)`
(cacheadas, no se liberan).

Escala: `TemaEcoAccess.Px(control, n)` convierte unidades lógicas (96 DPI) a píxeles según `DeviceDpi`. Los módulos usan
`AutoScaleMode.Dpi` con `AutoScaleDimensions = 96`. Todo tamaño dibujado a mano pasa por `Px`.

Iconos: `FontAwesome.Sharp` (`IconChar`). Bitmap cacheado: `CacheIconos.Obtener(icono, ladoPx, color)`.

---

## 2. Anatomía de un módulo aprobado (de arriba abajo)

```
┌ Cabecera de la ventana (FormPrincipal; no se toca) ─────────────────────────────────────────┐
│ icono  Título del módulo · «Área Supervisor · fecha»    ● Oracle conectado | Avatar ▾  ⇥     │
└──────────────────────────────────────────────────────────────────────────────────────────────┘
┌ BannerModulo (degradado verde del dashboard, Eco con tableta, campus a la derecha) ─────────┐
│  [Eco]   Nombre del módulo 🍃 (semibold 29 px, blanco)                                       │
│          Descripción (16 px, blanco 94 %)                                                    │
│          ⓘ Línea de estado con la información actual del módulo (14 px)                     │
└──────────────────────────────────────────────────────────────────────────────────────────────┘
┌ 🔍 Buscar … (tarjeta con filtros en UNA fila) ───────────────────────────────────────────────┐
│ Etiqueta        Etiqueta        Etiqueta                                                      │
│ [🔍 caja 46px] [▾ lista 46px] [▾ lista 46px]  [■ Buscar]  [↻ Actualizar lista]              │
└──────────────────────────────────────────────────────────────────────────────────────────────┘
┌ 👥 Lista (tarjeta, llena el alto) ───────────────┐ ┌ ⚙ Gestión de … (tarjeta lateral) ──────┐
│ ┌ encabezado verde suave ───────────────────────┐│ │ [■ Píldora 1][□ Píldora 2][□ Píldora 3] │
│ │ ◯AB Nombre  ● Activo  correo …               ││ │ ┌ página sin pestañas clásicas ───────┐ │
│ │ filas de 46 px, pastillas, iconos             ││ │ │ ⓘ Aviso verde con icono             │ │
│ └───────────────────────────────────────────────┘│ │ │ Etiqueta / [icono caja con guía]    │ │
│ [‹ Anterior] [› Siguiente]  Página 1 · …         │ │ │ [  Botón principal a todo el ancho ]│ │
│ Mensaje de estado (verde u error)                │ │ └─────────────────────────────────────┘ │
└──────────────────────────────────────────────────┘ └─────────────────────────────────────────┘
```

---

## 3. Archivos y piezas que debes usar (todas existen)

| Archivo | Pieza | Para qué |
|---|---|---|
| `ComponentesDashboard.cs` | `BannerModulo` | banner superior del módulo (§5) |
| `ComponentesDashboard.cs` | `BannerBienvenida.PintarFondo(g, c, anchoEco, anchoCampus, conCanecas)` | fondo compartido de los banners |
| `ComponentesGestion.cs` | `PaginasSinPestanas` | TabControl sin la franja clásica (§8) |
| `ComponentesGestion.cs` | `SelectorPestanas`, `BotonPestana` | píldoras con icono que eligen la página (§8) |
| `ComponentesGestion.cs` | `AvisoEco` | recuadro verde con icono de información (§9) |
| `ComponentesGestion.cs` | `DisenoGestion.BotonSuave` | botón verde suave a todo el ancho (§7) |
| `ComponentesGestion.cs` | `DisenoGestion.TextoGuia` | texto gris dentro de una caja vacía (§6) |
| `ComponentesGestion.cs` | `DisenoGestion.Ficha` | ficha del elemento elegido (§9) |
| `ComponentesGestion.cs` | `DisenoGestion.PulirTabla` | tabla con filas de 46 px, encabezado verde, mínimos y estado vacío (§10) |
| `ComponentesGestion.cs` | `DisenoGestion.PastillaEstado`, `DisenoGestion.Pastilla` | pastillas dibujadas en celdas (§10) |
| `ModuloEco.cs` | `Iniciar(form, icono, titulo, descripcion, banner)` | raíz del módulo (acepta banner) |
| `ModuloEco.cs` | `Seccion(titulo, descripcion, llenar, icono)` | tarjeta blanca con título verde e icono |
| `ModuloEco.cs` | `Campo`, `Boton`, `Rejilla`, `JuntoACampos`, `DosColumnas`, `Mensaje`, `Fila`, `Terminar` | ver §6–§7 |
| `VistaGestionCatalogo.cs` | `VistaGestionCatalogo(..., anchoLateral, banner, iconoBusqueda, iconoLista, iconoEditor)` | catálogo «buscar + tabla + lateral» completo |
| `DisenoFormularios.cs` | `Columna()`, `Agregar(columna, control)`, `Campo(columna, etiqueta, entrada, icono)`, `Texto(texto, destacado)` | contenido de cada página |
| `EstiloControlesEco.cs` | `Boton(b, TipoBoton)`, `TieneIcono(b)`, `Lista`, `MarcoEntrada`… | estilizado automático |
| `ComponentesExplorar.cs` | `EstiloCategoria.Para(categoria)` → `(Tono, Fondo, Icono, Destino)` | color e icono de cada categoría de residuo |
| `ComponentesEco.cs` | `TarjetaEco`, `PastillaEstado`, `EstadoVacioEco`, `RecursosEco`, `CacheEscalado`, `InsigniaIcono` | piezas base |

**Ejemplos completos para copiar:** `FormUsuarios.Diseno.cs` (catálogo con 3 páginas), `FormMateriales.Diseno.cs`
(catálogo con 2 páginas y acción fija), `FormContenedores.Diseno.cs` (módulo con `ModuloEco.Iniciar` sin
`VistaGestionCatalogo`).

---

## 4. Elige el esqueleto

- **Catálogo con filtros + tabla paginada + editor lateral** (como Usuarios y Materiales) → usa `VistaGestionCatalogo`:

  ```csharp
  Controls.Add(new VistaGestionCatalogo("Nombre del módulo", "descripción corta (solo si no hay banner)",
      IconChar.Users, _filtros, tablaX, _paginacion, panelAcciones, etiquetaEstado,
      tituloLista: "Cuentas", tituloEditor: "Gestión de la cuenta", tituloBusqueda: "Buscar cuentas", anchoLateral: 500,
      banner: banner, iconoBusqueda: IconChar.MagnifyingGlass, iconoLista: IconChar.Users, iconoEditor: IconChar.Gear));
  ```

- **Módulo con tabla + columna lateral sin filtros paginados** (como Canecas y QR) → usa `ModuloEco.Iniciar` con banner:

  ```csharp
  TableLayoutPanel raiz = ModuloEco.Iniciar(this, IconChar.Trash, "Título", "Descripción", banner);
  var lista = ModuloEco.Seccion("Canecas registradas", "Ayuda corta.", llenar: true, icono: IconChar.TrashCan);
  lista.Agregar(tablaX, llenar: true);
  lista.Agregar(ModuloEco.Mensaje(etiquetaEstado));
  // … tarjeta lateral «Gestión de …» con píldoras (§8) dentro de panelAcciones …
  ModuloEco.Fila(raiz, ModuloEco.DosColumnas(lista, panelAcciones, 470), llenar: true);
  ModuloEco.Terminar(this, raiz, new Size(640, 600));
  ```

- **Módulos de consulta/estadística/diagnóstico** (Auditar depósitos, Estadísticas, Sistema y Telegram…) → mismo
  `ModuloEco.Iniciar` con banner, tarjetas `Seccion` con icono, filtros en una fila, tablas pulidas y, si hay pestañas de
  resultados, `PaginasSinPestanas` + `SelectorPestanas`.

La regla del chequeo **E5** exige que cada módulo use `ModuloEco.Iniciar`, `VistaGestionCatalogo` o `Encabezado`.

---

## 5. El banner superior (`BannerModulo`) — obligatorio en los módulos rediseñados

### 5.1 Qué es

Es el **mismo banner del Dashboard** adaptado al módulo. El equipo lo pidió así: «para el apartado de arriba me gustaría la
estética que está en el dashboard, claramente asociada con la información actual de ese módulo». Reemplaza al encabezado
simple (icono en cuadro + título).

- Fondo: `BannerBienvenida.PintarFondo(g, this, anchoEco, anchoCampus, conCanecas: false)`:
  - rectángulo redondeado (radio 16 px) con degradado horizontal **medido sobre el diseño** (no cambiarlo por tokens):
    `#175C2E` 0 % → `#12532D` 17 % → `#1A583A` 32 % → `#256848` 46 % → `#2A7250` 60–100 %;
  - `eco/campus.jpg` a la derecha (45 % del ancho, máx. 640 px lógicos), al 85 % de opacidad, **fundido sin corte** desde el
    tono exacto del degradado en su borde (`TonoEn`) y con un velo verde de alfa 70;
  - **sin las tres canecas** (son exclusivas del dashboard);
  - Eco con la tableta a la izquierda (`eco/eco_tablet_banner.png`, 17 % del ancho, máx. 210 px), apoyado abajo. Esa imagen
    es `eco_tablet.png` sin el velo blanco y con los cuadros verdes de fondo al 27 %.
- Texto, desde `AnchoEco + 34 px`, a 22 px del borde superior:
  - **título** = nombre del módulo, `DibujoDashboard.Semibold(this, 29)`, blanco, con la **hoja** `IconChar.Leaf` 26 px en
    `VerdeHoja` justo después (si cabe);
  - **descripción**, `DibujoDashboard.Fuente(this, 16)`, blanco con alfa 240, en varias líneas si hace falta;
  - **línea de estado** (opcional), `Fuente(14)`, blanco, con un icono 16 px en `VerdeHoja` delante, una sola línea con «…».
- Alto mínimo 150 px; crece solo si la descripción ocupa más líneas. Margen inferior 14.

### 5.2 Cómo se usa

```csharp
var banner = new BannerModulo("Personas y permisos",
    "Administra cuentas, roles y accesos del sistema de forma segura. Los permisos se verifican en cada operación.",
    IconChar.UsersGear);                         // icono de la línea de estado
banner.Estado = _paginaTexto.Text;               // información ACTUAL del módulo
_paginaTexto.TextChanged += (_, _) => banner.Estado = _paginaTexto.Text;
```

La línea de estado **siempre refleja datos que el formulario ya muestra** (nunca consultes nada nuevo):

| Módulo | Título | Icono de estado | Estado |
|---|---|---|---|
| Usuarios y roles | «Personas y permisos» | `UsersGear` | el texto de paginación (`_paginaTexto`): «Página 1 · 25 cuentas · hasta 25 por página» |
| Materiales | «Materiales con propósito» | `LayerGroup` | `_paginaMaterial` |
| Canecas y QR | «Canecas y códigos QR» | `Qrcode` | calculado de las filas de la tabla en `DataBindingComplete`: «5 canecas registradas · 3 en servicio»; sin filas: «No hay canecas registradas.»; antes de cargar: «Sin canecas consultadas todavía.» |
| Módulo nuevo | nombre del módulo | icono temático | el total/página/filtro que el módulo ya muestre |

Para un módulo nuevo, la excepción de imágenes debe quedar anotada en `.claude/skills/eco-ui-supervisor/SKILL.md`
(sección «Excepción aprobada»), con la fecha y el nombre del módulo.

---

## 6. Filtros y cajas de texto

- Cada filtro es un **campo Eco**: `ModuloEco.Campo("Etiqueta", IconChar.X, control, ancho: N)` → etiqueta `Subtitulo`
  (semibold 11, `Texto`) arriba y caja de **46 px** con borde redondeado e icono a la izquierda (`MarcoEntrada`).
- Los filtros, la casilla y los botones van en un `FlowLayoutPanel` (el del Designer o uno `_filtros`), **en una sola
  fila** a 1760 px de ancho (125 %): ajusta los `ancho:` hasta que «Actualizar lista» no caiga a una segunda línea.
  Valores aprobados: Usuarios 240 / 190 / 195; Materiales 240 / 230. En pantallas angostas se reparten solos.
- Botones junto a los campos: `ModuloEco.JuntoACampos(ModuloEco.Boton(boton, icono))` (bajan lo que mide la etiqueta).
  Orden: **Buscar** (principal, `MagnifyingGlass`) → **Actualizar lista** (secundario, `ArrowsRotate`) → **Cancelar
  consulta** (`Xmark`, oculto salvo durante la consulta; lo controla la lógica).
- Los controles siguen siendo **hijos del panel que la lógica deshabilita** mientras consulta (no los saques de ahí).
- **Listas desplegables**: el estilizado automático las deja del mismo alto que las cajas de texto (la parte visible mide
  el alto de la letra; las opciones desplegadas, 1,5×). No cambies eso módulo por módulo.
- **Texto guía** (gris dentro de la caja vacía, se ve también con el foco): `DisenoGestion.TextoGuia(textoX, "Ingresa …")`.
  Solo funciona en cajas de **una línea**. Si la caja convierte a mayúsculas (`CharacterCasing.Upper`) el texto guía
  también sale en mayúsculas: usa algo corto («Ejemplo: PET»). Para `CajaTexto` (control propio) usa su `PlaceholderText`.
  Textos aprobados: «Ingresa el nombre completo», «Ingresa el correo electrónico», «Ingresa la contraseña», «Confirma la
  contraseña», «Ingresa el nombre del material», «Ej. EcoAccess · Caneca verde».
- Iconos de campo usados: nombre `User`/`Tag`, correo `Envelope`, rol `UserTag`, contraseña `Lock`, código `Barcode`,
  categoría `LayerGroup`, descripción `AlignLeft`, punto `LocationDot`, material `Leaf`, estado `ToggleOn`, búsqueda
  `MagnifyingGlass`, fecha `CalendarDays`. (`ModuloEco.IconoCampo(pista)` sugiere uno por el texto.)

---

## 7. Botones

Todos: semibold 11, **alto 42**, radio 8, icono FontAwesome **16 px del color del texto**, centrado junto al texto. Se crean
con `ModuloEco.Boton(boton, IconChar.X)` (o `ancho: true` para ocupar toda la celda). El **tipo** sale del nombre o texto
del botón (`EstiloControlesEco.Clasificar`), salvo que lo fijes tú:

| Tipo | Aspecto | Cuándo |
|---|---|---|
| **Principal** | relleno `VerdeOscuro`, texto blanco; hover `Hover` | la acción principal de un bloque: Buscar, Registrar, Crear, Guardar, Admitir, Establecer, Consultar, Asignar… (uno por bloque) |
| **Secundario** | blanco con borde `Borde`, texto `VerdeOscuro`; hover `VerdeSuave` + borde `Verde` | Actualizar lista, Anterior, Siguiente, Cancelar |
| **Peligro** | blanco con borde, texto `Error`; hover `ErrorSuave` | acciones destructivas: Dar de baja, Eliminar, Anular (siempre con confirmación de la lógica) |
| **Suave** (nuevo) | relleno `VerdeSuave` sin borde marcado, texto `Texto`; hover/pulsado `VerdeHoja` + borde `Verde` | acciones frecuentes de la tarjeta lateral: Activar, Desactivar, Asignar/Quitar rol, Habilitar, Deshabilitar, Ver QR, Poner en servicio, Fuera de servicio, Actualizar lista (dentro del lateral), Quitar material |

- Botón suave: **`DisenoGestion.BotonSuave(boton, IconChar.X)`** (fija el tipo **antes** de que el tema se aplique y lo pone a
  todo el ancho). Dos lado a lado: `ModuloEco.Rejilla(DisenoGestion.BotonSuave(a, …), DisenoGestion.BotonSuave(b, …))`.
- Botón principal a todo el ancho al final de una página: `ModuloEco.Boton(botonRegistrar, IconChar.Plus, ancho: true)`.
- `DisenoFormularios.Agregar` **respeta** el icono que ya le diste a un botón (`EstiloControlesEco.TieneIcono`); si no tiene,
  elige uno por su nombre (`ModuloEco.IconoBoton`).
- Iconos aprobados: Buscar `MagnifyingGlass`, Actualizar `ArrowsRotate`, Cancelar `Xmark`, Registrar/Crear/Admitir `Plus`,
  Activar/Habilitar `CircleCheck`, Desactivar/Deshabilitar `CircleXmark`, Asignar rol `UserPlus`, Quitar rol `UserMinus`,
  Contraseña `Lock`, Ver QR `Qrcode`, Poner en servicio `CirclePlay`, Fuera de servicio `CirclePause`, Dar de baja
  `TrashCan`, Quitar material `Xmark`, Anterior `ChevronLeft`, Siguiente `ChevronRight`.
- El anillo de foco se dibuja con teclado; los botones deshabilitados se ven en `Fondo` con texto `Suave`.

---

## 8. Pestañas: píldoras con icono (nunca la franja clásica de Windows)

El equipo dijo que la gestión «seguía siendo muy aburrida» con pestañas clásicas. Se reemplazaron por píldoras:

1. Declara el TabControl en el `.Diseno.cs` como **`PaginasSinPestanas`** (sigue siendo un `TabControl`, así que índice,
   páginas y Ctrl+Tab funcionan igual):
   ```csharp
   private readonly TabControl _edicionX = new PaginasSinPestanas { Dock = DockStyle.Fill, AccessibleName = "Acciones de …" };
   ```
2. Crea cada página con un ayudante como `CrearPestana` (copia el de `FormUsuarios.Diseno.cs`): `TabPage` blanco,
   `AutoScroll = true`, `Padding = Padding.Empty`; dentro una `DisenoFormularios.Columna()` con `Padding(0, 4, 6, 4)` y
   las etiquetas limitadas al ancho de la columna. Si alguna fila queda con un hueco (texto medido estrecho al inicio),
   vuelve a medir **solo si el control ya tiene ventana** (ver `CrearPaginaCaneca`: `if (columna.IsHandleCreated)
   columna.BeginInvoke(... PerformLayout ...)`; sin esa comprobación la app se cae al abrir el módulo).
3. Arriba de las páginas, **`new SelectorPestanas(_edicionX, IconChar.A, IconChar.B, IconChar.C)`** (un icono por página, en
   orden). Aspecto: una fila de píldoras del mismo ancho, **alto 40**, radio 8, separación 3 px, fuente `Pequena`; la
   **elegida** en `VerdeOscuro` con texto e icono blancos y semibold; las demás blancas con borde `Borde`, texto `Texto`;
   hover `VerdeSuave` + borde `Verde`; si el texto no cabe con icono, se muestra solo el texto. Accesibles como pestañas
   (Tab, Enter, Espacio). Deshabilitada (el formulario deshabilita el panel) se ve en gris.
4. Coloca selector + páginas dentro del panel lateral:
   - con `VistaGestionCatalogo`: una `TableLayoutPanel` de 2 filas (selector `AutoSize`, páginas `100 %`) dentro de
     `panelAcciones` (ver Usuarios/Materiales);
   - con `ModuloEco`: dentro de una `ModuloEco.Seccion("Gestión de …", llenar: true, icono: IconChar.Gear)` con
     `gestion.Agregar(selector)` y `gestion.Agregar(_paginas, llenar: true)`, y esa sección dentro de `panelAcciones` (ver
     Canecas).
5. Nombres e iconos aprobados:
   - Usuarios: «Crear cuenta» `UserPlus` · «Cuenta y roles» `Users` · «Acceso inicial» `Lock`.
   - Materiales: «Crear material» `Plus` · «Estado y detalle» `CircleInfo`.
   - Canecas: «Caneca» `TrashCan` · «Materiales» `Leaf` · «Registrar» `Plus`.
   Títulos cortos (caben con icono en ~140 px).

---

## 9. Contenido de cada página lateral

Orden típico, de arriba abajo:

1. **Título de bloque** opcional: `DisenoFormularios.Texto("Cuenta seleccionada", true)` (`TituloSeccion` en `VerdeOscuro`).
2. **Aviso verde** (`AvisoEco`): recuadro `VerdeSuave` con borde `VerdeHoja`, radio 10, relleno 14/12, icono
   `CircleInfo` 22 px en `Verde`, título opcional en `TituloSeccion` `VerdeOscuro` y texto en `Cuerpo` (`Texto` sin título,
   `Suave` con título).
   ```csharp
   DisenoFormularios.Agregar(registro, new AvisoEco(DisenoFormularios.Texto("El registro público crea solo ciudadanos. …")));
   DisenoFormularios.Agregar(acceso, new AvisoEco(_seleccionAcceso, "Solo para personal sin contraseña")); // etiqueta viva
   DisenoFormularios.Agregar(caneca, new AvisoEco(DisenoFormularios.Texto("Dar de baja es definitivo. …"), icono: IconChar.TriangleExclamation));
   ```
   Úsalo para las explicaciones que antes eran texto gris suelto («Alta administrativa», «Amplía el catálogo
   ambiental», reglas de negocio, advertencias antes de una acción de peligro).
3. **Ficha del elemento elegido** (`DisenoGestion.Ficha(etiqueta, icono, nombreAccesible)`): recuadro `VerdeSuave`, borde
   `VerdeHoja`, radio 12, relleno 14/12, icono de 40 px en círculo blanco (`InsigniaIcono`) y la etiqueta que la **lógica**
   ya escribe (`_seleccion`, `_detalleMaterial`…). Si la lógica no tiene etiqueta (Canecas), crea una en el `.Diseno.cs` y
   llénala leyendo la fila actual (`tablaX.CurrentRow?.DataBoundItem as FilaX`) en `SelectionChanged` **y**
   `CurrentCellChanged` y `DataBindingComplete`. Iconos: cuenta `UserCheck`, material `Leaf`, caneca `TrashCan`.
4. **Campos**: `DisenoFormularios.Campo(columna, etiquetaDelDesigner, controlDelDesigner, IconChar.X)` + `TextoGuia`.
5. **Botones**: suaves para acciones frecuentes, uno principal al final (`ancho: true`), peligro al final y precedido
   de un aviso.
6. Listas (`ListBox`): dejarles `Margin(3, 3, 3, 14)` después de `Agregar` (el borde redondeado se dibuja 2 px por
   fuera).

---

## 10. Tablas (aspecto del dashboard)

Llama una vez, después de construir la vista:

```csharp
DisenoGestion.PulirTabla(tablaX, columna => columna switch
{
    "Nombre" => 230, "Correo" => 260, "Estado" => 125, _ => 0,   // anchos mínimos (lógicos) por nombre de columna
}, "Busca por … o pulsa «Actualizar lista» para ver …", IconChar.UsersViewfinder);
tablaX.CellPainting += PintarCeldaX;
```

`PulirTabla` hace: filas de **46 px** sin autoajuste; encabezado de columnas en **`VerdeSuave`** con texto `VerdeOscuro`;
anchos mínimos (la lógica puede fijar anchos menores; el mínimo gana); **estado vacío** (`EstadoVacioEco` con icono en
cuadro verde suave y mensaje) mientras no haya filas, ubicado bajo el encabezado.

Dibujo de celdas (en `CellPainting`, siempre: `e.PaintBackground(e.CellBounds, elegida)` y al final `e.Handled = true`;
respeta `e.CellStyle.SelectionForeColor` en la fila elegida):

| Columna | Dibujo |
|---|---|
| Estado activo/inactivo | `DisenoGestion.PastillaEstado(g, celda, valor, activo, tabla)` → verde con punto («Activo», «Habilitado», «En servicio») o gris («Inactivo», «Deshabilitado», «Dada de baja») |
| Estado de advertencia | `DisenoGestion.Pastilla(g, celda, valor, Advertencia, AdvertenciaSuave, Borde, IconChar.None, Advertencia, tabla)` («Fuera de servicio», «Pendiente de verificación») |
| Categoría de residuo | `EstiloCategoria.Para(categoria)` → `Pastilla(…, tono, fondo, Borde, icono, tono, …)`: blanca `Recycle`, verde `Seedling`, negra `TrashCan`, peligroso rojo `TriangleExclamation`. El texto de la fila es el de `TextosCategoria`; recupera la categoría con `TextosCategoria.Opciones.FirstOrDefault(o => o.Texto == valor)` |
| Persona | círculo de 30 px `VerdeSuave` con **iniciales** (semibold 11,5 px `VerdeOscuro`) + nombre |
| Caneca | cuadro redondeado 30 px con `TrashCan` del color de la caneca (verde/negra/blanca según el nombre) vía `DibujoEco.Icono` + nombre |
| Código | semibold 14 px en `VerdeOscuro` |
| Consentimiento / sí-no | visto `Check` 14 px en `Verde` + «Sí…»; «No» en `Suave` |
| QR | icono `Qrcode` 15 px `VerdeOscuro` + identificador |

Orden de columnas: puedes cambiar **solo el orden en pantalla** con `DisplayIndex` en `DataBindingComplete` (Usuarios pone
Estado justo después de Nombre; ojo: la columna oculta `Id` ocupa el índice 0, así que usa
`nombre.DisplayIndex + 1`). Nunca cambies los datos ni las columnas que la lógica crea.

Paginación (Anterior/Siguiente + texto de página) se queda debajo de la tabla, como estaba.

---

## 11. Tarjetas y títulos con icono

- `ModuloEco.Seccion(titulo, descripcion, llenar, icono)`: tarjeta blanca con sombra; título `TituloSeccion` `VerdeOscuro`
  con **icono verde de 24 px** a la izquierda (se pinta en el hueco del relleno, 10 px antes del texto, 3 px más abajo del
  centro para alinear con las letras). Descripción en `Cuerpo` `Suave`.
- Iconos aprobados: Buscar `MagnifyingGlass`, lista de cuentas `Users`, catálogo `LayerGroup`, canecas `TrashCan`,
  gestión `Gear`.
- Ancho de la columna lateral: Usuarios **500**, Materiales y Canecas **470** (por debajo de 900 px la vista apila
  las columnas).

---

## 12. Cosas que ya se resolvieron (no las rompas ni las repitas)

- Un ancho que se recalcula dentro de su propio `SizeChanged` (p. ej. cambiar `ItemSize` de un TabControl) provoca
  **StackOverflow**. Por eso no se usan pestañas de ancho fijo: se usan las píldoras.
- Un panel de tamaño automático dentro de otro que se redimensionan mutuamente también desborda la pila: la ficha y el
  aviso son **una sola** `TableLayoutPanel` que limita el texto al ancho de **su contenedor** (`ParentChanged` →
  `SizeChanged` del padre), comparando antes de asignar.
- `BeginInvoke` antes de que exista la ventana lanza excepción: compruébalo con `IsHandleCreated`.
- `Label.Image` se dibuja **dentro** del relleno y tapa el texto: los iconos de títulos se pintan en `Paint`.
- `TabControl.SelectedIndex` vale -1 antes de crear la ventana: el selector usa `Math.Max(0, …)` y vuelve a marcar en
  `HandleCreated`.
- `DisenoFormularios.Agregar` cambia `Dock` y `Margin`: ajusta márgenes especiales **después** de agregar.
- Fuentes literales y colores literales disparan advertencias del chequeo: usa tokens o `DibujoDashboard.Fuente/Semibold`.

---

## 13. Verificación obligatoria

1. **Compila fuera de `bin\Debug`** (la app o Visual Studio bloquean archivos):
   `dotnet build tests/visual/CapturasGestion/CapturasGestion.csproj -c Debug --artifacts-path <carpeta temporal>`
2. **Capturas reales** con el arnés (Oracle inalcanzable a propósito):
   `CapturasGestion.exe <salida> supervisor 1760 1000` y también `1366 728`. Variables: `CAPTURAS_SOLO="Nombre del menú"`
   (varios con `|`), `CAPTURAS_VOLCAR=1` (vuelca posiciones de cada control a `.txt` para depurar huecos),
   `CAPTURAS_CONTRAER=1`. Para ver la tabla con datos, agrega al arnés una función `XDeEjemplo` como
   `UsuariosDeEjemplo`/`MaterialesDeEjemplo`/`CanecasDeEjemplo` (crea filas del record privado por reflexión, aplica los
   mismos ajustes de columnas que la lógica, habilita `panelAcciones` y elige una fila). Se genera
   `…_datos_de_ejemplo.png` y una captura por página del TabControl.
3. **Mira las capturas**: filtros en una fila, sin textos cortados, sin huecos, píldora elegida en verde oscuro con icono,
   pastillas visibles, banner con su línea de estado, nada encima de otra cosa. Revisa también los módulos que comparten
   piezas (si tocaste algo común, captura Usuarios, Materiales y Canecas).
4. **Chequeo de estética** (debe dar 0/0):
   `powershell -NoProfile -ExecutionPolicy Bypass -File .claude/skills/eco-chequeo-estetica/scripts/Verificar-EsteticaEco.ps1 -Estricto`
5. **Pruebas** dentro del repositorio: `dotnet test -c Release` → todas en verde (hoy 2280 + 62).
6. `git status --short -- src/EcoAccess.P3.Presentacion/FormX.cs src/EcoAccess.P3.Presentacion/FormX.Designer.cs` → vacío.
7. Actualiza `.claude/skills/eco-ui-supervisor/SKILL.md` si aprobaron algo nuevo (banner de otro módulo, pieza nueva).
8. Informa al equipo en español: qué cambió (por bloques), qué comprobaste, qué no pudiste comprobar (datos reales de
   Oracle) y qué falta.

---

## 14. Plantilla de `FormX.Diseno.cs` (catálogo con píldoras)

```csharp
public partial class FormX
{
    private readonly TabControl _paginasX = new PaginasSinPestanas { Dock = DockStyle.Fill, AccessibleName = "Gestión de …" };

    private void ConstruirDisenoX()   // el constructor ya llama a este método al final
    {
        // 1) Filtros en una fila (campos Eco + Buscar + Actualizar + Cancelar), hijos del panel que la lógica deshabilita.
        // 2) Páginas laterales:
        var crear = CrearPagina("Crear …");
        DisenoFormularios.Agregar(crear, new AvisoEco(DisenoFormularios.Texto("Explicación segura del bloque.")));
        DisenoFormularios.Campo(crear, etiquetaNombre, textoNombre, IconChar.Tag);
        DisenoGestion.TextoGuia(textoNombre, "Ingresa el nombre …");
        DisenoFormularios.Agregar(crear, ModuloEco.Boton(botonRegistrar, IconChar.Plus, ancho: true));
        var detalle = CrearPagina("Estado y detalle");
        DisenoFormularios.Agregar(detalle, DisenoFormularios.Texto("… seleccionado", true));
        DisenoFormularios.Agregar(detalle, DisenoGestion.Ficha(_detalle, IconChar.Leaf, "…"));
        DisenoFormularios.Agregar(detalle, DisenoGestion.BotonSuave(botonHabilitar, IconChar.CircleCheck));
        DisenoFormularios.Agregar(detalle, DisenoGestion.BotonSuave(botonDeshabilitar, IconChar.CircleXmark));
        // 3) Selector + páginas dentro de panelAcciones.
        var gestion = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = TemaEcoAccess.Superficie, Margin = Padding.Empty };
        gestion.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        gestion.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        gestion.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        gestion.Controls.Add(new SelectorPestanas(_paginasX, IconChar.Plus, IconChar.CircleInfo) { Dock = DockStyle.Fill }, 0, 0);
        gestion.Controls.Add(_paginasX, 0, 1);
        panelAcciones.Controls.Clear();
        panelAcciones.Dock = DockStyle.Fill;
        panelAcciones.AutoScroll = false;
        panelAcciones.BackColor = TemaEcoAccess.Superficie;
        panelAcciones.Controls.Add(gestion);
        // 4) Banner con la información actual del módulo.
        var banner = new BannerModulo("Nombre del módulo", "Descripción clara de lo que se hace aquí.", IconChar.LayerGroup);
        banner.Estado = _pagina.Text;
        _pagina.TextChanged += (_, _) => banner.Estado = _pagina.Text;
        // 5) Vista completa con iconos en las tarjetas.
        Controls.Add(new VistaGestionCatalogo("Nombre", "Descripción", IconChar.Leaf, _filtros, tablaX, _paginacion, panelAcciones, etiquetaEstado,
            tituloLista: "…", tituloEditor: "Gestión de …", tituloBusqueda: "Buscar …", anchoLateral: 470,
            banner: banner, iconoBusqueda: IconChar.MagnifyingGlass, iconoLista: IconChar.LayerGroup, iconoEditor: IconChar.Gear));
        // 6) Tabla pulida + pastillas.
        DisenoGestion.PulirTabla(tablaX, c => c switch { "Nombre" => 220, "Estado" => 150, _ => 0 }, "Mensaje de estado vacío.", IconChar.LayerGroup);
        tablaX.CellPainting += PintarCeldaX;
        // 7) Eventos de la lógica que ya existían en el .Diseno.cs se conservan tal cual (Click de Buscar, Anterior, …).
    }
}
```

---

## 15. Lista de comprobación final (copia y marca)

- [ ] Banner `BannerModulo` con título, descripción y línea de estado viva; excepción anotada en la skill.
- [ ] Tarjetas con icono verde en el título.
- [ ] Filtros como campos Eco de 46 px en una sola fila; listas del mismo alto que las cajas; Buscar principal,
      Actualizar secundario.
- [ ] Tabla con `PulirTabla`: filas 46 px, encabezado verde suave, anchos mínimos, estado vacío con icono.
- [ ] Pastillas de estado/categoría, iniciales o icono en la columna principal.
- [ ] Panel lateral con `PaginasSinPestanas` + `SelectorPestanas` (píldoras con icono, elegida en verde oscuro).
- [ ] Avisos `AvisoEco` en vez de texto gris suelto; ficha del elemento elegido.
- [ ] Botones suaves para acciones frecuentes, uno principal por bloque a todo el ancho, peligro con aviso previo.
- [ ] Texto guía en las cajas de una línea.
- [ ] Sin cambios en `FormX.cs` ni `FormX.Designer.cs`.
- [ ] Capturas a 1760×1000 y 1366×728 revisadas, con datos de ejemplo.
- [ ] Chequeo estricto 0/0 y pruebas en verde.
- [ ] Informe en español al equipo, sin commit salvo que lo pidan.

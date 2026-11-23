---
name: eco-ui-supervisor
description: Estilo de las pantallas del área Supervisor (administrador del sistema) de EcoAccess — misma estética que la app ciudadana pero SIN imágenes ni ilustraciones. Úsala al crear o modificar FormPrincipal en modo supervisor, PanelResumenAdministrativo, FormUsuarios, FormMateriales, FormContenedores, FormPuntos, FormEcopuntos (tarifas), FormInteraccionesDepositos (auditoría), FormEstadisticas, FormOpiniones (administración), FormDashboardInstitucional, FormDiagnosticoOracle o cualquier módulo nuevo de administración.
---

# EcoAccess · Área Supervisor (administrador)

Lee primero `eco-estilo-base`: tokens y controles son los mismos. Esta skill solo añade lo propio del rol.

## 1. Regla principal: sin imágenes

El supervisor trabaja con datos; la estética se conserva con **color, tipografía, tarjetas e iconos**, no con
ilustraciones.

Prohibido en archivos del área Supervisor (y en los módulos compartidos con Operario):

- `RecursosEco.Eco`, `RecursosEco.Imagen(...)`, `RecursosEco.Residuo(...)`, `IconosMaterial.Ilustracion(...)`
- `ImagenRedondeada`, `ImagenAjustada`, `BackgroundImage`, `PictureBox` con imágenes de archivo
- Mascota ECO, fotos del campus, canecas 3D, botellas, robots.

Permitido:

- `LogoEcoAccess` **solo** en la marca de la barra lateral de `FormPrincipal`.
- Iconos FontAwesome (`IconChar` + `CacheIconos.Obtener` / `ToBitmap`) en botones, menú, KPIs y estados vacíos.
- La imagen del código QR en `FormQrCaneca` (es un dato, no decoración).
- **Excepción aprobada (diseño del Dashboard, 22/11/2026):** el banner de bienvenida del Dashboard lleva a Eco con la
  tableta (`eco/eco_tablet_banner.png`: copia de `eco_tablet.png` sin el velo blanco y con los cuadros de fondo al 27 %
  de opacidad), el campus difuminado (`eco/campus.jpg`) y las tres canecas (`eco/canecas_portada.png`),
  y la barra lateral lleva hojas decorativas (`eco/hojas_superior.png`, `eco/hojas_inferior.png`).
- **Excepción aprobada (Usuarios y roles 22/11/2026; Materiales, Canecas y QR, Puntos de instalación, Tarifas y ecopuntos y Auditar depósitos 23/11/2026):** el encabezado del módulo es un `BannerModulo` (mismo fondo que el
  banner del dashboard vía `BannerBienvenida.PintarFondo`, con Eco y el campus, sin canecas): nombre del módulo con hoja,
  descripción y una línea de estado con la información actual del módulo. Para usarlo en otro módulo, que el equipo lo pida.

### Piezas de gestión reutilizables (`ComponentesGestion.cs`)

- `PaginasSinPestanas` + `SelectorPestanas`: páginas sin la franja de Windows y píldoras con icono (elegida `VerdeOscuro`
  con texto blanco; las demás blancas con borde).
- `AvisoEco`: recuadro `VerdeSuave` con icono de información, título opcional y texto.
- `TipoBoton.Suave` (`EstiloControlesEco.Boton(b, TipoBoton.Suave)` antes de `Aplicar`): relleno verde suave para
  acciones frecuentes de una tarjeta lateral.
- `DisenoGestion`: `BotonSuave`, `TextoGuia` (texto gris en cajas vacías), `Ficha` (elemento elegido con icono),
  `PulirTabla` (filas de 46 px, encabezado verde suave, anchos mínimos y estado vacío), `PastillaEstado` y `Pastilla`
  (categorías con el color e icono de su caneca vía `EstiloCategoria`).
- `ModuloEco.Seccion(..., icono:)` y `VistaGestionCatalogo(..., banner:, iconoBusqueda:, iconoLista:, iconoEditor:)`.

### Dashboard (diseño aprobado)

Tonos del degradado del banner, medidos sobre el diseño (no cambiarlos por tokens): `#175C2E` 0 % → `#12532D` 17 % →
`#1A583A` 32 % → `#256848` 46 % → `#2A7250` 60–100 %; el campus se funde desde el tono exacto de su borde.

`PanelResumenAdministrativo` + `ComponentesDashboard.cs`, en este orden: `BannerBienvenida` (degradado verde, saludo,
mensaje, «Actualizar datos» y hora de la consulta) → fila de `AccesoRapido` → `EncabezadoSeccion` «Indicadores
operativos» → 8 `IndicadorDashboard` (icono en cuadro verde suave, título, cifra grande verde, detalle) →
`EncabezadoSeccion` «Actividad y verificación» → 4 `TarjetaDashboard`: barras por día/semana con `SelectorSegmentado`,
anillo de materiales (leyenda a la derecha), anillo de verificación (leyenda abajo con cantidades) y «Últimos depósitos»
con estado en pastilla y «Ver todos». Accesos e indicadores van en una fila si caben (≥ 150 y 158 px por tarjeta); si no,
en dos. Las cuatro tarjetas van en una fila desde 1100 px; si no, 2×2. La cabecera de la ventana oculta el nombre por
debajo de 1100 px y la fecha por debajo de 900 px; «Cerrar sesión» siempre queda visible.
Para revisarlo con datos sin Oracle, el arnés `CapturasGestion` genera `sup_00_Dashboard_datos_de_ejemplo.png`.
- Gráficos de datos (`PanelGraficosResumen`, dashboard institucional).

Sustitutos de cada imagen:

| Antes (con imagen) | Ahora en Supervisor |
|---|---|
| ECO saludando en la tarjeta de bienvenida | Tarjeta `VerdeOscuro` con `Titulo` blanco + icono `IconChar.Leaf` o `UserShield` 48 px en `VerdeHoja` dentro de un círculo de `Hover` |
| ECO en estado vacío | Icono 40 px (`IconChar.Inbox`, `MagnifyingGlass`, `CircleCheck`) en `Suave` dentro de un cuadro redondeado `VerdeSuave`, texto en `Cuerpo` debajo |
| Ilustración de residuo | Icono de categoría `EstiloCategoria.Para(categoria).Icono` sobre su fondo suave (`DibujoEco.Icono`) + pastilla «Caneca blanca» |
| Foto de caneca | Pastilla de color de la caneca + icono `Recycle` / `Seedling` / `TrashCan` |

## 2. Estructura de pantalla

```
┌──────────────┬──────────────────────────────────────────────────────────┐
│ Logo         │ Título del módulo (TituloSeccion)   ● Oracle   Avatar ⚙ ⇥│
│ ÁREA         │ fecha (Suave)                                              │
│ SUPERVISOR   ├──────────────────────────────────────────────────────────┤
│ ▸ Dashboard  │  Fondo                                                     │
│ ▸ Usuarios   │  ┌ TarjetaEco: filtros (CampoEco/CajaBusqueda + botones) ┐ │
│ ▸ Materiales │  └──────────────────────────────────────────────────────┘ │
│ ...          │  ┌ TarjetaEco: tabla EstilizarTabla ─────────────────────┐ │
│ « Contraer   │  └──────────────────────────────────────────────────────┘ │
└──────────────┴──────────────────────────────────────────────────────────┘
```

- Barra lateral blanca de 276 px (76 contraída) con `BotonNavegacion`; separador de 1 px en `Borde`.
- Rótulo `ÁREA SUPERVISOR` en `Cuerpo`, color `Verde`, mayúsculas, con el icono «Contraer menú» (`AnglesLeft`/`AnglesRight`)
  a su derecha; no hay fila aparte para contraer.
- Opciones de 42 px: icono en `Texto` y texto regular; la activa con fondo `VerdeSuave`, icono `Verde` y texto semibold.
  Sin borde: el anillo de foco solo aparece al navegar con teclado.
- Hojas: `hojas_superior.png` en la esquina superior derecha (27 % del ancho, detrás del logo) y `hojas_inferior.png` a
  todo el ancho abajo, solo en el espacio libre bajo la última opción (se aplana hasta 40 % del ancho; si no cabe, no se
  dibuja). Menú y botones son transparentes para que las hojas se vean detrás.
- Cabecera blanca: título del módulo + fecha en `Suave` + `PastillaEstado` + `AvatarIniciales` y botones icono.
- Contenido en `Fondo` con tarjetas blancas (`TarjetaEco`) separadas 12 px.

## 3. Estructura obligatoria de cada módulo (kit `ModuloEco`)

Todo módulo del Supervisor se arma con `src/EcoAccess.P3.Presentacion/ModuloEco.cs`, reutilizando los controles
del Designer (mismos objetos y eventos) en un archivo `Form….Diseno.cs` que el constructor llama al final:

```csharp
TableLayoutPanel raiz = ModuloEco.Iniciar(this, IconChar.Trash, "Canecas y códigos QR", "Qué hace el módulo, en una frase.");
var lista = ModuloEco.Seccion("Canecas registradas", "Ayuda corta.", llenar: true);   // tarjeta blanca con título verde
lista.Agregar(tablaCanecas, llenar: true);
lista.Agregar(ModuloEco.Mensaje(etiquetaEstado));
var registro = ModuloEco.Seccion("Registrar caneca");
registro.Agregar(ModuloEco.Campo(etiquetaNombre, IconChar.Tag, textoNombre));         // etiqueta semibold + caja 46 px con icono
registro.Agregar(ModuloEco.Boton(botonRegistrar, IconChar.Plus, ancho: true));        // semibold 11, alto 42, icono
ModuloEco.Columna(panelAcciones, registro);                                           // columna lateral que se desplaza
ModuloEco.Fila(raiz, ModuloEco.DosColumnas(lista, panelAcciones, 440), llenar: true);
ModuloEco.Terminar(this, raiz, new Size(640, 600));
```

| Pieza | Aspecto (igual que el ejemplo de ciudadano) |
|---|---|
| `Iniciar` / `Encabezado` | Icono 56 px en cuadro `VerdeSuave`, título `Titulo` (20) en `VerdeOscuro`, descripción `Entrada` (11) en `Suave` |
| `Seccion` | `TarjetaEco` blanca con sombra, título `TituloSeccion` (14) en `VerdeOscuro`, descripción `Cuerpo` en `Suave` |
| `Campo` | Etiqueta `Subtitulo` (semibold 11) arriba; `MarcoEntrada` de 46 px con icono a la izquierda; sirve para TextBox, NumericUpDown, ComboBox y DateTimePicker |
| `Boton` | Semibold 11, alto 42, icono 16 px del color del texto; tipo por nombre (principal / secundario / peligro) |
| `Rejilla`, `Botones`, `FilaCampos`, `JuntoACampos` | Acciones en 2 columnas, fila de botones, fila de filtros, botón alineado con las cajas |
| `DosColumnas` | Principal + lateral (se apilan por debajo de 900 px) |
| `PaginaConTabla` | Pestaña con tabla arriba y acciones abajo |

Reglas: el panel que el formulario deshabilita durante una operación (`panelAcciones`, `panelFiltros`) se conserva
como contenedor; un botón que debe seguir activo mientras tanto (Cancelar) va fuera de él.

Módulos ya armados así: Usuarios y roles, Materiales (vía `VistaGestionCatalogo`), Canecas y QR, Puntos de instalación,
Tarifas y ecopuntos, Auditar depósitos, Estadísticas, Valoraciones, Dashboard institucional, Sistema y Telegram, y los
diálogos Perfil y contraseña y QR de la caneca.

## 4. Patrones por tipo de módulo

**Dashboard (`PanelResumenAdministrativo`)**
- Tarjeta de bienvenida `VerdeOscuro`, saludo en `Titulo` blanco, mensaje en `Entrada` `VerdeHoja`, botón
  «Actualizar» secundario. Columna derecha: icono, **no** la mascota.
- Fila de KPIs: 3–4 `TarjetaDato` iguales (icono en cuadro `VerdeSuave`, título `Subtitulo`, cifra `Valor`
  `VerdeOscuro`, nota `Pequena` `Suave`).
- Acciones rápidas: `KryptonButton` + `EstilizarAccion` con icono `Verde` 16 px.

**Gestión CRUD (Usuarios, Materiales, Canecas, Puntos, Tarifas)**
- Arriba: tarjeta de filtros con `CajaBusqueda` o `CampoEco` + combos + botón secundario «Buscar».
- Centro: tabla `EstilizarTabla`. Estados (activo/inactivo, en servicio) como texto + color, nunca solo color.
- Formulario de edición en una tarjeta lateral o inferior con `DisenoFormularios.Columna()`/`CampoEco`.
- Acciones: un principal («Guardar», «Registrar»), secundarios («Limpiar», «Cancelar»); acción destructiva
  («Deshabilitar») como secundario con texto `Error`, siempre con confirmación.

**Auditoría y diagnóstico (Depósitos, Sistema y Telegram)**
- Línea de estado bajo los filtros con `PastillaEstado` (OK `VerdeOscuro`/`VerdeSuave`, error `Error`/`ErrorSuave`,
  aviso `Advertencia`/`AdvertenciaSuave`).
- Mensajes técnicos traducidos a lenguaje claro; el detalle va en `ToolTip`/`AccessibleDescription`.

**Estadísticas y dashboard institucional**
- Gráficos con la paleta verde (`Verde`, `VerdeOscuro`, `VerdeHoja`) y colores de caneca para categorías.

## 5. Lista corta antes de entregar

- [ ] Módulo armado con `ModuloEco` (encabezado, tarjetas, campos con icono): regla E5.
- [ ] Ninguna imagen salvo logo lateral, QR o gráficos de datos.
- [ ] Todos los colores y fuentes desde `TemaEcoAccess`.
- [ ] Cajas de texto como `CampoEco`/`CajaBusqueda` (o `TextBox` dentro de `DisenoFormularios.Campo`).
- [ ] Un botón principal por bloque; secundarios blancos con borde.
- [ ] Tablas con `EstilizarTabla`.
- [ ] Ejecutado el chequeo de `eco-chequeo-estetica` sin errores.

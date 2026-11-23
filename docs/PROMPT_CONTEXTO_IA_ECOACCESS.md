# Prompt de contexto — EcoAccess: rediseño visual de las áreas Supervisor y Operario

> Copia todo lo que está debajo de la línea y pégalo como primer mensaje (o como instrucciones del sistema) a la IA que
> vaya a continuar el trabajo. Está escrito para que la IA no tenga que adivinar nada.

---

## 0. Tu papel

Vas a continuar un trabajo de interfaz gráfica en **EcoAccess**, una aplicación de escritorio en C# (WinForms) para la
gestión de residuos reciclables en una universidad colombiana. Trabajas con el equipo del proyecto en español. Tu tarea
principal es **replicar con exactitud diseños de referencia (imágenes) en pantallas WinForms reales**, manteniendo una
estética común («estética Eco») en todas las pantallas de las áreas **Supervisor** (administrador del sistema) y
**Operario**, sin romper la lógica, las pruebas ni la accesibilidad.

Forma de trabajo que espera el equipo:

- Se avanza **paso por paso, pantalla por pantalla**. Cuando se envía una imagen, se espera que la pantalla quede
  **igual a la imagen**: misma disposición, tipografía, tamaños relativos, colores, iconos, textos y espaciados.
- **No quieren errores**: antes de dar algo por terminado hay que compilar, ejecutar las pruebas, pasar el chequeo de
  estética y revisar capturas reales de la pantalla.
- Si falta algo para replicar el diseño (una imagen, un icono, un dato, una decisión), **pídelo**: el equipo ofrece
  conseguirlo. Cuando haya que decidir entre opciones (por ejemplo, quitar un elemento que no tiene función), pregunta
  con opciones concretas y una recomendación.
- Explica en español claro qué cambiaste, qué comprobaste y qué falta. No hagas commit sin que te lo pidan.

## 1. El proyecto

- Solución: `EcoAccess.P3.sln`, .NET Framework 4.8, C# 9 con `nullable` habilitado (`Directory.Build.props`,
  `global.json`).
- Proyectos en `src/`:
  - `EcoAccess.P3.Entidades`: entidades y registros (por ejemplo `FilaAgrupada`, `ActividadPanel`,
    `ResumenPanelAdministrativo`, `IndicadoresPanel`, `SesionActiva`, `CodigoRol`, `AreaAcceso`).
  - `EcoAccess.P3.Logica`: servicios y contratos (`ServiciosAplicacion`, autenticación, opiniones, informes...).
  - `EcoAccess.P3.Datos`: acceso a **Oracle** (base compartida en la nube con wallet / `TNS_ADMIN`).
  - `EcoAccess.P3.Presentacion`: la app WinForms (aquí está casi todo el trabajo visual).
  - `EcoAccess.P3.Bot`: bot de Telegram (`EcoAccessP3_JG_bot`); los ciudadanos depositan residuos escaneando el QR de una
    caneca y hablan con «Eco», la mascota.
  - `EcoAccess.P3.Instalador`, `EcoAccess.P3.Compatibilidad`.
- Pruebas en `tests/`: `unitarias/` (≈2280 pruebas en `EcoAccess.P3.Tests` + 254 en `EcoAccess.P3.Datos.Tests`),
  `integracion/`, `visual/` (arneses de captura, ver §8), `herramientas/`.
- Recursos gráficos en `assets/imagenes/` (subcarpeta `eco/` con mascota, campus, canecas, logo, hojas). Se incrustan
  **automáticamente** como recursos por un comodín del `.csproj` de Presentación:
  `..\..\assets\imagenes\**\*.png;*.jpg` → nombre lógico `EcoAccess.Img.<subcarpeta>.<archivo>`. Se cargan con
  `RecursosEco.Imagen("eco/archivo.png")`, que devuelve `null` si el archivo no existe (el código debe tolerarlo).
- Bibliotecas de UI: **Krypton Toolkit** (algunos controles), **FontAwesome.Sharp** (iconos vectoriales: `IconChar`,
  `IconFont.Solid`, `ToBitmap`, y la caché propia `CacheIconos.Obtener(icono, lado, color)`), **LiveCharts** (gráficos
  de algunos módulos; no se tocó).
- Áreas de la app: **Ciudadano/Invitado** (con ilustraciones y mascota), **Operario** (consultar, verificar depósitos,
  registro manual, ecopuntos, estadísticas) y **Supervisor** (usuarios y roles, materiales, canecas y QR, puntos de
  instalación, tarifas y ecopuntos, auditar depósitos, estadísticas, valoraciones, dashboard institucional, Power BI,
  sistema y Telegram). Una cuenta puede tener ambos roles y cambiar de área.

## 2. Lo que se ha hecho (historia resumida)

1. **Análisis de la estética** de las pantallas de ciudadano e invitado (cajas de texto, inputs, botones, tarjetas,
   pastillas, tablas, navegación) y creación de **cuatro skills** en `.claude/skills/` que la definen (ver §3).
2. **Supervisor «todo todo» con la estética**: botones, cajas, combos, fechas, tablas, pestañas, casillas y tarjetas se
   estilizan de forma automática (`EstiloControlesEco`), y cada módulo se reconstruyó con un kit común (`ModuloEco`).
3. **Todos los módulos del supervisor** siguen la estructura del ejemplo: encabezado con icono, tarjetas blancas con
   título verde, campos con etiqueta semibold y caja con icono, botones con icono. Incluye los diálogos «Perfil y
   contraseña» (`FormCambioContrasena`) y «QR de la caneca» (`FormQrCaneca`).
4. Commit `5707c1b feat(ui): estética Eco en todos los módulos del supervisor y skills de estilo` en la rama
   `fix/instalador-wallet-tns-admin` (rama principal: `main`).
5. **Dashboard** (`PanelResumenAdministrativo` + `ComponentesDashboard.cs`) replicado de una imagen de referencia
   (ver §5). **Sin commit todavía.**
6. **Banner del dashboard** con los colores exactos medidos en la referencia y la mascota limpia (ver §5.2).
7. **Barra lateral** replicada del modelo con las hojas decorativas que entregó el equipo (ver §6). **Sin commit.**

Estado del árbol (sin commit): modificados `FormPrincipal.cs`, `NavegacionEco.cs`, `PanelResumenAdministrativo.cs`,
`tests/visual/CapturasGestion/Program.cs`, `.claude/skills/eco-ui-supervisor/SKILL.md`; nuevos
`ComponentesDashboard.cs`, `assets/imagenes/eco/eco_tablet_banner.png`, `hojas_superior.png`, `hojas_inferior.png`.

**Nunca incluir en un commit** (no son de este trabajo): `BaseDeDatos_Amilkar.zip`, `BaseDeDatos_Amilkar/`,
`docs/GUIA_ESTUDIO_COMPACTA_ECOACCESS.md`, `sites/`. Agrega los archivos uno por uno, no con `git add -A`.

## 3. Las skills de estilo (léelas antes de tocar la UI)

Están en `.claude/skills/` y son la fuente de verdad del diseño:

- **`eco-estilo-base`**: tokens, inventario de controles, tabla de estilizado automático, `ModuloEco`, `MarcoEntrada`.
- **`eco-ui-supervisor`**: reglas del área Supervisor, estructura de pantalla, kit obligatorio por módulo, la sección
  «Dashboard (diseño aprobado)» y la barra lateral.
- **`eco-ui-operario`**: lo mismo para Operario (consultar, verificar, registro manual).
- **`eco-chequeo-estetica`**: la regla de chequeo y su script
  `.claude/skills/eco-chequeo-estetica/scripts/Verificar-EsteticaEco.ps1`.

### 3.1 Tokens (`TemaEcoAccess`)

| Token | Valor | Uso |
|---|---|---|
| `Fondo` | `#F6F8F7` | fondo de las áreas de contenido |
| `Superficie` | blanco | tarjetas, barra lateral, cabecera |
| `Borde` | `#E2E8E5` | bordes y separadores de 1 px |
| `Verde` | `#168A55` | acento principal, iconos activos |
| `VerdeOscuro` | `#105C3B` | títulos, botón principal |
| `VerdeSuave` | `#E8F6EE` | fondos suaves, opción activa del menú |
| `VerdeHoja` | `#B7E4CB` | detalles claros sobre verde |
| `Hover` | `#0C4A2F` | hover del botón principal |
| `Texto` | `#1E293B` | texto normal |
| `Suave` | `#556274` | texto secundario |
| `Error` / `Advertencia` / `Info` (+ `*Suave`) | — | estados y pastillas |

Fuentes (Segoe UI): `Titulo` 20, `TituloSeccion` 14, `Subtitulo` 11 semibold, `Valor` 24, `Entrada` 11, `Cuerpo` 10,
`Pequena` 9. **Nada por debajo de 9 pt.** Escalado: `TemaEcoAccess.Px(control, n)` convierte unidades lógicas a
píxeles según `DeviceDpi`.

### 3.2 Regla «sin imágenes» en gestión

Supervisor y Operario trabajan con datos: la estética se logra con color, tipografía, tarjetas e iconos FontAwesome,
**no** con ilustraciones. Permitido: el logo solo en la barra lateral de `FormPrincipal`, iconos, la imagen del QR en
`FormQrCaneca`, gráficos de datos y la **excepción aprobada del Dashboard** (22/11/2026): banner con
`eco/eco_tablet_banner.png`, `eco/campus.jpg`, `eco/canecas_portada.png`, y las hojas de la barra lateral
`eco/hojas_superior.png` / `eco/hojas_inferior.png`. Ningún otro módulo.

### 3.3 El chequeo de estética

```
powershell -NoProfile -ExecutionPolicy Bypass -File .claude/skills/eco-chequeo-estetica/scripts/Verificar-EsteticaEco.ps1 -Estricto
```

- Errores: **E1** imagen decorativa, **E2** fuente < 9 pt, **E3** aspecto clásico de Windows (`FlatStyle`, `Fixed3D`),
  **E4** logo fuera de `FormPrincipal`, **E5** módulo sin `ModuloEco.Iniciar` / `VistaGestionCatalogo` / `Encabezado`.
- Advertencias: **A1** color literal, **A2** fuente literal, **A3** `ShowDialog` sin `TemaEcoAccess.Aplicar`,
  **A4** `MessageBox` para confirmar éxito.
- Opciones: `-Estricto` (las advertencias cuentan), `-Archivos a.cs,b.cs`, `-Ruta`.
- Excepción puntual: comentario **en la misma línea** `// eco-estetica: permitido <motivo>`.
- Sale con código 2 si no revisó ningún archivo. Estado actual: **0 errores, 0 advertencias**.

## 4. Arquitectura visual (archivos clave de `src/EcoAccess.P3.Presentacion`)

- **`TemaEcoAccess.cs`**: tokens, `Px`, y `Aplicar(control)`, que recorre los hijos (con `Controls.Cast<Control>().ToArray()`)
  y delega en `EstiloControlesEco`.
- **`EstiloControlesEco.cs`**: estilizado automático.
  - **Botones**: tipo por el texto (lista de principales con «Guardar», «Registrar», «Acreditar»...; peligrosos con
    «Anular», «Deshabilitar»...); texto semibold, icono con `IconoBoton`/`MarcarEstilizado`.
  - **TextBox / NumericUpDown / ComboBox / DateTimePicker**: se envuelven en **`MarcoEntrada`** (caja redondeada de 46 px
    con icono a la izquierda; `AltoPara = natural*4/3+1`; `AltoEnPixeles`).
  - **ComboBox** owner-drawn (`PintorLista`), fechas (`PintorFecha`), `GroupBox` como tarjeta, pestañas (`PintorPestanas`),
    tablas (`MarcoTabla`/`EstilizarTabla`), casillas, listas.
  - Técnica: `NativeWindow` que intercepta `WM_PAINT`/`WM_PRINTCLIENT`; `ConditionalWeakTable` para no estilizar dos
    veces; visibilidad leída por reflexión (`Control.GetState`). `ShowFocusCues`/`ShowKeyboardCues` son protegidos y
    se leen por reflexión.
- **`ModuloEco.cs`** (kit obligatorio de cada módulo): `Iniciar`, `Encabezado` (ancho según el padre), `Seccion`
  (`SeccionEco : TarjetaEco`, con `ScaleControl` que fija el padding), `Campo`, `Boton` (semibold 11, alto 42, icono),
  `IconoCampo`, `Rejilla`, `Botones`, `FilaCampos`, `JuntoACampos`, `DosColumnas` (se apilan bajo 900 px), `Columna`,
  `Mensaje`, `AjustarTextos`, `PaginaConTabla`, `Fila`, `Terminar`. Ejemplo:

  ```csharp
  TableLayoutPanel raiz = ModuloEco.Iniciar(this, IconChar.Trash, "Canecas y códigos QR", "Qué hace el módulo.");
  var lista = ModuloEco.Seccion("Canecas registradas", "Ayuda corta.", llenar: true);
  lista.Agregar(tablaCanecas, llenar: true);
  var registro = ModuloEco.Seccion("Registrar caneca");
  registro.Agregar(ModuloEco.Campo(etiquetaNombre, IconChar.Tag, textoNombre));
  registro.Agregar(ModuloEco.Boton(botonRegistrar, IconChar.Plus, ancho: true));
  ModuloEco.Columna(panelAcciones, registro);
  ModuloEco.Fila(raiz, ModuloEco.DosColumnas(lista, panelAcciones, 440), llenar: true);
  ModuloEco.Terminar(this, raiz, new Size(640, 600));
  ```

  Cada módulo tiene un `Form….Diseno.cs` que reacomoda los controles **del Designer** (mismos objetos y eventos) y que
  el constructor llama al final. Si el formulario deshabilita un panel durante una operación, ese panel se conserva
  como contenedor; lo que debe seguir activo (Cancelar) va fuera.
- **`DisenoFormularios.cs`**: `Campo`, `Texto(destacado)`, `Accion`, `Agregar` (delegan en `ModuloEco`).
- **`ComponentesEco.cs`**: `TarjetaEco`, `PastillaEstado`, `LogoEcoAccess`, `EstadoVacioEco`, `RecursosEco`
  (`Imagen`, `Redondeado`), `CacheEscalado.Obtener(img, ancho, alto)`.
- **`VistaGestionCatalogo`**: vista común de Usuarios y Materiales.
- **`NavegacionEco.cs`**: `BotonNavegacion` (opción del menú lateral) y `AvatarIniciales`.
- **`FormPrincipal.cs`**: ventana principal: barra lateral, cabecera y área de contenido donde se incrustan los módulos.

## 5. El Dashboard (diseño aprobado)

Archivos: `PanelResumenAdministrativo.cs` (disposición y datos) y `ComponentesDashboard.cs` (controles dibujados).
Lo usan Supervisor y Operario (cada uno con sus accesos).

### 5.1 Estructura, de arriba abajo

1. **`BannerBienvenida`**: banner redondeado con degradado verde; Eco con tableta a la izquierda; campus difuminado y
   tres canecas a la derecha; «¡Hola, {nombre}! Bienvenido a EcoAccess» (semibold 29 px) con una hoja `VerdeHoja`;
   mensaje (16 px); botón blanco «Actualizar datos»; separador vertical translúcido; «Datos de Oracle actualizados a las
   HH:mm (hora de Colombia).». `AltoNecesario()` hace crecer el banner si el texto ocupa más líneas.
2. **Fila de `AccesoRapido`** (8 tarjetas blancas: icono verde 28 px + texto semibold 13,5 px en dos líneas). Supervisor:
   Crear cuenta o asignar rol, Gestionar materiales, Canecas y QR, Tarifas de ecopuntos, Auditar depósitos (ShieldHalved),
   Estadísticas (ChartColumn), Dashboard institucional (BuildingColumns), Análisis en Power BI (ChartSimple).
3. **`EncabezadoSeccion` «Indicadores operativos»** + descripción.
4. **8 `IndicadorDashboard`**: icono en cuadro `VerdeSuave`, título, cifra grande verde (30 px, sufijo 19 px), detalle
   (11,5 px). Orden: Ciudadanos registrados, Solicitudes registradas, Depósitos registrados («Total de depósitos en el
   sistema»), Depósitos verificados («N % de los depósitos registrados»), Ecopuntos acreditados, Ecopuntos reversados,
   Canecas activas (`6 de 9`), **Materiales habilitados** (el equipo eligió «habilitados en grande»).
5. **`EncabezadoSeccion` «Actividad y verificación»**.
6. **4 `TarjetaDashboard`**: barras de los últimos 30 días con `SelectorSegmentado` Día/Semana (semanas desde el lunes),
   anillo «Depósitos por material» con leyenda a la derecha, anillo «Estado de verificación» (Verificados/Pendientes con
   cantidades, centro «N Depósitos»), y tabla «Últimos depósitos» (Depósito, Fecha, Material, Caneca, Estado en pastilla
   ámbar/verde) con la pastilla «Ver todos ›» que abre Auditar depósitos. Cada tarjeta tiene una nota al pie en recuadro
   gris con icono.

Comportamiento adaptable (`Adaptar()`): accesos e indicadores en una fila si caben (mín. 150 y 158 px por tarjeta), si
no en 4 o 2 columnas; las 4 tarjetas de actividad en una fila desde 1100 px (anchos 25,5 % / 16,5 % / 15 % / resto),
si no 2×2 o una columna. Sin scroll horizontal. Los datos se pintan con
`MostrarResumen(resumen, porDia, porMaterial, primero, hoy)` (separado de la consulta a Oracle para poder probarlo).
`TarjetaIcono` se conserva porque lo usa `FormAsistenteDeposito`.

### 5.2 Colores del banner (medidos en la referencia; no reemplazar por tokens)

Degradado horizontal: `#175C2E` 0 % → `#12532D` 17 % → `#1A583A` 32 % → `#256848` 46 % → `#2A7250` 60–100 %.
El campus se dibuja al 85 % de opacidad, con un velo verde de alfa 70 encima, y se funde desde el **tono exacto del
degradado en su borde** (`TonoEn(fracción)`) con un `Blend` suave, para que no haya corte. El botón «Actualizar datos»
toma como fondo `Tonos[1]`. Cada color literal lleva `// eco-estetica: permitido tono del banner (diseño aprobado)`.

`eco_tablet.png` original tenía un **velo blanco casi transparente** (alfa 11–22) en todo su recuadro, que aclaraba el
verde en un rectángulo, y **cuadros verde lima** de fondo. Se creó `eco_tablet_banner.png` (el original no se tocó):
píxeles con alfa < 90 → transparentes; cuadros detectados por relleno desde semillas con tolerancia entre vecinos
(excluyendo el emblema de la oreja y el del pecho) y llevados al 27 % de opacidad. El código usa
`RecursosEco.Imagen("eco/eco_tablet_banner.png") ?? RecursosEco.Imagen("eco/eco_tablet.png")`.

### 5.3 Cabecera de la ventana (`ConstruirCabecera` en `FormPrincipal`)

Izquierda: icono del módulo (`IconoSimple`; cuadrícula `TableCellsLarge` para el Dashboard) + título + «Área Supervisor ·
domingo 22 de noviembre de 2026». Derecha: `PastillaEstado` «Oracle conectado», separador de 1 px, `AvatarIniciales` +
nombre + área + chevron que abre un `ContextMenuStrip` (Perfil y contraseña, Cambiar al área… solo si la cuenta tiene
ambos roles, Cerrar sesión) y el botón de salir. **La campana se quitó** por decisión del equipo (no tenía función).
`Compactar()` oculta el nombre bajo 1100 px y la fecha bajo 900 px.

## 6. Barra lateral (`ConstruirLateral` en `FormPrincipal`)

- Ancho 276 lógicos (76 contraída). `TableLayoutPanel` con **columna al 100 %** (si no, el menú no se encoge al
  contraer), filas: marca (auto), menú (100 %), fila vacía (auto).
- Marca: `LogoEcoAccess` 196 lógicos + lema «Gestión ambiental · ¡Cada residuo cuenta!» en `Pequena` `Suave`; panel con
  fondo transparente y margen inferior 26.
- Fila del área: «ÁREA SUPERVISOR» / «ÁREA OPERARIO» en `Cuerpo`, color `Verde`, y a la derecha el **icono «Contraer
  menú»** (`BotonNavegacion` siempre en modo icono, 36×32, `AnglesLeft` ↔ `AnglesRight`, tooltip). Ya no hay fila
  aparte de «Contraer menú».
- Opciones (`BotonNavegacion`, 42 px, Segoe UI 11, icono 19 px): icono en `Texto`, texto regular; **activa** con fondo
  `VerdeSuave` redondeado, icono `Verde`, texto semibold `Texto`; hover `#F1F5F3`. El **anillo de foco solo aparece al
  navegar con teclado** (bandera estática `_focoConTeclado`: se activa en `KeyUp`, se apaga en `MouseDown`).
  Iconos: Dashboard House, Usuarios Users, Materiales Leaf, Canecas Qrcode, Puntos LocationDot, Tarifas Coins, Auditar
  ShieldHalved, Estadísticas ChartColumn, Valoraciones Star, Dashboard institucional TableCellsLarge, Power BI ChartLine,
  Sistema y Telegram PaperPlane.
- **Transparencias**: el `FlowLayoutPanel` del menú y los `BotonNavegacion` tienen `BackColor = Color.Transparent`
  (`SupportsTransparentBackColor`, sin `g.Clear`), para que las hojas pintadas en la barra se vean detrás; el menú se
  invalida al desplazarse.
- **Hojas** (extraídas de las imágenes del equipo convirtiendo el blanco en transparencia: `a = 1 − min(r,g,b)/255`,
  color «des-mezclado» del blanco), pintadas en `lateral.Paint`, al ras de los bordes:
  - `hojas_superior.png`: esquina superior derecha, 27 % del ancho, detrás del logo.
  - `hojas_inferior.png`: abajo a todo el ancho, **solo en el espacio libre bajo la última opción** (nunca detrás del
    texto); se aplana hasta el 40 % del ancho y, si ni así cabe o hay barra de desplazamiento, no se dibuja.
  - Contraída la barra, no se dibujan.
- En ventanas bajas (p. ej. 1366×728 al 125 %) el menú no cabe: aparece desplazamiento vertical y las hojas se ocultan.

## 7. Compilar y probar

- **Compila fuera de `bin\Debug`** (la app abierta o Visual Studio bloquean los archivos):
  `dotnet build tests/visual/CapturasGestion/CapturasGestion.csproj -c Debug --artifacts-path <carpeta temporal>`
- **Pruebas unitarias dentro del repo** (buscan el `.sln` hacia arriba; desde `--artifacts-path` fallan 19):
  `dotnet test -c Release` → debe dar 2280/2280 y 62 superadas (192 omitidas) en Datos.
- La prueba `SinSecretosEnRepositorioTests` prohíbe `Password=` en cualquier archivo del repo (por eso el arnés usa una
  cadena de conexión sin contraseña).
- No hay Python en la máquina: usa PowerShell (System.Drawing; para `.webp` usa WPF `BitmapDecoder` de
  `PresentationCore`), `sed`/`awk` o la herramienta de edición.
- Si editas con scripts, revisa que no queden `\r` sueltos.

## 8. Arnés de capturas (`tests/visual/CapturasGestion`)

Abre la `FormPrincipal` **real** con Oracle inalcanzable a propósito (`127.0.0.1:1`, sin credenciales), la deja 1 px
dentro del monitor principal (para tomar su escala) y la dibuja con `DrawToBitmap`.

```
CapturasGestion.exe <carpeta salida> [supervisor|operario] [ancho alto]
```

Genera `sup_00_Dashboard.png`, **`sup_00_Dashboard_datos_de_ejemplo.png`** (llama `MostrarResumen` por reflexión con las
cifras del diseño: 80, 156, 135, 58, 0, 0, 6 de 9, 12 y depósitos 1382–1376), una captura por opción del menú (y por
pestaña) y los diálogos de perfil y QR. Variables:

- `CAPTURAS_SOLO="Canecas y QR|Materiales"`: solo esos módulos (y omite los diálogos); `"ninguno"` = solo el dashboard.
- `CAPTURAS_VOLCAR=1`: vuelca posiciones, padding y DPI de cada control a un `.txt` (muy útil para depurar).
- `CAPTURAS_CONTRAER=1`: captura además `sup_00_Dashboard_menu_contraido.png`.
- `errores.txt` lista excepciones no controladas.

**Ojo con el DPI**: en esa máquina el sistema está a 120 DPI (125 %) y la ventana del arnés a 96, así que algunas
medidas se mezclan (por ejemplo, tras contraer y expandir la barra en el arnés queda más angosta). Las alturas se hacen
proporcionales (`AltoPara`) y los paddings se fijan en `ScaleControl` por ese motivo. Si algo se ve raro solo en el
arnés, verifica en la app real antes de «arreglarlo».

Para comparar con una referencia: mide colores con `GetPixel` en la imagen del diseño y en la captura, en las mismas
fracciones del ancho, y recorta zonas para verlas de cerca.

## 9. Errores ya resueltos (no los repitas)

- Variable `$ruta` que tapaba `$Ruta` en PowerShell (no distingue mayúsculas) → `$archivoRuta`.
- Alturas de 49 px por calcular `Px` antes de conocer el monitor → alturas proporcionales.
- Padding que se re-escalaba en cada cambio de DPI (26 → 98 px) → fijado en `SeccionEco.ScaleControl`.
- `AutoSize` de la cabecera que desbordaba la columna central y cortaba los módulos → `Compactar()`.
- Encabezados de diálogo que se partían letra por letra → `Encabezado` usa el ancho del padre.
- `Margin` negativo no permitido → `Padding.Empty`.
- Fundido del campus con `SetSigmaBellShape(0,1)` invertido (dejaba corte) → `Blend` explícito + `WrapMode.TileFlipX`.
- Columna de la barra lateral en `AutoSize` que impedía contraerla → columna al 100 %.
- Problema previo no tocado: `FormRegistroManual` lanza una excepción de Oracle no controlada cuando Oracle no responde
  (`FormRegistroManual.cs:203`).

## 10. Pendiente y próximos pasos

- Hacer **commit** del dashboard + barra lateral cuando el equipo lo pida (solo los archivos de este trabajo; mensaje
  convencional en español, p. ej. `feat(ui): dashboard y barra lateral del diseño aprobado`).
- Verificar en la **app real con Oracle**: datos reales del dashboard, contraer/expandir la barra, cambio de área.
- Seguir **pantalla por pantalla** con las próximas imágenes del equipo. Para cada una:
  1. Comparar la imagen con la captura actual y listar las diferencias (disposición, tamaños, colores, iconos, textos).
  2. Preguntar lo que falte o lo que sea decisión del equipo (elementos sin función, imágenes que no existen).
  3. Implementar con los componentes existentes (`ModuloEco`, `ComponentesDashboard`, tokens).
  4. Compilar, capturar en 1760×1000, 1366×728 y una altura grande; comparar con la referencia.
  5. Chequeo de estética `-Estricto` en 0/0 y `dotnet test -c Release` en verde.
  6. Actualizar la skill correspondiente si se aprobó algo nuevo (excepciones, colores medidos, estructura).
  7. Contar al equipo qué cambió, qué se comprobó y qué no.

## 11. Reglas que no se negocian

- Español en textos de la interfaz, comentarios, mensajes y commits.
- Toda pantalla nueva o modificada pasa el chequeo de estética sin errores.
- Colores y fuentes desde `TemaEcoAccess`; los literales solo con el comentario de excepción y motivo.
- Nada de imágenes decorativas en gestión fuera de la excepción aprobada del Dashboard.
- Accesibilidad: estados con texto además de color, `AccessibleName`/`AccessibleDescription`, foco visible con teclado,
  tooltips en los botones de solo icono.
- La autorización real está en los servicios; ocultar opciones del menú es solo comodidad.
- No guardar credenciales ni `Password=` en el repositorio; el arnés nunca usa datos reales.
- No hacer commit, push ni PR sin que el equipo lo pida.

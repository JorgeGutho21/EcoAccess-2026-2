---
name: eco-chequeo-estetica
description: Regla de chequeo para verificar que las pantallas de Operario y Supervisor de EcoAccess cumplen la misma estética de la app (tokens de TemaEcoAccess, controles Eco, sin imágenes en gestión). Úsala SIEMPRE al terminar un cambio de UI en src/EcoAccess.P3.Presentacion, antes de dar el trabajo por hecho o hacer commit, y cuando pidan "revisar la estética", "chequear el estilo" o "validar el diseño".
---

# EcoAccess · Chequeo de estética

Un cambio de UI en Operario o Supervisor **solo está terminado** cuando pasa las dos partes:
el chequeo automático (A) sin errores y la lista visual (B) completa.

## A. Chequeo automático

Desde la raíz del repositorio:

```powershell
powershell -ExecutionPolicy Bypass -File .claude/skills/eco-chequeo-estetica/scripts/Verificar-EsteticaEco.ps1
```

Opciones: `-Archivos FormUsuarios.cs,FormMateriales.cs` para revisar solo lo tocado; `-Estricto` para que las
advertencias también fallen. Devuelve código 1 si no cumple.

| Regla | Nivel | Qué detecta | Cómo se corrige |
|---|---|---|---|
| E1 | Error | `RecursosEco.Eco/Imagen/Residuo`, `ImagenRedondeada`, `ImagenAjustada`, `IconosMaterial.Ilustracion`, `BackgroundImage`, `Image.FromFile`, `Properties.Resources` | Icono FontAwesome (ver tabla de sustitutos en `eco-ui-supervisor`) |
| E2 | Error | Fuente < 9 pt | `TemaEcoAccess.Pequena` como mínimo |
| E3 | Error | `FlatStyle.Standard/System/Popup`, `BorderStyle.Fixed3D` | `EstilizarBoton` / `EstilizarAccion`, `CampoEco` |
| E4 | Error | `LogoEcoAccess` fuera de `FormPrincipal` | Quitarlo; la marca ya está en la barra lateral |
| E5 | Error | Módulo del Supervisor que no usa la estructura Eco (`ModuloEco.Iniciar` o `VistaGestionCatalogo`) | Armarlo con el kit (skill `eco-ui-supervisor`, sección 3) |
| A1 | Advertencia | `Color.FromArgb(...)` o `Color.Red`, etc. | Token de `TemaEcoAccess` |
| A2 | Advertencia | `new Font(...)` | Token tipográfico de `TemaEcoAccess` |
| A3 | Advertencia | `ShowDialog` de un `Form…` sin `TemaEcoAccess.Aplicar` | `TemaEcoAccess.Aplicar(dialogo)` antes de mostrarlo |
| A4 | Advertencia | `MessageBox` para confirmar éxito | Mensaje en línea o `PastillaEstado` |

Excepción justificada (por ejemplo, la sombra translúcida de un control dibujado): añadir en la misma línea
`// eco-estetica: permitido <motivo>`. Toda excepción nueva se menciona al usuario.

Las advertencias heredadas de código existente no bloquean, pero **el código nuevo o modificado no debe sumar
advertencias**: compara el conteo antes y después del cambio.

## B. Lista visual (revisar con la app abierta o la captura)

Capturas reales de cada módulo sin Oracle ni credenciales (la ventana se dibuja fuera de la pantalla, no tapa el
escritorio). Si la app está abierta en Visual Studio, compila con `--artifacts-path` para no chocar con `bin\Debug`:

```powershell
dotnet build tests/visual/CapturasGestion -c Debug --artifacts-path $env:TEMP\eco_art
```

```powershell
& "$env:TEMP\eco_art\bin\CapturasGestion\debug\CapturasGestion.exe" "$env:TEMP\capturas_gestion" supervisor
```

Usa `operario` en lugar de `supervisor` para la otra área; agrega ancho y alto (`supervisor 1366 1000`) para otro tamaño.
Con `$env:CAPTURAS_SOLO = "Canecas y QR|Materiales"` captura solo esos módulos (sin los diálogos). Sin filtro también
captura los diálogos Perfil y contraseña y QR de la caneca. Con `$env:CAPTURAS_VOLCAR = '1'` también escribe la
posición de cada control (útil cuando una fila se desplaza). Revisa cada PNG contra esta lista, comparando con la
pantalla de inicio de sesión y la de inicio del ciudadano:

**Cajas de texto**
- [ ] Etiqueta arriba en Semibold 11, color `Texto`.
- [ ] Caja blanca, radio 10, borde 1 px `Borde`, alto 46 (40 compacto), icono gris a la izquierda.
- [ ] Al enfocar: borde e icono 2 px en `Verde`. Con error: borde rojo + mensaje rojo debajo.
- [ ] Texto de ayuda (placeholder) en segunda persona.

**Botones**
- [ ] Principal: fondo `VerdeOscuro`, texto blanco Semibold 11, radio 10, hover `Hover`. Uno por bloque.
- [ ] Secundario: blanco, borde `Borde`, texto `VerdeOscuro`, hover `VerdeSuave`.
- [ ] Iconos de 16 px del mismo color que el texto; anillo de foco visible.

**Superficies y datos**
- [ ] Fondo `Fondo`; contenido agrupado en tarjetas blancas radio 12 con sombra suave.
- [ ] Tablas sin cabecera de fila, encabezado gris suave, filas de 38 px, selección `VerdeSuave`.
- [ ] Estados como pastilla (punto + palabra), nunca solo color.
- [ ] KPIs: icono en cuadro `VerdeSuave`, cifra Semibold 24 `VerdeOscuro`, nota gris 9 pt.

**Rol**
- [ ] Sin imágenes ni mascota; solo iconos, logo lateral, QR y gráficos de datos.
- [ ] Rótulo de área correcto (`ÁREA OPERARIO` / `ÁREA SUPERVISOR`) y menú del rol.
- [ ] Nada se corta a 1366×768 con escala 125 %.

## C. Cómo reportar

Al terminar, informar: resultado del script (errores/advertencias, antes → después), las casillas de B que no se
pudieron comprobar (por ejemplo, si no se abrió la app) y cualquier excepción `eco-estetica: permitido` añadida.

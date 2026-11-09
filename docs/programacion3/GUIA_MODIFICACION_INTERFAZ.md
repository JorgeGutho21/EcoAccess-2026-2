# Guía de modificación de la interfaz (Windows Forms)

Para cambios rápidos durante la sustentación. Todas las rutas son de `src/EcoAccess.P3.Presentacion/` salvo que se indique otra. Después de cada cambio: **Compilar → Recompilar solución** y F5 con el perfil «EcoAccess - Oracle Cloud».

## 0. Mapa rápido

| Quiero cambiar… | Archivo | Qué tocar |
|---|---|---|
| Colores | `TemaEcoAccess.cs` | campos `Verde`, `VerdeOscuro`, `Fondo`, `Texto`, `Error`, `Caneca*` |
| Tipografía | `TemaEcoAccess.cs` | `Titulo`, `Subtitulo`, `Cuerpo`… |
| Estilo de botones | `TemaEcoAccess.EstilizarAccion` (Krypton) y `EstilizarBoton` (nativos) | |
| Logo | `assets/identidad/Logo_EcoAccess.png` | reemplazar la imagen (mismo nombre) |
| Mascota Eco | `assets/identidad/Eco_perfil_Telegram.jpg` | ídem |
| Nombres de categoría | `TextosCategoria.cs` y `EstiloCategoria` en `ComponentesExplorar.cs` | |
| Menú del administrador | `FormPrincipal.cs`, método que llama a `Agregar(menu, …)` | |
| Conexión Oracle | User Secrets `ecoaccess-p3` / instalador `guardar-conexion` | nunca en el código |

## 1. Cambiar el color principal

Todos los formularios toman los colores de **`TemaEcoAccess.cs`**. Ejemplo, verde de marca más claro:

```csharp
internal static readonly Color VerdeOscuro = Color.FromArgb(0x10, 0x5C, 0x3B); // botones principales y textos de éxito
internal static readonly Color Verde       = Color.FromArgb(0x16, 0x8A, 0x55); // iconos, gráficos, foco
```

* `VerdeOscuro` pinta el fondo de los botones principales, los mensajes de éxito (`ColorEstado(false)`) y la caneca verde.
* Mantener contraste: texto blanco sobre el color del botón ≥ 4,5:1 (el comentario de la clase indica los contrastes medidos).
* Los mensajes de estado de **todos** los formularios usan `TemaEcoAccess.ColorEstado(esError)`: cambiar `Error` o `VerdeOscuro` los cambia a la vez.

## 2. Cambiar el logo

1. Reemplazar `assets/identidad/Logo_EcoAccess.png` por la nueva imagen (PNG con fondo transparente; se conserva la proporción automáticamente con `LogoEcoAccess.AltoPara`).
2. Recompilar: el `.csproj` la incrusta como recurso `EcoAccess.Logo.png` (`RecursosEco.Logo`).

## 3. Cambiar el nombre «EcoAccess»

El nombre aparece en el logotipo (imagen) y en textos de títulos y mensajes repartidos por los formularios (por ejemplo `Text = "EcoAccess · Bienvenida"`).

1. Logotipo: ver §2.
2. Textos: Visual Studio → **Editar → Buscar y reemplazar → Reemplazar en archivos** (Ctrl+Mayús+H), buscar `EcoAccess ·` en `src\EcoAccess.P3.Presentacion\*.cs` y revisar cada coincidencia antes de reemplazar. No reemplazar en `namespace` ni en `using` (son nombres de código, no texto visible).
3. Producto del ejecutable: `<Product>` en `EcoAccess.P3.Presentacion.csproj`.

## 4. Cambiar el nombre de una caneca

* **Nombre de la categoría** (lo que se muestra como «Caneca blanca», «Caneca verde», «Caneca negra»): `EstiloCategoria.Para` en `ComponentesExplorar.cs` y `TextosCategoria.Texto` en `TextosCategoria.cs`.
* **Nombre de una caneca concreta** (dato de `CONTENEDOR.nombre`): la lógica ya tiene `ServicioTresCanecas.RenombrarAsync(idAdmin, idContenedor, nombre)` (valida 2–100 caracteres y exige administrador); falta un botón en `FormContenedores` que lo llame. Mientras tanto, en SQL Developer como `ECOACCESS_P3_APP`:

```sql
UPDATE contenedor SET nombre = 'Caneca blanca - Bloque A' WHERE id_contenedor = 1;
COMMIT;
```

La restricción `CK_V006_CONTENEDOR_NOMBRE` exige al menos 2 caracteres sin espacios sobrantes.

## 5. Agregar un botón

En un formulario que ya usa Krypton, por ejemplo `PanelResumenAdministrativo.cs` o `FormExplorarEcoAccess.cs`:

```csharp
var exportar = new KryptonButton { Text = "Exportar", AutoSize = true, AccessibleName = "Exportar" };
TemaEcoAccess.EstilizarAccion(exportar);              // mismo aspecto que el resto
exportar.Click += async (_, _) => await ExportarAsync(); // la acción llama a un servicio de Lógica
panelBotones.Controls.Add(exportar);
```

* Asignar `AccessibleName` con el texto del botón para lectores de pantalla.
* La acción **no ejecuta SQL**: llama a un método de un servicio (`_servicios.X.MetodoAsync`) y muestra el `Resultado` (éxito o `Mensaje`).
* En formularios con diseñador (`*.Designer.cs`, por ejemplo `FormMateriales`), arrastrar un `Button` desde el Cuadro de herramientas, darle `Name` (empezando por `boton…`), y hacer doble clic para crear el manejador. `TemaEcoAccess.Aplicar` le da el estilo automáticamente; si el nombre contiene «Registrar», «Entrar» o «Guardar» se pinta como principal.

Para un nuevo acceso en el menú lateral del administrador (`FormPrincipal.cs`):

```csharp
Agregar(menu, "Reportes", IconChar.FileLines, () => AbrirModulo("Reportes", () => new FormReportes(...), null));
```

## 6. Modificar un formulario o una etiqueta

* Formularios con diseñador: abrir `FormX.cs` en modo diseño (Mayús+F7), seleccionar el control y cambiar `Text` en la ventana Propiedades. Visual Studio actualiza `FormX.Designer.cs`.
* Formularios construidos por código (`FormInicioSesion`, `FormExplorarEcoAccess`, `FormActivacionOracle`, `FormRegistroCiudadano`): buscar el texto con Ctrl+F y cambiar la cadena.
* No editar a mano posiciones dentro de `InitializeComponent` salvo que se sepa lo que se hace; para disposición se prefieren `TableLayoutPanel`/`FlowLayoutPanel` (escalan con el DPI).

## 7. Ajustar un mensaje

* Mensajes de **validación de datos** (nombre corto, correo inválido…): están en las entidades (`src/EcoAccess.P3.Entidades/*.cs`, excepciones `ArgumentException`) y se traducen en `MensajesValidacion` (Lógica). Cambiarlos allí los cambia en Windows Forms y en Telegram.
* Mensajes de **reglas de negocio** («No tienes permiso…»): en el servicio correspondiente de `src/EcoAccess.P3.Logica/Servicios/`, en `Resultado.Rechazo(...)`.
* Mensajes de **conexión Oracle**: `Program.Explicar` (Presentación).
* Mensajes **solo de pantalla** (títulos, ayudas): en el formulario.

## 8. Añadir un campo a una entidad (ejemplo: teléfono del usuario)

1. Base de datos: nueva migración `database/06_migraciones/V022__usuario_telefono.sql` (`ALTER TABLE usuario ADD telefono VARCHAR2(20 CHAR);`) y una línea al final de `PlanInstalacion.Pasos`. Aplicar con `instalar`.
2. Entidad: propiedad `Telefono` en `Usuario.cs` con su validación en el constructor.
3. Datos: leer/escribir la columna en `RepositorioUsuariosOracle.cs` (consultas parametrizadas `:telefono`).
4. Lógica: recibir el dato en `ServicioUsuarios.RegistrarAsync`.
5. Presentación: campo en `FormUsuarios` y paso del valor al servicio.
6. Pruebas: unitaria de la entidad y de integración del repositorio.

## 9. Modificar una consulta

Las consultas viven en los repositorios Oracle (`src/EcoAccess.P3.Datos/Repositorios/Repositorio*Oracle.cs`), como constantes `Sql…` o en el método. Reglas: siempre parámetros (`:id`), nunca concatenar texto del usuario; probar la consulta primero en SQL Developer; ejecutar `dotnet test tests\integracion\EcoAccess.P3.Datos.Tests` (contra Oracle Cloud).

## 10. Cambiar la conexión Oracle

No está en el código. En cada equipo:

```bash
dotnet run --project src/EcoAccess.P3.Instalador -- guardar-conexion
```

o abrir EcoAccess: si la conexión falla aparece «Configuración inicial (Oracle Cloud)» con carpeta del wallet, servicio (`ecoaccess_tp`), usuario y contraseña. La clase que arma la cadena es `WalletOracleNube` (Datos).

## 11. Regenerar un QR

Administrador → **Canecas y QR** → seleccionar la caneca → **Ver QR** (`FormContenedores.BotonVerQr_Click` abre `FormQrCaneca`) → **Guardar** (PNG) o **Imprimir**. El QR contiene el enlace público `https://t.me/<bot>?start=<identificador>`: el identificador de 128 bits se crea al registrar la caneca (`GeneradorIdentificadorQrAleatorio`) y **no** contiene contraseñas. El nombre del bot sale de la configuración `Telegram:NombreBot`.

## 12. Agregar un material al catálogo

Administrador → **Materiales** → completar código (mayúsculas, por ejemplo `TETRAPAK`), nombre, categoría y descripción → **Registrar** (`FormMateriales.BotonRegistrar_Click` → `ServicioMateriales.RegistrarAsync`). Para que una caneca lo acepte: **Canecas y QR** → seleccionar la caneca → elegir el material → **Admitir**. Para la instalación inicial de un equipo nuevo también se puede agregar al `MERGE` de `database/03_datos_prueba/01_catalogos.sql`.

## 13. Revisión de la interfaz (2026-11-09)

| Comprobación | Resultado |
|---|---|
| Colores de estado repetidos (6 formularios) y colores de caneca sueltos | centralizados en `TemaEcoAccess` (`ColorEstado`, `CanecaBlanca/Verde/Negra`) |
| Arranque contra Oracle Cloud sin Docker | abre «EcoAccess · Bienvenida» |
| Formularios de administrador en pantalla | **no probados**: la base nueva no tiene administrador; su lógica y datos están cubiertos por las pruebas contra Oracle Cloud |
| Pantalla para configurar las tres canecas | **pendiente** (servicio y pruebas listos) |

### Lista para probar los formularios de administrador

Requiere que el propietario fije la contraseña del primer administrador: Bienvenida → «Configuración inicial: contraseña del primer administrador».

| Formulario | Acción | Comprobar en SQL Developer |
|---|---|---|
| Usuarios | registrar usuario, asignar rol, desactivar | `SELECT * FROM usuario`, `usuario_rol` |
| Materiales | registrar, deshabilitar, habilitar | `SELECT codigo, habilitado FROM material` |
| Puntos | registrar, desactivar | `punto_instalacion` |
| Canecas y QR | registrar caneca, admitir material, ver/guardar QR, fuera de servicio | `contenedor`, `contenedor_material` |
| Depósitos | consultar, verificar | `deposito`, `interaccion` |
| Ecopuntos | crear regla, consultar historial | `regla_ecopuntos`, `movimiento_ecopuntos` |
| Estadísticas | consultar con filtros, paginar | `database/04_consultas/01_estadisticas.sql` |
| Diagnóstico Oracle | actualizar | 22 tablas, 0 inválidos |

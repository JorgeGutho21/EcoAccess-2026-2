# Programación en C#

EcoAccess se escribe en **C# 9** sobre **.NET Framework 4.8** (`Directory.Build.props`). Los conceptos se explican con clases reales del repositorio.

## Clases y objetos

Una **clase** es un molde que define datos y comportamiento; un **objeto** es un ejemplar concreto creado con `new`. `Material` es una clase; `new Material("PET", "Botella PET", CategoriaMaterial.Aprovechable, null)` crea un objeto con esos datos. En la base de datos cada objeto corresponde a una fila de `MATERIAL`.

## Propiedades y encapsulamiento

Las propiedades exponen los datos controlando quién puede cambiarlos. En `Contenedor`, `Estado` tiene `get` público y `private set`: desde fuera solo se cambia con los métodos `PonerEnServicio()`, `PonerFueraDeServicio()` y `DarDeBaja()`, que aplican las reglas (por ejemplo, una caneca en servicio debe admitir al menos un material). Eso es **encapsulamiento**: el objeto protege su propio estado. `Usuario.Roles` se expone como `IReadOnlyCollection<CodigoRol>` para que nadie agregue roles sin `AsignarRol`.

## Métodos

Un método es una acción del objeto. `OrdenApertura` tiene la máquina de estados de una apertura: `MarcarEnviada()`, `RegistrarAcuse()`, `RegistrarApertura()`, `RegistrarDeteccion()`, `Cerrar()`, `Vencer()`, `Fallar()`; `PuedeAvanzarA(estado)` impide saltos inválidos y `EstaVencida(ahora)` aplica la vigencia. `Interaccion` registra la sugerencia de la IA (`RegistrarSugerencia`), la autoriza con el material confirmado (`Autorizar`) o la rechaza (`Rechazar`).

## Constructores y validaciones

El constructor deja al objeto válido desde que nace. `Usuario` lanza `ArgumentException` si no tiene roles, si los roles se repiten o si hay fecha de consentimiento sin versión. `ConfiguracionCaneca` (fase de tres canecas) rechaza la categoría peligrosa, un identificador de dispositivo con espacios o una fecha que no esté en UTC. Varias clases tienen dos constructores: uno para **crear** (sin `Id`, la base lo asigna) y otro para **reconstruir** desde Oracle.

## Interfaces

Una interfaz es un contrato sin implementación. La lógica depende de `IRepositorioMateriales`, `IRepositorioContenedores` o `IRepositorioConfiguracionCanecas`, no de Oracle. En producción se entrega `RepositorioMaterialesOracle`; en las pruebas, `RepositorioMaterialesEnMemoria`. `IClasificadorMaterial` permite cambiar el modelo visual sin tocar el bot.

## Herencia

Una clase hereda de otra para reutilizar y especializar. Todos los formularios heredan de `System.Windows.Forms.Form` (`FormActivacionOracle : Form`). `TarjetaDibujada` es una clase **abstracta** (no se instancia) que define cómo se pinta una tarjeta; `TarjetaAccionRapida` y `TarjetaCategoria` heredan y solo implementan `PintarContenido`. `ConflictoUnicidadException : Exception` especializa una excepción del sistema.

## Polimorfismo

El mismo llamado se comporta distinto según el objeto real. `ServicioClasificacionImagen` llama `ClasificarAsync(imagen, catalogo)` sobre un `IClasificadorMaterial`: puede ser `ClasificadorOllamaLocal` (LFM3-VL), `ClasificadorGroq` o `ClasificadorMaterialSimulado` en pruebas. El servicio no sabe cuál es, y no necesita saberlo.

## Manejo de errores

* Errores de datos esperables se devuelven como `Resultado.Rechazo(motivo, mensaje)` (no excepciones): la interfaz muestra el mensaje.
* Errores de Oracle se traducen: `TraductorErroresOracle` convierte `ORA-00001` en `ConflictoUnicidadException`, y la lógica responde «ese dispositivo ya está vinculado a otra caneca».
* Mensajes al usuario nunca incluyen la cadena de conexión ni el texto interno del proveedor (`Program.Explicar`).

## Programación asíncrona

Las operaciones de red usan `async`/`await` para no congelar la interfaz ni bloquear el bot: `FabricaConexionesOracle.AbrirAsync`, `RepositorioMaterialesOracle.ListarAsync`, `ClasificadorOllamaLocal.ClasificarAsync`. Un `SemaphoreSlim(1,1)` en el clasificador deja una sola foto en la GPU mientras el bot sigue recibiendo mensajes. `CancellationToken` permite cancelar consultas largas.

## Validaciones

Tres niveles: la **entidad** valida su propia forma (longitudes, formatos); la **lógica** valida reglas que dependen de otras tablas o permisos (`ControlAcceso.ExigirRolAsync`, «la caneca admite el material»); **Oracle** garantiza integridad con `CHECK`, `UNIQUE` y claves foráneas aunque alguien escriba directo en SQL Developer.

# Entidades

| Entidad | Responsabilidad | Atributos principales | Métodos / reglas | Relación |
|---|---|---|---|---|
| `Usuario` | persona con uno o varios roles | `Nombre`, `CorreoElectronico`, `Activo`, `Roles`, consentimiento | `TieneRol`, `AsignarRol`, `QuitarRol`, `Activar`, `Desactivar`, `RegistrarConsentimiento` | `USUARIO_ROL`, `CREDENCIAL_ACCESO` |
| `Rol` / `CodigoRol` | ciudadano, operario, administrador | `Codigo`, `Descripcion` | — | N:M con usuario |
| `CredencialAcceso` | contraseña como hash PBKDF2 (600 000 iteraciones) y sal | `Hash`, `Sal`, `Iteraciones`, `IntentosFallidos`, `BloqueadaHastaUtc` | `EstaBloqueada` | 1:1 con usuario |
| `Material` | residuo del catálogo | `Codigo`, `Nombre`, `Categoria`, `Sinonimos`, `Precauciones`, `RutaSeparacion`, `Habilitado` | `Habilitar`, `Deshabilitar` | admitido por canecas |
| `CategoriaMaterial` | Aprovechable, Orgánico aprovechable, No aprovechable, Peligroso | — | decide el color de caneca | — |
| `PuntoInstalacion` | estación | `Nombre`, `ReferenciaUbicacion`, `Activo` | — | 1:N canecas |
| `Contenedor` | caneca con QR | `Nombre`, `IdentificadorQr`, `Estado`, `MaterialesAdmitidos` | `AdmiteMaterial`, `AdmitirMaterial`, `PonerEnServicio`, `DarDeBaja` | punto, materiales, órdenes |
| `ConfiguracionCaneca` (nueva) | color, dispositivo y ubicación de una caneca | `Categoria`, `IdentificadorDispositivo`, `UbicacionEnEstacion`, `Color` | `ColorPara`, `TieneDispositivo` | 1:1 con contenedor |
| `Interaccion` | visita de una persona a una caneca por un canal | `Canal`, `Estado`, material sugerido/confirmado, `CantidadDeclarada` | `RegistrarSugerencia`, `Autorizar`, `Rechazar`, `CerrarConDeposito` | contenedor, usuario |
| `OrdenApertura` | autorización de abrir una caneca | `ClaveIdempotencia`, `Estado`, emisión, vencimiento | máquina de estados, `EstaVencida` | interacción |
| `EventoDispositivo` | lo que reporta el ESP32 | tipo, fecha, orden | — | contenedor, orden |
| `Deposito` | depósito detectado o declarado | material declarado/verificado, `CantidadVerificada`, `PesoMedidoKg` (solo con báscula), `SinSensor` | verificación por operario | evento, material, usuario |
| `ReglaEcopuntos` | puntos por material, vigencia, por unidad | `Puntos`, `PorUnidad` | `CalcularPuntos`, `EstaVigente` | material |
| `VinculoTelegram`, `TokenVinculacion`, `ConversacionTelegram` | identidad y diálogo en Telegram | `TelegramUserId`, estado, versión | estados del diálogo | usuario, contenedor |
| `AnalisisImagen` | traza de una propuesta visual (sin la foto) | proveedor, modelo, estado | — | interacción, material |
| `Opinion` | valoración ciudadana | — | — | usuario |

# Acceso a datos

## ADO.NET y Oracle.ManagedDataAccess

EcoAccess usa ADO.NET con el proveedor **Oracle.ManagedDataAccess 23.26** (administrado, sin cliente Oracle instalado). Cada repositorio abre una `OracleConnection` con `FabricaConexionesOracle.AbrirAsync`, crea un `OracleCommand` con `ComandosOracle.Crear` y lee con `DbDataReader`.

## Cadena de conexión y fábrica

La cadena vive en los User Secrets del perfil de Windows (`ecoaccess-p3-framework`). `WalletOracleNube.CrearCadena` agrega `Tns_Admin` y `Wallet_Location` para mTLS con Oracle Cloud. `FabricaConexionesOracle` recibe la cadena una sola vez en `Program.cs` (punto de composición) y la entrega a todos los repositorios.

## Repositorios y consultas parametrizadas

Cada tabla principal tiene su repositorio (`RepositorioUsuariosOracle`, `RepositorioMaterialesOracle`, `RepositorioContenedoresOracle`, `RepositorioConfiguracionCanecasOracle`…). Todas las consultas usan parámetros (`:id`, `:nombre`) agregados con `comando.Agregar("id", OracleDbType.Int32, valor)`: así se evita la inyección SQL y Oracle reutiliza el plan.

## Transacciones

Las operaciones de varias filas usan `BeginTransaction` (24 usos) y bloquean filas con `SELECT … FOR UPDATE` (22 usos en 7 repositorios) donde hay riesgo de concurrencia: vincular Telegram, acreditar ecopuntos, cambiar roles. Si algo falla, `ROLLBACK`; la prueba de persistencia comprobó que un cambio revertido no queda guardado.
